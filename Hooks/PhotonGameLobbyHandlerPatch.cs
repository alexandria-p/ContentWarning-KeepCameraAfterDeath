using MyceliumNetworking;

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
        if (MyceliumNetwork.IsHost
            && KeepCameraAfterDeath.Instance.AllowCrewToWatchFootageEvenIfQuotaNotMet
            && KeepCameraAfterDeath.Instance.IsFinalDayAndQuotaNotMet())
        {
            // intercept when returning from InitSurface, set objective to Extract video
            if (KeepCameraAfterDeath.Instance.Debug_InitSurfaceActive)
            {
                objective = new ExtractVideoObjective();
            }
            // intercept when finished watching TV, inform crew they failed quota
            // the text seems almost identical, so this is just cosmetic future-proofing
            if (objective is GoToBedSuccessObjective)
            {
                objective = new GoToBedFailedObjective();                
            }            
        }

        orig(self, objective);
    }
}
