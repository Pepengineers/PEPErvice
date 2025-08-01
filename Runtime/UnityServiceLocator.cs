using System;
using System.Collections.Generic;
using System.Linq;
using PEPEngineers.PEPErvice.Data;
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
	public abstract class UnityServiceLocator : ScriptableObject, ILocator, IRegister
	{
		[SerializeField]
#if ODIN_INSPECTOR
		[TitleGroup("Register", order: 100)]
		[PropertySpace(5, 10)]
		[Searchable]
		[InlineEditor]
#endif
		private List<GlobalService> staticServices = new();

		[SerializeField]
#if ODIN_INSPECTOR
		[TitleGroup("Register")]
		[PropertySpace(5, 10)]
		[Searchable]
		[InlineEditor]
#endif
		private List<SceneService> sceneServices = new();

		[SerializeField]
#if ODIN_INSPECTOR
		[TitleGroup("Register")]
		[PropertySpace(0, 50)]
#endif
		private bool createParentForSceneServices;

#if ODIN_INSPECTOR
		[TitleGroup("Register", order: 100)] [PropertySpace(5, 10)] [ShowInInspector] [InlineEditor]
#endif
		private readonly List<GlobalService> autoCreatedServices = new();

		private Transform gameParent;
		private Transform localSceneParent;
		private ServiceLocator serviceLocator = new();
		public static UnityServiceLocator Instance { get; private set; }

#if ODIN_INSPECTOR
		[TitleGroup("Locator")]
		[PropertyOrder(100)]
		[ShowInInspector]
		[ReadOnly]
#endif
		protected IReadOnlyDictionary<Type, Func<IService>> serviceFactories => serviceLocator.ServiceFactories;

		protected IReadOnlyCollection<GlobalService> StaticServices => staticServices;
		protected IReadOnlyCollection<SceneService> SceneServices => sceneServices;

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
		protected IReadOnlyDictionary<Type, IService> RegisteredServices => serviceLocator.RegisteredServices;

		protected virtual void Awake()
		{
			SceneManager.activeSceneChanged += OnSceneChanged;
			Construct();
		}

		protected virtual void OnEnable()
		{
			var types = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
				from type in assembly.GetTypes()
				where typeof(GlobalService).IsAssignableFrom(type) && type.IsAbstract == false
				select type).ToList();

			foreach (var sceneService in staticServices)
			{
				if (sceneService == null) continue;
				types.Remove(sceneService.GetType());
			}

			autoCreatedServices.Clear();
			foreach (var type in types) autoCreatedServices.Add(CreateInstance(type) as GlobalService);

			Construct();
		}

		protected virtual void OnDestroy()
		{
			SceneManager.activeSceneChanged -= OnSceneChanged;

			foreach (var service in autoCreatedServices)
			{
				if (service == null) continue;
				service.Destroy();
			}

			autoCreatedServices.Clear();
		}

		protected virtual void OnValidate()
		{
			foreach (var scriptableService in staticServices)
				if (scriptableService != null)
					Assert.IsTrue(scriptableService.Is<IService>());

			foreach (var sceneService in sceneServices)
				if (sceneService != null)
					Assert.IsTrue(sceneService.Is<IService>());

			foreach (var service in staticServices)
			{
				if (service == null) continue;
				var existService =
					autoCreatedServices.FirstOrDefault(s => s != null && s.GetType() == service.GetType());
				if (existService) autoCreatedServices.Remove(existService);
			}

			Construct();
		}

		public TService Get<TService>() where TService : IService
		{
			Debug.Log($"{nameof(UnityServiceLocator)} Get Service {typeof(TService).Name}");
			return serviceLocator.Get<TService>();
		}

		public ILocator Get<TService>(out TService value) where TService : IService
		{
			Debug.Log($"{nameof(UnityServiceLocator)} Get Service {typeof(TService).Name}");
			return serviceLocator.Get(out value);
		}

		public IRegister Bind<TService>(Func<IService> resolver, Lifetime lifetime = Lifetime.Singleton) where TService : IService
		{
			Debug.Log($"{nameof(UnityServiceLocator)} Bind Service {typeof(TService).Name}");
			return serviceLocator.Bind<TService>(resolver, lifetime);
		}

		public void Unbind<TService>() where TService : IService
		{
			Debug.Log($"{nameof(UnityServiceLocator)} Unbind Service {typeof(TService).Name}");
			serviceLocator.Unbind<TService>();
		}

		public IRegister Register<TService>(TService service, Lifetime lifetime = Lifetime.Singleton) where TService : IService
		{
			Debug.Log($"{nameof(UnityServiceLocator)} Register Service {typeof(TService).Name}");
			return serviceLocator.Register(service, lifetime);
		}

		public void Unregister<TService>() where TService : IService
		{
			Debug.Log($"{nameof(UnityServiceLocator)} Unregister Service {typeof(TService).Name}");
			serviceLocator.Unregister<TService>();
		}


#if UNITY_EDITOR
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		protected static void FindAndSetupEditorInstance()
		{
			var preloadedAssets = PlayerSettings.GetPreloadedAssets();
			foreach (var asset in preloadedAssets)
				if (asset is UnityServiceLocator)
					return;

			var type = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes()).FirstOrDefault(t =>
					typeof(UnityServiceLocator).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

			if (type == null)
			{
				Debug.LogError($"Create class inherited from {nameof(UnityServiceLocator)}!");
				return;
			}

			var guids = AssetDatabase.FindAssets($"t:{type.Name}");
			if (guids.Any() == false)
			{
				var serviceLocator = CreateInstance(type) as UnityServiceLocator;
				AssetDatabase.CreateAsset(serviceLocator, $"Assets/{type.Name}");
				AssetDatabase.SaveAssets();
				PlayerSettings.SetPreloadedAssets(preloadedAssets.Concat(new[] { serviceLocator }).ToArray());
			}
		}
#endif

		private void OnSceneChanged(Scene _, Scene __)
		{
			serviceLocator.RemoveSceneServices();

			localSceneParent.Destroy();
			localSceneParent = null;
		}


		private static bool TryFindInstance(out UnityServiceLocator value)
		{
			value = FindAnyObjectByType<UnityServiceLocator>();
			return value != null;
		}

#if ODIN_INSPECTOR
		[Button]
#endif
		protected void Construct()
		{
			Instance = this;
			serviceLocator.Clear();
			RegisterStaticServices();
			RegisterSceneServices();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void AfterSceneLoaded()
		{
			if (TryFindInstance(out var serviceLocator) == false)
			{
				Debug.LogError($"Not Find {nameof(UnityServiceLocator)} after scene loaded! Game is in not consistent state!");
				return;
			}

			Instance = serviceLocator;

			var services = FindObjectsByType<SceneService>(FindObjectsSortMode.None);
			foreach (var sceneService in serviceLocator.sceneServices)
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
						serviceLocator.CreateService(sceneService.gameObject);
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
				{
					Debug.Log($"{nameof(UnityServiceLocator)} Register Service {service.GetType().Name}");
					service.Register(this, () => CreateService(sceneServicePrefab.gameObject));
				}
			}
		}

		private void RegisterStaticServices()
		{
			foreach (var service in staticServices)
			{
				if (service == null) continue;

				Debug.Log($"{nameof(UnityServiceLocator)} Register Service {service.GetType().Name}");
				service.Register(this);
			}

			foreach (var service in autoCreatedServices)
			{
				if (service == null) continue;

				Debug.Log($"{nameof(UnityServiceLocator)} Register Service {service.GetType().Name}");
				service.Register(this);
			}
		}
	}
}