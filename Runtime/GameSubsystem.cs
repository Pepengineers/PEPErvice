using System;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;
using UnityEngine.Assertions;
using ISubsystem = PEPEngineers.PEPErvice.Interfaces.ISubsystem;

namespace PEPEngineers.PEPErvice.Runtime
{
	public abstract class GameSubsystem<T> : GameSubsystem where T : class, ISubsystem
	{
		internal sealed override IRegister Register(IRegister register)
		{
			Assert.IsTrue(this is T);
			return register.RegisterSubsystem(this as T);
		}

		private void Awake()
		{
			// Assets can load before the locator; registration and ownership belong to the locator.
			OnInitialized();
		}

		protected virtual void OnInitialized()
		{
		}
	}

	public abstract class GameSubsystem : ScriptableObject, ISubsystem
	{
		protected bool IsApplicationQuitting { get; private set; }
		internal bool IsDisposed { get; private set; }

		void IDisposable.Dispose()
		{
			if (this == null || IsDisposed) return;
			IsDisposed = true;
			this.Destroy();
		}

		private void OnApplicationQuit()
		{
			IsApplicationQuitting = true;
		}

		internal abstract IRegister Register(IRegister register);
	}
}
