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
		private List<GameSubsystem> gameSubsystems = new();

		[FormerlySerializedAs("sceneServices")]
		[SerializeField]
#if ODIN_INSPECTOR
		[TitleGroup("Register")]
		[PropertySpace(5, 10)]
		[Searchable]
		[InlineEditor]
#endif
		private List<SceneSubsystem> sceneSubsystems = new();

		[SerializeField]
#if ODIN_INSPECTOR
		[TitleGroup("Register")]
		[PropertySpace(0, 50)]
#endif
		private bool createParentForSceneSystems;

		private Transform gameParent;
		private Transform localSceneParent;
		private SubsystemLocator subsystemLocator = new();

		private Type[] subsystemTypes = Array.Empty<Type>();
		protected internal static UnityLocator Instance { get; private set; }

#if ODIN_INSPECTOR
		[TitleGroup("Locator")]
		[PropertyOrder(100)]
		[ShowInInspector]
		[ReadOnly]
#endif
		protected internal IReadOnlyDictionary<Type, Func<ISubsystem>> SystemFactories => subsystemLocator.SubsystemFactories;

		protected internal IReadOnlyCollection<GameSubsystem> GameSubsystems => gameSubsystems;
		protected internal IReadOnlyCollection<SceneSubsystem> SceneSubsystems => sceneSubsystems;

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
		protected internal IReadOnlyDictionary<Type, ISubsystem> RegisteredSystems => subsystemLocator.RegisteredSubsystems;

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
			foreach (var subsystem in gameSubsystems)
				if (subsystem != null)
					Assert.IsTrue(subsystem.Is<ISubsystem>());

			foreach (var subsystem in sceneSubsystems)
				if (subsystem != null)
					Assert.IsTrue(subsystem.Is<ISubsystem>());

			Construct();
		}

		public ISubsystem GetSubsystem(Type type)
		{
			Assert.IsNotNull(type);
			Debug.Log($"{nameof(UnityLocator)} Get System {type.Name}");
			return subsystemLocator.GetSubsystem(type);
		}

		public T GetSubsystem<T>() where T : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Get System {typeof(T).Name}");
			return subsystemLocator.GetSubsystem<T>();
		}

		public ILocator GetSubsystem<T>(out T value) where T : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Get System {typeof(T).Name}");
			return subsystemLocator.GetSubsystem(out value);
		}

		public IRegister BindSubsystem(Type type, Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton)
		{
			Assert.IsNotNull(type);
			Assert.IsNotNull(resolver);
			Debug.Log($"{nameof(UnityLocator)} Bind System {type.Name}");
			return subsystemLocator.BindSubsystem(type, resolver, lifetime);
		}

		public IRegister BindSubsystem<T>(Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton) where T : ISubsystem
		{
			Assert.IsNotNull(resolver);
			Debug.Log($"{nameof(UnityLocator)} Bind System {typeof(T).Name}");
			return subsystemLocator.BindSubsystem<T>(resolver, lifetime);
		}

		public void UnbindSubsystem(Type type)
		{
			Assert.IsNotNull(type);
			Debug.Log($"{nameof(UnityLocator)} Unbind System {type.Name}");
			subsystemLocator.UnbindSubsystem(type);
		}

		public void UnbindSubsystem<T>() where T : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Unbind System {typeof(T).Name}");
			subsystemLocator.UnbindSubsystem<T>();
		}

		public IRegister RegisterSubsystem(Type type, ISubsystem subsystem, Lifetime lifetime = Lifetime.Singleton)
		{
			Assert.IsNotNull(type);
			Assert.IsNotNull(subsystem);
			Debug.Log($"{nameof(UnityLocator)} Register System {type.Name}");
			return subsystemLocator.RegisterSubsystem(type, subsystem, lifetime);
		}

		public IRegister RegisterSubsystem<T>(T system, Lifetime lifetime = Lifetime.Singleton) where T : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Register System {typeof(T).Name}");
			return subsystemLocator.RegisterSubsystem(system, lifetime);
		}

		public void UnregisterSystem<T>() where T : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Unregister System {typeof(T).Name}");
			subsystemLocator.UnregisterSystem<T>();
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
			subsystemLocator.RemoveSceneSystems();

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
			subsystemTypes = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
				from type in assembly.GetTypes()
				where typeof(ISubsystem).IsAssignableFrom(type) && !type.IsAbstract
				select type).ToArray();
			subsystemLocator.Clear();
			RegisterGameSubsystems();
			RegisterSceneSubsystems();
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
			foreach (var system in locator.sceneSubsystems)
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
						locator.CreateSceneSubsystem(system.GetType(), () => Instantiate(system));
				}
		}

		private ISubsystem CreateSceneSubsystem(Type type, Func<SceneSubsystem> factory)
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

		private void RegisterSceneSubsystems()
		{
			var sceneTypes = (from t in subsystemTypes
				where typeof(SceneSubsystem).IsAssignableFrom(t)
				select t).ToList();

			foreach (var systemPrefab in sceneSubsystems)
			{
				if (systemPrefab == null) continue;
				sceneTypes.Remove(systemPrefab.GetType());

				var systems = systemPrefab.GetComponents<SceneSubsystem>();
				Assert.IsNotNull(systems);
				foreach (var system in systems) system.Register(this, () => CreateSceneSubsystem(systemPrefab.GetType(), () => Instantiate(systemPrefab)));
			}

			foreach (var type in sceneTypes)
			{
				Debug.Log($"{nameof(UnityLocator)} Add Default Bind for {type.Name} system");
				subsystemLocator.BindSubsystem(type,
					() => CreateSceneSubsystem(type, () => new GameObject(type.Name).AddComponent(type) as SceneSubsystem));
			}
		}

		private void RegisterGameSubsystems()
		{
			var existGameTypes = (from t in subsystemTypes
				where typeof(GameSubsystem).IsAssignableFrom(t)
				select t).ToList();

			foreach (var gameSystem in gameSubsystems)
			{
				if (gameSystem == null) continue;
				var type = gameSystem.GetType();
				existGameTypes.Remove(type);
				gameSystem.Register(this);
			}

			foreach (var type in existGameTypes)
			{
				Debug.Log($"{nameof(UnityLocator)} Add Default Bind for {type.Name} system");
				subsystemLocator.BindSubsystem(type, () => CreateInstance(type) as GameSubsystem);
			}
		}
	}
}