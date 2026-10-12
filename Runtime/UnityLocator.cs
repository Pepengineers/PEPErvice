using System;
using System.Collections.Generic;
using System.Linq;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Extensions;
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

		[FormerlySerializedAs("createParentForSceneServices")]
		[SerializeField]
#if ODIN_INSPECTOR
		[TitleGroup("Register")]
		[PropertySpace(0, 50)]
#endif
		private bool createParentForSceneSystems;

		private Transform gameParent;
		private Transform localSceneParent;
		private SubsystemLocator subsystemLocator;
		private SubsystemLocator Locator => subsystemLocator ??= new SubsystemLocator(DisposeSubsystem);
		private readonly HashSet<GameSubsystem> createdGameSubsystems = new();
		private readonly HashSet<SceneSubsystem> createdSceneSubsystems = new();

		private readonly List<Type> subsystemTypes = new();
		private static UnityLocator instance;
		protected internal static UnityLocator Instance
		{
			get
			{
#if UNITY_EDITOR
				FindAndSetupEditorInstance();
#endif
				return instance;
			}
		}

#if ODIN_INSPECTOR
		[TitleGroup("Locator")]
		[PropertyOrder(100)]
		[ShowInInspector]
		[ReadOnly]
#endif
		protected internal IReadOnlyDictionary<Type, Func<ISubsystem>> SystemFactories => Locator.SubsystemFactories;

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
		protected internal IReadOnlyDictionary<Type, ISubsystem> RegisteredSystems => Locator.RegisteredSubsystems;

		protected virtual void Awake()
		{
			instance = this;
		}

		protected virtual void OnEnable()
		{
			instance = this;
			SceneManager.activeSceneChanged -= OnSceneChanged;
			SceneManager.activeSceneChanged += OnSceneChanged;
			Construct();
		}

		protected virtual void OnDisable()
		{
			SceneManager.activeSceneChanged -= OnSceneChanged;
		}

		protected virtual void OnDestroy()
		{
			SceneManager.activeSceneChanged -= OnSceneChanged;
			ClearOwnedSubsystems();
			if (ReferenceEquals(instance, this)) instance = null;
		}

		protected virtual void OnValidate()
		{
			foreach (var subsystem in gameSubsystems)
				if (subsystem != null)
					Assert.IsTrue(subsystem.Is<ISubsystem>());

			foreach (var subsystem in sceneSubsystems)
				if (subsystem != null)
					Assert.IsTrue(subsystem.Is<ISubsystem>());

			if (!Application.isPlaying) Construct();
		}

		public ISubsystem GetSubsystem(Type type)
		{
			Assert.IsNotNull(type);
			Debug.Log($"{nameof(UnityLocator)} Get System {type.Name}");
			return Locator.GetSubsystem(type);
		}

		public T GetSubsystem<T>() where T : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Get System {typeof(T).Name}");
			return Locator.GetSubsystem<T>();
		}

		public ILocator GetSubsystem<T>(out T value) where T : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Get System {typeof(T).Name}");
			Locator.GetSubsystem(out value);
			return this;
		}

		public IRegister BindSubsystem(Type type, Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton)
		{
			Assert.IsNotNull(type);
			Assert.IsNotNull(resolver);
			Debug.Log($"{nameof(UnityLocator)} Bind System {type.Name}");
			Locator.BindSubsystem(type, resolver, lifetime);
			return this;
		}

		public IRegister BindSubsystem<T>(Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton) where T : ISubsystem
		{
			Assert.IsNotNull(resolver);
			Debug.Log($"{nameof(UnityLocator)} Bind System {typeof(T).Name}");
			Locator.BindSubsystem<T>(resolver, lifetime);
			return this;
		}

		public void UnbindSubsystem(Type type)
		{
			Assert.IsNotNull(type);
			Debug.Log($"{nameof(UnityLocator)} Unbind System {type.Name}");
			Locator.UnbindSubsystem(type);
		}

		public void UnbindSubsystem<T>() where T : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Unbind System {typeof(T).Name}");
			Locator.UnbindSubsystem<T>();
		}

		public IRegister RegisterSubsystem(Type type, ISubsystem subsystem, Lifetime lifetime = Lifetime.Singleton)
		{
			Assert.IsNotNull(type);
			Assert.IsNotNull(subsystem);
			Debug.Log($"{nameof(UnityLocator)} Register System {type.Name}");
			Locator.RegisterSubsystem(type, subsystem, lifetime);
			return this;
		}

		public IRegister RegisterSubsystem<T>(T system, Lifetime lifetime = Lifetime.Singleton) where T : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Register System {typeof(T).Name}");
			Locator.RegisterSubsystem(system, lifetime);
			return this;
		}

		public void UnregisterSystem<T>() where T : ISubsystem
		{
			Debug.Log($"{nameof(UnityLocator)} Unregister System {typeof(T).Name}");
			Locator.UnregisterSystem<T>();
		}


#if UNITY_EDITOR
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		protected static void FindAndSetupEditorInstance()
		{
			if (instance != null) return;
			var preloadedAssets = PlayerSettings.GetPreloadedAssets();
			foreach (var asset in preloadedAssets)
				if (asset is UnityLocator locator)
				{
					instance = locator;
					return;
				}

			Assert.IsNotNull(instance, $"Add a {nameof(UnityLocator)} asset to Player Settings / Preloaded Assets.");
		}
