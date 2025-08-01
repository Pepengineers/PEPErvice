using System.Collections.Generic;
using PEPEngineers.PEPErvice.Runtime;

namespace PEPEngineers.PEPErvice.Interfaces
{
	public interface ILocator
	{
		TService Get<TService>() where TService : IService;
		ILocator Get<TService>(out TService value) where TService : IService;
	}
}