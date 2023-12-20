namespace LethalRegeneration.config;

using BepInEx.Configuration;
using Unity.Netcode;
using Unity.Collections;
using System;
using HarmonyLib;
using GameNetcodeStuff;

[Serializable]
public class Configuration : ConfigurationSync<Configuration>
{
    public static readonly int defaultTicksPerRegeneration = 2;
    public static readonly int defaultRegenerationPower = 5;
    public static readonly bool defaultregenerationOutsideShip = false;
    public static readonly bool defaultHealingUpgradeEnabled = false;
    public static readonly int defaultHealingUpgradePrice = 800;
    public bool healingUpgradeUnlocked { get; set; }
    public int ticksPerRegeneration { get; private set; }
    public int regenerationPower { get; private set; }
    public bool regenerationOutsideShip { get; private set; }
    public bool healingUpgradeEnabled { get; private set; }
    public int healingUpgradePrice { get; private set; }

    [NonSerialized]
    readonly ConfigFile configFile;

    public Configuration(ConfigFile cfg)
    {
        Instance = this;

        configFile = cfg;
        Instance = this;
        InitConfigEntries();
        LethalRegenerationBase.Logger.LogInfo("Configuration Set");
    }

    private void InitConfigEntries()
    {
        regenerationPower = NewEntry("Values", "Regeneration Power", defaultRegenerationPower, "Amount of life regenerated each time triggered (Between 0 an 100)");
        ticksPerRegeneration = NewEntry("Values", "Ticks Per Regeneration", defaultTicksPerRegeneration, "Number of ticks until the regeneration is triggered (1 tick equals each time the minutes of the clock are changed)");
        regenerationOutsideShip = NewEntry("Values", "Regeneration Outside Ship", defaultregenerationOutsideShip, "Whether health is regenerated also outside the ship or only inside.");
        healingUpgradeEnabled = NewEntry("Values", "Regeneration As Upgrade", defaultHealingUpgradeEnabled, "Makes natural health regeneration an upgrade for the ship and has to be purchased to make it work.");
        healingUpgradePrice = NewEntry("Values", "Upgrade Price", defaultHealingUpgradePrice, "Changes the price of ship upgrade for health regeneration. Only works if ship upgrade is enabled");

    }
    private T NewEntry<T>(string category, string key, T defaultVal, string desc)
    {
        return configFile.Bind(category, key, defaultVal, desc).Value;
    }

    public static void RequestSync()
    {
        LethalRegenerationBase.Logger.LogInfo("RequestSync");
        if (!IsClient) return;
        using FastBufferWriter stream = new(4, Allocator.Temp);
        MessageManager.SendNamedMessage("LethalRegeneration_OnRequestConfigSync", 0uL, stream);
    }
    public static void OnRequestSync(ulong clientId, FastBufferReader _)
    {
        LethalRegenerationBase.Logger.LogInfo("OnRequestSync: " + IsClient);

        if (!IsHost) return;

        LethalRegenerationBase.Logger.LogInfo($"Config sync request received from client: {clientId}");

        byte[] array = SerializeToBytes(Instance);
        int value = array.Length;

        using FastBufferWriter stream = new(array.Length + 4, Allocator.Temp);

        try
        {
            stream.WriteValueSafe(in value, default);
            stream.WriteBytesSafe(array);

            MessageManager.SendNamedMessage("LethalRegeneration_OnReceiveConfigSync", clientId, stream);
        }
        catch (Exception e)
        {
            LethalRegenerationBase.Logger.LogInfo($"Error occurred syncing config with client: {clientId}\n{e}");
        }
    }

