using System.Text;
using UnityEditor;

namespace PEPErvice.Debug
{
	internal static class DebugServiceLocator
	{
		private static readonly StringBuilder Builder = new StringBuilder();
		
		[MenuItem("Debug/Tools/Services Dump")]
		public static void PrintDump()
		{
			Builder.Clear();
			Builder.AppendLine("Services DUMP");
			var services = ServiceLocator.Instance.Services;
			Builder.AppendLine($"Alive Services {services.Count}");
			foreach (var service in services)
			{
				Builder.AppendLine($"\t {service.GetType().FullName}");
			}
			UnityEngine.Debug.Log(Builder.ToString());
		}
	}
}