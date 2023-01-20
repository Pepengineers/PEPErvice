using System;
using System.Collections.Generic;
using PEPErvice.Interfaces;

namespace PEPErvice.Implementations
{
	internal class CachedServiceLocator : IServiceLocator
	{
		private readonly Dictionary<Type, IService> registeredTypes = new();
		private readonly Dictionary<Type, Func<IService>> serviceFactories = new();
		
		public IReadOnlyCollection<IService> Services => registeredTypes.Values;
		
		public void Bind<TService>(Func<TService> resolver) where TService : class, IService
		{
			if (resolver == null)
				throw new ArgumentNullException();
			
			var type = typeof(TService);
			serviceFactories[type] = resolver;
		}

		public TService Resolve<TService>() where TService : class, IService
		{
			var type = typeof(TService);
			
			if (registeredTypes.TryGetValue(type, out var service)) 
				return service as TService;
			
			if (serviceFactories.TryGetValue(type, out var factory))
			{
				service = factory();
				registeredTypes[type] = service;
				return service as TService;
			}

			return null;
		}

		public void Unbind<TService>() where TService : class, IService
		{
			var type = typeof(TService);
			serviceFactories.Remove(type);
		}

		public void Register<TService>(IService service) where TService : class, IService
		{
			if (service == null)
				throw new ArgumentNullException();
			
			var type = typeof(TService);
			registeredTypes[type] = service;
		}

		public void Unregister<TService>() where TService : class, IService
		{
			var type = typeof(TService);
			registeredTypes.Remove(type);
		}
	}
}