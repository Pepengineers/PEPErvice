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
		private readonly Dictionary<Type, ISubsystem> registeredServices = new();
		private readonly HashSet<Type> sceneOnlyTypes = new();
		private readonly Dictionary<Type, Func<ISubsystem>> serviceFactories = new();

		public IReadOnlyDictionary<Type, ISubsystem> RegisteredServices => registeredServices;
		public IReadOnlyDictionary<Type, Func<ISubsystem>> ServiceFactories => serviceFactories;
		public IReadOnlyCollection<Type> SceneOnlyTypes => sceneOnlyTypes;

		public TService GetService<TService>() where TService : ISubsystem
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

		public ILocator GetService<TService>(out TService value) where TService : ISubsystem
		{
			value = GetService<TService>();
			return this;
		}

		public IRegister BindService<TService>(Func<ISubsystem> resolver, Lifetime lifetime) where TService : ISubsystem
		{
			Assert.IsNotNull(resolver);
			var type = TypeCache<TService>.Value;
			serviceFactories[type] = resolver;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public void UnbindService<TService>() where TService : ISubsystem
		{
			var type = TypeCache<TService>.Value;
			serviceFactories.Remove(type);
			RemoveRegisteredType(type);
		}

		public IRegister RegisterService<TService>([NotNull] TService service, Lifetime lifetime) where TService : ISubsystem
		{
			var type = TypeCache<TService>.Value;
			registeredServices[type] = service;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public void UnregisterService<TService>() where TService : ISubsystem
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