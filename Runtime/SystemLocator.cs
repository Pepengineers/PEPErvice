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
	public sealed class SystemLocator : ILocator, IRegister
	{
		private readonly Dictionary<Type, ISubsystem> registeredSystems = new();
		private readonly HashSet<Type> sceneOnlyTypes = new();
		private readonly Dictionary<Type, Func<ISubsystem>> systemFactories = new();

		public IReadOnlyDictionary<Type, ISubsystem> RegisteredSystems => registeredSystems;
		public IReadOnlyDictionary<Type, Func<ISubsystem>> SystemFactories => systemFactories;
		public IReadOnlyCollection<Type> SceneOnlyTypes => sceneOnlyTypes;

		public ISubsystem GetSystem(Type type)
		{
			Assert.IsNotNull(type);
			Assert.IsTrue(typeof(ISubsystem).IsAssignableFrom(type));

			if (registeredSystems.TryGetValue(type, out var service) && service != null)
				return service;

			if (systemFactories.TryGetValue(type, out var factory))
			{
				service = factory();
				registeredSystems[type] = service;
				return service;
			}

			return null;
		}

		public TSystem GetSystem<TSystem>() where TSystem : ISubsystem
		{
			var type = TypeCache<TSystem>.Value;
			var system = GetSystem(type);
			if (system != null)
				return (TSystem)system;
			return default;
		}

		public ILocator GetSystem<TSystem>(out TSystem value) where TSystem : ISubsystem
		{
			value = GetSystem<TSystem>();
			return this;
		}

		public IRegister BindSystem(Type type, Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton)
		{
			Assert.IsNotNull(type);
			Assert.IsNotNull(resolver);
			Assert.IsTrue(typeof(ISubsystem).IsAssignableFrom(type));

			sceneOnlyTypes.Remove(type);
			systemFactories[type] = resolver;
			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);
			return this;
		}

		public IRegister BindSystem<TSystem>(Func<ISubsystem> resolver, Lifetime lifetime) where TSystem : ISubsystem
		{
			Assert.IsNotNull(resolver);
			var type = TypeCache<TSystem>.Value;
			return BindSystem(type, resolver, lifetime);
		}

		public void UnbindSystem(Type type)
		{
			Assert.IsNotNull(type);
			Assert.IsTrue(typeof(ISubsystem).IsAssignableFrom(type));
			systemFactories.Remove(type);
			RemoveRegisteredType(type);
		}

		public void UnbindSystem<TSystem>() where TSystem : ISubsystem
		{
			var type = TypeCache<TSystem>.Value;
			UnbindSystem(type);
		}

		public IRegister RegisterSystem(Type type, ISubsystem subsystem, Lifetime lifetime = Lifetime.Singleton)
		{
			Assert.IsNotNull(type);
			Assert.IsNotNull(subsystem);
			Assert.IsTrue(typeof(ISubsystem).IsAssignableFrom(type));
			sceneOnlyTypes.Remove(type);
			registeredSystems[type] = subsystem;
			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public IRegister RegisterSystem<TSystem>([NotNull] TSystem service, Lifetime lifetime) where TSystem : ISubsystem
		{
			var type = TypeCache<TSystem>.Value;
			return RegisterSystem(type, service, lifetime);
		}

		public void UnregisterSystem<TSystem>() where TSystem : ISubsystem
		{
			var type = TypeCache<TSystem>.Value;
			sceneOnlyTypes.Remove(type);
			RemoveRegisteredType(type);
		}

		private void RemoveRegisteredType(in Type type)
		{
			if (!registeredSystems.Remove(type, out var service)) return;
			try
			{
				service?.Dispose();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		public void RemoveSceneSystems()
		{
			foreach (var sceneOnlyType in SceneOnlyTypes)
				RemoveRegisteredType(sceneOnlyType);
		}

		public void Clear()
		{
			registeredSystems.Clear();
			sceneOnlyTypes.Clear();
			systemFactories.Clear();
		}
	}
}