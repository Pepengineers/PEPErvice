using System.Collections.Generic;
using System.Linq;
using PEPEngineers.PEPErvice.Extensions;
using PEPEngineers.PEPErvice.Interfaces;
using PEPEngineers.PEPErvice.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace PEPEngineers.PEPErvice
{
	[CreateAssetMenu(menuName = @"Game/" + nameof(Bootstrapper), fileName = nameof(Bootstrapper), order = 0)]
	internal sealed class Bootstrapper : ScriptableObject
	{
		private static Bootstrapper instance;

#if ODIN_INSPECTOR
		[Title("Static Services", titleAlignment: TitleAlignments.Centered, bold: true)]
		[HideLabel]
		[PropertySpace(50, 25)]
		[LabelText(" ")]
		[AssetsOnly]
		[Searchable]
#endif
		[SerializeField]
		private List<InstanceService> staticServices = new();

#if ODIN_INSPECTOR
		[Title("Dynamic Services", titleAlignment: TitleAlignments.Centered, bold: true)]
		[HideLabel]
		[PropertySpace(25, 50)]
		[LabelText(" ")]
		[AssetsOnly]
		[Searchable]
#endif
		[SerializeField]
		private List<RuntimeService> runtimeServices = new();

		private void Awake()
		{
			instance = this;
			Construct();
		}

		private void OnEnable()
		{
			instance = this;
			Construct();
		}

		private void OnValidate()
		{
			foreach (var scriptableService in staticServices)
			{
				Assert.IsNotNull(scriptableService);
				Assert.IsTrue(scriptableService.Is<IService>());
			}

			foreach (var runtimeService in runtimeServices)
			{
				Assert.IsNotNull(runtimeService);
				Assert.IsTrue(runtimeService.Is<IService>());
			}

			Construct();
		}

		private static bool TryFindInstance(out Bootstrapper value)
		{
			value = FindAnyObjectByType<Bootstrapper>();
			return value != null;
		}

		private void Construct()
		{
			RegisterStaticServices(AllServices.Register);
			RegisterRuntimeServices(AllServices.Register);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void AfterSceneLoaded()
		{
			if (instance || TryFindInstance(out instance))
			{
				var services = FindObjectsByType<RuntimeService>(FindObjectsSortMode.None);
				foreach (var runtimeService in instance.runtimeServices)
					if (runtimeService.SpawnOnLoad)
					{
						var exist = false;
						foreach (var existService in services)
						{
							if (existService.GetType() != runtimeService.GetType()) continue;
							exist = true;
							break;
						}

						if (exist == false)
							CreateService(runtimeService.gameObject);
					}
			}
		}

		private static IService CreateService(GameObject prefab)
		{
			var service = Instantiate(prefab);
			service.name = $"<{prefab.name}>";
			return service.GetComponent<IService>();
		}

		private void RegisterRuntimeServices(IRegister register)
		{
			foreach (var spawned in runtimeServices)
			{
				var services = spawned.GetComponents<RuntimeService>();
				foreach (var service in services) service.Register(register, () => CreateService(spawned.gameObject));
			}
		}

		private void RegisterStaticServices(IRegister register)
		{
			foreach (var staticService in staticServices)
				staticService.Register(register);
		}
	}
}