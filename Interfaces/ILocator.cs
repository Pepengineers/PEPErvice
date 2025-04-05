using System.Collections.Generic;
using PEPEngineers.PEPErvice.Runtime;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface ILocator
	{
		static ILocator Instance => ServiceLocator.Instance;
		TService Resolve<TService>() where TService : IService;
		ILocator Resolve<TService>(out TService value) where TService : IService;
	}
}