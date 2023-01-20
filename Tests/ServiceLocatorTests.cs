using System;
using System.Collections;
using GameAssets.Code.Services.Tests.Objects;
using NUnit.Framework;
using PEPErvice;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameAssets.Code.Services.Tests
{
	[RequiresPlayMode(false)]
	internal class ServiceLocatorTests
	{
		[SetUp]
		public void SetUp()
		{
			ServiceLocator.Instance.Unregister<ITestService>();
		}

		[TearDown]
		public void TearDown()
		{
			ServiceLocator.Instance.Unregister<ITestService>();
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
	}
}