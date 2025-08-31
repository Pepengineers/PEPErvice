using System;
using System.Collections.Generic;
using System.Linq;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using ISubsystem = PEPEngineers.PEPErvice.Interfaces.ISubsystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif


namespace PEPEngineers.PEPErvice.Runtime
{
	[Serializable]
	public abstract class UnityLocator : ScriptableObject, ILocator, IRegister
	{
		[FormerlySerializedAs("staticServices")]
		[SerializeField]
#if ODIN_INSPECTOR
		[TitleGroup("Register", order: 100)]
		[PropertySpace(5, 10)]
		[Searchable]
		[InlineEditor]
#endif
		private List<GameSubsystem> gameSystems = new();

		[FormerlySerializedAs("sceneServices")]
		[SerializeField]
#if ODIN_INSPECTOR
		[TitleGroup("Register")]
		[PropertySpace(5, 10)]
		[Searchable]
		[InlineEditor]
#endif
		private List<SceneSubsystem> sceneSystems = new();

		[SerializeField]
#if ODIN_INSPECTOR
		[TitleGroup("Register")]
		[PropertySpace(0, 50)]
#endif
		private bool createParentForSceneSystems;

		private Transform gameParent;
		private Transform localSceneParent;
		private SystemLocator systemLocator = new();

		private Type[] systemTypes = Array.Empty<Type>();
		protected internal static UnityLocator Instance { get; private set; }

#if ODIN_INSPECTOR
		[TitleGroup("Locator")]
		[PropertyOrder(100)]
		[ShowInInspector]
		[ReadOnly]
#endif
		protected internal IReadOnlyDictionary<Type, Func<ISubsystem>> SystemFactories => systemLocator.SystemFactories;

		protected internal IReadOnlyCollection<GameSubsystem> GameSystems => gameSystems;
		protected internal IReadOnlyCollection<SceneSubsystem> SceneSystems => sceneSystems;

#if ODIN_INSPECTOR
		[TitleGroup("Locator")]
		[PropertyOrder(100)]
		[DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.OneLine,
			KeyLabel = "Type",
			ValueLabel = "System")]
		[ShowInInspector]
		[ReadOnly]
		[PropertySpace(5, 25)]
#endif
		protected internal IReadOnlyDictionary<Type, ISubsystem> RegisteredSystems => systemLocator.RegisteredSystems;

		protected virtual void Awake()
		{
			SceneManager.activeSceneChanged += OnSceneChanged;
			Construct();
		}

		protected virtual void OnEnable()
		{
			Construct();
		}

		protected virtual void OnDestroy()
		{
			SceneManager.activeSceneChanged -= OnSceneChanged;
		}

		protected virtual void OnValidate()
		{
			foreach (var subsystem in gameSystems)
				if (subsystem != null)
					Assert.IsTrue(subsystem.Is<ISubsystem>());

			foreach (var subsystem in sceneSystems)
				if (subsystem != null)
					Assert.IsTrue(subsystem.Is<ISubsystem>());

			Construct();
		}

		public ISubsystem GetSystem(Type type)
		{
			Assert.IsNotNull(type);
			Debug.Log($"{nameof(UnityLocator)} Get System {type.Name}");
			return systemLocator.GetSystem(type);
		}

		public TSystem GetSystem<TSystem>() where TSystem : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Get System {typeof(TSystem).Name}");
			return systemLocator.GetSystem<TSystem>();
		}

		public ILocator GetSystem<TSystem>(out TSystem value) where TSystem : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Get System {typeof(TSystem).Name}");
			return systemLocator.GetSystem(out value);
		}

		public IRegister BindSystem(Type type, Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton)
		{
			Assert.IsNotNull(type);
			Assert.IsNotNull(resolver);
			Debug.Log($"{nameof(UnityLocator)} Bind System {type.Name}");
			return systemLocator.BindSystem(type, resolver, lifetime);
		}

		public IRegister BindSystem<TSystem>(Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton) where TSystem : ISubsystem
		{
			Assert.IsNotNull(resolver);
			Debug.Log($"{nameof(UnityLocator)} Bind System {typeof(TSystem).Name}");
			return systemLocator.BindSystem<TSystem>(resolver, lifetime);
		}

		public void UnbindSystem(Type type)
		{
			Assert.IsNotNull(type);
			Debug.Log($"{nameof(UnityLocator)} Unbind System {type.Name}");
			systemLocator.UnbindSystem(type);
		}

		public void UnbindSystem<TSystem>() where TSystem : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Unbind System {typeof(TSystem).Name}");
			systemLocator.UnbindSystem<TSystem>();
		}

		public IRegister RegisterSystem(Type type, ISubsystem subsystem, Lifetime lifetime = Lifetime.Singleton)
		{
			Assert.IsNotNull(type);
			Assert.IsNotNull(subsystem);
			Debug.Log($"{nameof(UnityLocator)} Register System {type.Name}");
			return systemLocator.RegisterSystem(type, subsystem, lifetime);
		}

		public IRegister RegisterSystem<TSystem>(TSystem system, Lifetime lifetime = Lifetime.Singleton) where TSystem : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Register System {typeof(TSystem).Name}");
			return systemLocator.RegisterSystem(system, lifetime);
		}

		public void UnregisterSystem<TSystem>() where TSystem : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Unregister System {typeof(TSystem).Name}");
			systemLocator.UnregisterSystem<TSystem>();
		}


