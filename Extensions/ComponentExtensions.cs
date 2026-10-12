using System;
using PEPEngineers.PEPErvice.Interfaces;
using PEPEngineers.PEPErvice.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PEPEngineers.PEPErvice.Extensions
{
	internal static class TypeCache<T>
	{
		public static Type Value { get; } = typeof(T);
	}

	internal static class ComponentExtensions
	{
		internal static bool IsAlive(this IService service)
		{
			return service != null && (service is not Object obj || obj != null)
				&& (service is not SceneService sceneService || !sceneService.IsDisposed);
		}

		public static bool Is<T>(this Object item)
		{
			if (ReferenceEquals(item, null))
				return false;

			return item switch
			{
				T => true,
				GameObject go => go.TryGetComponent<T>(out _),
				Component component => component.TryGetComponent<T>(out _),
				_ => false
			};
		}
	}
}
