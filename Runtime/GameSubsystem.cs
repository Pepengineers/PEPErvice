using System;
using PEPEngineers.PEPErvice.Extensions;
using PEPEngineers.PEPErvice.Interfaces;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Assertions;
using ISubsystem = PEPEngineers.PEPErvice.Interfaces.ISubsystem;

namespace PEPEngineers.PEPErvice.Runtime
{
	public abstract class GameSubsystem<T> : GameSubsystem where T : class, ISubsystem
	{
		private static T instance;

		private bool CanInitialize =>
			ReferenceEquals(instance, null) && instance == null && !IsApplicationQuitting;

		internal sealed override IRegister Register(IRegister register)
		{
			Assert.IsTrue(this.Is<T>());
			return register.RegisterSubsystem<T>(this as T);
		}

		protected virtual void OnInitialized()
		{
		}

		private void Awake()
		{
			string path = string.Empty;
#if UNITY_EDITOR
			path = AssetDatabase.GetAssetPath(this);
#endif
			if (CanInitialize)
			{
#if DEBUG
				Debug.Log($"#{TypeCache<T>.Value.Name}# Created {path}", this);
#endif
				instance = this as T;
				UnityLocator.Instance.RegisterSubsystem(instance);

				OnInitialized();
			}
			else
			{

#if DEBUG
				Debug.LogWarning($"#{TypeCache<T>.Value.Name}# Failed Initialize System {path}", this);
#endif
				this.Destroy();
			}
		}
	}

	public abstract class GameSubsystem : ScriptableObject, ISubsystem
	{
		protected bool IsApplicationQuitting { get; private set; }

		void IDisposable.Dispose()
		{
			this.Destroy();
		}

		private void OnApplicationQuit()
		{
			IsApplicationQuitting = true;
		}

		internal abstract IRegister Register(IRegister register);
	}
}