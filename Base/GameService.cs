using System;
using PEPErvice.Interfaces;
using UnityEngine;

namespace PEPErvice.Base
{
	public abstract class GameService : MonoBehaviour, IService 
	{
		[SerializeField] private bool isDontDestroy;
		protected bool IsDontDestroy => isDontDestroy;

		protected static bool IsApplicationQuitting { get; private set; }

		private void OnApplicationQuit()
		{
			IsApplicationQuitting = true;
		}

		private bool IsCanDispose => IsApplicationQuitting == false && IsDontDestroy == false;

		void IDisposable.Dispose()
		{
			if(IsCanDispose)
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
				
				if (IsDontDestroy) 
					DontDestroyOnLoad(this);
				
				ServiceLocator.Instance.Register<TService>(this, 
					IsDontDestroy ? Lifetime.Singleton : Lifetime.Scene);
				
				OnInitialized();
			}
			else
				DestroyImmediate(this);
		}

		private void OnDestroy()
		{
#if UNITY_EDITOR || DEBUG
			UnityEngine.Debug.Log($"#GameService# <{typeof(TService).Name}> {name} Destroyed", this);
#endif
			if (instance != this) 
				return;
			OnDestroyed();
		}

		protected virtual void OnInitialized()
		{
		}

		protected virtual void OnDestroyed()
		{
		}
	}
}