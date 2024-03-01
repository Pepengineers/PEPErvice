using System;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Extensions;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;

namespace PEPEngineers.PEPErvice.Runtime
{
	public abstract class RuntimeService : MonoBehaviour, IService
	{
		[SerializeField] private Lifetime lifetime = Lifetime.Singleton;
		[SerializeField] private bool spawnOnLoad;

		internal Lifetime Lifetime => lifetime;
		internal bool SpawnOnLoad => spawnOnLoad;
		protected bool IsApplicationQuitting { get; private set; }

		private void OnApplicationQuit()
		{
			IsApplicationQuitting = true;
		}

		void IDisposable.Dispose()
		{
			Destroy(this);
		}

		protected internal abstract IRegister Register(in IRegister register, Func<IService> factory);
	}

	public abstract class RuntimeService<TService> : RuntimeService
		where TService : IService
	{
		private static TService instance;

		private bool CanInitialize =>
			ReferenceEquals(instance, null) && instance == null && IsApplicationQuitting == false;

		private void Awake()
		{
			if (CanInitialize)
			{
#if DEBUG
				Debug.Log($"#{TypeCache<TService>.Value.Name}# Created", this);
#endif
				instance = GetComponent<TService>();
				AllServices.Register.Register(instance, Lifetime);

				if (Lifetime == Lifetime.Singleton)
					DontDestroyOnLoad(this);

				OnInitialized();
			}
			else
			{
				DestroyImmediate(gameObject);
			}
		}

		private void OnDestroy()
		{
			if (ReferenceEquals(instance, this) == false)
				return;
#if DEBUG
			Debug.Log($"#{TypeCache<TService>.Value.Name}# Destroyed", this);
#endif
			OnDestroyed();
			instance = default;
			AllServices.Register.Unregister<TService>();
		}

		protected internal sealed override IRegister Register(in IRegister register, Func<IService> factory)
		{
			return register.Bind<TService>(() =>
			{
				var existServices = FindObjectsByType<RuntimeService>(FindObjectsSortMode.None);
				foreach (var existService in existServices)
					if (existService.TryGetComponent<TService>(out var service))
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