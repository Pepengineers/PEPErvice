using PEPEngineers.PEPErvice.Implementations;
using PEPEngineers.PEPErvice.Interfaces;

namespace PEPEngineers.PEPErvice
{
	public static class ServiceLocator
	{
		public static IServiceLocator Instance { get; set; } = new CachedServiceLocator();
	}
}