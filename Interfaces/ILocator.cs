using System;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface ILocator
	{
		ISubsystem GetSubsystem(Type type);
		TSystem GetSubsystem<TSystem>() where TSystem : ISubsystem;
		ILocator GetSubsystem<TSystem>(out TSystem value) where TSystem : ISubsystem;
	}
}