using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Extensions;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif


namespace PEPEngineers.PEPErvice.Runtime
{
    [Serializable]
    public abstract class ServiceLocator : ScriptableObject, ILocator, IRegister
    {
        private static ServiceLocator instance;

        [SerializeField]
#if ODIN_INSPECTOR
        [TitleGroup("Register", order: 100)]
        [PropertySpace(5, 10)]
        [Searchable]
        [InlineEditor]
#endif
        private List<GameService> staticServices = new();
        protected IReadOnlyCollection<GameService> StaticServices => staticServices;

        [SerializeField]
#if ODIN_INSPECTOR
        [TitleGroup("Register")]
        [PropertySpace(5, 10)]
        [Searchable]
        [InlineEditor]
#endif
        private List<SceneService> sceneServices = new();
        protected IReadOnlyCollection<SceneService> SceneServices => sceneServices;

        [SerializeField]
#if ODIN_INSPECTOR
        [TitleGroup("Register")]
        [PropertySpace(0, 50)]
#endif
        private bool createParentForSceneServices;

#if ODIN_INSPECTOR
        [TitleGroup("Register", order: 100)] [PropertySpace(5, 10)] [ShowInInspector] [InlineEditor]
#endif
        private readonly List<GameService> autoCreatedServices = new();

#if ODIN_INSPECTOR
        [TitleGroup("Locator")]
        [PropertyOrder(100)]
        [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.OneLine,
            KeyLabel = "Type",
            ValueLabel = "Service")]
        [ShowInInspector]
        [ReadOnly]
        [PropertySpace(5, 25)]
#endif
        private readonly Dictionary<Type, IService> registeredServices = new();
        protected IReadOnlyDictionary<Type, IService> RegisteredServices => registeredServices;

#if ODIN_INSPECTOR
        [TitleGroup("Locator")] [PropertyOrder(100)] [ShowInInspector] [ReadOnly]
#endif
        private readonly HashSet<Type> sceneOnlyTypes = new();

        private readonly Dictionary<Type, Func<IService>> serviceFactories = new();
        private readonly HashSet<SceneService> createdSceneServices = new();

        private Transform gameParent;
        private Transform localSceneParent;

        public static ServiceLocator Instance
        {
            get
            {
#if UNITY_EDITOR
                FindAndSetupEditorInstance();
#endif
                return instance;
            }
        }

        protected virtual void Awake()
        {
            instance = this;
        }

        protected virtual void OnEnable()
        {
            instance = this;
            SceneManager.activeSceneChanged -= OnSceneChanged;
            SceneManager.activeSceneChanged += OnSceneChanged;

            ClearRegisteredServices();
            foreach (var service in autoCreatedServices) DestroyService(service);
            autoCreatedServices.Clear();

            var types = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where typeof(GameService).IsAssignableFrom(type) && type.IsAbstract == false
                select type).ToList();

            foreach (var sceneService in staticServices)
            {
                if (sceneService == null) continue;
                types.Remove(sceneService.GetType());
            }

            foreach (var type in types) autoCreatedServices.Add(CreateInstance(type) as GameService);

            Construct();
        }

        protected virtual void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        protected virtual void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            ClearRegisteredServices();
            if (instance == this) instance = null;

            foreach (var service in autoCreatedServices)
            {
                if (service == null) continue;
                DestroyService(service);
            }

            autoCreatedServices.Clear();
        }
		
		private static bool Is<T>(object item)
        {
#if UNITY_EDITOR
            if (ReferenceEquals(item, null))
                return false;

            return item switch
            {
                T => true,
                GameObject go => go.TryGetComponent<T>(out _),
                Component component => component.TryGetComponent<T>(out _),
                _ => false
            };
#else
			return item is T;
#endif
        }
        
        private static void DestroyService(UnityEngine.Object obj)
        {
            if (obj == null) return;

#if UNITY_EDITOR
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
#else
            UnityEngine.Object.Destroy(obj);
#endif
        }

        protected virtual void OnValidate()
        {
            foreach (var scriptableService in staticServices)
                if (scriptableService != null)
                    Assert.IsTrue(Is<IService>(scriptableService));

            foreach (var sceneService in sceneServices)
                if (sceneService != null)
                    Assert.IsTrue(Is<IService>(sceneService));

            if (Application.isPlaying) return;

            foreach (var service in staticServices)
            {
                if (service == null) continue;
                var existService =
                    autoCreatedServices.FirstOrDefault(s => s != null && s.GetType() == service.GetType());
                if (existService)
                {
                    autoCreatedServices.Remove(existService);
                    DestroyService(existService);
                }
            }

            Construct();
        }

        public TService Resolve<TService>() where TService : IService
        {
            var type = TypeCache<TService>.Value;

            if (registeredServices.TryGetValue(type, out var service) && service.IsAlive())
                return (TService)service;
            registeredServices.Remove(type);

            if (serviceFactories.TryGetValue(type, out var factory))
            {
                service = factory();
                if (!service.IsAlive())
                    throw new InvalidOperationException($"The factory for {type.FullName} returned a null, destroyed or disposed service.");
                var result = (TService)service;
                registeredServices[type] = service;
                return result;
            }

            return default;
        }

        public ILocator Resolve<TService>(out TService value) where TService : IService
        {
            value = Resolve<TService>();
            return this;
        }

        IRegister IRegister.Bind<TService>(Func<IService> resolver, Lifetime lifetime)
        {
            Assert.IsNotNull(resolver, $"A factory is required for {typeof(TService).FullName}.");
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var type = TypeCache<TService>.Value;
            serviceFactories[type] = resolver;

            if (lifetime == Lifetime.Scene)
                sceneOnlyTypes.Add(type);
            else
                sceneOnlyTypes.Remove(type);
            RemoveRegisteredType(type);

            return this;
        }

        void IRegister.Unbind<TService>()
        {
            var type = TypeCache<TService>.Value;
            serviceFactories.Remove(type);
            sceneOnlyTypes.Remove(type);
            RemoveRegisteredType(type);
        }

        IRegister IRegister.Register<TService>([NotNull] TService service, Lifetime lifetime)
        {
            if (!service.IsAlive()) throw new ArgumentNullException(nameof(service));
            var type = TypeCache<TService>.Value;
            serviceFactories.Remove(type);
            registeredServices[type] = service;

            if (lifetime == Lifetime.Scene)
                sceneOnlyTypes.Add(type);
            else
                sceneOnlyTypes.Remove(type);

            return this;
        }

        void IRegister.Unregister<TService>()
        {
            var type = TypeCache<TService>.Value;
            serviceFactories.Remove(type);
            sceneOnlyTypes.Remove(type);
            RemoveRegisteredType(type);
        }


