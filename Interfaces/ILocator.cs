using System;
using System.Collections.Generic;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public enum Lifetime : sbyte
	{
		Singleton,
		Scene
	}

	public interface IRegister
	{
		IRegister Bind<TService>(Func<TService> resolver, Lifetime lifetime = Lifetime.Singleton)
			where TService : class;
		
		void Unbind<TService>() where TService : class;
		
		IRegister Register<TService>(object service, Lifetime lifetime = Lifetime.Singleton)
			where TService : class;

		void Unregister<TService>() where TService : class;
	}
	
	public interface ILocator
	{
		IReadOnlyCollection<object> Instances { get; }

		TImpl Resolve<TImpl>() where TImpl : class;
		ILocator Resolve<TImpl>(out TImpl value) where TImpl : class;
	}
}