namespace LethalRegeneration.patches;

using HarmonyLib;
using GameNetcodeStuff;
using LethalRegeneration.config;

[HarmonyPatch(typeof(HUDManager))]
internal class HUDManagerPatch
{

    private static readonly PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
    private static int currentTicksPerRegeneration = 0;
    private static readonly int ticksPerRegeneration = ConfigurationSync.instance.ticksPerRegeneration;
    private static readonly int regenerationPower = ConfigurationSync.instance.regenerationPower;
    private static readonly bool regenerationOutsideShip = ConfigurationSync.instance.regenerationOutsideShip;

    [HarmonyPatch("SetClock")]
    [HarmonyPostfix]
    public static void healingPostfix()
    {
        if (!playerControllerB.IsOwner || playerControllerB.isPlayerDead || !playerControllerB.AllowPlayerDeath() || (!regenerationOutsideShip && !playerControllerB.isInHangarShipRoom) || playerControllerB.health >= 100) return;

        if (currentTicksPerRegeneration == 0)
        {
            currentTicksPerRegeneration = ticksPerRegeneration;
            LethalRegenerationBase.mls.LogInfo("Healed " + regenerationPower);
            int regeneratedHealth = playerControllerB.health + regenerationPower;
            playerControllerB.health = regeneratedHealth > 100 ? 100 : regeneratedHealth;
            HUDManager.Instance.UpdateHealthUI(playerControllerB.health, false);
            if (playerControllerB.IsServer)
            {
                playerControllerB.DamagePlayerClientRpc(-regenerationPower, playerControllerB.health);
            }
            else
            {
                playerControllerB.DamagePlayerServerRpc(-regenerationPower, playerControllerB.health);
            }
            if (playerControllerB.health >= 10 && playerControllerB.criticallyInjured)
            {
                playerControllerB.MakeCriticallyInjured(false);
            }
        }
        currentTicksPerRegeneration--;
    }
}