#endif

		private void OnSceneChanged(Scene _, Scene __)
		{
			Locator.RemoveSceneSystems();
			foreach (var subsystem in createdSceneSubsystems)
				if (subsystem != null && subsystem.Lifetime == Lifetime.Scene) DisposeSubsystem(subsystem);
			createdSceneSubsystems.RemoveWhere(subsystem => !subsystem.IsAlive());
			if (localSceneParent != null) localSceneParent.gameObject.Destroy();
			localSceneParent = null;
		}

		private void DisposeSubsystem(ISubsystem subsystem)
		{
			if (!subsystem.IsAlive()) return;
			if (subsystem is GameSubsystem gameSubsystem && !createdGameSubsystems.Contains(gameSubsystem)) return;
			if (subsystem is SceneSubsystem sceneSubsystem && !createdSceneSubsystems.Contains(sceneSubsystem)) return;
			subsystem.Dispose();
		}

		private void ClearOwnedSubsystems()
		{
			subsystemLocator?.Clear();
			foreach (var subsystem in createdGameSubsystems) DisposeSubsystem(subsystem);
			foreach (var subsystem in createdSceneSubsystems) DisposeSubsystem(subsystem);
			createdGameSubsystems.Clear();
			createdSceneSubsystems.Clear();
			if (localSceneParent != null) localSceneParent.gameObject.Destroy();
			if (gameParent != null) gameParent.gameObject.Destroy();
			localSceneParent = null;
			gameParent = null;
		}

#if ODIN_INSPECTOR
		[Button]
#endif
		protected void Construct()
		{
			ClearOwnedSubsystems();
			subsystemTypes.Clear();
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				foreach (var type in assembly.GetTypes())
					if (typeof(ISubsystem).IsAssignableFrom(type) && !type.IsAbstract && !type.ContainsGenericParameters)
						subsystemTypes.Add(type);
			RegisterGameSubsystems();
			RegisterSceneSubsystems();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void AfterSceneLoaded()
		{
			var locator = Instance;
			Assert.IsNotNull(locator);
			if (locator == null) throw new InvalidOperationException($"A preloaded {nameof(UnityLocator)} asset is required.");
			foreach (var system in locator.sceneSubsystems)
				if (system && system.SpawnOnLoad)
				{
					var existSystems = FindObjectsByType<SceneSubsystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
					var existOnScene = false;
					foreach (var existSystem in existSystems)
					{
						if (existSystem.IsDisposed || existSystem.GetType() != system.GetType()) continue;
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
			Assert.IsNotNull(system, $"Failed to create scene subsystem {type.FullName}.");
			if (!system.IsAlive()) throw new InvalidOperationException($"Failed to create scene subsystem {type.FullName}.");
			system.name = $"<{type.Name}>";
			foreach (var component in system.GetComponentsInChildren<SceneSubsystem>(true)) createdSceneSubsystems.Add(component);

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
					if (Application.isPlaying) DontDestroyOnLoad(gameParent);
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

				var systems = systemPrefab.GetComponents<SceneSubsystem>();
				Assert.IsNotNull(systems);
				foreach (var system in systems)
				{
					var type = system.GetType();
					sceneTypes.Remove(type);
					system.Register(this, () => CreateSceneSubsystem(type, () => Instantiate(system)));
					var contract = GetSubsystemContract(type);
					if (contract != type) Locator.BindSubsystem(type, () => GetSubsystem(contract), system.Lifetime);
				}
			}

			foreach (var type in sceneTypes)
			{
				Debug.Log($"{nameof(UnityLocator)} Add Default Bind for {type.Name} system");
				Locator.BindSubsystem(type, () =>
				{
					foreach (var existing in FindObjectsByType<SceneSubsystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
						if (existing.IsAlive() && existing.GetType() == type) return existing;
					return CreateSceneSubsystem(type, () => new GameObject(type.Name).AddComponent(type) as SceneSubsystem);
				});
				BindDiscoveredContract(type);
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
				if (GetSubsystemContract(type) != type) Locator.RegisterSubsystem(type, gameSystem);
			}

			foreach (var type in existGameTypes)
			{
				Debug.Log($"{nameof(UnityLocator)} Add Default Bind for {type.Name} system");
				Locator.BindSubsystem(type, () =>
				{
					var subsystem = CreateInstance(type) as GameSubsystem;
					Assert.IsNotNull(subsystem);
					createdGameSubsystems.Add(subsystem);
					return subsystem;
				});
				BindDiscoveredContract(type);
			}
		}

		private void BindDiscoveredContract(Type type)
		{
			var contract = GetSubsystemContract(type);
			if (contract == type || Locator.RegisteredSubsystems.ContainsKey(contract) || Locator.SubsystemFactories.ContainsKey(contract)) return;
			Locator.BindSubsystem(contract, () => GetSubsystem(type));
		}

		private static Type GetSubsystemContract(Type type)
		{
			for (var parent = type; parent != null; parent = parent.BaseType)
			{
				if (!parent.IsGenericType) continue;
				var definition = parent.GetGenericTypeDefinition();
				if (definition == typeof(GameSubsystem<>) || definition == typeof(SceneSubsystem<>))
					return parent.GetGenericArguments()[0];
			}
			return type;
		}
	}
}