    public static void OnReceiveSync(ulong _, FastBufferReader reader)
    {

        if (!reader.TryBeginRead(4))
        {
            LethalRegenerationBase.Logger.LogError("Config sync error: Could not begin reading buffer.");
            return;
        }

        reader.ReadValueSafe(out int val, default);
        if (!reader.TryBeginRead(val))
        {
            LethalRegenerationBase.Logger.LogError("Config sync error: Host could not sync.");
            return;
        }

        byte[] data = new byte[val];
        reader.ReadBytesSafe(ref data, val);

        UpdateInstance(data);

        LethalRegenerationBase.Logger.LogInfo("Successfully synced config with host.");
    }
    public void SendHealingUpgradeStatusToHost()
    {
        LethalRegenerationBase.Logger.LogInfo($"IsHost: {IsHost}, IsClient: {IsClient}, SendHealingUpgradeStatusToHost");
        if (IsClient)
        {
            using FastBufferWriter stream = new(4, Allocator.Temp);
            stream.WriteValueSafe(healingUpgradeUnlocked ? 1 : 0, default);
            MessageManager.SendNamedMessage("LethalRegeneration_SendHealingUpgradeStatus", 0uL, stream);
        }
    }

    public static void OnReceiveHealingUpgradeStatus(ulong _, FastBufferReader reader)
    {
        LethalRegenerationBase.Logger.LogInfo($"IsHost: {IsHost}, IsClient: {IsClient}, OnReceiveHealingUpgradeStatus");
        if (IsHost)
        {
            if (reader.TryBeginRead(4))
            {
                reader.ReadValueSafe(out int status, default);
                Instance.healingUpgradeUnlocked = status == 1;
                LethalRegenerationBase.Logger.LogInfo($"Received healingUpgradeUnlocked status: {Instance.healingUpgradeUnlocked}");

                // Ahora, después de actualizar el valor en el host, broadcaste a todos los clientes
                Instance.BroadcastHealingUpgradeStatusToClients();
            }
            else
            {
                LethalRegenerationBase.Logger.LogError("Error reading healingUpgradeUnlocked status.");
            }
        }
    }

    public void BroadcastHealingUpgradeStatusToClients()
    {
        LethalRegenerationBase.Logger.LogInfo($"IsHost: {IsHost}, IsClient: {IsClient}, BroadcastHealingUpgradeStatusToClients");
        if (IsHost)
        {

            using FastBufferWriter stream = new(4, Allocator.Temp);
            stream.WriteValueSafe(healingUpgradeUnlocked ? 1 : 0, default);
            MessageManager.SendNamedMessage("LethalRegeneration_BroadcastHealingUpgradeStatus", 0uL, stream);
        }
    }

    public static void OnReceiveBroadcastedHealingUpgradeStatus(ulong _, FastBufferReader reader)
    {
        LethalRegenerationBase.Logger.LogInfo($"IsHost: {IsHost}, IsClient: {IsClient}, OnReceiveBroadcastedHealingUpgradeStatus");
        if (IsClient && !IsHost)
        {
            if (reader.TryBeginRead(4))
            {
                reader.ReadValueSafe(out int status, default);
                Instance.healingUpgradeUnlocked = status == 1;
                LethalRegenerationBase.Logger.LogInfo($"Received broadcasted healingUpgradeUnlocked status: {Instance.healingUpgradeUnlocked}");
            }
            else
            {
                LethalRegenerationBase.Logger.LogError("Error reading broadcasted healingUpgradeUnlocked status.");
            }
        }
    }




    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
    public static void InitializeLocalPlayer()
    {
        Instance.healingUpgradeUnlocked = false;
        MessageManager.RegisterNamedMessageHandler("LethalRegeneration_BroadcastHealingUpgradeStatus", OnReceiveBroadcastedHealingUpgradeStatus);
        if (IsHost)
        {
            MessageManager.RegisterNamedMessageHandler("LethalRegeneration_OnRequestConfigSync", OnRequestSync);
            MessageManager.RegisterNamedMessageHandler("LethalRegeneration_SendHealingUpgradeStatus", OnReceiveHealingUpgradeStatus);

            Synced = true;
            Instance.healingUpgradeUnlocked = ES3.Load("LethalRegeneration_healingUpgradeUnlocked", GameNetworkManager.Instance.currentSaveFileName, false);
            LethalRegenerationBase.Logger.LogInfo("HealingUpgrade Loaded as " + Instance.healingUpgradeUnlocked);
            return;
        }
        MessageManager.RegisterNamedMessageHandler("LethalRegeneration_OnReceiveConfigSync", OnReceiveSync);
        Synced = false;
        RequestSync();
    }
}