using BepInEx;
using BepInEx.Logging;
using System.Reflection;
using MonoMod.RuntimeDetour.HookGen;
using KeepCameraAfterDeath.Patches;
using MyceliumNetworking;
using Zorro.Settings;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

namespace KeepCameraAfterDeath;

// since this alters the gameplay experience by removing risk of leaving camera behind,
// I have set it to "not vanilla"
[ContentWarningPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION, false)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class FixR2Modman : BaseUnityPlugin
{
    internal new static ManualLogSource Logger { get; private set; } = null!;

    // Awake for a BepInPlugin can be used to init the Thunderstore version of this mod.
    private void Awake()
    {
        Logger = base.Logger;

        var gameObject = new GameObject("KeepCameraAfterDeathPluginR2ModMan")
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        gameObject.AddComponent<KeepCameraAfterDeath>();

        KeepCameraAfterDeath.Instance.SetLogger(base.Logger);

        // Jan 2025 - make sure CW update doesnt destroy this mod
        DontDestroyOnLoad(gameObject);
    }
}

public class KeepCameraAfterDeath : MonoBehaviour // prev. BaseUnityPlugin
{
    // Actual mod logic

    const uint myceliumNetworkModId = 61812; // meaningless, as long as it is the same between all the clients
    public static KeepCameraAfterDeath Instance { get; private set; } = null!;


    internal static ManualLogSource Logger { get; private set; } = null!;
    public bool PlayerSettingEnableRewardForCameraReturn { get; private set; }
    public bool PlayerSettingDoNotRewardRecoveredSpookTubeFootage { get; private set; }
    public float PlayerSettingMetaCoinReward { get; private set; }
    public float PlayerSettingCashReward { get; private set; }
    public bool PlayerSettingEnableSplitRewardsForMultipleCameras { get; private set; }

    public List<ItemInstanceData> PreservedCameraInstanceDataCollectionForHost { get; private set; } = new List<ItemInstanceData>();
    public List<Guid> RestoredVideoHandleIdsCollectionForHost { get; private set; } = new List<Guid>();
    public (float cash, float mc)? ClientPendingRewardForCameraReturn { get; private set; } = null;
    public List<Guid> ClientDoNotPlayTheseSpookTubeVideoWithRewards { get; private set; } = new List<Guid>();


