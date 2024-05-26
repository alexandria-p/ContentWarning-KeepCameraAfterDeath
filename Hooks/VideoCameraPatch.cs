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

        KeepCameraAfterDeath.Logger.LogInfo("ALEX: configure new camera");

        bool isEvening = TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening;
        bool newCameraIsEmpty = !data.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry t);

        // Restore preserved footage if camera was lost Underground
        if (isEvening && newCameraIsEmpty && KeepCameraAfterDeath.Instance.PreservedCameraInstanceData != null)
        {
            // TODO - is KeepCameraAfterDeath.Instance.PreservedCameraInstanceData check failing .....

            KeepCameraAfterDeath.Logger.LogInfo("ALEX: RESTORE footage to camera");
            data = KeepCameraAfterDeath.Instance.PreservedCameraInstanceData;

            // logs
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: preserved video ID is: " + t?.videoID ?? "NONE");
            data.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry l);
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: loading video ID is: " + l?.videoID ?? "NONE");
        }

        // Call the Trampoline for the Original method
        orig(self, data, playerView);
    }


    /*
    // i clearly am not preserving camera film in the right place.
    // i need to do it just before the scene changes?

    // Preserve film (ItemInstanceData) when camera is destroyed
    private static void VideoCamera_OnDestroy(On.VideoCamera.orig_OnDestroy orig, VideoCamera self)
    {
        KeepCameraAfterDeath.Logger.LogInfo("ALEX: on camera destroy");

        // we only want to preserve the camera footage, if we have data to preserve
        bool cameraIsNotEmpty = self.m_instanceData.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry t);

        if (cameraIsNotEmpty)
        {
            KeepCameraAfterDeath.Instance.SetPreservedCameraInstanceData(self.m_instanceData);
        }
        
        orig(self);
    }
    */
}
