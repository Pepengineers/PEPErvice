using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Extensions;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;
using UnityEngine.Assertions;
using ISubsystem = PEPEngineers.PEPErvice.Interfaces.ISubsystem;

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

		public TSystem GeTSystem<TSystem>() where TSystem : ISubsystem
		{
			var type = TypeCache<TSystem>.Value;

			if (registeredServices.TryGetValue(type, out var service) && service != null)
				return (TSystem)service;

			if (serviceFactories.TryGetValue(type, out var factory))
			{
				service = factory();
				registeredServices[type] = service;
				return (TSystem)service;
			}

			return default;
		}

		public ILocator GeTSystem<TSystem>(out TSystem value) where TSystem : ISubsystem
		{
			value = GeTSystem<TSystem>();
			return this;
		}

		public IRegister BindService<TSystem>(Func<ISubsystem> resolver, Lifetime lifetime) where TSystem : ISubsystem
		{
			Assert.IsNotNull(resolver);
			var type = TypeCache<TSystem>.Value;
			serviceFactories[type] = resolver;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public void UnbindService<TSystem>() where TSystem : ISubsystem
		{
			var type = TypeCache<TSystem>.Value;
			serviceFactories.Remove(type);
			RemoveRegisteredType(type);
		}

		public IRegister RegisterService<TSystem>([NotNull] TSystem service, Lifetime lifetime) where TSystem : ISubsystem
		{
			var type = TypeCache<TSystem>.Value;
			registeredServices[type] = service;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public void UnregisterService<TSystem>() where TSystem : ISubsystem
		{
			var type = TypeCache<TSystem>.Value;
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