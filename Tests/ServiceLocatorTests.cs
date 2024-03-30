using System;
using System.Collections;
using DetectiveAsylum.Tests.Services.Objects;
using NUnit.Framework;
using PEPEngineers.PEPErvice;
using PEPEngineers.PEPErvice.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace DetectiveAsylum.Tests.Services
{
	[RequiresPlayMode(false)]
	internal class ServiceLocatorTests
	{
		private ServiceCache AllServices = new();
		
		[SetUp]
		public void SetUp()
		{
			AllServices.Unregister<ITestService>();
			AllServices.Unbind<ITestService>();
		}

		[TearDown]
		public void TearDown()
		{
			AllServices.Unregister<ITestService>();
			AllServices.Unbind<ITestService>();
		}

		[Test]
		public void RegisterService()
		{
			AllServices.Register<ITestService>(new TestService());
			var service = AllServices.Resolve<ITestService>();

			Assert.IsNotNull(service);
			Assert.IsInstanceOf<TestService>(service);
		}

		[Test]
		public void CheckNullService()
		{
			Assert.Throws<ArgumentNullException>(() => AllServices.Register<ITestService>(null));
		}

		[UnityTest]
		[RequiresPlayMode]
		public IEnumerator CheckGameService()
		{
			var gameService = new GameObject("Test").AddComponent<UnityTestSingleton>();
			AllServices.Register<ITestService>(gameService);

			yield return null;

			var service = AllServices.Resolve<ITestService>();
			Assert.IsNotNull(service);
			Assert.IsInstanceOf<UnityTestSingleton>(service);
			Assert.AreSame(service, gameService);
		}

		[Test]
		public void CheckDIBinding()
		{
			var di = AllServices;
			di.Bind<ITestService>(() => new TestService());

			var service = AllServices.Resolve<ITestService>();
			Assert.IsNotNull(service);
			Assert.IsInstanceOf<TestService>(service);
		}

		[UnityTest]
		[RequiresPlayMode]
		public IEnumerator CheckDIFactory()
		{
			var di = AllServices;
			di.Bind<ITestService>(() => new GameObject().AddComponent<UnityTestSingleton>());

			yield return null;

			var service = AllServices.Resolve<ITestService>();
			Assert.IsNotNull(service);
			Assert.IsInstanceOf<UnityTestSingleton>(service);
		}
	}
}