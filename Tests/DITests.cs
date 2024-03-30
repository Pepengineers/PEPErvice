using System.Collections;
using NUnit.Framework;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Interfaces;
using PEPEngineers.PEPErvice.Runtime;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DetectiveAsylum.Tests.Services
{
	public class Service : IService
	{
		public void Dispose()
		{
		}

		public void DoSomething()
		{
			// do something
		}
	}

	[TestFixture]
	public class DITests
	{
		[SetUp]
		public void Setup()
		{
			di = AllServices;
			locator = AllServices;
		}

		private readonly ServiceCache AllServices = new();

		private IRegister di;
		private ILocator locator;

		[Test]
		[RequiresPlayMode(false)]
		public void BindTest()
		{
			di.Bind<IService>(() => new Service());
			var service = locator.Resolve<IService>();
			Assert.IsNotNull(service);
		}

		[Test]
		[RequiresPlayMode(false)]
		public void BindSceneLifetimeTest()
		{
			di.Bind<IService>(() => new Service(), Lifetime.Scene);
			var service = locator.Resolve<IService>();
			Assert.IsNotNull(service);
		}

		[Test]
		[RequiresPlayMode(false)]
		public void RegisterTest()
		{
			IService service = new Service();
			di.Register(service);
			var registeredService = locator.Resolve<IService>();
			Assert.AreEqual(service, registeredService);
		}

		[Test]
		[RequiresPlayMode(false)]
		public void UnbindTest()
		{
			di.Bind<IService>(() => new Service());
			di.Unbind<IService>();
			var service = locator.Resolve<IService>();
			Assert.IsNull(service);
		}

		[Test]
		[RequiresPlayMode(false)]
		public void UnregisterTest()
		{
			IService service = new Service();
			di.Register(service);
			di.Unregister<IService>();
			var registeredService = locator.Resolve<IService>();
			Assert.IsNull(registeredService);
		}

		[UnityTest]
		[RequiresPlayMode]
		public IEnumerator SceneUnloadTest()
		{
			di.Bind<IService>(() => new Service(), Lifetime.Scene);
			var service = locator.Resolve<IService>();
			yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
			var newSceneScervice = locator.Resolve<IService>();
			Assert.IsNotNull(service);
			Assert.IsNotNull(newSceneScervice);
			Assert.AreNotEqual(newSceneScervice, service);
		}
	}
}