#if UNITY_EDITOR
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		protected static void FindAndSetupEditorInstance()
		{
			var preloadedAssets = PlayerSettings.GetPreloadedAssets();
			foreach (var asset in preloadedAssets)
				if (asset is UnityLocator locator)
				{
					Instance = locator;
					return;
				}

			var type = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes()).FirstOrDefault(t =>
					typeof(UnityLocator).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

			if (type == null)
			{
				Debug.LogError($"Create class inherited from {nameof(UnityLocator)}!");
				return;
			}

			var guids = AssetDatabase.FindAssets($"t:{type.Name}");
			if (!guids.Any())
			{
				Instance = CreateInstance(type) as UnityLocator;
				AssetDatabase.CreateAsset(Instance, $"Assets/{type.Name}");
				AssetDatabase.SaveAssets();
			}
			else
			{
				Instance = AssetDatabase.LoadAssetAtPath<UnityLocator>(AssetDatabase.GUIDToAssetPath(guids.First()));
			}

			PlayerSettings.SetPreloadedAssets(preloadedAssets.Concat(new[] { Instance }).ToArray());
		}
#endif

		private void OnSceneChanged(Scene _, Scene __)
		{
			systemLocator.RemoveSceneSystems();

			localSceneParent.Destroy();
			localSceneParent = null;
		}


		private static bool TryFindInstance(out UnityLocator value)
		{
			if (Instance)
			{
				value = Instance;
				return true;
			}

			value = FindAnyObjectByType<UnityLocator>();
			Instance = value;
			return value != null;
		}

#if ODIN_INSPECTOR
		[Button]
#endif
		protected void Construct()
		{
			Instance = this;
			systemTypes = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
				from type in assembly.GetTypes()
				where typeof(ISubsystem).IsAssignableFrom(type) && !type.IsAbstract
				select type).ToArray();
			systemLocator.Clear();
			RegisterGameSystems();
			RegisterSceneSystems();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void AfterSceneLoaded()
		{
			if (!TryFindInstance(out var locator))
			{
				Debug.LogError($"Not Find {nameof(UnityLocator)} after scene loaded! Game is in not consistent state!");
				return;
			}

			Instance = locator;

			var existSystems = FindObjectsByType<SceneSubsystem>(FindObjectsSortMode.None);
			foreach (var system in locator.sceneSystems)
				if (system && system.SpawnOnLoad)
				{
					var existOnScene = false;
					foreach (var existSystem in existSystems)
					{
						if (existSystem.GetType() != system.GetType()) continue;
						existOnScene = true;
						break;
					}

					if (!existOnScene)
						locator.CreateSceneSystem(system.GetType(), () => Instantiate(system));
				}
		}

		private ISubsystem CreateSceneSystem(Type type, Func<SceneSubsystem> factory)
		{
			Assert.IsNotNull(type);
			Assert.IsNotNull(factory);
			var system = factory();
			system.name = $"<{type.Name}>";

			if (!createParentForSceneSystems) return system;

			if (system.Lifetime == Lifetime.Scene)
			{
				if (localSceneParent == null)
					localSceneParent = new GameObject($@"====<{nameof(SceneSubsystem)}>====").transform;
				system.transform.SetParent(localSceneParent);
			}
			else
			{
				if (gameParent == null)
				{
					gameParent = new GameObject($@"====<Global{nameof(SceneSubsystem)}>====").transform;
					DontDestroyOnLoad(gameParent);
				}

				system.transform.SetParent(gameParent);
			}

			return system;
		}

		private void RegisterSceneSystems()
		{
			var sceneTypes = (from t in systemTypes
				where typeof(SceneSubsystem).IsAssignableFrom(t)
				select t).ToList();

			foreach (var systemPrefab in sceneSystems)
			{
				if (systemPrefab == null) continue;
				sceneTypes.Remove(systemPrefab.GetType());

				var systems = systemPrefab.GetComponents<SceneSubsystem>();
				Assert.IsNotNull(systems);
				foreach (var system in systems) system.Register(this, () => CreateSceneSystem(systemPrefab.GetType(), () => Instantiate(systemPrefab)));
			}

			foreach (var type in sceneTypes)
			{
				Debug.Log($"{nameof(UnityLocator)} Add Default Bind for {type.Name} system");
				systemLocator.BindSystem(type,
					() => CreateSceneSystem(type, () => new GameObject(type.Name).AddComponent(type) as SceneSubsystem));
			}
		}

		private void RegisterGameSystems()
		{
			var existGameTypes = (from t in systemTypes
				where typeof(GameSubsystem).IsAssignableFrom(t)
				select t).ToList();

			foreach (var gameSystem in gameSystems)
			{
				if (gameSystem == null) continue;
				var type = gameSystem.GetType();
				existGameTypes.Remove(type);
				gameSystem.Register(this);
			}

			foreach (var type in existGameTypes)
			{
				Debug.Log($"{nameof(UnityLocator)} Add Default Bind for {type.Name} system");
				systemLocator.BindSystem(type, () => CreateInstance(type) as GameSubsystem);
			}
		}
	}
}