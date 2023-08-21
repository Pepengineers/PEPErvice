using System;
using System.Collections.Generic;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine.SceneManagement;

namespace PEPEngineers.PEPErvice.Implementations
{
	internal static class TypeFactory<T>
	{
		public static Type Type { get; } = typeof(T);
	}

	internal class CachedInstanceHub : ILocator, IRegister
	{
		private readonly Dictionary<Type, object> registeredServices = new();
		private readonly HashSet<Type> sceneOnlyTypes = new();
		private readonly Dictionary<Type, Func<object>> serviceFactories = new();

		public CachedInstanceHub()
		{
			SceneManager.activeSceneChanged += OnSceneChanged;
		}

		public IReadOnlyCollection<object> Instances => registeredServices.Values;

		public TService Resolve<TService>() where TService : class
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

		public ILocator Resolve<TImpl>(out TImpl value) where TImpl : class
		{
			value = Resolve<TImpl>();
			return this;
		}

		public IRegister Bind<TService>(Func<TService> resolver, Lifetime lifetime = Lifetime.Singleton)
			where TService : class
		{
			if (resolver == null)
				throw new ArgumentNullException();

			var type = TypeFactory<TService>.Type;
			serviceFactories[type] = resolver;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public void Unbind<TService>() where TService : class
		{
			var type = TypeFactory<TService>.Type;
			serviceFactories.Remove(type);
			RemoveRegisteredType(type);
		}

		public IRegister Register<TService>(object service, Lifetime lifetime = Lifetime.Singleton)
			where TService : class
		{
			if (service == null)
				throw new ArgumentNullException();

			var type = TypeFactory<TService>.Type;
			registeredServices[type] = service;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);

			return this;
		}

		public void Unregister<TService>() where TService : class
		{
			var type = TypeFactory<TService>.Type;
			sceneOnlyTypes.Remove(type);
			RemoveRegisteredType(type);
		}

		private void OnSceneChanged(Scene _, Scene __)
		{
			foreach (var sceneOnlyType in sceneOnlyTypes)
				RemoveRegisteredType(sceneOnlyType);
			sceneOnlyTypes.Clear();
		}

		private void RemoveRegisteredType(Type type)
		{
			if (registeredServices.Remove(type, out var item))
				try
				{
					if (item is IService service)
						service.Dispose();
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogException(e);
				}
		}
	}
}