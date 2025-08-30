using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PEPEngineers.PEPErvice.Runtime
{
	public sealed class ServiceCache : ILocator, IRegister
	{
		private readonly Dictionary<Type, ISubsystem> registeredServices = new();
		private readonly HashSet<Type> sceneOnlyTypes = new();
		private readonly Dictionary<Type, Func<ISubsystem>> serviceFactories = new();

		public ServiceCache()
		{
			SceneManager.activeSceneChanged += OnSceneChanged;
		}

		public IReadOnlyCollection<object> Instances => registeredServices.Values;

		public TSubsystem Resolve<TSubsystem>() where TSubsystem : ISubsystem
		{
			var type = TypeFactory<TSubsystem>.Type;

			if (registeredServices.TryGetValue(type, out var service) && service != default)
				return (TSubsystem)service;

			if (serviceFactories.TryGetValue(type, out var factory))
			{
				service = factory();
				registeredServices[type] = service;
				return (TSubsystem)service;
			}

			return default;
		}

		public ILocator Resolve<TSubsystem>(out TSubsystem value) where TSubsystem : ISubsystem
		{
			value = Resolve<TSubsystem>();
			return this;
		}

		public IRegister Bind<TSubsystem>([NotNull] Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton)
			where TSubsystem : ISubsystem
		{
			var type = TypeFactory<TSubsystem>.Type;
			serviceFactories[type] = resolver;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public void Unbind<TSubsystem>() where TSubsystem : ISubsystem
		{
			var type = TypeFactory<TSubsystem>.Type;
			serviceFactories.Remove(type);
			RemoveRegisteredType(type);
		}

		public IRegister Register<TSubsystem>([NotNull] TSubsystem service, Lifetime lifetime = Lifetime.Singleton)
			where TSubsystem : ISubsystem
		{
			var type = TypeFactory<TSubsystem>.Type;
			registeredServices[type] = service;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public void Unregister<TSubsystem>() where TSubsystem : ISubsystem
		{
			var type = TypeFactory<TSubsystem>.Type;
			sceneOnlyTypes.Remove(type);
			RemoveRegisteredType(type);
		}

		private void OnSceneChanged(Scene _, Scene __)
		{
			foreach (var sceneOnlyType in sceneOnlyTypes)
				RemoveRegisteredType(sceneOnlyType);
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

		private static class TypeFactory<T>
		{
			public static Type Type { get; } = typeof(T);
		}
	}
}