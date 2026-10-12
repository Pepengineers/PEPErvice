using System;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Extensions;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;

namespace PEPEngineers.PEPErvice.Runtime
{
	public abstract class SceneService : MonoBehaviour, IService
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
			foreach (var service in GetComponentsInChildren<SceneService>(true)) service.IsDisposed = true;
			if (Application.isPlaying) Destroy(gameObject);
			else DestroyImmediate(gameObject);
		}

		public abstract IRegister Register(in IRegister register, Func<IService> factory);
	}

	public abstract class SceneService<TService> : SceneService
		where TService : IService
	{
		private static TService instance;
		private bool initialized;

		private bool CanInitialize =>
			!instance.IsAlive() && IsApplicationQuitting == false;

		private void Awake()
		{
			if (CanInitialize)
			{
#if DEBUG
				Debug.Log($"#{TypeCache<TService>.Value.Name}# Created", this);
#endif
				instance = GetComponent<TService>();
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
#if DEBUG
			Debug.Log($"#{TypeCache<TService>.Value.Name}# Destroyed", this);
#endif
			OnDestroyed();
		}

		public sealed override IRegister Register(in IRegister register, Func<IService> factory)
		{
			return register.Bind<TService>(() =>
			{
				var existServices = FindObjectsByType<SceneService>(FindObjectsInactive.Include, FindObjectsSortMode.None);
				foreach (var existService in existServices)
					if (!existService.IsDisposed && existService.TryGetComponent<TService>(out var service) && service.IsAlive())
						return service;
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
}
