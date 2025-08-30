using System;
using PEPEngineers.PEPErvice.Runtime;
using PEPEngineers.PEPErvice.Data;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface IRegister
	{
		static IRegister Instance => ServiceLocator.Instance;
		
		IRegister Bind<TSubsystem>(Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton)
			where TSubsystem : ISubsystem;
		
		void Unbind<TSubsystem>() where TSubsystem : ISubsystem;
		
		IRegister Register<TSubsystem>(TSubsystem service, Lifetime lifetime = Lifetime.Singleton)
			where TSubsystem : ISubsystem;

		void Unregister<TSubsystem>() where TSubsystem : ISubsystem;
	}
}