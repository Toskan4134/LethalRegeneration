namespace LethalRegeneration.patches;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LethalRegeneration.config;
using LethalRegeneration.utils;
using UnityEngine;

[HarmonyPatch(typeof(StartOfRound))]
public class StartOfRoundPatch
{

    [HarmonyPatch("ResetShip")]
    [HarmonyPostfix]
    public static void ResetShip()
    {
        Configuration.Instance.HealingUpgradeUnlocked = false;
    }
}