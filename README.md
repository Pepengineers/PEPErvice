# PEPErvice
Service Locator Implementation for unity

This package implements the Servic Locator pattern.
This will reduce the code coherence between the code modules.

## System Requirements
Unity **2019.4** or later versions. Don't forget to include the PEPools namespace and add assemly defenition reference. 

## Installation
You can also install via git url by adding this entry in your **manifest.json**
```
"com.pepervice": "https://github.com/Pepengineers/PEPErvice",
```

# Overview

ServiceLocator only allows you to add classes that inherit from the IService interface and implement Dispose method.

# GameService
GameService implement the basic functionality for a game service (i.e. a service that exists directly in the game world). 
Also it implements the Singleton pattern and logs information about itself
```csharp
  internal sealed class UnityAudioService : GameService<UnityAudioService>, IAudioService
	{
		
	}
```

# Bootstrapper example
```csharp

	interface IAudioService : IService
	{
		
	}

	internal sealed class WiseAudioService : IAudioService
	{
		public void Dispose()
		{
		}
	}

	internal sealed class FModAudioService : IAudioService
	{
		public void Dispose()
		{
		}
	}

	internal sealed class UnityAudioService : GameService<UnityAudioService>, IAudioService
	{
		
	}

	internal static class Bootstrapper
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		internal static void Construct()
		{
			var locator = ServiceLocator.Instance;

#if USE_WISE
			locator.Bind<IAudioService>(() => new WiseAudioService());
#elif USE_FMOD
			locator.Bind<IAudioService>(() => new FModAudioService());
#else
			locator.Bind<IAudioService>(() => new GameObject().AddComponent<UnityAudioService>());
#endif

		}
	}
```
