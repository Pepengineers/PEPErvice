using System.Collections.Generic;
using PEPEngineers.PEPErvice.Runtime;
using ISubsystem = PEPEngineers.PEPErvice.Interfaces.ISubsystem;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface ILocator
	{
		TSystem GetSystem<TSystem>() where TSystem : ISubsystem;
		ILocator GetSystem<TSystem>(out TSystem value) where TSystem : ISubsystem;
	}
}