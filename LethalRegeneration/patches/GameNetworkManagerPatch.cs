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
        LethalRegenerationBase.Logger.LogInfo("SaveGameValuesPatch GameNetwork");
        if (!__instance.isHostingGame) return;
        try
        {
            ES3.Save("LethalRegeneration_healingUpgradeUnlocked", Configuration.Instance.healingUpgradeUnlocked, __instance.currentSaveFileName);
            // LethalRegenerationBase.Logger.LogInfo("HealingUpgrade Saved as " + Configuration.Instance.healingUpgradeUnlocked);
        }
        catch (Exception arg)
        {
            Debug.LogError($"Error while trying to save game values when disconnecting as host: {arg}");
        }
    }

}