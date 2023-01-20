using System;
using System.Collections.Generic;

namespace PEPErvice.Interfaces
{
	public interface IServiceLocator
	{
		IReadOnlyCollection<IService> Services { get; }

		void Bind<TService>(Func<TService> resolver) where TService : class, IService;
		TService Resolve<TService>() where TService : class, IService;
		void Unbind<TService>() where TService : class, IService;
		void Register<TService>(IService service) where TService : class, IService;
		void Unregister<TService>() where TService : class, IService;
	}
}