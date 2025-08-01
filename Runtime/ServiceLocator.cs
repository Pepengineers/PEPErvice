using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Extensions;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;
using UnityEngine.Assertions;

namespace PEPEngineers.PEPErvice.Runtime
{
	public sealed class ServiceLocator : ILocator, IRegister
	{
		private readonly Dictionary<Type, IService> registeredServices = new();
		private readonly HashSet<Type> sceneOnlyTypes = new();
		private readonly Dictionary<Type, Func<IService>> serviceFactories = new();

		public IReadOnlyDictionary<Type, IService> RegisteredServices => registeredServices;
		public IReadOnlyDictionary<Type, Func<IService>> ServiceFactories => serviceFactories;
		public IReadOnlyCollection<Type> SceneOnlyTypes => sceneOnlyTypes;

		public TService Get<TService>() where TService : IService
		{
			var type = TypeCache<TService>.Value;

			if (registeredServices.TryGetValue(type, out var service) && service != null)
				return (TService)service;

			if (serviceFactories.TryGetValue(type, out var factory))
			{
				service = factory();
				registeredServices[type] = service;
				return (TService)service;
			}

			return default;
		}

		public ILocator Get<TService>(out TService value) where TService : IService
		{
			value = Get<TService>();
			return this;
		}

		public IRegister Bind<TService>(Func<IService> resolver, Lifetime lifetime) where TService : IService
		{
			Assert.IsNotNull(resolver);
			var type = TypeCache<TService>.Value;
			serviceFactories[type] = resolver;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public void Unbind<TService>() where TService : IService
		{
			var type = TypeCache<TService>.Value;
			serviceFactories.Remove(type);
			RemoveRegisteredType(type);
		}

		public IRegister Register<TService>([NotNull] TService service, Lifetime lifetime) where TService : IService
		{
			var type = TypeCache<TService>.Value;
			registeredServices[type] = service;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public void Unregister<TService>() where TService : IService
		{
			var type = TypeCache<TService>.Value;
			sceneOnlyTypes.Remove(type);
			RemoveRegisteredType(type);
		}

		private void RemoveRegisteredType(in Type type)
		{
			if (registeredServices.Remove(type, out var service) == false) return;
			try
			{
				service?.Dispose();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		public void RemoveSceneServices()
		{
			foreach (var sceneOnlyType in SceneOnlyTypes)
				RemoveRegisteredType(sceneOnlyType);
		}

		public void Clear()
		{
			registeredServices.Clear();
			sceneOnlyTypes.Clear();
			serviceFactories.Clear();
		}
	}
}