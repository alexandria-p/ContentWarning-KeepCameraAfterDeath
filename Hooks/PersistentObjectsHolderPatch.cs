using UnityEngine;

namespace KeepCameraAfterDeath.Patches;

public class PersistentObjectsHolderPatch
{
    internal static void Init()
    {
        On.PersistentObjectsHolder.AddPersistentObject_Pickup_Item += PersistentObjectsHolder_AddPersistentObject_Pickup_Item;
        On.PersistentObjectsHolder.FindPersistantObjects += PersistentObjectsHolder_FindPersistantObjects;
    }

    private static void PersistentObjectsHolder_FindPersistantObjects(On.PersistentObjectsHolder.orig_FindPersistantObjects orig, PersistentObjectsHolder self)
    {
        KeepCameraAfterDeath.Instance.SearchingForUndergroundPersistentObjects = true;
        orig(self);
        KeepCameraAfterDeath.Instance.SearchingForUndergroundPersistentObjects = false;
    }

    private static void PersistentObjectsHolder_AddPersistentObject_Pickup_Item(On.PersistentObjectsHolder.orig_AddPersistentObject_Pickup_Item orig, PersistentObjectsHolder self, Pickup p, Item itemToUse)
    {
        // Dev note: right now we only recover the FIRST camera we find...if folks are using mods to have multiple cameras in circulation, the rest of them will stay dropped & persist in the world.
        // it will not attempt to preserve footage of any camera it finds beyond the first one.

        bool cameraDataAlreadyExists = KeepCameraAfterDeath.Instance.PreservedCameraInstanceData != null;

        if (p == null || cameraDataAlreadyExists)
        {
            orig(self, p, itemToUse);
            return;
        }

        // only intercept adding persistent objects if we are doing it underground (we're not interested in persistent objects on the surface)
        if (KeepCameraAfterDeath.Instance.SearchingForUndergroundPersistentObjects)
        {
            ItemInstance componentInChildren = p.GetComponentInChildren<ItemInstance>();
            if (componentInChildren.m_guid.IsSome && ItemInstanceDataHandler.TryGetInstanceData(componentInChildren.m_guid.Value, out var o))
            {
                Transform transform = p.Rigidbody.transform;
                PersistentObjectInfo persistentObjectInfo = new PersistentObjectInfo(itemToUse, transform.position, transform.rotation, o, p);

                // if this is a new persistent object we are trying to add...(isn't already in the list)
                if (!self.m_PersistentObjects.Contains(persistentObjectInfo))
                {
                    // Then if it is a camera, intercept it!
                    if (CameraHandler.TryGetCamera(o.m_guid, out var videoCamera))
                    {
                        KeepCameraAfterDeath.Instance.SetPreservedCameraInstanceData(o);

                        return; // early out,
                        // We don't want to leave a clone of the camera underground when we are gonna make a new one on the surface.
                        // so we don't want to let the rest of the function play out and add this camera to persistent objects.
                    }

                }
            }
        }

        // falls through to here if we are not underground or if we didn't find camera to save that was dropped this round
        orig(self, p, itemToUse);
    }

}
