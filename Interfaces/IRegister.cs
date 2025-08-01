using System;
using PEPEngineers.PEPErvice.Runtime;
using PEPEngineers.PEPErvice.Data;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface IRegister
	{
		IRegister BindService<TService>(Func<IService> resolver, Lifetime lifetime = Lifetime.Singleton)
			where TService : IService;
		
		void UnbindService<TService>() where TService : IService;
		
		IRegister RegisterService<TService>(TService service, Lifetime lifetime = Lifetime.Singleton)
			where TService : IService;

		void UnregisterService<TService>() where TService : IService;
	}
}