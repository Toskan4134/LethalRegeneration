namespace LethalRegeneration.patches;
using HarmonyLib;
using LethalRegeneration.config;
using GameNetcodeStuff;


[HarmonyPatch(typeof(StartMatchLever))]
public class StartMatchLeverPatch
{
    public static int maxHealth = 100;

    [HarmonyPatch("PlayLeverPullEffectsClientRpc")]
    [HarmonyPostfix]
    public static void StartGameHpPostfix(ref StartOfRound ___playersManager, ref bool ___leverHasBeenPulled)
    {
        if (___leverHasBeenPulled)
        {
            maxHealth = ___playersManager.localPlayerController.health;
        }
    }
}