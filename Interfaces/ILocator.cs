using System.Collections.Generic;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface ILocator
	{
		TService Resolve<TService>() where TService : IService;
		ILocator Resolve<TService>(out TService value) where TService : IService;
	}
}