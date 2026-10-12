using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Extensions;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PEPEngineers.PEPErvice.Runtime
{
	public sealed class ServiceCache : ILocator, IRegister, IDisposable
	{
		private readonly Dictionary<Type, IService> registeredServices = new();
		private readonly HashSet<Type> sceneOnlyTypes = new();
		private readonly Dictionary<Type, Func<IService>> serviceFactories = new();
		private bool disposed;

		public ServiceCache()
		{
			SceneManager.activeSceneChanged += OnSceneChanged;
		}

		public IReadOnlyCollection<object> Instances => registeredServices.Values;

		public TService Resolve<TService>() where TService : IService
		{
			if (disposed) throw new ObjectDisposedException(nameof(ServiceCache));
			var type = TypeFactory<TService>.Type;

			if (registeredServices.TryGetValue(type, out var service) && service.IsAlive())
				return (TService)service;
			registeredServices.Remove(type);

			if (serviceFactories.TryGetValue(type, out var factory))
			{
				service = factory();
				if (!service.IsAlive())
					throw new InvalidOperationException($"The factory for {type.FullName} returned a null, destroyed or disposed service.");
				var result = (TService)service;
				registeredServices[type] = service;
				return result;
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
			if (disposed) throw new ObjectDisposedException(nameof(ServiceCache));
			if (resolver == null) throw new ArgumentNullException(nameof(resolver));
			var type = TypeFactory<TService>.Type;
			serviceFactories[type] = resolver;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);
			else
				sceneOnlyTypes.Remove(type);
			RemoveRegisteredType(type);

			return this;
		}

		public void Unbind<TService>() where TService : IService
		{
			var type = TypeFactory<TService>.Type;
			serviceFactories.Remove(type);
			sceneOnlyTypes.Remove(type);
			RemoveRegisteredType(type);
		}

		public IRegister Register<TService>([NotNull] TService service, Lifetime lifetime = Lifetime.Singleton)
			where TService : IService
		{
			if (disposed) throw new ObjectDisposedException(nameof(ServiceCache));
			if (!service.IsAlive()) throw new ArgumentNullException(nameof(service));
			var type = TypeFactory<TService>.Type;
			serviceFactories.Remove(type);
			registeredServices[type] = service;

			if (lifetime == Lifetime.Scene)
				sceneOnlyTypes.Add(type);
			else
				sceneOnlyTypes.Remove(type);

			return this;
		}

		public void Unregister<TService>() where TService : IService
		{
			Unbind<TService>();
		}

		public void Dispose()
		{
			if (disposed) return;
			disposed = true;
			SceneManager.activeSceneChanged -= OnSceneChanged;
			foreach (var service in registeredServices.Values.Distinct()) DisposeService(service);
			registeredServices.Clear();
			serviceFactories.Clear();
			sceneOnlyTypes.Clear();
		}

		private void OnSceneChanged(Scene _, Scene __)
		{
			foreach (var sceneOnlyType in sceneOnlyTypes)
				RemoveRegisteredType(sceneOnlyType);
		}

		private void RemoveRegisteredType(in Type type)
		{
			if (registeredServices.Remove(type, out var service) == false) return;
			DisposeService(service);
		}

		private static void DisposeService(IService service)
		{
			if (!service.IsAlive()) return;
			try
			{
				service.Dispose();
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
