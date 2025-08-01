using UnityEditor;
using UnityEngine;

namespace PEPEngineers.PEPErvice.Runtime
{
	public static class ServiceLocatorExtensions
	{
		public static void Destroy(this Object obj)
		{
			if (obj == null) return;

#if UNITY_EDITOR
			if (Application.isPlaying && !EditorApplication.isPaused)
				Object.Destroy(obj);
			else
				Object.DestroyImmediate(obj);
#else
            UnityEngine.Object.Destroy(obj);
#endif
		}

		public static bool Is<T>(this object item)
		{
#if UNITY_EDITOR
			if (ReferenceEquals(item, null))
				return false;

			return item switch
			{
				T => true,
				GameObject go => go.TryGetComponent<T>(out _),
				Component component => component.TryGetComponent<T>(out _),
				_ => false
			};
#else
			return item is T;
#endif
		}
	}
}