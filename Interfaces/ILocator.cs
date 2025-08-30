using System.Collections.Generic;
using PEPEngineers.PEPErvice.Runtime;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface ILocator
	{
		static ILocator Instance => ServiceLocator.Instance;
		TSubsystem Resolve<TSubsystem>() where TSubsystem : ISubsystem;
		ILocator Resolve<TSubsystem>(out TSubsystem value) where TSubsystem : ISubsystem;
	}
}