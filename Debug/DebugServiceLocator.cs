using System.Text;

#if UNITY_EDITOR

using UnityEditor;
#endif

namespace PEPEngineers.PEPErvice.Debug
{
#if UNITY_EDITOR
	internal static class DebugServiceLocator
	{
		private static readonly StringBuilder Builder = new();

		[MenuItem("Debug/Tools/Services Dump")]
		public static void PrintDump()
		{
			Builder.Clear();
			Builder.AppendLine("Services DUMP");
			var services = ServiceLocator.Instance.Services;
			Builder.AppendLine($"Alive Services {services.Count}");
			foreach (var service in services) Builder.AppendLine($"\t {service.GetType().FullName}");
			UnityEngine.Debug.Log(Builder.ToString());
		}
	}
#endif
}