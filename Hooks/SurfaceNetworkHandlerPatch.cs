using System;
using System.Runtime.CompilerServices;

namespace KeepCameraAfterDeath.Patches;

public class SurfaceNetworkHandlerPatch
{
    private bool EnableRewardForCameraReturn;
    private int MetaCoinRewardForCameraReturn;    
    private int CashRewardForCameraReturn;  

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

    public void SetEnableRewardForCameraReturn(bool rewardEnabled)
    {
        EnableRewardForCameraReturn = rewardEnabled;
    }

    public void SetMetaCoinRewardForCameraReturn(int mcReward)
    {
        MetaCoinRewardForCameraReturn = mcReward;
    }

    public void SetCashRewardForCameraReturn(int cashReward)
    {
        CashRewardForCameraReturn = cashReward;
    }

    private static void SurfaceNetworkHandler_InitSurface(On.SurfaceNetworkHandler.orig_InitSurface orig)
    {
        // this all needs to happen before the original SurfaceNetworkHandler.InitSurface runs - otherwise, it will realise that ReturnedFromLostWorldWithCamera = CheckIfCameraIsPresent(includeBrokencamera: true);
        // has failed, and will set the day to a failure.
        InitSurfaceHook(orig); // hopefully this passes through the SurfaceNetworkHandler instance for me to use .....

        // Call the Trampoline for the Original method
        orig();

        // Please note: SurfaceNetworkHandler.ReturnedFromLostWorldWithCamera will always be true with our mod
        // (since we add the camera back in the the Surface before ReturnedFromLostWorldWithCamera = CheckIfCameraIsPresent(includeBrokencamera: true) is checked after our hook InitSurface.
    }

    private void InitSurfaceHook(On.SurfaceNetworkHandler.orig_InitSurface orig)
    {
        if (CheckIfCameraIsPresent(includeBrokencamera: true))
        {
            if (EnableRewardForCameraReturn)
            {
                AddCashToRoom();
                AddMCToPlayers();                
            }
        }
        else
        {
            AddCameraToSurface(On.SurfaceNetworkHandler.orig_InitSurface orig);
        }
    }

    private void AddCameraToSurface(On.SurfaceNetworkHandler.orig_InitSurface orig)
    {
        if (PhotonNetwork.IsMasterClient) // PhotonNetwork is a static class, so can be called here
        {
            orig.m_VideoCameraSpawner.SpawnMe(force: true);
        }
    }

    private void AddCashToRoom()
    {
        if (CashRewardForCameraReturn =< 0)
        {
            return;
        }

        UserInterface.ShowMoneyNotification("Cash Received", $"${CashRewardForCameraReturn}", MoneyCellUI.MoneyCellType.Revenue);
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Awarding revenue for camera return");
            SurfaceNetworkHandler.RoomStats.AddMoney(CashRewardForCameraReturn);
        }
    }

    // Dev Note: following the format of HatShop.HatBuyClicked
    private void AddMCToPlayers()
    {
        if (MetaCoinRewardForCameraReturn =< 0)
        {
            return;
        }

        Debug.Log($"PhotonNetwork.LocalPlayer.ActorNumber {PhotonNetwork.LocalPlayer.ActorNumber}");
        view.RPC("RPCM_TryAwardMC", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    public void RPCM_TryAwardMC(int buyerActorNumber)
    {
        Debug.Log("RPCM_TryAwardMC");
        PhotonNetwork.PlayerList.First((Photon.Realtime.Player o) => o.ActorNumber == buyerActorNumber);

        Debug.Log("Calling RPCA_AwardMC");
        view.RPC("RPCA_AwardMC", RpcTarget.All, buyerActorNumber);
    }

    [PunRPC]
    public void RPCA_AwardMC(int buyerActorNumber)
    {
        Debug.Log("RPCA_AwardMC");

        if (!PlayerHandler.instance.TryGetPlayerFromOwnerID(buyerActorNumber, out var o))
        {
            Debug.LogError("Player not found to award MC to on camera return");
            return;
        }
        if (Player.localPlayer == o)
        {
            Debug.Log("Awarding MC coins for camera return");
            MetaProgressionHandler.AddMetaCoins(MetaCoinRewardForCameraReturn);
        }
    }
}
