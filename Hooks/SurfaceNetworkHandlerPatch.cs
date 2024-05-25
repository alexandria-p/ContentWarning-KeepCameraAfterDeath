using MyceliumNetworking;

namespace KeepCameraAfterDeath.Patches;

public class SurfaceNetworkHandlerPatch
{
    public static SurfaceNetworkHandlerPatch Instance { get; private set; } = null!;

    

    private void Awake()
    {
        Instance = this;
    }

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
            // PhotonNetwork.IsMasterClient
            if (MyceliumNetwork.IsHost) // PhotonNetwork is a static class, so can be called here
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
            //// PhotonNetwork.IsMasterClient
            if (MyceliumNetwork.IsHost)
            {
                KeepCameraAfterDeath.Logger.LogInfo("Awarding revenue for camera return");
                SurfaceNetworkHandler.RoomStats.AddMoney(KeepCameraAfterDeath.Instance.CashRewardForCameraReturn);
            }
        }

        // Dev Note: following the format of HatShop.HatBuyClicked
        void AddMCToPlayers()
        {
            if (KeepCameraAfterDeath.Instance.MetaCoinRewardForCameraReturn <= 0)
            {
                return;
            }

            KeepCameraAfterDeath.Logger.LogInfo("Awarding MC coins for camera return");
            MetaProgressionHandler.AddMetaCoins(KeepCameraAfterDeath.Instance.MetaCoinRewardForCameraReturn);

            //KeepCameraAfterDeath.Logger.LogInfo($"PhotonNetwork.LocalPlayer.ActorNumber {PhotonNetwork.LocalPlayer.ActorNumber}");
            //view.RPC("RPCM_TryAwardMC", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }
    
    /*
    [PunRPC]
    public void RPCM_TryAwardMC(int buyerActorNumber)
    {
        KeepCameraAfterDeath.Logger.LogInfo("RPCM_TryAwardMC");
        PhotonNetwork.PlayerList.First((Photon.Realtime.Player o) => o.ActorNumber == buyerActorNumber);

        KeepCameraAfterDeath.Logger.LogInfo("Calling RPCA_AwardMC");
        view.RPC("RPCA_AwardMC", RpcTarget.All, buyerActorNumber);
    }

    [PunRPC]
    public void RPCA_AwardMC(int buyerActorNumber)
    {
        KeepCameraAfterDeath.Logger.LogInfo("RPCA_AwardMC");

        if (!PlayerHandler.instance.TryGetPlayerFromOwnerID(buyerActorNumber, out var o))
        {
            KeepCameraAfterDeath.Logger.LogError("Player not found to award MC to on camera return");
            return;
        }
        if (Player.localPlayer == o)
        {
            KeepCameraAfterDeath.Logger.LogInfo("Awarding MC coins for camera return");
            MetaProgressionHandler.AddMetaCoins(MetaCoinRewardForCameraReturn);
        }
    }
    */
}
