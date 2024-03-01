#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PEPEngineers.PEPErvice.Editor
{
	internal static class DebugServiceLocator
	{
		private static readonly StringBuilder Builder = new();

		[MenuItem("Debug/Tools/Services Dump")]
		public static void PrintDump()
		{
			Builder.Clear();
			Builder.AppendLine("Services DUMP");
			var services = AllServices.Locator.Instances;
			Builder.AppendLine($"Alive Services {services.Count}");
			foreach (var service in services) Builder.AppendLine($"\t {service.GetType().FullName}");
			Debug.Log(Builder.ToString());
		}
	}
}
#endif