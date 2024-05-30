using MyceliumNetworking;
using Photon.Pun;
using System;

namespace KeepCameraAfterDeath.Patches;

public class VideoCameraPatch
{
    internal static void Init()
    {
        On.VideoCamera.ConfigItem += VideoCamera_ConfigItem;

    }
    // triggered in several ways, 
    // one of the things that triggers this is SpawnMe -> PickupHandler.CreatePickup  -> InitItem
    // we want to catch when our mod in SNH.InitSurface creates a new camera using SpawnMe, to copy footage onto it.
    // run by the host
    private static void VideoCamera_ConfigItem(On.VideoCamera.orig_ConfigItem orig, VideoCamera self, ItemInstanceData data, PhotonView playerView)
    {
        bool isEvening = TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening;

        var emptyVideoInfoOnCamera = !data.TryGetEntry<VideoInfoEntry>(out var l);
        var noValidVideoDataOnCamera = emptyVideoInfoOnCamera || l.videoID.id == Guid.Empty;

        var preservedVideoDataExists = KeepCameraAfterDeath.Instance.PreservedCameraInstanceDataForHost != null;

        // if a camera was lost underground, and empty of footage
        if (MyceliumNetwork.IsHost 
            && preservedVideoDataExists 
            && isEvening 
            && noValidVideoDataOnCamera)
        {
            var foundPreservedVIE = KeepCameraAfterDeath.Instance.PreservedCameraInstanceDataForHost!.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry vie);
            var validPreservedDataExists = foundPreservedVIE && vie.videoID.id != Guid.Empty;

            if (validPreservedDataExists)
            {
                KeepCameraAfterDeath.Logger.LogInfo("KeepCameraAfterDeath: Restore camera footage: " + vie.videoID.id);

                // Restore preserved footage onto this empty camera
                data.AddDataEntry(vie);

                // Once restored, clear preserved data as we no longer need it
                KeepCameraAfterDeath.Instance.ClearPreservedCameraInstanceDataForHost();
            }

        }

        orig(self, data, playerView);
    }
}
