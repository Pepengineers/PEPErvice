using System;
using System.Collections.Generic;
using PEPErvice.Interfaces;
using UnityEngine.SceneManagement;

namespace PEPErvice.Implementations
{
	internal static class TypeFactory<T>
	{
		public static Type Type { get; } = typeof(T);
	}

	internal class CachedServiceLocator : IServiceLocator
	{
		private readonly Dictionary<Type, IService> registeredServices = new();
		private readonly HashSet<Type> sceneOnlyTypes = new();
		private readonly Dictionary<Type, Func<IService>> serviceFactories = new();

		public CachedServiceLocator()
		{
			SceneManager.sceneUnloaded += OnSceneUnloaded;
		}

		public IReadOnlyCollection<IService> Services => registeredServices.Values;

		public IServiceLocator Bind<TService>(Func<TService> resolver, Lifetime lifetime = Lifetime.Singleton)
			where TService : class, IService
		{
			if (resolver == null)
				throw new ArgumentNullException();

			var type = TypeFactory<TService>.Type;
			serviceFactories[type] = resolver;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public TService Resolve<TService>() where TService : class, IService
		{
			var type = TypeFactory<TService>.Type;

			if (registeredServices.TryGetValue(type, out var service))
				return service as TService;

			if (serviceFactories.TryGetValue(type, out var factory))
			{
				service = factory();
				registeredServices[type] = service;
				return service as TService;
			}

			return null;
		}

		public void Unbind<TService>() where TService : class, IService
		{
			var type = TypeFactory<TService>.Type;
			serviceFactories.Remove(type);
			RemoveRegisteredType(type);
		}

		public void Register<TService>(IService service, Lifetime lifetime = Lifetime.Singleton)
			where TService : class, IService
		{
			if (service == null)
				throw new ArgumentNullException();

			var type = TypeFactory<TService>.Type;
			registeredServices[type] = service;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);
		}

		public void Unregister<TService>() where TService : class, IService
		{
			var type = TypeFactory<TService>.Type;
			sceneOnlyTypes.Remove(type);
			RemoveRegisteredType(type);
		}

		private void RemoveRegisteredType(Type type)
		{
			if (registeredServices.Remove(type, out var service) == false) return;

			try
			{
				service.Dispose();
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogException(e);
			}
		}

		private void OnSceneUnloaded(Scene scene)
		{
			foreach (var sceneOnlyType in sceneOnlyTypes)
				RemoveRegisteredType(sceneOnlyType);
		}
	}
}