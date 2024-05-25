using BepInEx;
using BepInEx.Logging;
using System.Reflection;
using MonoMod.RuntimeDetour.HookGen;
using KeepCameraAfterDeath.Patches;

namespace KeepCameraAfterDeath;

/*
[ContentWarningPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION, false)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
*/

//[ContentWarningPlugin("alexandria-p.KeepCameraAfterDeath", "1.0.0", false)]
//[BepInPlugin("alexandria-p.KeepCameraAfterDeath", "KeepCameraAfterDeath", "1.0.0")]

[ContentWarningPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION, false)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class KeepCameraAfterDeath : BaseUnityPlugin
{
    public static KeepCameraAfterDeath Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        //SettingsLoader.RegisterSetting("KeepCameraAfterDeath Mod Settings", new EnableRewardForCameraReturnSetting());


        HookAll();

        Logger.LogInfo($"{"alexandria-p.KeepCameraAfterDeath"} v{"1.0.0"} has loaded!");
    }

    internal static void HookAll()
    {
        Logger.LogDebug("Hooking...");

        SurfaceNetworkHandlerPatch.Init();

        Logger.LogDebug("Finished Hooking!");
    }

    internal static void UnhookAll()
    {
        Logger.LogDebug("Unhooking...");

        /*
         *  HookEndpointManager is from MonoMod.RuntimeDetour.HookGen, and is used by the MMHOOK assemblies.
         *  We can unhook all methods hooked with HookGen using this.
         *  Or we can unsubscribe specific patch methods with 'On.Namespace.Type.Method -= CustomMethod;'
         */
        HookEndpointManager.RemoveAllOwnedBy(Assembly.GetExecutingAssembly());

        Logger.LogDebug("Finished Unhooking!");
    }

    [SettingRegister("KeepCameraAfterDeath")]
    public class EnableRewardForCameraReturnSetting : BoolSetting, ICustomSetting
    {
        public override void ApplyValue()
        {
            Main.Logger.LogInfo($"MC Reward for camera return: {Value}");
            SurfaceNetworkHandlerPatch/*.Instance*/.SetEnableRewardForCameraReturn(Value);
        }

        public string GetDisplayName() => "Award MC as an incentive for bringing the camera back to the surface (overrides MC value below)";

        protected override bool GetDefaultValue() => true;
    }

    [SettingRegister("KeepCameraAfterDeath")]
    public class SetMetaCoinRewardForCameraReturnSetting : IntSetting, ICustomSetting
    {
        public override void ApplyValue()
        {
            Main.Logger.LogInfo($"Meta Coin (MC) Reward for camera return: {Value}");
            SurfaceNetworkHandlerPatch/*.Instance*/.SetMetaCoinRewardForCameraReturn(Value);
        }

        public string GetDisplayName() => "Integer Feature";

        protected override int GetDefaultValue() => 25;

        override protected (int, int) GetMinMaxValue() => (0, 100);
    }

    [SettingRegister("KeepCameraAfterDeath")]
    public class SetCashRewardForCameraReturnSetting : IntSetting, ICustomSetting
    {
        public override void ApplyValue()
        {
            Main.Logger.LogInfo($"Cash Reward for camera return: {Value}");
            SurfaceNetworkHandlerPatch/*.Instance*/.SetCashRewardForCameraReturn(Value);
        }

        public string GetDisplayName() => "Integer Feature";

        protected override int GetDefaultValue() => 0;

        override protected (int, int) GetMinMaxValue() => (0, 1000);
    }
}
