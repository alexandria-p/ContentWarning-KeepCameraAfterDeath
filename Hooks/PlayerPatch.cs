using MyceliumNetworking;
using System.Collections;

namespace KeepCameraAfterDeath.Patches;

public class PlayerPatch
{
    internal static void Init()
    {
        On.Player.Start += Player_Start;
    }

    // When returning from spelunking, must wait until camera.main and players exist before running rewards 
    // (or SFX that plays when UI message is shown will fail and wreak havoc) 
    // so we run the code here on Player.Start()
    private static IEnumerator Player_Start(On.Player.orig_Start orig, Player self)
    {
        var returnValue = orig(self);

        if (self.IsLocal)
        {
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: local player spawn");
            // if we respawn after spelunking underground,
            // check if there is a pending reward for our safe return of the camera
            if (TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening
                && KeepCameraAfterDeath.Instance.PendingRewardForCameraReturn != null)
            {
                KeepCameraAfterDeath.Logger.LogInfo("ALEX: try add reward");
                AddCashToRoom();
                AddMCToPlayers();
                KeepCameraAfterDeath.Instance.ClearPendingRewardForCameraReturn();
            }
        }

        return returnValue;


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
