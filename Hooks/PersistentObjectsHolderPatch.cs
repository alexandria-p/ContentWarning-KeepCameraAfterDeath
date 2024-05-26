using BepInEx.Logging;
using MyceliumNetworking;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;



using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using BepInEx;
using Microsoft.CodeAnalysis;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCameraAfterDeath.Patches;

public class PersistentObjectsHolderPatch
{
    internal static void Init()
    {
        /*
         *  Subscribe with 'On.Namespace.Type.Method += CustomMethod;' for each method you're patching.
         *  Or if you are writing an ILHook, use 'IL.' instead of 'On.'
         *  Note that not all types are in a namespace, especially in Unity games.
         */

        //On.PersistentObjectsHolder.AddPersistentObject_PersistantObject += PersistentObjectsHolder_AddPersistentObject_PersistantObject;
        On.PersistentObjectsHolder.AddPersistentObject_Pickup_Item += PersistentObjectsHolder_AddPersistentObject_Pickup_Item;
        On.PersistentObjectsHolder.FindPersistantObjects += PersistentObjectsHolder_FindPersistantObjects;
    }

    // Preserve film (ItemInstanceData) from camera performing current recording when returning to surface
    private static void PersistentObjectsHolder_FindPersistantObjects(On.PersistentObjectsHolder.orig_FindPersistantObjects orig, PersistentObjectsHolder self)
    {
        KeepCameraAfterDeath.Logger.LogInfo("ALEX: looking for underground persistent objects");

        KeepCameraAfterDeath.Instance.SearchingForUndergroundPersistentObjects = true;

        orig(self);

        KeepCameraAfterDeath.Instance.SearchingForUndergroundPersistentObjects = false;
    }

    private static void PersistentObjectsHolder_AddPersistentObject_Pickup_Item(On.PersistentObjectsHolder.orig_AddPersistentObject_Pickup_Item orig, PersistentObjectsHolder self, Pickup p, Item itemToUse)
    {
        // TODO - we only recover the FIRST camera we find...if folks are using multiple camera mods, the rest of them will stay dropped & persist in the world.


        KeepCameraAfterDeath.Logger.LogInfo("ALEX: adding persistent obj");

        bool noPreservedData = KeepCameraAfterDeath.Instance.PreservedCameraInstanceData == null;

        // only if returning to surface
        // intercept if this is a camera
        if (noPreservedData && KeepCameraAfterDeath.Instance.SearchingForUndergroundPersistentObjects && p != null)
        {
            ItemInstance componentInChildren = p.GetComponentInChildren<ItemInstance>();
            if (componentInChildren.m_guid.IsSome && ItemInstanceDataHandler.TryGetInstanceData(componentInChildren.m_guid.Value, out var o))
            {
                Transform transform = p.Rigidbody.transform;
                PersistentObjectInfo persistentObjectInfo = new PersistentObjectInfo(itemToUse, transform.position, transform.rotation, o, p);

                // if this is a new persistent object we are trying to add...
                if (!self.m_PersistentObjects.Contains(persistentObjectInfo))
                {
                    if (CameraHandler.TryGetCamera(o.m_guid, out var videoCamera))
                    {
                        KeepCameraAfterDeath.Logger.LogInfo("ALEX: found camera added to persistant objs while returning to surface:");
                        KeepCameraAfterDeath.Instance.SetPreservedCameraInstanceData(o);

                        return; // early out, we don't want to leave a clone of the camera underground when we are gonna make a new one on the surface.
                    }

                }
            }
        }

        // falls through to here if we did not find a (new) camera
        orig(self, p, itemToUse);
    }

}
