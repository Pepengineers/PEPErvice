using System;
using PEPEngineers.PEPErvice.Data;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface IRegister
	{
		IRegister BindSystem(Type type, Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton);
		IRegister BindSystem<TSystem>(Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton) where TSystem : ISubsystem;

		void UnbindSystem(Type type);
		void UnbindSystem<TSystem>() where TSystem : ISubsystem;

		IRegister RegisterSystem(Type type, ISubsystem subsystem, Lifetime lifetime = Lifetime.Singleton);
		IRegister RegisterSystem<TSystem>(TSystem service, Lifetime lifetime = Lifetime.Singleton) where TSystem : ISubsystem;
		void UnregisterSystem<TSystem>() where TSystem : ISubsystem;
	}
}