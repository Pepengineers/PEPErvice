using System;
using PEPEngineers.PEPErvice.Runtime;
using PEPEngineers.PEPErvice.Data;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface IRegister
	{
		IRegister Bind<TService>(Func<IService> resolver, Lifetime lifetime = Lifetime.Singleton)
			where TService : IService;
		
		void Unbind<TService>() where TService : IService;
		
		IRegister Register<TService>(TService service, Lifetime lifetime = Lifetime.Singleton)
			where TService : IService;

		void Unregister<TService>() where TService : IService;
	}
}