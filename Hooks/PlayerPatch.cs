using MyceliumNetworking;

namespace KeepCameraAfterDeath.Patches;

public class PlayerPatch
{
    internal static void Init()
    {
        On.Player.Update += Player_Update;
    }

    // When returning from spelunking, must wait until camera.main and players exist before running rewards 
    // (or SFX that plays when UI message is shown will fail and wreak havoc) 
    // so we run the code here on Player.Update()
    private static void Player_Update(On.Player.orig_Update orig, Player self)
    {
        // See if there is a pending reward
        // (and that the player & room exist)
        if (KeepCameraAfterDeath.Instance.PendingRewardForCameraReturn != null 
            && self.IsLocal 
            && self.data.playerSetUpAndReady
            && SurfaceNetworkHandler.RoomStats != null
            && TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening)
        {
            AddCashToRoom();
            AddMCToPlayers();
            KeepCameraAfterDeath.Instance.ClearPendingRewardForCameraReturn();
        }

        orig(self);


        void AddCashToRoom()
        {
            var hostSpecifiedCashReward = KeepCameraAfterDeath.Instance.PendingRewardForCameraReturn!.Value.cash;
            if (hostSpecifiedCashReward <= 0)
            {
                return;
            }

            UserInterface.ShowMoneyNotification("Cash Received", $"${(int)hostSpecifiedCashReward}", MoneyCellUI.MoneyCellType.Revenue);

            // We only want money to be added to the room once, so let the host do it
            if (MyceliumNetwork.IsHost)
            {
                SurfaceNetworkHandler.RoomStats.AddMoney((int)hostSpecifiedCashReward);
            }
        }

        void AddMCToPlayers()
        {
            var hostSpecifiedMCReward = KeepCameraAfterDeath.Instance.PendingRewardForCameraReturn!.Value.mc;
            if (hostSpecifiedMCReward <= 0)
            {
                return;
            }
            // Client's handle adding their own MC reward, but the amount is set by the host
            MetaProgressionHandler.AddMetaCoins((int)hostSpecifiedMCReward);
        }
    }
}
