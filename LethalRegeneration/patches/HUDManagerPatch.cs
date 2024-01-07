namespace LethalRegeneration.patches;

using HarmonyLib;
using GameNetcodeStuff;
using LethalRegeneration.config;

[HarmonyPatch(typeof(HUDManager))]
internal class HUDManagerPatch
{

    private static PlayerControllerB playerControllerB;
    private static int currentTicksPerRegeneration = 0;
    private static int currentTicksPerRegenerationOutsideShip = 0;
    private static int ticksPerRegeneration => Configuration.Instance.TicksPerRegeneration;
    private static int regenerationPower => Configuration.Instance.RegenerationPower;
    private static bool regenerationOutsideShip => Configuration.Instance.RegenerationOutsideShip;
    private static int ticksPerRegenerationOutsideShip => Configuration.Instance.TicksPerRegenerationOutsideShip;
    private static int regenerationPowerOutsideShip => Configuration.Instance.RegenerationPowerOutsideShip;
    private static bool healingUpgradeUnlocked => Configuration.Instance.HealingUpgradeUnlocked;
    private static bool healingUpgradeEnabled => Configuration.Instance.HealingUpgradeEnabled;
    private static int maxHealth => StartMatchLeverPatch.maxHealth;

    [HarmonyPatch("SetClock")]
    [HarmonyPostfix]
    public static void healingPostfix()
    {

        playerControllerB = GameNetworkManager.Instance.localPlayerController;
        if (healingUpgradeEnabled && !healingUpgradeUnlocked) return;
        if (!playerControllerB.IsOwner || playerControllerB.isPlayerDead || !playerControllerB.AllowPlayerDeath() || playerControllerB.health >= maxHealth) return;
        if (playerControllerB.isInHangarShipRoom)
        {
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
        else if (regenerationOutsideShip && !playerControllerB.isInHangarShipRoom)
        {
            if (currentTicksPerRegenerationOutsideShip == 0)
            {
                currentTicksPerRegenerationOutsideShip = ticksPerRegenerationOutsideShip;
                LethalRegenerationBase.Logger.LogInfo("Healed " + regenerationPowerOutsideShip);
                int regeneratedHealth = playerControllerB.health + regenerationPowerOutsideShip;
                playerControllerB.health = regeneratedHealth > maxHealth ? maxHealth : regeneratedHealth;
                HUDManager.Instance.UpdateHealthUI(playerControllerB.health, false);
                playerControllerB.DamagePlayerClientRpc(-regenerationPowerOutsideShip, playerControllerB.health);

                if (playerControllerB.health >= 10 && playerControllerB.criticallyInjured)
                {
                    playerControllerB.MakeCriticallyInjured(false);
                }
            }
            currentTicksPerRegenerationOutsideShip--;
        }
    }
}