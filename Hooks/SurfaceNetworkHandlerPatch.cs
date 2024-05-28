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
                /*
                // destroy any existing cameras
                VideoCamera[] array = UnityEngine.Object.FindObjectsOfType<VideoCamera>();
                for (int i = 0; i < array.Length; i++)
                {
                    PhotonView component = array[i].transform.parent.GetComponent<PhotonView>();
                    if (component != null)
                    {
                        PhotonNetwork.Destroy(component);
                    }
                }
                */
                // add camera to the surface
                self.m_VideoCameraSpawner.SpawnMe(force: true);
            }
        }
        orig(self);

        // wait until after initsurface has run to set if reward should be made, as we need to wait for surface pickups to spawn

        if (TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening && MyceliumNetwork.IsHost)
        {
            var successfullyBroughtCameraHome = self.CheckIfCameraIsPresent(includeBrokencamera: true);
            
            if (successfullyBroughtCameraHome)
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
