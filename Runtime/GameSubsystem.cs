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
	public abstract class GameSubsystem<TSubsystem> : GameSubsystem where TSubsystem : class, ISubsystem
	{
		private static TSubsystem instance;

		private bool CanInitialize =>
			ReferenceEquals(instance, null) && instance == null && !IsApplicationQuitting;

		internal sealed override IRegister Register(IRegister register)
		{
			Assert.IsTrue(this.Is<TSubsystem>());
			return register.RegisterSystem<TSubsystem>(this as TSubsystem);
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
				Debug.Log($"#{TypeCache<TSubsystem>.Value.Name}# Created {path}", this);
#endif
				instance = this as TSubsystem;
				UnityLocator.Instance.RegisterSystem(instance);

				OnInitialized();
			}
			else
			{

#if DEBUG
				Debug.LogWarning($"#{TypeCache<TSubsystem>.Value.Name}# Failed Initialize System {path}", this);
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