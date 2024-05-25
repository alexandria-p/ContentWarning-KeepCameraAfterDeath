using BepInEx;
using BepInEx.Logging;
using System.Reflection;
using MonoMod.RuntimeDetour.HookGen;
using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;
using KeepCameraAfterDeath.Patches;

namespace KeepCameraAfterDeath;

[ContentWarningPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION, false)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class KeepCameraAfterDeath : BaseUnityPlugin
{
    public static KeepCameraAfterDeath Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;

    public bool EnableRewardForCameraReturn { get; private set; }
    public int MetaCoinRewardForCameraReturn { get; private set; }
    public int CashRewardForCameraReturn { get; private set; }

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

    public void SetEnableRewardForCameraReturn(bool rewardEnabled)
    {
        EnableRewardForCameraReturn = rewardEnabled;
    }

    public void SetMetaCoinRewardForCameraReturn(int mcReward)
    {
        MetaCoinRewardForCameraReturn = mcReward;
    }

    public void SetCashRewardForCameraReturn(int cashReward)
    {
        CashRewardForCameraReturn = cashReward;
    }

    [SettingRegister("KeepCameraAfterDeath")]
    public class EnableRewardForCameraReturnSetting : BoolSetting, ICustomSetting
    {
        public override void ApplyValue()
        {
            KeepCameraAfterDeath.Logger.LogInfo($"MC Reward for camera return: {Value}");
            KeepCameraAfterDeath.Instance.SetEnableRewardForCameraReturn(Value);
        }

        public string GetDisplayName() => "Award MC as an incentive for bringing the camera back to the surface (overrides MC value below)";

        protected override bool GetDefaultValue() => true;
    }

    [SettingRegister("KeepCameraAfterDeath")]
    public class SetMetaCoinRewardForCameraReturnSetting : IntSetting, ICustomSetting
    {
        public override void ApplyValue()
        {
            KeepCameraAfterDeath.Logger.LogInfo($"Meta Coin (MC) Reward for camera return: {Value}");
            KeepCameraAfterDeath.Instance.SetMetaCoinRewardForCameraReturn(Value);
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
            KeepCameraAfterDeath.Logger.LogInfo($"Cash Reward for camera return: {Value}");
            KeepCameraAfterDeath.Instance.SetCashRewardForCameraReturn(Value);
        }

        public string GetDisplayName() => "Integer Feature";

        protected override int GetDefaultValue() => 0;

        override protected (int, int) GetMinMaxValue() => (0, 1000);
    }
}
