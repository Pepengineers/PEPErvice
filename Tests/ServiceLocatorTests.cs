using System;
using System.Collections;
using NUnit.Framework;
using PEPEngineers.PEPErvice.Tests.Objects;
using UnityEngine;
using UnityEngine.TestTools;

namespace PEPEngineers.PEPErvice.Tests
{
	[RequiresPlayMode(false)]
	internal class ServiceLocatorTests
	{
		[SetUp]
		public void SetUp()
		{
			ServiceRegister.Register.Unregister<ITestService>();
			ServiceRegister.Register.Unbind<ITestService>();
		}

		[TearDown]
		public void TearDown()
		{
			ServiceRegister.Register.Unregister<ITestService>();
			ServiceRegister.Register.Unbind<ITestService>();
		}

		[Test]
		public void RegisterService()
		{
			ServiceRegister.Register.Register<ITestService>(new TestService());
			var service = ServiceRegister.Locator.Resolve<ITestService>();

			Assert.IsNotNull(service);
			Assert.IsInstanceOf<TestService>(service);
		}

		[Test]
		public void CheckNullService()
		{
			Assert.Throws<ArgumentNullException>(() => ServiceRegister.Register.Register<ITestService>(null));
		}

		[UnityTest]
		[RequiresPlayMode]
		public IEnumerator CheckGameService()
		{
			var gameService = new GameObject("Test").AddComponent<UnityTestService>();
			ServiceRegister.Register.Register<ITestService>(gameService);

			yield return null;

			var service = ServiceRegister.Locator.Resolve<ITestService>();
			Assert.IsNotNull(service);
			Assert.IsInstanceOf<UnityTestService>(service);
			Assert.AreSame(service, gameService);
		}

		[Test]
		public void CheckDIBinding()
		{
			var di = ServiceRegister.Register;
			di.Bind<ITestService>(() => new TestService());

			var service = ServiceRegister.Locator.Resolve<ITestService>();
			Assert.IsNotNull(service);
			Assert.IsInstanceOf<TestService>(service);
		}

		[UnityTest]
		[RequiresPlayMode]
		public IEnumerator CheckDIFactory()
		{
			var di = ServiceRegister.Register;
			di.Bind<ITestService>(() => new GameObject().AddComponent<UnityTestService>());

			yield return null;

			var service = ServiceRegister.Locator.Resolve<ITestService>();
			Assert.IsNotNull(service);
			Assert.IsInstanceOf<UnityTestService>(service);
		}
	}
}