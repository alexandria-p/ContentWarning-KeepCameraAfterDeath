using MyceliumNetworking;

namespace KeepCameraAfterDeath.Patches;

public class SurfaceNetworkHandlerPatch
{
    internal static void Init()
    {
        On.SurfaceNetworkHandler.InitSurface += SurfaceNetworkHandler_InitSurface;
        On.SurfaceNetworkHandler.NextDay += SurfaceNetworkHandler_NextDay;
    }

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
        if (TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening && MyceliumNetwork.IsHost)
        {
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: is evening");
            
            if (KeepCameraAfterDeath.Instance.PreservedCameraInstanceData != null)
            {
                KeepCameraAfterDeath.Logger.LogInfo("ALEX: respawn camera");
                self.m_VideoCameraSpawner.SpawnMe(force: true);
            }
        }
        orig(self);

        // wait until after initsurface has run,
        // we need to wait for surface pickups to spawn before checking if a reward should be made
        if (TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening && MyceliumNetwork.IsHost)
        {
            // check if camera is present on crew or in divebell
            var crewHasCamera = self.CheckIfCameraIsPresent(includeBrokencamera: true);
            // for now, we assume that if there was no footage preserved, then the crew did not drop their camera underground.
            // there could be a hole in this, if crew has 2 cameras, drop one underground and leave one on the floor of the diving bell. They would not get a reward.
            // (though...should they really be getting a reward if they left a camera behind?)
            var divingBellHasCamera = KeepCameraAfterDeath.Instance.PreservedCameraInstanceData == null; // todo - if I could access the internal properties of PersistentObjectsHolderPatch, I'd be able to search the diving bell objects for a camera ....

            if (crewHasCamera || divingBellHasCamera)
            {
                KeepCameraAfterDeath.Logger.LogInfo("ALEX: brought camera home");
                // use host settings to set rewards
                if (KeepCameraAfterDeath.Instance.PlayerSettingEnableRewardForCameraReturn)
                {
                    KeepCameraAfterDeath.Instance.SetPendingRewardForAllPlayers();
                }
            }
        }
    }

    // called by OnSlept, but also when quota fails
    private static void SurfaceNetworkHandler_NextDay(On.SurfaceNetworkHandler.orig_NextDay orig, SurfaceNetworkHandler self)
    {
        KeepCameraAfterDeath.Instance.Command_ResetDataforDay();
        orig(self);
    }
}
