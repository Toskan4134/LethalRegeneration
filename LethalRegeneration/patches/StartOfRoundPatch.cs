namespace LethalRegeneration.patches;
using HarmonyLib;
using LethalRegeneration.config;

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