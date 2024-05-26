using BepInEx.Logging;
using MyceliumNetworking;
using Photon.Pun;
using System;

namespace KeepCameraAfterDeath.Patches;

public class VideoCameraPatch
{
    internal static void Init()
    {
        /*
         *  Subscribe with 'On.Namespace.Type.Method += CustomMethod;' for each method you're patching.
         *  Or if you are writing an ILHook, use 'IL.' instead of 'On.'
         *  Note that not all types are in a namespace, especially in Unity games.
         */

        On.VideoCamera.ConfigItem += VideoCamera_ConfigItem;
        //On.VideoCamera.OnDestroy += VideoCamera_OnDestroy;
    }

    private static void VideoCamera_ConfigItem(On.VideoCamera.orig_ConfigItem orig, VideoCamera self, ItemInstanceData data, PhotonView playerView)
    {
        // gets called every time I pick it up or throw it down

        // todo - only call this during initsurface.

        KeepCameraAfterDeath.Logger.LogInfo("ALEX: configure camera 2");

        bool isEvening = TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening;


        /*
        bool newCameraVideo = !data.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry t);

        KeepCameraAfterDeath.Logger.LogInfo("ALEX: try get: " + t?.videoID ?? "NONE");

        bool newCameraIsEmpty = t.videoID.id == Guid.Empty;

        KeepCameraAfterDeath.Logger.LogInfo("ALEX: current camera video ID is: " + t?.videoID ?? "NONE");

        KeepCameraAfterDeath.Logger.LogInfo("ALEX: current camera video ID id is: " + t?.videoID.id ?? "NONE");
        */

        var emptyVideoInfoOnCamera = !data.TryGetEntry<VideoInfoEntry>(out var l);
        var noValidVideoDataOnCamera = emptyVideoInfoOnCamera || l.videoID.id == Guid.Empty;

        if (!emptyVideoInfoOnCamera)
        {
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: current camera video info entry id is: " + l.videoID + "  :  " + l.videoID.id);
        }

        // Restore preserved footage if camera was lost Underground
        if (isEvening && noValidVideoDataOnCamera && KeepCameraAfterDeath.Instance.PreservedCameraInstanceData != null)
        {
            // logs            
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: attempt RESTORE footage to camera");

            //data = KeepCameraAfterDeath.Instance.PreservedCameraInstanceData;

            var foundPreservedVIE = KeepCameraAfterDeath.Instance.PreservedCameraInstanceData.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry vie);

            if (foundPreservedVIE)
            {
                data.AddDataEntry(vie);

                // logs
                data.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry m);
                KeepCameraAfterDeath.Logger.LogInfo("ALEX: loaded video ID is: " + m?.videoID.id ?? "NONE");


                // once restored, clear preserved.
                KeepCameraAfterDeath.Instance.ClearPreservedCameraInstanceData(); // is being cleared?
            }

        }

        // Call the Trampoline for the Original method
        orig(self, data, playerView);
        KeepCameraAfterDeath.Logger.LogInfo("ALEX: finish orig");

        if (data.TryGetEntry<VideoInfoEntry>(out var t))
        {
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: current camera video info entry id is: " + t.videoID.id);
        }
        else
        {
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: current camera video info entry id is: NONE");
        }

        KeepCameraAfterDeath.Logger.LogInfo("ALEX: end configure camera");

    }

    // i clearly am not preserving camera film in the right place.
    // i need to do it just before the scene changes?

    // Preserve film (ItemInstanceData) when camera is destroyed
    private static void VideoCamera_OnDestroy(On.VideoCamera.orig_OnDestroy orig, VideoCamera self)
    {
        /*
        if (!KeepCameraAfterDeath.Instance.ReturningToSurface)
        {
            return;
        }

        KeepCameraAfterDeath.Logger.LogInfo("ALEX: on camera destroy & returning to surface:");

        // we only want to preserve the camera footage, if we have data to preserve
        bool cameraIsNotEmpty = self.m_instanceData.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry t);

        if (cameraIsNotEmpty)
        {
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: camera is not empty");

            KeepCameraAfterDeath.Instance.SetPreservedCameraInstanceData(self.m_instanceData);
        }
        */
        orig(self);
    }
}
