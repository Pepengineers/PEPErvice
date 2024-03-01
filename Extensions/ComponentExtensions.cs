using System;
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