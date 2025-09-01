using System;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface ILocator
	{
		ISubsystem GetSubsystem(Type type);
		T GetSubsystem<T>() where T : ISubsystem;
		ILocator GetSubsystem<T>(out T value) where T : ISubsystem;
	}
}