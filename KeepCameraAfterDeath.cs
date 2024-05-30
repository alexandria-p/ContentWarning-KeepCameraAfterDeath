using BepInEx;
using BepInEx.Logging;
using System.Reflection;
using MonoMod.RuntimeDetour.HookGen;
using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;
using KeepCameraAfterDeath.Patches;
using MyceliumNetworking;

namespace KeepCameraAfterDeath;

// since this alters the gameplay experience by removing risk of leaving camera behind,
// I have set it to "not vanilla"
[ContentWarningPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION, false)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class KeepCameraAfterDeath : BaseUnityPlugin
{
    const uint myceliumNetworkModId = 61812; // meaningless, as long as it is the same between all the clients
    public static KeepCameraAfterDeath Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;

    public bool PlayerSettingEnableRewardForCameraReturn { get; private set; }
    public int PlayerSettingMetaCoinReward { get; private set; }
    public int PlayerSettingCashReward { get; private set; }

    public ItemInstanceData? PreservedCameraInstanceDataForHost { get; private set; } = null;
    public (int cash, int mc)? PendingRewardForCameraReturn { get; private set; } = null;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        HookAll();

        Logger.LogInfo($"{"alexandria-p.KeepCameraAfterDeath"} v{"1.0.0"} has loaded!");

    }

    private void Start()
    {
        MyceliumNetwork.RegisterNetworkObject(Instance, myceliumNetworkModId);
    }

    void OnDestroy()
    {
        MyceliumNetwork.DeregisterNetworkObject(Instance, myceliumNetworkModId);
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

    public void SetPlayerSettingEnableRewardForCameraReturn(bool rewardEnabled)
    {
        PlayerSettingEnableRewardForCameraReturn = rewardEnabled;
    }

    public void SetPlayerSettingMetaCoinReward(int mcReward)
    {
        PlayerSettingMetaCoinReward = mcReward;
    }

    public void SetPlayerSettingCashReward(int cashReward)
    {
        PlayerSettingCashReward = cashReward;
    }

    public void SetPreservedCameraInstanceDataForHost(ItemInstanceData data)
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        PreservedCameraInstanceDataForHost = data;
    }

    public void SetPendingRewardForAllPlayers()
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        // Send out host's setting for rewards to all players
        MyceliumNetwork.RPC(myceliumNetworkModId, nameof(RPC_SetPendingRewardForCameraReturn), ReliableType.Reliable, PlayerSettingCashReward, PlayerSettingMetaCoinReward);
    }

    [CustomRPC]
    public void RPC_SetPendingRewardForCameraReturn(int cash, int mc)
    {
        PendingRewardForCameraReturn = (cash, mc);
    }

    public void Command_ResetDataforDay()
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        MyceliumNetwork.RPC(myceliumNetworkModId, nameof(RPC_ResetDataforDay), ReliableType.Reliable);
    }

    [CustomRPC]
    public void RPC_ResetDataforDay()
    {
        KeepCameraAfterDeath.Instance.ClearData();
    }

    public void ClearData()
    {
        // Clear any camera film that was preserved from the lost world on the previous day
        // Clear pending rewards for camera return
        KeepCameraAfterDeath.Instance.ClearPreservedCameraInstanceDataForHost();
        KeepCameraAfterDeath.Instance.ClearPendingRewardForCameraReturn();
    }

    public void ClearPreservedCameraInstanceDataForHost()
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        PreservedCameraInstanceDataForHost = null;
    }

    public void ClearPendingRewardForCameraReturn()
    {
        PendingRewardForCameraReturn = null;
    }


    [SettingRegister("KeepCameraAfterDeath Mod Settings")]
    public class EnableRewardForCameraReturnSetting : BoolSetting, ICustomSetting
    {
        public override void ApplyValue()
        {
            KeepCameraAfterDeath.Instance.SetPlayerSettingEnableRewardForCameraReturn(Value);
        }

        public string GetDisplayName() => "Turn on incentives for bringing the camera back to the surface (uses the host's game settings)";

        protected override bool GetDefaultValue() => true;
    }

    [SettingRegister("KeepCameraAfterDeath Mod Settings")]
    public class SetMetaCoinRewardForCameraReturnSetting : IntSetting, ICustomSetting
    {
        public override void ApplyValue()
        {
            KeepCameraAfterDeath.Instance.SetPlayerSettingMetaCoinReward(Value);
        }

        public string GetDisplayName() => "Meta Coin (MC) reward for camera return (uses the host's game settings)";

        protected override int GetDefaultValue() => 10;

        override protected (int, int) GetMinMaxValue() => (0, 100);
    }

    [SettingRegister("KeepCameraAfterDeath Mod Settings")]
    public class SetCashRewardForCameraReturnSetting : IntSetting, ICustomSetting
    {
        public override void ApplyValue()
        {
            KeepCameraAfterDeath.Instance.SetPlayerSettingCashReward(Value);
        }

        public string GetDisplayName() => "Cash reward for camera return (uses the host's game settings)";

        protected override int GetDefaultValue() => 0;

        override protected (int, int) GetMinMaxValue() => (0, 1000);
    }
}
