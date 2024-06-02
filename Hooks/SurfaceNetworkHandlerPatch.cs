using MyceliumNetworking;
using Photon.Pun;

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
                // Host spawns new camera
                self.m_VideoCameraSpawner.SpawnMe(force: true);
            }
            else
            {
                // Only reward players if they do not leave any cameras behind
                // - uses host settings to set rewards
                if (KeepCameraAfterDeath.Instance.PlayerSettingEnableRewardForCameraReturn)
                {
                    KeepCameraAfterDeath.Instance.SetPendingRewardForAllPlayers();
                }
            }
        }

        orig(self);

        // Do not run NextDay if quota not met on final day in InitSurface
        // instead, spawn players and let them extract camera.
        if (MyceliumNetwork.IsHost
            && KeepCameraAfterDeath.Instance.AllowCrewToWatchFootageEvenIfQuotaNotMet
            && SurfaceNetworkHandler.RoomStats != null && SurfaceNetworkHandler.RoomStats.IsQuotaDay && !SurfaceNetworkHandler.RoomStats.CalculateIfReachedQuota())
        {
            if (!Player.justDied)
            {
                SpawnHandler.Instance.SpawnLocalPlayer(Spawns.DiveBell);
                // 2.6.24 - hospital spawns are handled by SpawnHandler.Start(), so we don't have to handle it here
            }
        }
    }
    
    // Called by every client's OnSlept, but also when quota fails
    private static void SurfaceNetworkHandler_NextDay(On.SurfaceNetworkHandler.orig_NextDay orig, SurfaceNetworkHandler self)
    {
        // Do not run NextDay if quota not met on final day in InitSurface
        // instead, spawn players and let them extract camera.
        if (MyceliumNetwork.IsHost
            && KeepCameraAfterDeath.Instance.AllowCrewToWatchFootageEvenIfQuotaNotMet
            && SurfaceNetworkHandler.RoomStats != null && SurfaceNetworkHandler.RoomStats.IsQuotaDay && !SurfaceNetworkHandler.RoomStats.CalculateIfReachedQuota())
        {
            // early out - we do not want to end the day before players get to watch their video.
            return;
        }

        KeepCameraAfterDeath.Instance.Command_ResetDataforDay();
        orig(self);
    }
    
}
