using System;
using PEPEngineers.PEPErvice.Runtime;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface IService : IDisposable
	{
	}
	
	public interface IService<T> : IService where T : IService
	{
		static T Instance => ServiceLocator.Instance.Resolve<T>();
	}
}