namespace LethalRegeneration.patches;
using HarmonyLib;
using LethalRegeneration.config;
using GameNetcodeStuff;


[HarmonyPatch(typeof(StartOfRound))]
public class StartOfRoundPatch
{
    public static int maxHealth = 100;

    [HarmonyPatch("ResetShip")]
    [HarmonyPostfix]
    public static void ResetShip()
    {
        Configuration.Instance.HealingUpgradeUnlocked = false;
    }

    [HarmonyPatch("StartGame")]
    [HarmonyPostfix]
    public static void StartGameHpPostfix(ref PlayerControllerB ___localPlayerController)
    {
        maxHealth = ___localPlayerController.health;
    }
}