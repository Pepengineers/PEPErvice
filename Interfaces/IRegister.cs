using System;
using PEPEngineers.PEPErvice.Runtime;
using PEPEngineers.PEPErvice.Data;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface IRegister
	{
		IRegister BindService<TService>(Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton)
			where TService : ISubsystem;
		
		void UnbindService<TService>() where TService : ISubsystem;
		
		IRegister RegisterService<TService>(TService service, Lifetime lifetime = Lifetime.Singleton)
			where TService : ISubsystem;

		void UnregisterService<TService>() where TService : ISubsystem;
	}
}