using MyceliumNetworking;
using Photon.Pun;

namespace KeepCameraAfterDeath.Patches;

public class PhotonGameLobbyHandlerPatch
{
    internal static void Init()
    {
        On.PhotonGameLobbyHandler.SetCurrentObjective += PhotonGameLobbyHandler_SetCurrentObjective;
    }

    // this method is run on every client, but only the host should be able to do things with it
    private static void PhotonGameLobbyHandler_SetCurrentObjective(On.PhotonGameLobbyHandler.orig_SetCurrentObjective orig, PhotonGameLobbyHandler self, Objective objective)
    {
        // Dev note: this is the most fragile part of the code in this mod, as it is vulnerable to breaking if 
        // the Content Warning devs change or update how the game handles when quota is not met
        if (MyceliumNetwork.IsHost
            && KeepCameraAfterDeath.Instance.AllowCrewToWatchFootageEvenIfQuotaNotMet
            && SurfaceNetworkHandler.RoomStats != null && SurfaceNetworkHandler.RoomStats.IsQuotaDay && !SurfaceNetworkHandler.RoomStats.CalculateIfReachedQuota())
        {
            // intercept when returning from InitSurface, set objective to Extract video
            if (objective is GoToBedFailedObjective)
            {
                objective = new ExtractVideoObjective();
            }
            // intercept when finished watching TV, inform crew they failed quota
            if (objective is GoToBedSuccessObjective)
            {
                objective = new GoToBedFailedObjective();
            }            
        }

        orig(self, objective);
    }
}
