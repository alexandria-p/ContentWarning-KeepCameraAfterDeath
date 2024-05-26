using MyceliumNetworking;
using Photon.Pun;
using Zorro.Core;

namespace KeepCameraAfterDeath.Patches;

public class SurfaceNetworkHandlerPatch
{
    internal static void Init()
    {
        /*
         *  Subscribe with 'On.Namespace.Type.Method += CustomMethod;' for each method you're patching.
         *  Or if you are writing an ILHook, use 'IL.' instead of 'On.'
         *  Note that not all types are in a namespace, especially in Unity games.
         */

        // Dev Note: SurfaceNetworkHandler.InitSurface runs after PhotonGameLobbyHandler.ReturnToSurface
        On.SurfaceNetworkHandler.InitSurface += SurfaceNetworkHandler_InitSurface;
        On.SurfaceNetworkHandler.OnSlept += SurfaceNetworkHandler_OnSlept;
    }

    private static void SurfaceNetworkHandler_InitSurface(On.SurfaceNetworkHandler.orig_InitSurface orig, SurfaceNetworkHandler self)
    {
        KeepCameraAfterDeath.Logger.LogInfo("ALEX: init surface");

        // When returning from spelunking,
        // Set if camera was brought home
        if (TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening)
        {
            KeepCameraAfterDeath.Instance.SetSuccessfullyBroughtCameraHome(self.CheckIfCameraIsPresent(includeBrokencamera: true));

            if (!KeepCameraAfterDeath.Instance.SuccessfullyBroughtCameraHome)
            {
                AddCameraToSurface();
            }            
        }

        // Call the Trampoline for the Original method
        orig(self);

        // HACK for solo player
        if (PhotonGameLobbyHandler.CurrentObjective is InviteFriendsObjective)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonGameLobbyHandler.Instance.SetCurrentObjective(new LeaveHouseObjective());

            }
        }

        // When returning from spelunking,
        // Must wait until camera.main and players exist before running rewards (or SFX that plays when UI message is shown will fail and wreak havoc) 
        if (TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening 
            && KeepCameraAfterDeath.Instance.SuccessfullyBroughtCameraHome 
            && KeepCameraAfterDeath.Instance.EnableRewardForCameraReturn)
        {
            AddCashToRoom();
            AddMCToPlayers();
        }

        void AddCameraToSurface()
        {
            if (MyceliumNetwork.IsHost)
            {
                self.m_VideoCameraSpawner.SpawnMe(force: true);
            }
        }

        void AddCashToRoom()
        {
            if (KeepCameraAfterDeath.Instance.CashRewardForCameraReturn <= 0)
            {
                return;
            }

            UserInterface.ShowMoneyNotification("Cash Received", $"${KeepCameraAfterDeath.Instance.CashRewardForCameraReturn}", MoneyCellUI.MoneyCellType.Revenue);

            if (MyceliumNetwork.IsHost)
            {
                KeepCameraAfterDeath.Logger.LogInfo("Awarding revenue for camera return");
                SurfaceNetworkHandler.RoomStats.AddMoney(KeepCameraAfterDeath.Instance.CashRewardForCameraReturn);
            }
        }

        void AddMCToPlayers()
        {
            if (KeepCameraAfterDeath.Instance.MetaCoinRewardForCameraReturn <= 0)
            {
                return;
            }

            KeepCameraAfterDeath.Logger.LogInfo("Awarding MC coins for camera return");
            MetaProgressionHandler.AddMetaCoins(KeepCameraAfterDeath.Instance.MetaCoinRewardForCameraReturn);
        }
    }

    private static void SurfaceNetworkHandler_OnSlept(On.SurfaceNetworkHandler.orig_OnSlept orig, SurfaceNetworkHandler self)
    {
        KeepCameraAfterDeath.Logger.LogInfo("ALEX: on slept, clear all");

        // Clear any camera film that was preserved from the lost world on the previous day
        KeepCameraAfterDeath.Instance.ClearPreservedCameraInstanceData();
        KeepCameraAfterDeath.Instance.ClearSuccessfullyBroughtCameraHome();

        orig(self);
    }
}
