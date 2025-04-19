using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Interfaces;
using PEPEngineers.PEPErvice.Runtime;
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
            SceneManager.activeSceneChanged += OnSceneChanged;

            instance = this;
            Construct();
        }

        protected virtual void OnEnable()
        {
            instance = this;

            var types = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where typeof(GameService).IsAssignableFrom(type) && type.IsAbstract == false
                select type).ToList();

            foreach (var sceneService in staticServices)
            {
                if (sceneService == null) continue;
                types.Remove(sceneService.GetType());
            }

            autoCreatedServices.Clear();
            foreach (var type in types) autoCreatedServices.Add(CreateInstance(type) as GameService);

            Construct();
        }

        protected virtual void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;

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
            if (Application.isPlaying && !EditorApplication.isPaused)
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

            foreach (var service in staticServices)
            {
                if (service == null) continue;
                var existService =
                    autoCreatedServices.FirstOrDefault(s => s != null && s.GetType() == service.GetType());
                if (existService) autoCreatedServices.Remove(existService);
            }

            Construct();
        }

        public TService Resolve<TService>() where TService : IService
        {
            var type = TypeCache<TService>.Value;

            if (registeredServices.TryGetValue(type, out var service) && service != null)
                return (TService)service;

            if (serviceFactories.TryGetValue(type, out var factory))
            {
                service = factory();
                registeredServices[type] = service;
                return (TService)service;
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
            Assert.IsNotNull(resolver);
            var type = TypeCache<TService>.Value;
            serviceFactories[type] = resolver;

            if (lifetime == Lifetime.Scene)
                sceneOnlyTypes.Add(type);

            return this;
        }

        void IRegister.Unbind<TService>()
        {
            var type = TypeCache<TService>.Value;
            serviceFactories.Remove(type);
            RemoveRegisteredType(type);
        }

        IRegister IRegister.Register<TService>([NotNull] TService service, Lifetime lifetime)
        {
            var type = TypeCache<TService>.Value;
            registeredServices[type] = service;

            if (lifetime == Lifetime.Scene)
                sceneOnlyTypes.Add(type);

            return this;
        }

        void IRegister.Unregister<TService>()
        {
            var type = TypeCache<TService>.Value;
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

            if (instance == null)
            {
                var type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes()).FirstOrDefault(t =>
                        typeof(GameService).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
                if (type == null)
                {
                    Debug.LogError($"Create class inherited from {nameof(GameService)}!");
                    return;
                }

                var guids = AssetDatabase.FindAssets($"t:{type.Name}");
                if (guids.Any())
                {
                    instance = (ServiceLocator)AssetDatabase.LoadAssetAtPath(
                        AssetDatabase.GUIDToAssetPath(guids.First()), type);
                }
                else
                {
                    instance = CreateInstance(type) as ServiceLocator;
                    AssetDatabase.CreateAsset(instance, $"Assets/{type.Name}");
                    AssetDatabase.SaveAssets();
                    PlayerSettings.SetPreloadedAssets(preloadedAssets.Concat(new[] { instance }).ToArray());
                }
            }

            Assert.IsNotNull(instance);
            instance.Construct();
        }
#endif
        private void OnSceneChanged(Scene _, Scene __)
        {
            foreach (var sceneOnlyType in sceneOnlyTypes)
                RemoveRegisteredType(sceneOnlyType);

            Destroy(localSceneParent);
            localSceneParent = null;
        }

        private void RemoveRegisteredType(in Type type)
        {
            if (registeredServices.Remove(type, out var service) == false) return;
            try
            {
                service?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static bool TryFindInstance(out ServiceLocator value)
        {
            value = FindAnyObjectByType<ServiceLocator>();
            return value != null;
        }

#if ODIN_INSPECTOR
        [Button]
#endif
        protected void Construct()
        {
            registeredServices.Clear();
            RegisterStaticServices();
            RegisterSceneServices();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoaded()
        {
            if (instance == null && TryFindInstance(out instance) == false)
                return;

            var services = FindObjectsByType<SceneService>(FindObjectsSortMode.None);
            foreach (var sceneService in instance.sceneServices)
                if (sceneService && sceneService.SpawnOnLoad)
                {
                    var existOnScene = false;
                    foreach (var existService in services)
                    {
                        if (existService.GetType() != sceneService.GetType()) continue;
                        existOnScene = true;
                        break;
                    }

                    if (existOnScene == false)
                        instance.CreateService(sceneService.gameObject);
                }
        }

        private IService CreateService(GameObject prefab)
        {
            Assert.IsNotNull(prefab);
            var service = Instantiate(prefab);
            service.name = $"<{prefab.name}>";
            var serviceInstance = service.GetComponent<SceneService>();
			Assert.IsNotNull(serviceInstance);

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
                    DontDestroyOnLoad(gameParent);
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
                    service.Register(this, () => CreateService(sceneServicePrefab.gameObject));
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