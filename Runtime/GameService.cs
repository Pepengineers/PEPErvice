using System;
using PEPEngineers.PEPErvice.Implementations;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;

namespace PEPEngineers.PEPErvice.Runtime
{
	public abstract class GameService : MonoBehaviour, IService
	{
		protected static bool IsApplicationQuitting { get; private set; }

		private static bool CanDispose => IsApplicationQuitting == false;

		private void OnApplicationQuit()
		{
			IsApplicationQuitting = true;
		}

		void IDisposable.Dispose()
		{
			if (CanDispose)
				Destroy(this);
		}
	}

	public abstract class GameService<TService> : GameService
		where TService : GameService<TService>, IService
	{
		private static TService instance;

		private static bool CanInitialize =>
			ReferenceEquals(instance, null) && IsApplicationQuitting == false;

		private void Awake()
		{
			if (CanInitialize)
			{
#if UNITY_EDITOR || DEBUG
				UnityEngine.Debug.Log($"#GameService# <{TypeFactory<TService>.Type.Name}> Created", this);
#endif
				instance = this as TService;
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
			if (instance != this)
				return;
#if UNITY_EDITOR || DEBUG
			UnityEngine.Debug.Log($"#GameService# <{TypeFactory<TService>.Type.Name}> {name} Destroyed", this);
#endif
			OnDestroyed();
			instance = null;
		}

		protected virtual void OnInitialized()
		{
		}

		protected virtual void OnDestroyed()
		{
		}
	}
}