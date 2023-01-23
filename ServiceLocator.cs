using PEPErvice.Implementations;
using PEPErvice.Interfaces;

namespace PEPErvice
{
	public static class ServiceLocator
	{
		public static IServiceLocator Instance { get; set; } = new CachedServiceLocator();
	}
}