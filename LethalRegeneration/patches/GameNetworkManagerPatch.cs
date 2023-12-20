namespace LethalRegeneration.patches;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LethalRegeneration.config;
using LethalRegeneration.utils;
using UnityEngine;

[HarmonyPatch(typeof(GameNetworkManager))]
public class GameNetworkManagerPatch
{

    [HarmonyPatch("SaveGameValues")]
    [HarmonyPostfix]
    public static void SaveGameValuesPatch(GameNetworkManager __instance)
    {
        if (!__instance.isHostingGame) return;
        try
        {
            ES3.Save("LethalRegeneration_healingUpgradeUnlocked", Configuration.Instance.HealingUpgradeUnlocked, __instance.currentSaveFileName);
        }
        catch (Exception arg)
        {
            Debug.LogError($"Error while trying to save game values when disconnecting as host: {arg}");
        }
    }

}