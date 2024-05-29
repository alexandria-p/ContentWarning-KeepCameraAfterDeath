using Photon.Pun;
using System;

namespace KeepCameraAfterDeath.Patches;

public class VideoCameraPatch
{
    internal static void Init()
    {
        On.VideoCamera.ConfigItem += VideoCamera_ConfigItem;
    }

    // runs when cameras are picked up & dropped
    private static void VideoCamera_ConfigItem(On.VideoCamera.orig_ConfigItem orig, VideoCamera self, ItemInstanceData data, PhotonView playerView)
    {
        bool isEvening = TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening;

        var emptyVideoInfoOnCamera = !data.TryGetEntry<VideoInfoEntry>(out var l);
        var noValidVideoDataOnCamera = emptyVideoInfoOnCamera || l.videoID.id == Guid.Empty;

        var preservedVideoDataExists = KeepCameraAfterDeath.Instance.PreservedCameraInstanceData != null;

        // if a camera was lost underground, and this camera is empty
        if (isEvening && noValidVideoDataOnCamera && preservedVideoDataExists)
        {
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: camera interaction - this is an EMPTY camera");
            var foundPreservedVIE = KeepCameraAfterDeath.Instance.PreservedCameraInstanceData!.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry vie);
            var validPreservedDataExists = foundPreservedVIE && vie.videoID.id != Guid.Empty;

            if (validPreservedDataExists)
            {
                KeepCameraAfterDeath.Logger.LogInfo("ALEX: restore camera footage");
                // Restore preserved footage onto this empty camera
                data.AddDataEntry(vie);

                KeepCameraAfterDeath.Logger.LogInfo("ALEX: SET CAMERA DATA video ID: " + vie.videoID.id);

                // once restored, clear preserved data as we no longer need it
                KeepCameraAfterDeath.Instance.ClearPreservedCameraInstanceData();
            }

        }

        orig(self, data, playerView);
    }
}
