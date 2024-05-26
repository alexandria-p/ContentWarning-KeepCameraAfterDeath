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

    public bool IsRewardForCameraReturnEnabled { get; private set; }
    public int MetaCoinRewardForCameraReturn { get; private set; }
    public int CashRewardForCameraReturn { get; private set; }

    public ItemInstanceData? PreservedCameraInstanceData { get; private set; } = null;
    public bool PendingRewardForCameraReturn { get; private set; } = false;

    public bool SearchingForUndergroundPersistentObjects = false;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        HookAll();

        Logger.LogInfo($"{"alexandria-p.KeepCameraAfterDeath"} v{"1.0.0"} has loaded!");
    }

    internal static void HookAll()
    {
        SurfaceNetworkHandlerPatch.Init();
        VideoCameraPatch.Init();
        PersistentObjectsHolderPatch.Init();
        PlayerPatch.Init();
    }

    internal static void UnhookAll()
    {
        HookEndpointManager.RemoveAllOwnedBy(Assembly.GetExecutingAssembly());
    }

    public void SetEnableRewardForCameraReturn(bool rewardEnabled)
    {
        IsRewardForCameraReturnEnabled = rewardEnabled;
    }

    public void SetMetaCoinRewardForCameraReturn(int mcReward)
    {
        MetaCoinRewardForCameraReturn = mcReward;
    }

    public void SetCashRewardForCameraReturn(int cashReward)
    {
        CashRewardForCameraReturn = cashReward;
    }

    public void SetPreservedCameraInstanceData(ItemInstanceData data)
    {
        //data.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry t);
        //KeepCameraAfterDeath.Logger.LogInfo("ALEX: SET PRESERVED CAMERA DATA video ID: " + t != null ? t.videoID.id : "NONE");
        PreservedCameraInstanceData = data;
    }

    public void ClearPreservedCameraInstanceData()
    {
        PreservedCameraInstanceData = null;
    }

    public void SetPendingRewardForCameraReturn(bool success)
    {
        PendingRewardForCameraReturn = success;
    }

    public void ClearPendingRewardForCameraReturn()
    {
        PendingRewardForCameraReturn = false;
    }

    [SettingRegister("KeepCameraAfterDeath Mod Settings")]
    public class EnableRewardForCameraReturnSetting : BoolSetting, ICustomSetting
    {
        public override void ApplyValue()
        {
            //KeepCameraAfterDeath.Logger.LogInfo($"MC Reward for camera return: {Value}");
            KeepCameraAfterDeath.Instance.SetEnableRewardForCameraReturn(Value);
        }

        // TODO - just use the hosts' values for everyone.

        public string GetDisplayName() => "Award incentives for bringing the camera back to the surface (OVERRIDES any values below)";

        protected override bool GetDefaultValue() => true;
    }

    [SettingRegister("KeepCameraAfterDeath Mod Settings")]
    public class SetMetaCoinRewardForCameraReturnSetting : IntSetting, ICustomSetting
    {
        public override void ApplyValue()
        {
            //KeepCameraAfterDeath.Logger.LogInfo($"Meta Coin (MC) reward for camera return: {Value}");
            KeepCameraAfterDeath.Instance.SetMetaCoinRewardForCameraReturn(Value);
        }

        public string GetDisplayName() => "Meta Coin (MC) reward for camera return (uses the client's game settings)";

        protected override int GetDefaultValue() => 25;

        override protected (int, int) GetMinMaxValue() => (0, 100);
    }

    [SettingRegister("KeepCameraAfterDeath Mod Settings")]
    public class SetCashRewardForCameraReturnSetting : IntSetting, ICustomSetting
    {
        public override void ApplyValue()
        {
            //KeepCameraAfterDeath.Logger.LogInfo($"Cash reward for camera return: {Value}");
            KeepCameraAfterDeath.Instance.SetCashRewardForCameraReturn(Value);
        }

        public string GetDisplayName() => "Cash reward for camera return (uses the host's game settings)";

        protected override int GetDefaultValue() => 0;

        override protected (int, int) GetMinMaxValue() => (0, 1000);
    }
}
