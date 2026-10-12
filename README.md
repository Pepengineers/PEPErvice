# PEPErvice

A subsystem locator for Unity. Unity 6000.6 is used for validation.

## Installation

Add the package through Unity Package Manager using its Git URL, or add this dependency to `Packages/manifest.json`:

```json
"com.pepengineers.pepervice": "https://github.com/Pepengineers/PEPErvice.git"
```

If your code uses assembly definitions, reference the package's `PEPEngineers.PEPErvice` assembly.

## Setup

Create a locator asset and add it to **Project Settings > Player > Preloaded Assets**. Keep each Unity object class in a matching `.cs` file.

```csharp
using PEPEngineers.PEPErvice.Runtime;
using UnityEngine;
using ISubsystem = PEPEngineers.PEPErvice.Interfaces.ISubsystem;

[CreateAssetMenu(menuName = "Example/Game")]
public sealed class Game : UnityLocator
{
    public new static Game Instance => (Game)UnityLocator.Instance;
}

public interface IGameSettings : ISubsystem { }

[CreateAssetMenu(menuName = "Example/Game Settings")]
public sealed class GameSettings : GameSubsystem<IGameSettings>, IGameSettings { }

public interface IAudio : ISubsystem { }

public sealed class AudioSubsystem : SceneSubsystem<IAudio>, IAudio { }
```

`GameSubsystem<T>` is a `ScriptableObject`; `SceneSubsystem<T>` is a `MonoBehaviour`. Add settings assets to the locator's **Game Subsystems** list and scene component prefabs to **Scene Subsystems**. The locator scans loaded assemblies for concrete subclasses of both kinds and binds missing types lazily. A discovered subsystem resolves to the same instance through its concrete type and its generic contract `T`.

```csharp
var settings = Game.Instance.GetSubsystem<IGameSettings>();
var audio = Game.Instance.GetSubsystem<IAudio>();
```

Plain C# subsystems implement `ISubsystem` and its `Dispose()` method. Bind them explicitly after locator initialization; the factory runs on the first lookup and its result is cached:

```csharp
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Interfaces;

public interface IClock : ISubsystem { }

public sealed class SystemClock : IClock
{
    public void Dispose() { }
}

public static class ClockSetup
{
    // Call after the locator is enabled.
    public static void Initialize()
    {
        Game.Instance.BindSubsystem<IClock>(() => new SystemClock(), Lifetime.Singleton);
        var clock = Game.Instance.GetSubsystem<IClock>();
    }
}
```

## Lifetime and ownership

- `Lifetime.Singleton` is the default. `Lifetime.Scene` clears the cached value when the active scene changes; factory bindings remain available for the next lookup. Scene components use their serialized **Lifetime** setting; singleton components persist across scene loads.
- `RegisterSubsystem<T>(instance)` replaces the registration without disposing the previous value. `BindSubsystem<T>(factory)` invalidates and disposes the previous cached value according to ownership. Registrations and replacements are per type key; changing one key does not rewrite other interface or concrete keys.
- `UnityLocator` destroys game assets and scene hierarchies it creates. Configured settings assets and existing scene components are borrowed and survive locator cleanup; configured prefab assets are preserved. Plain C# subsystems are disposed on cleanup. `GameSubsystem` and `SceneSubsystem` objects returned by custom factories remain the caller's responsibility.
- `SubsystemLocator` provides the same registry without Unity discovery or asset setup. Its `Clear()` and `Dispose()` dispose registered instances; `Dispose()` is idempotent.
- Do not combine a prefab containing subsystems A + B with a separately existing singleton A: rejecting the duplicate A disposes the whole prefab GameObject, including B.
