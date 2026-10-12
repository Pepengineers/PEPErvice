using System;
using PEPEngineers.PEPErvice.Data;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface IRegister
	{
		IRegister BindSubsystem(Type type, Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton);
		IRegister BindSubsystem<T>(Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton) where T : ISubsystem;

		void UnbindSubsystem(Type type);
		void UnbindSubsystem<T>() where T : ISubsystem;

		IRegister RegisterSubsystem(Type type, ISubsystem subsystem, Lifetime lifetime = Lifetime.Singleton);
		IRegister RegisterSubsystem<T>(T service, Lifetime lifetime = Lifetime.Singleton) where T : ISubsystem;
		void UnregisterSystem<T>() where T : ISubsystem;
	}
}