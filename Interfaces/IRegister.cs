using System;
using PEPEngineers.PEPErvice.Runtime;
using PEPEngineers.PEPErvice.Data;
using ISubsystem = PEPEngineers.PEPErvice.Interfaces.ISubsystem;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface IRegister
	{
		IRegister BindService<TSystem>(Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton)
			where TSystem : ISubsystem;
		
		void UnbindService<TSystem>() where TSystem : ISubsystem;
		
		IRegister RegisterService<TSystem>(TSystem service, Lifetime lifetime = Lifetime.Singleton)
			where TSystem : ISubsystem;

		void UnregisterService<TSystem>() where TSystem : ISubsystem;
	}
}