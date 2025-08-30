using System;
using PEPEngineers.PEPErvice.Runtime;
using PEPEngineers.PEPErvice.Data;
using ISubsystem = PEPEngineers.PEPErvice.Interfaces.ISubsystem;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface IRegister
	{
		IRegister BindSystem<TSystem>(Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton)
			where TSystem : ISubsystem;
		
		void UnbindSystem<TSystem>() where TSystem : ISubsystem;
		
		IRegister RegisterSystem<TSystem>(TSystem service, Lifetime lifetime = Lifetime.Singleton)
			where TSystem : ISubsystem;

		void UnregisterSystem<TSystem>() where TSystem : ISubsystem;
	}
}