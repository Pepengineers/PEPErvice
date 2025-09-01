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
	public sealed class SubsystemLocator : ILocator, IRegister
	{
		private readonly Dictionary<Type, ISubsystem> registeredSubsystems = new();
		private readonly HashSet<Type> sceneOnlyTypes = new();
		private readonly Dictionary<Type, Func<ISubsystem>> subsystemFactories = new();

		public IReadOnlyDictionary<Type, ISubsystem> RegisteredSubsystems => registeredSubsystems;
		public IReadOnlyDictionary<Type, Func<ISubsystem>> SubsystemFactories => subsystemFactories;
		public IReadOnlyCollection<Type> SceneOnlyTypes => sceneOnlyTypes;

		public ISubsystem GetSubsystem(Type type)
		{
			Assert.IsNotNull(type);
			Assert.IsTrue(typeof(ISubsystem).IsAssignableFrom(type));

			if (registeredSubsystems.TryGetValue(type, out var service) && service != null)
				return service;

			if (subsystemFactories.TryGetValue(type, out var factory))
			{
				service = factory();
				registeredSubsystems[type] = service;
				return service;
			}

			return null;
		}

		public T GetSubsystem<T>() where T : ISubsystem
		{
			var type = TypeCache<T>.Value;
			var system = GetSubsystem(type);
			if (system != null)
				return (T)system;
			return default;
		}

		public ILocator GetSubsystem<T>(out T value) where T : ISubsystem
		{
			value = GetSubsystem<T>();
			return this;
		}

		public IRegister BindSubsystem(Type type, Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton)
		{
			Assert.IsNotNull(type);
			Assert.IsNotNull(resolver);
			Assert.IsTrue(typeof(ISubsystem).IsAssignableFrom(type));

			sceneOnlyTypes.Remove(type);
			subsystemFactories[type] = resolver;
			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);
			return this;
		}

		public IRegister BindSubsystem<T>(Func<ISubsystem> resolver, Lifetime lifetime) where T : ISubsystem
		{
			Assert.IsNotNull(resolver);
			var type = TypeCache<T>.Value;
			return BindSubsystem(type, resolver, lifetime);
		}

		public void UnbindSubsystem(Type type)
		{
			Assert.IsNotNull(type);
			Assert.IsTrue(typeof(ISubsystem).IsAssignableFrom(type));
			subsystemFactories.Remove(type);
			RemoveRegisteredType(type);
		}

		public void UnbindSubsystem<T>() where T : ISubsystem
		{
			var type = TypeCache<T>.Value;
			UnbindSubsystem(type);
		}

		public IRegister RegisterSubsystem(Type type, ISubsystem subsystem, Lifetime lifetime = Lifetime.Singleton)
		{
			Assert.IsNotNull(type);
			Assert.IsNotNull(subsystem);
			Assert.IsTrue(typeof(ISubsystem).IsAssignableFrom(type));
			sceneOnlyTypes.Remove(type);
			registeredSubsystems[type] = subsystem;
			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public IRegister RegisterSubsystem<T>([NotNull] T service, Lifetime lifetime) where T : ISubsystem
		{
			var type = TypeCache<T>.Value;
			return RegisterSubsystem(type, service, lifetime);
		}

		public void UnregisterSystem<T>() where T : ISubsystem
		{
			var type = TypeCache<T>.Value;
			sceneOnlyTypes.Remove(type);
			RemoveRegisteredType(type);
		}

		private void RemoveRegisteredType(in Type type)
		{
			if (!registeredSubsystems.Remove(type, out var service)) return;
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
			registeredSubsystems.Clear();
			sceneOnlyTypes.Clear();
			subsystemFactories.Clear();
		}
	}
}