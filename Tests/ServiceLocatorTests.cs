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
			ServiceLocator.Instance.Unregister<ITestService>();
			ServiceLocator.Instance.Unbind<ITestService>();
		}

		[TearDown]
		public void TearDown()
		{
			ServiceLocator.Instance.Unregister<ITestService>();
			ServiceLocator.Instance.Unbind<ITestService>();
		}

		[Test]
		public void RegisterService()
		{
			ServiceLocator.Instance.Register<ITestService>(new TestService());
			var service = ServiceLocator.Instance.Resolve<ITestService>();

			Assert.IsNotNull(service);
			Assert.IsInstanceOf<TestService>(service);
		}

		[Test]
		public void CheckNullService()
		{
			Assert.Throws<ArgumentNullException>(() => ServiceLocator.Instance.Register<ITestService>(null));
		}

		[UnityTest]
		[RequiresPlayMode]
		public IEnumerator CheckGameService()
		{
			var gameService = new GameObject("Test").AddComponent<UnityTestService>();
			ServiceLocator.Instance.Register<ITestService>(gameService);

			yield return null;

			var service = ServiceLocator.Instance.Resolve<ITestService>();
			Assert.IsNotNull(service);
			Assert.IsInstanceOf<UnityTestService>(service);
			Assert.AreSame(service, gameService);
		}

		[Test]
		public void CheckDIBinding()
		{
			var di = ServiceLocator.Instance;
			di.Bind<ITestService>(() => new TestService());

			var service = di.Resolve<ITestService>();
			Assert.IsNotNull(service);
			Assert.IsInstanceOf<TestService>(service);
		}

		[UnityTest]
		[RequiresPlayMode]
		public IEnumerator CheckDIFactory()
		{
			var di = ServiceLocator.Instance;
			di.Bind<ITestService>(() => new GameObject().AddComponent<UnityTestService>());

			yield return null;

			var service = di.Resolve<ITestService>();
			Assert.IsNotNull(service);
			Assert.IsInstanceOf<UnityTestService>(service);
		}
	}
}