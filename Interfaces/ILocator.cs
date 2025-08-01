using System.Collections.Generic;
using PEPEngineers.PEPErvice.Runtime;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface ILocator
	{
		TService GetService<TService>() where TService : IService;
		ILocator GetService<TService>(out TService value) where TService : IService;
	}
}