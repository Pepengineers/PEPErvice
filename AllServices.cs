using PEPEngineers.PEPErvice.Interfaces;
using PEPEngineers.PEPErvice.Runtime;

namespace PEPEngineers.PEPErvice
{
	public static class AllServices
	{
		private static readonly ServiceCache Hub = new();
		public static IRegister Register => Hub;
		public static ILocator Locator => Hub;

		public static TService Resolve<TService>() where TService : IService
		{
			return Locator.Resolve<TService>();
		}
	}
}