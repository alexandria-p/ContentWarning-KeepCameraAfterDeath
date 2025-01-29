using BepInEx;
using BepInEx.Logging;
using System.Reflection;
using MonoMod.RuntimeDetour.HookGen;
using KeepCameraAfterDeath.Patches;
using MyceliumNetworking;
using Zorro.Settings;
using Unity.Mathematics;
using UnityEngine;

namespace KeepCameraAfterDeath;

// since this alters the gameplay experience by removing risk of leaving camera behind,
// I have set it to "not vanilla"
[ContentWarningPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION, false)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class FixR2Modman : BaseUnityPlugin
{
    //const uint myceliumNetworkModId = 61812; // meaningless, as long as it is the same between all the clients
    internal new static ManualLogSource Logger { get; private set; } = null!;

    private void Awake()
    {
        // Jan 2025 - make sure CW update doesnt destroy this mod /////////////////
        Logger = base.Logger;

        var gameObject = new GameObject("KeepCameraAfterDeathPluginR2ModMan")
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        gameObject.AddComponent<KeepCameraAfterDeath>();

        KeepCameraAfterDeath.Instance.SetLogger(base.Logger);

        DontDestroyOnLoad(gameObject);
        ////////////////////////////////////////////////////////////////////////////
    }
}

public class KeepCameraAfterDeath : MonoBehaviour // prev. BaseUnityPlugin
{
    // Actual mod logic

    const uint myceliumNetworkModId = 61812; // meaningless, as long as it is the same between all the clients
    public static KeepCameraAfterDeath Instance { get; private set; } = null!;
    internal static ManualLogSource Logger { get; private set; } = null!;

    public bool PlayerSettingEnableRewardForCameraReturn { get; private set; }
    public float PlayerSettingMetaCoinReward { get; private set; }
    public float PlayerSettingCashReward { get; private set; }

    public ItemInstanceData? PreservedCameraInstanceDataForHost { get; private set; } = null;
    public (float cash, float mc)? PendingRewardForCameraReturn { get; private set; } = null;


    // Jan 2025 - make sure CW update doesnt destroy this mod /////////////////
    public void SetLogger(ManualLogSource logger)
    {
        Logger = logger;
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }
    ////////////////////////////////////////////////////////////////////////////
    

    private void Awake()
    {
        // Logger = base.Logger;
        Instance = this;

        HookAll();
    }

    private void Start()
    {
        //Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} ON START!");
        MyceliumNetwork.RegisterNetworkObject(Instance, myceliumNetworkModId);
    }

    void OnDestroy()
    {
        //Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} ON DESTROY!");
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

    public void SetPlayerSettingMetaCoinReward(float mcReward)
    {
        PlayerSettingMetaCoinReward = mcReward;
    }

    public void SetPlayerSettingCashReward(float cashReward)
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
    public void RPC_SetPendingRewardForCameraReturn(float cash, float mc)
    {
        PendingRewardForCameraReturn = (cash, mc);
    }

    public void Command_ResetDataforDay()
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} reset data for day");
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

    public bool IsFinalDayAndQuotaNotMet()
    {
        return SurfaceNetworkHandler.RoomStats != null && SurfaceNetworkHandler.RoomStats.IsQuotaDay && !SurfaceNetworkHandler.RoomStats.CalculateIfReachedQuota();
    }

    [ContentWarningSetting]
    public class EnableRewardForCameraReturnSetting : BoolSetting, IExposedSetting
    {
        public SettingCategory GetSettingCategory() => SettingCategory.Mods;

        public override void ApplyValue()
        {
            KeepCameraAfterDeath.Instance.SetPlayerSettingEnableRewardForCameraReturn(Value);
        }

        public string GetDisplayName() => "KeepCameraAfterDeath: Turn on incentives for bringing the camera back to the surface (uses the host's game settings)";

        protected override bool GetDefaultValue() => true;
    }

    [ContentWarningSetting]
    public class SetMetaCoinRewardForCameraReturnSetting : FloatSetting, IExposedSetting
    {
        public SettingCategory GetSettingCategory() => SettingCategory.Mods;

        public override void ApplyValue()
        {
            KeepCameraAfterDeath.Instance.SetPlayerSettingMetaCoinReward(Value);
        }

        public string GetDisplayName() => "KeepCameraAfterDeath: Meta Coin (MC) reward for camera return (uses the host's game settings)";

        protected override float GetDefaultValue() => 10;

        protected override float2 GetMinMaxValue() => new float2(0f, 100);
    }

    [ContentWarningSetting]
    public class SetCashRewardForCameraReturnSetting : FloatSetting, IExposedSetting
    {
        public SettingCategory GetSettingCategory() => SettingCategory.Mods;

        public override void ApplyValue()
        {
            KeepCameraAfterDeath.Instance.SetPlayerSettingCashReward(Value);
        }

        public string GetDisplayName() => "KeepCameraAfterDeath: Cash reward for camera return (uses the host's game settings)";

        protected override float GetDefaultValue() => 0;

        protected override float2 GetMinMaxValue() => new float2(0f, 1000);
    }
}
