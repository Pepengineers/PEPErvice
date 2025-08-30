using System;
using PEPEngineers.PEPErvice.Runtime;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface ISubsystem : IDisposable
	{
	}
	
	public interface ISubsystem<T> : ISubsystem where T : ISubsystem
	{
		static T Instance => ServiceLocator.Instance.Resolve<T>();
	}
}