using MyceliumNetworking;
using System.Collections;

namespace KeepCameraAfterDeath.Patches;

public class PlayerPatch
{
    internal static void Init()
    {
        // Dev Note: SurfaceNetworkHandler.InitSurface runs after PhotonGameLobbyHandler.ReturnToSurface
        On.Player.Start += Player_Start;
    }

    // When returning from spelunking,
    // Must wait until camera.main and players exist before running rewards (or SFX that plays when UI message is shown will fail and wreak havoc) 

    private static IEnumerator Player_Start(On.Player.orig_Start orig, Player self)
    {
        var value = orig(self);

        // TODO - just use the hosts' values for everyone.

        if (self.IsLocal)
        {
            // if we respawn after spelunking underground and there is a pending reward for our safe return of the camera
            if (TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening
                && KeepCameraAfterDeath.Instance.IsRewardForCameraReturnEnabled
                && KeepCameraAfterDeath.Instance.PendingRewardForCameraReturn)
            {
                AddCashToRoom();
                AddMCToPlayers();
                KeepCameraAfterDeath.Instance.ClearPendingRewardForCameraReturn();
            }
        }

        return value;


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
