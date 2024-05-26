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

        //KeepCameraAfterDeath.Instance.ReturningToSurface = true;


        orig(self, playersInside);

        //KeepCameraAfterDeath.Instance.ReturningToSurface = false;
    }

}
