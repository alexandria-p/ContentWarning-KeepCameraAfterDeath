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
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: new lobby");
            KeepCameraAfterDeath.Instance.ClearData();
        }

        // When returning from spelunking,
        // Set if camera was brought home
        if (MyceliumNetwork.IsHost && TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening)
        {
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: is evening");
            
            if (KeepCameraAfterDeath.Instance.PreservedCameraInstanceDataForHost != null)
            {
                KeepCameraAfterDeath.Logger.LogInfo("ALEX: respawn camera");
                self.m_VideoCameraSpawner.SpawnMe(force: true);
            }
            else
            {
                // only reward players if they do not leave any cameras behind
                KeepCameraAfterDeath.Logger.LogInfo("ALEX: brought camera home");
                // use host settings to set rewards
                if (KeepCameraAfterDeath.Instance.PlayerSettingEnableRewardForCameraReturn)
                {
                    KeepCameraAfterDeath.Instance.SetPendingRewardForAllPlayers();
                }
            }
        }

        orig(self);
    }

    // called by every client OnSlept, but also when quota fails
    private static void SurfaceNetworkHandler_NextDay(On.SurfaceNetworkHandler.orig_NextDay orig, SurfaceNetworkHandler self)
    {
        KeepCameraAfterDeath.Instance.Command_ResetDataforDay();
        orig(self);
    }
}
