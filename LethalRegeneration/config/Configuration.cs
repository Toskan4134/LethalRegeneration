namespace LethalRegeneration.config;

using BepInEx.Configuration;
using Unity.Netcode;
using Unity.Collections;
using System;
using HarmonyLib;
using GameNetcodeStuff;
using System.Reflection;

[Serializable]
public class Configuration : ConfigurationSync<Configuration>
{
    public const int DefaultTicksPerRegeneration = 2;
    public const int DefaultRegenerationPower = 5;
    public const bool DefaultregenerationOutsideShip = false;
    public const bool DefaultHealingUpgradeEnabled = false;
    public const int DefaultHealingUpgradePrice = 800;
    public bool HealingUpgradeUnlocked { get; set; }
    public int TicksPerRegeneration { get; private set; }
    public int RegenerationPower { get; private set; }
    public bool RegenerationOutsideShip { get; private set; }
    public bool HealingUpgradeEnabled { get; private set; }
    public int HealingUpgradePrice { get; private set; }

    [NonSerialized]
    readonly ConfigFile configFile;

    public Configuration(ConfigFile cfg)
    {
        Instance = this;

        configFile = cfg;
        InitConfigEntries();
        LethalRegenerationBase.Logger.LogInfo("Configuration Set");
    }

    private void InitConfigEntries()
    {
        RegenerationPower = NewEntry("Values", "Regeneration Power", DefaultRegenerationPower, "Amount of life regenerated each time triggered (Between 0 an 100)");
        TicksPerRegeneration = NewEntry("Values", "Ticks Per Regeneration", DefaultTicksPerRegeneration, "Number of ticks until the regeneration is triggered (1 tick equals each time the minutes of the clock are changed)");
        RegenerationOutsideShip = NewEntry("Values", "Regeneration Outside Ship", DefaultregenerationOutsideShip, "Whether health is regenerated also outside the ship or only inside.");
        HealingUpgradeEnabled = NewEntry("Values", "Regeneration As Upgrade", DefaultHealingUpgradeEnabled, "Makes natural health regeneration an upgrade for the ship and has to be purchased to make it work.");
        HealingUpgradePrice = NewEntry("Values", "Upgrade Price", DefaultHealingUpgradePrice, "Changes the price of ship upgrade for health regeneration. Only works if ship upgrade is enabled");

    }
    private T NewEntry<T>(string category, string key, T defaultVal, string desc)
    {
        return configFile.Bind(category, key, defaultVal, desc).Value;
    }

    public static void RequestSync()
    {
        if (!IsClient) return;
        using FastBufferWriter stream = new(4, Allocator.Temp);
        MessageManager.SendNamedMessage("LethalRegeneration_OnRequestConfigSync", 0uL, stream);
    }
    public static void OnRequestSync(ulong clientId, FastBufferReader _)
    {
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
        if (IsClient)
        {
            using FastBufferWriter stream = new(4, Allocator.Temp);
            stream.WriteValueSafe(HealingUpgradeUnlocked ? 1 : 0, default);
            MessageManager.SendNamedMessage("LethalRegeneration_SendHealingUpgradeStatus", 0uL, stream);
        }
    }

    public static void OnReceiveHealingUpgradeStatus(ulong _, FastBufferReader reader)
    {
        if (IsHost)
        {
            if (reader.TryBeginRead(4))
            {
                reader.ReadValueSafe(out int status, default);
                Instance.HealingUpgradeUnlocked = status == 1;
                LethalRegenerationBase.Logger.LogInfo($"Received sent HealingUpgrade status: {Instance.HealingUpgradeUnlocked}");
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
        if (IsHost)
        {
            using (FastBufferWriter stream = new(4, Allocator.Temp))
            {
                stream.WriteValueSafe(HealingUpgradeUnlocked ? 1 : 0, default);
                foreach (var clientId in NetworkManager.Singleton.ConnectedClients.Keys)
                {
                    MessageManager.SendNamedMessage("LethalRegeneration_BroadcastHealingUpgradeStatus", clientId, stream);
                }
            }
        }
    }

    public static void OnReceiveBroadcastedHealingUpgradeStatus(ulong _, FastBufferReader reader)
    {
        if (IsClient && !IsHost)
        {
            if (reader.TryBeginRead(4))
            {
                reader.ReadValueSafe(out int status, default);
                Instance.HealingUpgradeUnlocked = status == 1;
                LethalRegenerationBase.Logger.LogInfo($"Received broadcasted healingUpgradeUnlocked status: {Instance.HealingUpgradeUnlocked}");
                Synced = true;
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
        Instance.HealingUpgradeUnlocked = false;
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("LethalRegeneration_BroadcastHealingUpgradeStatus", OnReceiveBroadcastedHealingUpgradeStatus);

        if (IsHost)
        {
            MessageManager.RegisterNamedMessageHandler("LethalRegeneration_OnRequestConfigSync", OnRequestSync);
            MessageManager.RegisterNamedMessageHandler("LethalRegeneration_SendHealingUpgradeStatus", OnReceiveHealingUpgradeStatus);

            Synced = true;
            Instance.HealingUpgradeUnlocked = ES3.Load("LethalRegeneration_healingUpgradeUnlocked", GameNetworkManager.Instance.currentSaveFileName, false);
            return;
        }
        MessageManager.RegisterNamedMessageHandler("LethalRegeneration_OnReceiveConfigSync", OnReceiveSync);

        Synced = false;
        RequestSync();
    }
}