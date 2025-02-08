using MyceliumNetworking;
using Photon.Pun;
using System;
using System.Linq;
using UnityEngine;

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

    // This relies on camera videoID typically not being initialised on an empty camera until the first time the object is picked up by a player.
    // right now it is easy to hook in here, and if the camera is empty I add my footage.
    // if the camera ever initialises the video id on spawn (instead of on pickup) in the future, 
    // then it will be harder for me to determine which cameras are safe for me to override the footage of in the surfacenetworkhandler.
    // right now all i have to do is create a new camera and check if videoid is empty.
    private static void VideoCamera_ConfigItem(On.VideoCamera.orig_ConfigItem orig, VideoCamera self, ItemInstanceData data, PhotonView playerView)
    {
        KeepCameraAfterDeath.Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Trigger VideoCamera_ConfigItem");
        bool isEvening = TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening;

        var emptyVideoInfoOnCamera = !data.TryGetEntry<VideoInfoEntry>(out var l);
        var noValidVideoDataOnCamera = emptyVideoInfoOnCamera || l.videoID.id == Guid.Empty;

        var preservedVideoDataExists = KeepCameraAfterDeath.Instance.PreservedCameraInstanceDataCollectionForHost.Any();
        KeepCameraAfterDeath.Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] " + ((noValidVideoDataOnCamera) ? "Camera is empty" : "Camera has footage"));

        // if a camera was lost underground, and empty of footage
        if (MyceliumNetwork.IsHost 
            && preservedVideoDataExists 
            && isEvening 
            && noValidVideoDataOnCamera)
        {
            ItemInstanceData firstAvailablePreservedData = KeepCameraAfterDeath.Instance.PreservedCameraInstanceDataCollectionForHost.FirstOrDefault();
            var foundPreservedVIE = firstAvailablePreservedData.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry vie);

            var validPreservedDataExists = foundPreservedVIE && vie.videoID.id != Guid.Empty;

            if (validPreservedDataExists)
            {
                KeepCameraAfterDeath.Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Restore preserved footage with ID {vie.videoID.id} onto empty camera");

                // Restore preserved footage onto this empty camera
                data.AddDataEntry(vie);

                // Once restored, clear preserved data as we no longer need it
                KeepCameraAfterDeath.Instance.DeletePreservedCameraInstanceDataFromCollection(firstAvailablePreservedData);
            }

        }

        orig(self, data, playerView);
    }
}
