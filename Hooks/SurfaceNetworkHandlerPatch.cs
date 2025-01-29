using MyceliumNetworking;

namespace KeepCameraAfterDeath.Patches;

public class SurfaceNetworkHandlerPatch
{
    internal static void Init()
    {
        On.SurfaceNetworkHandler.InitSurface += SurfaceNetworkHandler_InitSurface;
        On.SurfaceNetworkHandler.NextDay += SurfaceNetworkHandler_NextDay;
    }

    // this method is run on every client
    private static void SurfaceNetworkHandler_InitSurface(On.SurfaceNetworkHandler.orig_InitSurface orig, SurfaceNetworkHandler self)
    {
        // Clear data when entering new lobby
        if (SurfaceNetworkHandler.RoomStats == null)
        {
            KeepCameraAfterDeath.Instance.ClearData();
        }

        // When returning from spelunking,
        // Set if camera was brought home
        if (MyceliumNetwork.IsHost && TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening)
        {            
            if (KeepCameraAfterDeath.Instance.PreservedCameraInstanceDataForHost != null)
            {
                KeepCameraAfterDeath.Logger.LogInfo("KeepCameraAfterDeath: Found backed-up camera footage");

                // Host spawns new camera
                self.m_VideoCameraSpawner.SpawnMe(force: true);
            }
            else
            {
                KeepCameraAfterDeath.Logger.LogInfo("KeepCameraAfterDeath: Could not find camera footage to restore");
                // Only reward players if they do not leave any cameras behind
                // - uses host settings to set rewards
                if (KeepCameraAfterDeath.Instance.PlayerSettingEnableRewardForCameraReturn)
                {
                    KeepCameraAfterDeath.Instance.SetPendingRewardForAllPlayers();
                }
            }
        }

        orig(self);
    }

    private static void SurfaceNetworkHandler_NextDay(On.SurfaceNetworkHandler.orig_NextDay orig, SurfaceNetworkHandler self)
    {        
        orig(self);

        // camera spawning doesnt happen till later in onSlept, so resetting the data here after NextDay is complete within OnSlept should be fine.
        KeepCameraAfterDeath.Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} Surface network handler patch NEXT DAY: reset data for day");
        KeepCameraAfterDeath.Instance.Command_ResetDataforDay();
    }
    
}
