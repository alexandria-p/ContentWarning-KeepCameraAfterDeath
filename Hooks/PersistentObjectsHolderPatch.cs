using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KeepCameraAfterDeath.Patches;

public class PersistentObjectsHolderPatch
{
    internal static void Init()
    {
        On.PersistentObjectsHolder.FindPersistantObjects += PersistentObjectsHolder_FindPersistantObjects;
    }

    private static void PersistentObjectsHolder_FindPersistantObjects(On.PersistentObjectsHolder.orig_FindPersistantObjects orig, PersistentObjectsHolder self)
    {
        var existingCamerasUnderground = FindVideoCamerasInSet(self.m_PersistentObjects);
        KeepCameraAfterDeath.Logger.LogInfo("ALEX: search for cameras underground");
        orig(self);

        var numObjects = self.m_PersistentObjects.Count;


        for (int i = numObjects - 1; i >= 0; i--)
        {
            PersistentObjectInfo item = self.m_PersistentObjects[i];
            var objectInstanceData = item.InstanceData;
            var objectGuid = objectInstanceData.m_guid;

            // Then if it is a dropped camera, intercept it!
            if (CameraHandler.TryGetCamera(objectGuid, out var videoCamera))
            {
                // if it was already underground, skip
                if (existingCamerasUnderground.Contains(videoCamera))
                {
                    continue;
                }

                KeepCameraAfterDeath.Logger.LogInfo("ALEX: found a camera");
                KeepCameraAfterDeath.Instance.SetPreservedCameraInstanceData(objectInstanceData);

                // We don't want to leave a clone of the camera underground when we are gonna make a new one on the surface.
                // so we want to remove this camera from persistent objects.
                if (self.m_PersistentObjectDic.ContainsKey(item.Pickup))
                {
                    self.m_PersistentObjectDic.Remove(item.Pickup);
                }

                self.m_PersistentObjects.Remove(item);

                return; // break here, don't look for more cameras.
            }
        }
    }

    private static List<VideoCamera> FindVideoCamerasInSet(List<PersistentObjectInfo> persistantObjects)
    {
        var list = new List<VideoCamera>();

        foreach (var item in persistantObjects)
        {
            var objectInstanceData = item.InstanceData;
            var objectGuid = objectInstanceData.m_guid;

            if (CameraHandler.TryGetCamera(objectGuid, out var videoCamera))
            {
                list.Add(videoCamera);
            }
        }

        return list;
    }
}
