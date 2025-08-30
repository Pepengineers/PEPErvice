using System.Collections.Generic;
using PEPEngineers.PEPErvice.Runtime;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface ILocator
	{
		TService GetService<TService>() where TService : ISubsystem;
		ILocator GetService<TService>(out TService value) where TService : ISubsystem;
	}
}