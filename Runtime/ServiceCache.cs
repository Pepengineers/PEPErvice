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
		private readonly Dictionary<Type, IService> registeredServices = new();
		private readonly HashSet<Type> sceneOnlyTypes = new();
		private readonly Dictionary<Type, Func<IService>> serviceFactories = new();

		public ServiceCache()
		{
			SceneManager.activeSceneChanged += OnSceneChanged;
		}

		public IReadOnlyCollection<object> Instances => registeredServices.Values;

		public TService Resolve<TService>() where TService : IService
		{
			var type = TypeFactory<TService>.Type;

			if (registeredServices.TryGetValue(type, out var service) && service != default)
				return (TService)service;

			if (serviceFactories.TryGetValue(type, out var factory))
			{
				service = factory();
				registeredServices[type] = service;
				return (TService)service;
			}

			return default;
		}

		public ILocator Resolve<TService>(out TService value) where TService : IService
		{
			value = Resolve<TService>();
			return this;
		}

		public IRegister Bind<TService>([NotNull] Func<IService> resolver, Lifetime lifetime = Lifetime.Singleton)
			where TService : IService
		{
			var type = TypeFactory<TService>.Type;
			serviceFactories[type] = resolver;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public void Unbind<TService>() where TService : IService
		{
			var type = TypeFactory<TService>.Type;
			serviceFactories.Remove(type);
			RemoveRegisteredType(type);
		}

		public IRegister Register<TService>([NotNull] TService service, Lifetime lifetime = Lifetime.Singleton)
			where TService : IService
		{
			var type = TypeFactory<TService>.Type;
			registeredServices[type] = service;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public void Unregister<TService>() where TService : IService
		{
			var type = TypeFactory<TService>.Type;
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