# Content Warning MonoMod Template

Thank you for using the mod template! Here are a few tips to help you on your journey:

## Versioning

BepInEx uses [semantic versioning, or semver](https://semver.org/), for the mod's version info.
To increment it, you can either modify the version tag in the `.csproj` file directly, or use your IDE's UX to increment the version. Below is an example of modifying the `.csproj` file directly:

```xml
<!-- BepInEx Properties -->
<PropertyGroup>
    <AssemblyName>alexandria-p.KeepCameraAfterDeath</AssemblyName>
    <Product>KeepCameraAfterDeath</Product>
    <!-- Change to whatever version you're currently on. -->
    <Version>1.0.0</Version>
</PropertyGroup>
```

Your IDE will have the setting in `Package` or `NuGet` under `General` or `Metadata`, respectively.

## Logging

A logger is provided to help with logging to the console. You can access it by doing `Plugin.Logger` in any class outside the `Plugin` class.

***Please use*** `LogDebug()` ***whenever possible, as any other log method will be displayed to the console and potentially cause performance issues for users.***

If you chose to do so, make sure you change the following line in the `BepInEx.cfg` file to see the Debug messages:

```toml
[Logging.Console]

# ... #

## Which log levels to show in the console output.
# Setting type: LogLevel
# Default value: Fatal, Error, Warning, Message, Info
# Acceptable values: None, Fatal, Error, Warning, Message, Info, Debug, All
# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)
LogLevels = All
```

## MonoMod

This template uses MonoMod. For more specifics on how to use it, look at
[the MonoMod Examples on lethal.wiki](https://lethal.wiki/dev/fundamentals/patching-code/monomod-examples) and
[the unofficial MonoMod Documentation on lethal.wiki](https://lethal.wiki/dev/fundamentals/patching-code/monomod-documentation). Even though these resources are made for the Lethal Company Modding Wiki, they do apply for Content Warning modding.
Only things to note are that the CW modding community uses [AutoHookGenPatcher](https://thunderstore.io/c/content-warning/p/Hamunii/AutoHookGenPatcher/) instead of the older [HookGenPatcher](https://github.com/harbingerofme/Bepinex.Monomod.HookGenPatcher), and also that AutoHookGenPatcher already depends on [DetourContext.Dispose Fix](https://thunderstore.io/c/content-warning/p/Hamunii/DetourContext_Dispose_Fix/) on Thunderstore.

See [AutoHookGenPatcher - Usage For Developers](https://github.com/Hamunii/BepInEx.MonoMod.AutoHookGenPatcher?tab=readme-ov-file#usage-for-developers) for information on how to generate MMHOOK files for assemblies other than `Assembly-CSharp.dll` or already referenced `MMHOOK` assemblies.

### Notice
[AutoHookGenPatcher](https://thunderstore.io/c/content-warning/p/Hamunii/AutoHookGenPatcher/) must be installed and set as a dependency on Thunderstore as it will generate the MMHOOK assemblies your plugin depends on.

This can be done by adding the following dependency string in the dependencies of your manifest file when you upload your plugin to Thunderstore:
`"Hamunii-AutoHookGenPatcher-1.0.3"`. Dependency strings for mods can be found on their Thunderstore pages. 
