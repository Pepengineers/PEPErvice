using System;
using System.Collections;
using DetectiveAsylum.Tests.Services.Objects;
using NUnit.Framework;
using PEPEngineers.PEPErvice;
using UnityEngine;
using UnityEngine.TestTools;

namespace DetectiveAsylum.Tests.Services
{
	[RequiresPlayMode(false)]
	internal class ServiceLocatorTests
	{
		[SetUp]
		public void SetUp()
		{
			AllServices.Register.Unregister<ITestService>();
			AllServices.Register.Unbind<ITestService>();
		}

		[TearDown]
		public void TearDown()
		{
			AllServices.Register.Unregister<ITestService>();
			AllServices.Register.Unbind<ITestService>();
		}

		[Test]
		public void RegisterService()
		{
			AllServices.Register.Register<ITestService>(new TestService());
			var service = AllServices.Locator.Resolve<ITestService>();

			Assert.IsNotNull(service);
			Assert.IsInstanceOf<TestService>(service);
		}

		[Test]
		public void CheckNullService()
		{
			Assert.Throws<ArgumentNullException>(() => AllServices.Register.Register<ITestService>(null));
		}

		[UnityTest]
		[RequiresPlayMode]
		public IEnumerator CheckGameService()
		{
			var gameService = new GameObject("Test").AddComponent<UnityTestSingleton>();
			AllServices.Register.Register<ITestService>(gameService);

			yield return null;

			var service = AllServices.Locator.Resolve<ITestService>();
			Assert.IsNotNull(service);
			Assert.IsInstanceOf<UnityTestSingleton>(service);
			Assert.AreSame(service, gameService);
		}

		[Test]
		public void CheckDIBinding()
		{
			var di = AllServices.Register;
			di.Bind<ITestService>(() => new TestService());

			var service = AllServices.Locator.Resolve<ITestService>();
			Assert.IsNotNull(service);
			Assert.IsInstanceOf<TestService>(service);
		}

		[UnityTest]
		[RequiresPlayMode]
		public IEnumerator CheckDIFactory()
		{
			var di = AllServices.Register;
			di.Bind<ITestService>(() => new GameObject().AddComponent<UnityTestSingleton>());

			yield return null;

			var service = AllServices.Locator.Resolve<ITestService>();
			Assert.IsNotNull(service);
			Assert.IsInstanceOf<UnityTestSingleton>(service);
		}
	}
}