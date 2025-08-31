using System;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface ILocator
	{
		ISubsystem GetSystem(Type type);
		TSystem GetSystem<TSystem>() where TSystem : ISubsystem;
		ILocator GetSystem<TSystem>(out TSystem value) where TSystem : ISubsystem;
	}
}