#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        protected static void FindAndSetupEditorInstance()
        {
            if (instance != null) return;

            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            foreach (var asset in preloadedAssets)
            {
                if (asset is not ServiceLocator game) continue;
                instance = game;
                break;
            }

            Assert.IsNotNull(instance, $"Add a {nameof(ServiceLocator)} asset to Player Settings / Preloaded Assets.");
        }
#endif
        private void OnSceneChanged(Scene _, Scene __)
        {
            foreach (var sceneOnlyType in sceneOnlyTypes)
                RemoveRegisteredType(sceneOnlyType);

            foreach (var service in createdSceneServices)
                if (service == null || service.Lifetime == Lifetime.Scene)
                    DisposeRegisteredService(service);
            createdSceneServices.RemoveWhere(service => service == null || service.IsDisposed);

            if (localSceneParent != null) DestroyService(localSceneParent.gameObject);
            localSceneParent = null;
        }

        private void RemoveRegisteredType(in Type type)
        {
            if (registeredServices.Remove(type, out var service) == false) return;
            DisposeRegisteredService(service);
        }

        private void DisposeRegisteredService(IService service)
        {
            if (!service.IsAlive()) return;
            // Configured ScriptableObjects are borrowed assets; only auto-created ones are owned here.
            if (service is GameService gameService && !autoCreatedServices.Contains(gameService)) return;
            try
            {
                if (service is GameService ownedService) DestroyService(ownedService);
                else service.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void ClearRegisteredServices()
        {
            var services = registeredServices.Values
                .Where(service => service is not GameService && service is not SceneService)
                .Concat(createdSceneServices).Distinct();
            foreach (var service in services) DisposeRegisteredService(service);
            registeredServices.Clear();
            serviceFactories.Clear();
            sceneOnlyTypes.Clear();
            createdSceneServices.Clear();
            if (localSceneParent != null) DestroyService(localSceneParent.gameObject);
            if (gameParent != null) DestroyService(gameParent.gameObject);
            localSceneParent = null;
            gameParent = null;
        }

#if ODIN_INSPECTOR
        [Button]
#endif
        protected void Construct()
        {
            ClearRegisteredServices();
            RegisterStaticServices();
            RegisterSceneServices();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoaded()
        {
            if (instance == null) return;

            foreach (var sceneService in instance.sceneServices)
                if (sceneService && sceneService.SpawnOnLoad)
                {
                    var services = FindObjectsByType<SceneService>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    var existOnScene = false;
                    foreach (var existService in services)
                    {
                        if (existService.IsDisposed || existService.GetType() != sceneService.GetType()) continue;
                        existOnScene = true;
                        break;
                    }

                    if (existOnScene == false)
                        instance.CreateService(sceneService);
                }
        }

        private IService CreateService(SceneService prefab)
        {
            Assert.IsNotNull(prefab);
            var serviceInstance = Instantiate(prefab);
            Assert.IsNotNull(serviceInstance, $"Failed to instantiate service prefab {prefab.name}.");
            serviceInstance.gameObject.name = $"<{prefab.name}>";
            createdSceneServices.Add(serviceInstance);

            if (createParentForSceneServices == false) return serviceInstance;

            if (serviceInstance.Lifetime == Lifetime.Scene)
            {
                if (localSceneParent == null)
                    localSceneParent = new GameObject("====<SceneServices>====").transform;
                serviceInstance.transform.SetParent(localSceneParent);
            }
            else
            {
                if (gameParent == null)
                {
                    gameParent = new GameObject("====<GameServices>====").transform;
                    if (Application.isPlaying) DontDestroyOnLoad(gameParent);
                }

                serviceInstance.transform.SetParent(gameParent);
            }

            return serviceInstance;
        }

        private void RegisterSceneServices()
        {
            foreach (var sceneServicePrefab in sceneServices)
            {
                if (sceneServicePrefab == null)
                    continue;

                var services = sceneServicePrefab.GetComponents<SceneService>();
				Assert.IsNotNull(services);
                foreach (var service in services)
                    service.Register(this, () => CreateService(service));
            }
        }

        private void RegisterStaticServices()
        {
            foreach (var service in staticServices)
            {
                if (service == null) continue;

                service.Register(this);
            }

            foreach (var service in autoCreatedServices)
            {
                if (service == null) continue;

                service.Register(this);
            }
        }
		
		static class TypeCache<T>
		{
			public static Type Value { get; } = typeof(T);
		}
    }
}
