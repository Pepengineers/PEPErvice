using System;
using PEPEngineers.PEPErvice.Extensions;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;
using UnityEngine.Assertions;

namespace PEPEngineers.PEPErvice.Runtime
{
	public abstract class GameService<TService> : GameService where TService : class, IService
	{
		public sealed override IRegister Register(IRegister register)
		{
			Assert.IsTrue(this.Is<TService>());
			return register.Register<TService>(this as TService);
		}
	}

	public abstract class GameService : ScriptableObject, IService
	{
		void IDisposable.Dispose()
		{
			Destroy(this);
		}

		public abstract IRegister Register(IRegister register);
	}
}