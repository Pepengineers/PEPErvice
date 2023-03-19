using System.Collections;
using NUnit.Framework;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PEPEngineers.PEPErvice.Tests
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
			di = ServiceRegister.Register;
			locator = ServiceRegister.Locator;
		}

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
			di.Register<IService>(service);
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
			di.Register<IService>(service);
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