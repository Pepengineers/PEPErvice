using System;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Extensions;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;
using UnityEngine.Assertions;
using ISubsystem = PEPEngineers.PEPErvice.Interfaces.ISubsystem;

namespace PEPEngineers.PEPErvice.Runtime
{
	public abstract class SceneSubsystem<T> : SceneSubsystem where T : ISubsystem
	{
		private static T instance;
		private bool initialized;

		private void Awake()
		{
			if (!instance.IsAlive() && !IsApplicationQuitting)
			{
				instance = GetComponent<T>();
				Assert.IsNotNull((object)instance);
				initialized = true;
				if (Lifetime == Lifetime.Singleton && Application.isPlaying)
					DontDestroyOnLoad(this);
				OnInitialized();
			}
			else
			{
				((IDisposable)this).Dispose();
			}
		}

		private void OnDestroy()
		{
			if (!initialized) return;
			initialized = false;
			if (ReferenceEquals(instance, this)) instance = default;
			OnDestroyed();
		}

		internal sealed override IRegister Register(in IRegister register, Func<ISubsystem> factory)
		{
			return register.BindSubsystem<T>(() =>
			{
				var subsystems = FindObjectsByType<SceneSubsystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
				foreach (var subsystem in subsystems)
					if (!subsystem.IsDisposed && subsystem.TryGetComponent<T>(out var existing) && existing.IsAlive())
						return existing;
				return factory();
			}, Lifetime);
		}

		protected virtual void OnInitialized()
		{
		}

		protected virtual void OnDestroyed()
		{
		}
	}

	public abstract class SceneSubsystem : MonoBehaviour, ISubsystem
	{
		[SerializeField] private Lifetime lifetime = Lifetime.Singleton;
		[SerializeField] private bool spawnOnLoad;

		public Lifetime Lifetime => lifetime;
		public bool SpawnOnLoad => spawnOnLoad;
		protected bool IsApplicationQuitting { get; private set; }
		internal bool IsDisposed { get; private set; }

		private void OnApplicationQuit()
		{
			IsApplicationQuitting = true;
		}

		void IDisposable.Dispose()
		{
			if (this == null || IsDisposed) return;
			// Destroy is deferred; exclude the entire hierarchy from lookups immediately.
			foreach (var subsystem in GetComponentsInChildren<SceneSubsystem>(true)) subsystem.IsDisposed = true;
			gameObject.Destroy();
		}

		internal abstract IRegister Register(in IRegister register, Func<ISubsystem> factory);
	}
}
