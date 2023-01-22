using System;
using PEPErvice.Interfaces;
using UnityEngine;

namespace PEPErvice.Base
{
	public abstract class GameService : MonoBehaviour, IService
	{
		protected static bool IsApplicationQuitting { get; private set; }

		private static bool IsCanDispose => IsApplicationQuitting == false;

		private void OnApplicationQuit()
		{
			IsApplicationQuitting = true;
		}

		void IDisposable.Dispose()
		{
			if (IsCanDispose)
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
#if UNITY_EDITOR || DEBUG
			UnityEngine.Debug.Log($"#GameService# <{typeof(TService).Name}> Created", this);
#endif
			if (CanInitialize)
			{
				instance = this as TService;
				OnInitialized();
			}
			else
			{
				DestroyImmediate(this);
			}
		}

		private void OnDestroy()
		{
#if UNITY_EDITOR || DEBUG
			UnityEngine.Debug.Log($"#GameService# <{typeof(TService).Name}> {name} Destroyed", this);
#endif
			if (instance != this)
				return;
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