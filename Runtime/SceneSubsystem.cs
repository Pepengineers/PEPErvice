using System;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Extensions;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;
using ISubsystem = PEPEngineers.PEPErvice.Interfaces.ISubsystem;

namespace PEPEngineers.PEPErvice.Runtime
{
	public abstract class SceneSubsystem<TService> : SceneSubsystem where TService : ISubsystem
	{
		private static TService instance;

		private bool CanInitialize =>
			ReferenceEquals(instance, null) && instance == null && !IsApplicationQuitting;

		private void Awake()
		{
			if (CanInitialize)
			{
#if DEBUG
				Debug.Log($"#{TypeCache<TService>.Value.Name}# Created", this);
#endif
				instance = GetComponent<TService>();

				UnityLocator.Instance.RegisterSystem(instance);
				
				if (Lifetime == Lifetime.Singleton)
					DontDestroyOnLoad(this);

				OnInitialized();
			}
			else
			{
				this.Destroy();
			}
		}

		private void OnDestroy()
		{
			if (!ReferenceEquals(instance, this))
				return;
#if DEBUG
			Debug.Log($"#{TypeCache<TService>.Value.Name}# Destroyed", this);
#endif
			OnDestroyed();
			instance = default;
		}

		internal sealed override IRegister Register(in IRegister register, Func<ISubsystem> factory)
		{
			return register.BindSystem<TService>(() =>
			{
				var existServices = FindObjectsByType<SceneSubsystem>(FindObjectsSortMode.None);
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

	public abstract class SceneSubsystem : MonoBehaviour, ISubsystem
	{
		[SerializeField] private Lifetime lifetime = Lifetime.Singleton;
		[SerializeField] private bool spawnOnLoad;

		public Lifetime Lifetime => lifetime;
		public bool SpawnOnLoad => spawnOnLoad;
		protected bool IsApplicationQuitting { get; private set; }

		private void OnApplicationQuit()
		{
			IsApplicationQuitting = true;
		}

		void IDisposable.Dispose()
		{
			this.Destroy();
		}

		internal abstract IRegister Register(in IRegister register, Func<ISubsystem> factory);
	}
}