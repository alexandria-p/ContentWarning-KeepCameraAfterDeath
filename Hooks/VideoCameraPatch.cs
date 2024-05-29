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

    /*
     * 
    // called by SpawnMe in SurfaceNetworkHandler
    // will be triggered by self.m_VideoCameraSpawner.SpawnMe(force: true);in SNH.InitSurface...so it will already be evening
    // check if this is a camera type && then run this logic
    // and just let the host-only run this....as the instancedata will be sent via RPC to all players anyway.
    private static Pickup PickupHandler_CreatePickup_byte_ItemInstanceData_Vector3_Quaternion(On.PickupHandler.orig_CreatePickup_byte_ItemInstanceData_Vector3_Quaternion orig, byte itemID, ItemInstanceData data, Vector3 pos, Quaternion rot)
    { 
        // If there is preserved footage to restore, and this is the host:
        if (MyceliumNetwork.IsHost
            && KeepCameraAfterDeath.Instance.PreservedCameraInstanceDataForHost != null
            && ItemDatabase.TryGetItemFromID(itemID, out var item))
        {
            
            // And if it is a new camera, intercept it!
            if (item.itemType == Item.ItemType.Camera)
            {
                KeepCameraAfterDeath.Logger.LogInfo("ALEX: camera interaction - this is an EMPTY camera");
                var foundPreservedVIE = KeepCameraAfterDeath.Instance.PreservedCameraInstanceDataForHost!.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry vie);
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

        }

        return orig(itemID, data, pos, rot);
}

// TODO - move to PickupHandler.CreatePickup 
// will be triggered by self.m_VideoCameraSpawner.SpawnMe(force: true);in SNH.InitSurface...so it will already be evening
// check if this is a camera type && then run this logic
// and just let the host-only run this....as the instancedata will be sent via RPC to all players anyway.

*/
    // triggered in several ways, 
    // one of the things that triggers this is SpawnMe -> PickupHandler.CreatePickup  -> InitItem
    private static void VideoCamera_ConfigItem(On.VideoCamera.orig_ConfigItem orig, VideoCamera self, ItemInstanceData data, PhotonView playerView)
    {
        // todo - if only the host runs this code,
        // do the clients still get the footage?


        KeepCameraAfterDeath.Logger.LogInfo("ALEX: camera CONFIG ITEM call");
        bool isEvening = TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening;

        var emptyVideoInfoOnCamera = !data.TryGetEntry<VideoInfoEntry>(out var l);
        var noValidVideoDataOnCamera = emptyVideoInfoOnCamera || l.videoID.id == Guid.Empty;

        var preservedVideoDataExists = KeepCameraAfterDeath.Instance.PreservedCameraInstanceDataForHost != null;

        // todo - check that this camera is newly spawned ?
        // we want to catch when our mod in SNH.InitSurface creates a new camera, to copy footage onto it.

        // there's currently a chance that if there is already an empty camera, that footage gets copied onto that one instead of the newly spawned one (....does this really matter though? the mod does its job)

        KeepCameraAfterDeath.Logger.LogInfo("ALEX: camera interaction - did I find the camera was empty?: " + noValidVideoDataOnCamera);


        // if a camera was lost underground, and this camera is unregistered (so is newly spawned) and empty of footage
        if (MyceliumNetwork.IsHost 
            && preservedVideoDataExists 
            && isEvening 
            && noValidVideoDataOnCamera)
        {
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: camera interaction - this is an EMPTY camera");
            var foundPreservedVIE = KeepCameraAfterDeath.Instance.PreservedCameraInstanceDataForHost!.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry vie);
            var validPreservedDataExists = foundPreservedVIE && vie.videoID.id != Guid.Empty;

            if (validPreservedDataExists)
            {
                KeepCameraAfterDeath.Logger.LogInfo("ALEX: restore camera footage");
                // Restore preserved footage onto this empty camera

                // TODO - does this do it for all clients?
                data.AddDataEntry(vie);

                KeepCameraAfterDeath.Logger.LogInfo("ALEX: SET CAMERA DATA video ID: " + vie.videoID.id);

                // once restored, clear preserved data as we no longer need it
                KeepCameraAfterDeath.Instance.ClearPreservedCameraInstanceData();
            }

        }

        orig(self, data, playerView);
    }
}
