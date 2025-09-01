using System;
using PEPEngineers.PEPErvice.Data;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface IRegister
	{
		IRegister BindSubsystem(Type type, Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton);
		IRegister BindSubsystem<TSystem>(Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton) where TSystem : ISubsystem;

		void UnbindSubsystem(Type type);
		void UnbindSubsystem<TSystem>() where TSystem : ISubsystem;

		IRegister RegisterSubsystem(Type type, ISubsystem subsystem, Lifetime lifetime = Lifetime.Singleton);
		IRegister RegisterSubsystem<TSystem>(TSystem service, Lifetime lifetime = Lifetime.Singleton) where TSystem : ISubsystem;
		void UnregisterSystem<TSystem>() where TSystem : ISubsystem;
	}
}