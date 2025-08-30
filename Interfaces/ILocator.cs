using System.Collections.Generic;
using PEPEngineers.PEPErvice.Runtime;
using ISubsystem = PEPEngineers.PEPErvice.Interfaces.ISubsystem;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface ILocator
	{
		TSystem GeTSystem<TSystem>() where TSystem : ISubsystem;
		ILocator GeTSystem<TSystem>(out TSystem value) where TSystem : ISubsystem;
	}
}