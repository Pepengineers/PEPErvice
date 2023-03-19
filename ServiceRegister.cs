using PEPEngineers.PEPErvice.Implementations;
using PEPEngineers.PEPErvice.Interfaces;

namespace PEPEngineers.PEPErvice
{
	public static class ServiceRegister
	{
		private static readonly CachedInstanceDI Di = new CachedInstanceDI();
		public static ILocator Locator => Di;
		public static IRegister Register => Di;
	}
}