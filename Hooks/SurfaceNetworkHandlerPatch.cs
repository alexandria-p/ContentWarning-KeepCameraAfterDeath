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

        KeepCameraAfterDeath.Instance.Debug_InitSurfaceActive = true;

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

        // if quota not met on final day in InitSurface
        // spawn players and let them extract camera.
        if (MyceliumNetwork.IsHost
            && KeepCameraAfterDeath.Instance.AllowCrewToWatchFootageEvenIfQuotaNotMet
            && KeepCameraAfterDeath.Instance.IsFinalDayAndQuotaNotMet())
        {
            if (!Player.justDied)
            {
                SpawnHandler.Instance.SpawnLocalPlayer(Spawns.DiveBell);
                // 2.6.24 - hospital spawns are handled by SpawnHandler.Start(), so we don't have to handle it here
            }
        }

        KeepCameraAfterDeath.Instance.Debug_InitSurfaceActive = false;
    }

    // 7.6.24: This method is interesting, because it is Called by every client's OnSlept, but also on the final day in the Evening if quota fails as you return from spelunking.
    // Dev note: this is the most fragile part of the code in this mod, as it is vulnerable to breaking if 
    // the Content Warning devs change or update how the game handles when quota is not met
    // if quota failed as crew return to surface on final day
    // This method is called by host in InitSurface.
    // The game tries to jump straight to 'Sleeping', runs _NextDay (this method) and immediately game overs players (without letting them watch their last video)
    private static void SurfaceNetworkHandler_NextDay(On.SurfaceNetworkHandler.orig_NextDay orig, SurfaceNetworkHandler self)
    {
        // Do not run NextDay if quota not met on final day DURING InitSurface (as this ends the run immediately)
        if (MyceliumNetwork.IsHost
            && KeepCameraAfterDeath.Instance.AllowCrewToWatchFootageEvenIfQuotaNotMet
            && KeepCameraAfterDeath.Instance.IsFinalDayAndQuotaNotMet()
            && KeepCameraAfterDeath.Instance.Debug_InitSurfaceActive)
        {
            // early out - we do not want to skip to 'quota failed' in InitSurface before players get to watch their video.
            KeepCameraAfterDeath.Logger.LogInfo("KeepCameraAfterDeath: Cancelled gameover, to allow players to watch their video on the final day when they didn't meet quota");
            return;
        }

        KeepCameraAfterDeath.Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} Surface network handler patch NEXT DAY: reset data for day");
        KeepCameraAfterDeath.Instance.Command_ResetDataforDay();
        orig(self);
    }
    
}
