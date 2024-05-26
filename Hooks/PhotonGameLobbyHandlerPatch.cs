using BepInEx.Logging;
using MyceliumNetworking;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KeepCameraAfterDeath.Patches;

public class PhotonGameLobbyHandlerPatch
{
    internal static void Init()
    {
        /*
         *  Subscribe with 'On.Namespace.Type.Method += CustomMethod;' for each method you're patching.
         *  Or if you are writing an ILHook, use 'IL.' instead of 'On.'
         *  Note that not all types are in a namespace, especially in Unity games.
         */

        On.PhotonGameLobbyHandler.ReturnToSurface += PhotonGameLobbyHandler_ReturnToSurface;
    }

    // Preserve film (ItemInstanceData) from camera performing current recording when returning to surface
    private static void PhotonGameLobbyHandler_ReturnToSurface(On.PhotonGameLobbyHandler.orig_ReturnToSurface orig, PhotonGameLobbyHandler self, ICollection<Player> playersInside)
    {
        KeepCameraAfterDeath.Logger.LogInfo("ALEX: on return to surface");

        var currentRecording = RecordingsHandler.GetCamerasCurrentRecording();

        if (currentRecording != null)
        {
            KeepCameraAfterDeath.Logger.LogInfo("ALEX: found recording");
            var currentRecordingKeys = currentRecording.GetKeys();
            if (currentRecordingKeys != null)
            {
                KeepCameraAfterDeath.Logger.LogInfo("ALEX: has " + currentRecordingKeys.Count() + "keys");

                // search keys for a camera guid
                foreach (Guid key in currentRecordingKeys)
                {
                    if (!ItemInstanceDataHandler.TryGetInstanceData(key, out var o))
                    {
                        continue;
                    }

                    KeepCameraAfterDeath.Logger.LogInfo("ALEX: found instance data");

                    // see if instance data belongs to a camera
                    if (CameraHandler.TryGetCamera(key, out VideoCamera videoCamera)) // o.m_guid
                    {
                        KeepCameraAfterDeath.Logger.LogInfo("ALEX: found a camera");
                        // get VideoInfoEntry from Camera Instance Data
                        bool cameraIsNotEmpty = o.TryGetEntry<VideoInfoEntry>(out VideoInfoEntry t);

                        if (cameraIsNotEmpty)
                        {
                            KeepCameraAfterDeath.Logger.LogInfo("ALEX: camera is not empty");
                            KeepCameraAfterDeath.Instance.SetPreservedCameraInstanceData(o);
                        }
                        break;
                    }
                }
            }
        }

        orig(self, playersInside);
    }

}
