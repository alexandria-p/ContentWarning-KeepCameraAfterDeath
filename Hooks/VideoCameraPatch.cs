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

    private static void VideoCamera_ConfigItem(On.VideoCamera.orig_ConfigItem orig, VideoCamera self, ItemInstanceData data, PhotonView playerView)
    {
        bool isEvening = TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening;

        var emptyVideoInfoOnCamera = !data.TryGetEntry<VideoInfoEntry>(out var l);
        var noValidVideoDataOnCamera = emptyVideoInfoOnCamera || l.videoID.id == Guid.Empty;

        var preservedVideoDataExists = KeepCameraAfterDeath.Instance.PreservedCameraInstanceData != null;

        // Restore preserved footage if camera was lost Underground
        if (isEvening && noValidVideoDataOnCamera && preservedVideoDataExists)
        {
            var foundPreservedVIE = KeepCameraAfterDeath.Instance.PreservedCameraInstanceData.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry vie);
            var validPreservedDataExists = foundPreservedVIE && vie.videoID.id != Guid.Empty;

            if (validPreservedDataExists)
            {
                data.AddDataEntry(vie);

                // once restored, clear preserved data as we no longer need it
                KeepCameraAfterDeath.Instance.ClearPreservedCameraInstanceData();
            }

        }

        orig(self, data, playerView);
    }
}
