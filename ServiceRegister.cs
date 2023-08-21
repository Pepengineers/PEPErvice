using PEPEngineers.PEPErvice.Implementations;
using PEPEngineers.PEPErvice.Interfaces;

namespace PEPEngineers.PEPErvice
{
	public static class ServiceHub
	{
		private static readonly CachedInstanceHub Hub = new();
		public static IRegister Register => Hub;
		public static ILocator Locator => Hub;
	}

	public static class ServiceRegister
	{
		public static IRegister Register => ServiceHub.Register;
	}

	public static class ServiceLocator
	{
		public static ILocator Locator => ServiceHub.Locator;
	}
}