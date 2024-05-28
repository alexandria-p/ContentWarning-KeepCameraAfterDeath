using MyceliumNetworking;
using Photon.Pun;

namespace KeepCameraAfterDeath.Patches;

public class SurfaceNetworkHandlerPatch
{
    internal static void Init()
    {
        // Dev Note: SurfaceNetworkHandler.InitSurface runs after PhotonGameLobbyHandler.ReturnToSurface
        On.SurfaceNetworkHandler.InitSurface += SurfaceNetworkHandler_InitSurface;
        On.SurfaceNetworkHandler.OnSlept += SurfaceNetworkHandler_OnSlept;
    }

    private static void SurfaceNetworkHandler_InitSurface(On.SurfaceNetworkHandler.orig_InitSurface orig, SurfaceNetworkHandler self)
    {
        // When returning from spelunking,
        // Set if camera was brought home
        if (TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening)
        {
            var successfullyBroughtCameraHome = self.CheckIfCameraIsPresent(includeBrokencamera: true);

            if (MyceliumNetwork.IsHost)
            {
                if (successfullyBroughtCameraHome)
                {
                    // use host settings to set rewards
                    if (KeepCameraAfterDeath.Instance.PlayerSettingEnableRewardForCameraReturn)
                    {
                        // todo - RPC
                        KeepCameraAfterDeath.Instance.SetPendingRewardForCameraReturn();
                    }
                }
                else
                {
                    // add camera to the surface
                    self.m_VideoCameraSpawner.SpawnMe(force: true);
                }
            }
        }

        orig(self);
    }

    private static void SurfaceNetworkHandler_OnSlept(On.SurfaceNetworkHandler.orig_OnSlept orig, SurfaceNetworkHandler self)
    {
        // Safety checks: these should already have been reset as soon as they were used .. but lets clear them at the end of every day just to be sure.
        // Clear any camera film that was preserved from the lost world on the previous day
        // Clear pending rewards for camera return
        KeepCameraAfterDeath.Instance.ClearPreservedCameraInstanceData();
        KeepCameraAfterDeath.Instance.ClearPendingRewardForCameraReturn();

        orig(self);
    }
}
