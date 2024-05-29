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
        // see if there is a pending reward
        // (and that the player & room exist)
        if (KeepCameraAfterDeath.Instance.PendingRewardForCameraReturn != null 
            && self.IsLocal 
            && self.data.playerSetUpAndReady
            && SurfaceNetworkHandler.RoomStats != null
            && TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening)
        {
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: try add reward");
            AddCashToRoom();
            AddMCToPlayers();
            KeepCameraAfterDeath.Instance.ClearPendingRewardForCameraReturn();
        }

        orig(self);


        void AddCashToRoom()
        {
            var hostSpecifiedCashReward = KeepCameraAfterDeath.Instance.PendingRewardForCameraReturn!.Value.cash;
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: pending cash reward: $"+ hostSpecifiedCashReward);
            if (hostSpecifiedCashReward <= 0)
            {
                return;
            }

            UserInterface.ShowMoneyNotification("Cash Received", $"${hostSpecifiedCashReward}", MoneyCellUI.MoneyCellType.Revenue);

            // we only want money to be added to the room once, so let the host do it
            if (MyceliumNetwork.IsHost)
            {
                KeepCameraAfterDeath.Logger.LogInfo("Awarding revenue for camera return: $" + hostSpecifiedCashReward);
                SurfaceNetworkHandler.RoomStats.AddMoney(hostSpecifiedCashReward);
            }
        }

        void AddMCToPlayers()
        {
            var hostSpecifiedMCReward = KeepCameraAfterDeath.Instance.PendingRewardForCameraReturn!.Value.mc;
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: pending mc reward: " + hostSpecifiedMCReward+ "MC");
            if (hostSpecifiedMCReward <= 0)
            {
                return;
            }
            KeepCameraAfterDeath.Logger.LogInfo("Awarding MC coins for camera return: " + hostSpecifiedMCReward);
            MetaProgressionHandler.AddMetaCoins(hostSpecifiedMCReward);
        }
    }
}