    public void SetLogger(ManualLogSource logger)
    {
        Logger = logger;
        Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] has loaded!");
    }


    private void Awake()
    {
        Instance = this;
        HookAll();
    }

    private void Start()
    {
        //Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] ON START!");
        MyceliumNetwork.RegisterNetworkObject(Instance, myceliumNetworkModId);
    }

    void OnDestroy()
    {
        //Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] ON DESTROY!");
        MyceliumNetwork.DeregisterNetworkObject(Instance, myceliumNetworkModId);
    }

    internal static void HookAll()
    {
        SurfaceNetworkHandlerPatch.Init();
        VideoCameraPatch.Init();
        PersistentObjectsHolderPatch.Init();
        PlayerPatch.Init();
        UploadCompleteStatePatch.Init();
    }

    internal static void UnhookAll()
    {
        HookEndpointManager.RemoveAllOwnedBy(Assembly.GetExecutingAssembly());
    }

    public void SetPlayerSettingDoNotRewardRecoveredSpookTubeFootage(bool settingEnabled)
    {
        PlayerSettingDoNotRewardRecoveredSpookTubeFootage = settingEnabled;
    }

    public void SetPlayerSettingEnableSplitRewardsForMultipleCameras(bool settingEnabled)
    {
        PlayerSettingEnableSplitRewardsForMultipleCameras = settingEnabled;
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
        Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Found camera, adding it to list");

        PreservedCameraInstanceDataCollectionForHost.Add(data);
        Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Preserved camera count is now {PreservedCameraInstanceDataCollectionForHost.Count}");
    }

    public void SetRestoredVideoHandleIdForHost(Guid id)
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        RestoredVideoHandleIdsCollectionForHost.Add(id);
        Command_SetIfClientShouldPlayRestoredSpookTubeVideoWithRewards(id);
        Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Restored camera count is now {RestoredVideoHandleIdsCollectionForHost.Count}");
    }

    public void Command_SetPendingRewardForAllPlayers()
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        var numOfCamerasSafelyReturned = MyceliumNetwork.PlayerCount - PreservedCameraInstanceDataCollectionForHost.Count;

        if (numOfCamerasSafelyReturned == 0)
        {
            Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] No cameras returned, do not set reward.");
            return;
        }

        Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Set reward for bringing {numOfCamerasSafelyReturned} camera(s) back successfully");

        var cashRewardForThisRound = PlayerSettingCashReward;
        var mcRewardForThisRound = PlayerSettingMetaCoinReward;

        if (PlayerSettingEnableSplitRewardsForMultipleCameras)
        {
            // Set a partial reward based on how many cameras were saved and which were not
            var singleCameraCashReward = cashRewardForThisRound / MyceliumNetwork.PlayerCount;
            var singleCameraMcReward = mcRewardForThisRound / MyceliumNetwork.PlayerCount;

            cashRewardForThisRound = singleCameraCashReward * numOfCamerasSafelyReturned;
            mcRewardForThisRound = singleCameraMcReward * numOfCamerasSafelyReturned;
        }
        else
        {
            cashRewardForThisRound = PlayerSettingCashReward * numOfCamerasSafelyReturned;
            mcRewardForThisRound = PlayerSettingMetaCoinReward * numOfCamerasSafelyReturned;
        }

        // Send out host's setting for rewards to all players
        MyceliumNetwork.RPC(myceliumNetworkModId, nameof(RPC_SetClientPendingRewardForCameraReturn), ReliableType.Reliable, cashRewardForThisRound, mcRewardForThisRound);
    }

    public void Command_SetIfClientShouldPlayRestoredSpookTubeVideoWithRewards(Guid id)
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        var footageWasRescued = RestoredVideoHandleIdsCollectionForHost.Any(_ => _.Equals(id));
        var doNotGiveRewardsForVideo = PlayerSettingDoNotRewardRecoveredSpookTubeFootage && footageWasRescued;

        // If this video should not be played with rewards, send out instruction from host to all players to run on their clients
        if (doNotGiveRewardsForVideo)
        {
            Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Send from host to clients: do not allot Spooktube rewards for video ID #{id}.");
            MyceliumNetwork.RPC(myceliumNetworkModId, nameof(RPC_SetClientDoNotPlayRestoredSpookTubeVideoWithRewards), ReliableType.Reliable, id.ToString());
        }
    }

    [CustomRPC] // sent out by host to run on all players
    public void RPC_SetClientPendingRewardForCameraReturn(float cash, float mc)
    {
        ClientPendingRewardForCameraReturn = (cash, mc);
    }

    [CustomRPC] // sent out by host to run on all players
    public void RPC_SetClientDoNotPlayRestoredSpookTubeVideoWithRewards(string idString)
    {
        Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Received on client from host: do not allot Spooktube rewards for video ID #{idString}.");
        ClientDoNotPlayTheseSpookTubeVideoWithRewards.Add(new Guid(idString));
        ListClientDoNotPlayTheseSpookTubeVideoWithRewards();
    }

    // debugging helper method
    public void ListClientDoNotPlayTheseSpookTubeVideoWithRewards()
    {
        Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] There are {ClientDoNotPlayTheseSpookTubeVideoWithRewards.Count} videos that should not be rewarded by SpookTube.");
        if (ClientDoNotPlayTheseSpookTubeVideoWithRewards.Count > 0)
        {
            ClientDoNotPlayTheseSpookTubeVideoWithRewards.ForEach(videoGuid =>
            {
                Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] #{videoGuid}.");
            });
        }
    }

    public void Command_ResetDataforDay()
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        //Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] reset data for day");
        MyceliumNetwork.RPC(myceliumNetworkModId, nameof(RPC_ResetDataforDay), ReliableType.Reliable);
    }

    [CustomRPC] // sent out by host to run on all players
    public void RPC_ResetDataforDay()
    {
        ClearData();
    }

    public void ClearData()
    {
        // Clear any camera film that was preserved by the host from the lost world on the previous day
        if (MyceliumNetwork.IsHost)
        {
            ClearAllPreservedCameraInstanceData(); // this should already be empty (as they should all have been restored)
            ClearAllRestoredVideoHandleIds();
        }
        // Clear information set onto clients by host
        ClearPendingRewardForCameraReturn();
        ClearClientDoNotPlayTheseSpookTubeVideoWithRewards();
    }

    public void DeletePreservedCameraInstanceDataFromCollection(ItemInstanceData preservedCameraData)
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        PreservedCameraInstanceDataCollectionForHost.Remove(preservedCameraData);
    }

    public void ClearAllPreservedCameraInstanceData()
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        PreservedCameraInstanceDataCollectionForHost.Clear();
    }

    public void ClearAllRestoredVideoHandleIds()
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        RestoredVideoHandleIdsCollectionForHost.Clear();
    }

    public void ClearPendingRewardForCameraReturn()
    {
        ClientPendingRewardForCameraReturn = null;
    }

    public void ClearClientDoNotPlayTheseSpookTubeVideoWithRewards()
    {
        Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Clear ClientDoNotPlayTheseSpookTubeVideoWithRewards collection");
        ClientDoNotPlayTheseSpookTubeVideoWithRewards.Clear();
    }

    public bool IsFinalDayAndQuotaNotMet()
    {
        return SurfaceNetworkHandler.RoomStats != null && SurfaceNetworkHandler.RoomStats.IsQuotaDay && !SurfaceNetworkHandler.RoomStats.CalculateIfReachedQuota();
    }

    public void SpawnCamerasAndRestoreFootage(SurfaceNetworkHandler surfaceNetworkHandler)
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Host is trying to spawn new cameras on surface");
        //Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] surfacehandler id: {surfaceNetworkHandler.gameObject.GetInstanceID()}");

        StartCoroutine(SpawnCouroutine(surfaceNetworkHandler));
    }

    IEnumerator SpawnCouroutine(SurfaceNetworkHandler surfaceNetworkHandler)
    {
        Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Starting coroutine");

        var totalNumCamerasNeededToSpawn = PreservedCameraInstanceDataCollectionForHost.Count;
        // camera number works down
        for (int cameraNumber = totalNumCamerasNeededToSpawn; cameraNumber > 0; cameraNumber--)
        {
            Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Spawn camera #{cameraNumber}");
            SpawnNewCamera(surfaceNetworkHandler, cameraNumber);

            var secondsWaitingForCameraFootageToBeRestored = 0;
            var timeoutInSeconds = 20;

            // waits until the new camera has spawned, initialised its footage in VideoCamera_ConfigItem, and then removed that footage from the collection before moving on to spawn the next camera.
            // this is to try and avoid a race-condition, if two cameras spawn at the same time and try to recover the same camera data.
            // once the current camera has spawned and init footage, it will have removed the footage from the list (PreservedCameraInstanceDataCollectionForHost)
            while (PreservedCameraInstanceDataCollectionForHost.Count == cameraNumber && secondsWaitingForCameraFootageToBeRestored <= timeoutInSeconds)
            {
                Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Waiting for camera to finish initialising with the preserved data");
                secondsWaitingForCameraFootageToBeRestored++;
                yield return new WaitForSeconds(1);
            }

            Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Waited {secondsWaitingForCameraFootageToBeRestored} seconds for preserved camera #{cameraNumber} to spawn and init (restore) its data");


        }

        Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Finished spawning all {totalNumCamerasNeededToSpawn} camera(s)");
    }

    void SpawnNewCamera(SurfaceNetworkHandler surfaceNetworkHandler, int cameraNumber)
    {
        if (!MyceliumNetwork.IsHost)
        {
            return;
        }

        // Manually create the camera at a given location, rather than piggy-backing "Spawn Me".
        // (8.2.25) "magic numbers" for location from ContentPOVs mod 
        PickupHandler.CreatePickup((byte)1, new ItemInstanceData(Guid.NewGuid()), new Vector3(-14.805f - (cameraNumber * 0.487f), 2.418f, 8.896f - (cameraNumber * 0.487f)), Quaternion.Euler(0f, 315f, 0f));
    }

    [ContentWarningSetting]
    public class EnableRewardForCameraReturnSetting : BoolSetting, IExposedSetting
    {
        public SettingCategory GetSettingCategory() => SettingCategory.Mods;

        public override void ApplyValue()
        {
            KeepCameraAfterDeath.Instance.SetPlayerSettingEnableRewardForCameraReturn(Value);
        }

        public string GetDisplayName() => "[KeepCameraAfterDeath] Turn on incentives for bringing the camera back to the surface (uses the host's game settings)";

        protected override bool GetDefaultValue() => true;
    }

    [ContentWarningSetting]
    public class EnableSplitRewardsForMultipleCamerasSetting : BoolSetting, IExposedSetting
    {
        public SettingCategory GetSettingCategory() => SettingCategory.Mods;

        public override void ApplyValue()
        {
            KeepCameraAfterDeath.Instance.SetPlayerSettingEnableSplitRewardsForMultipleCameras(Value);
        }

        public string GetDisplayName() => "[KeepCameraAfterDeath] Split the reward incentives for each camera successfully returned (instead of each camera being worth the full sum). This is for games that are modded to allow more than one camera at a time. (uses the host's game settings)";

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

        public string GetDisplayName() => "[KeepCameraAfterDeath] Meta Coin (MC) reward for camera return (uses the host's game settings)";

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

        public string GetDisplayName() => "[KeepCameraAfterDeath] Cash reward for camera return (uses the host's game settings)";

        protected override float GetDefaultValue() => 0;

        protected override float2 GetMinMaxValue() => new float2(0f, 1000);
    }

    [ContentWarningSetting]
    public class EnableAlwaysRewardSpookTubeSetting : BoolSetting, IExposedSetting
    {
        public SettingCategory GetSettingCategory() => SettingCategory.Mods;

        public override void ApplyValue()
        {
            KeepCameraAfterDeath.Instance.SetPlayerSettingDoNotRewardRecoveredSpookTubeFootage(Value);
        }
        // todo
        public string GetDisplayName() => "[KeepCameraAfterDeath] Do not award Spooktube views/money when you watch recovered camera footage on the TV, if the camera footage was lost underground and had to be recovered (uses the host's game settings)";

        protected override bool GetDefaultValue() => false;
    }

}
