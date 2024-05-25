using MyceliumNetworking;

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
    }

    private static void SurfaceNetworkHandler_InitSurface(On.SurfaceNetworkHandler.orig_InitSurface orig, SurfaceNetworkHandler self)
    {
        // this all needs to happen before the original SurfaceNetworkHandler.InitSurface runs - otherwise, it will realise that ReturnedFromLostWorldWithCamera = CheckIfCameraIsPresent(includeBrokencamera: true);
        // has failed, and will set the day to a failure.
        InitSurfaceHook(); // hopefully this passes through the SurfaceNetworkHandler instance for me to use .....

        // Call the Trampoline for the Original method
        orig(self);

        
        void InitSurfaceHook()
        {
            if (self.CheckIfCameraIsPresent(includeBrokencamera: true))
            {
                if (KeepCameraAfterDeath.Instance.EnableRewardForCameraReturn)
                {
                    AddCashToRoom();
                    AddMCToPlayers();
                }
            }
            else
            {
                AddCameraToSurface();
            }
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
}
