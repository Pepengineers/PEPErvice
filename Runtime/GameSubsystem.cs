using System;
using PEPEngineers.PEPErvice.Extensions;
using PEPEngineers.PEPErvice.Interfaces;

namespace PEPEngineers.PEPErvice.Runtime
{
	public abstract class GameSubsystem<TSubsystem> : GameSubsystem where TSubsystem : class, ISubsystem
	{
		private static TSubsystem instance;

		private bool CanInitialize =>
			ReferenceEquals(instance, null) && instance == null && !IsApplicationQuitting;

		public sealed override IRegister Register(IRegister register)
		{
			Assert.IsTrue(this.Is<TSubsystem>());
			return register.RegisterService<TSubsystem>(this as TSubsystem);
		}

		protected virtual void OnInitialized()
		{
		}

		private void Awake()
		{
			if (CanInitialize)
			{
#if DEBUG
				Debug.Log($"#{TypeCache<TSubsystem>.Value.Name}# Created", this);
#endif
				instance = this as TSubsystem;

				OnInitialized();
			}
			else
			{
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

		public abstract IRegister Register(IRegister register);
	}
}