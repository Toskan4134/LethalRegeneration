namespace LethalRegeneration.patches;

using HarmonyLib;
using GameNetcodeStuff;
using LethalRegeneration.config;

[HarmonyPatch(typeof(HUDManager))]
internal class HUDManagerPatch
{

    private static PlayerControllerB playerControllerB;
    private static int currentTicksPerRegeneration = 0;
    private static int ticksPerRegeneration => Configuration.Instance.TicksPerRegeneration;
    private static int regenerationPower => Configuration.Instance.RegenerationPower;
    private static bool regenerationOutsideShip => Configuration.Instance.RegenerationOutsideShip;
    private static bool healingUpgradeUnlocked => Configuration.Instance.HealingUpgradeUnlocked;
    private static bool healingUpgradeEnabled => Configuration.Instance.HealingUpgradeEnabled;
    private static int maxHealth => StartOfRoundPatch.maxHealth;

    [HarmonyPatch("SetClock")]
    [HarmonyPostfix]
    public static void healingPostfix()
    {

        playerControllerB = GameNetworkManager.Instance.localPlayerController;
        if (healingUpgradeEnabled && !healingUpgradeUnlocked) return;
        if (!playerControllerB.IsOwner || playerControllerB.isPlayerDead || !playerControllerB.AllowPlayerDeath() || (!regenerationOutsideShip && !playerControllerB.isInHangarShipRoom) || playerControllerB.health >= maxHealth) return;

        if (currentTicksPerRegeneration == 0)
        {
            currentTicksPerRegeneration = ticksPerRegeneration;
            LethalRegenerationBase.Logger.LogInfo("Healed " + regenerationPower);
            int regeneratedHealth = playerControllerB.health + regenerationPower;
            playerControllerB.health = regeneratedHealth > maxHealth ? maxHealth : regeneratedHealth;
            HUDManager.Instance.UpdateHealthUI(playerControllerB.health, false);
            playerControllerB.DamagePlayerClientRpc(-regenerationPower, playerControllerB.health);

            if (playerControllerB.health >= 10 && playerControllerB.criticallyInjured)
            {
                playerControllerB.MakeCriticallyInjured(false);
            }
        }
        currentTicksPerRegeneration--;
    }
}