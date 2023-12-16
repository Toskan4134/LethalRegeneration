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
    public int ticksPerRegeneration { get; private set; }
    public int regenerationPower { get; private set; }
    public bool regenerationOutsideShip { get; private set; }

    [NonSerialized]
    readonly ConfigFile configFile;

    public Configuration(ConfigFile cfg)
    {
        Instance = this;

        configFile = cfg;
        Instance = this;
        regenerationPower = NewEntry("Values", "Regeneration Power", defaultRegenerationPower, "Amount of life regenerated each time triggered (Between 0 an 100)");
        ticksPerRegeneration = NewEntry("Values", "Ticks Per Regeneration", defaultTicksPerRegeneration, "Number of ticks until the regeneration is triggered (1 tick equals each time the minutes of the clock are changed)");
        regenerationOutsideShip = NewEntry("Values", "Regeneration Outside Ship", defaultregenerationOutsideShip, "Whether health is regenerated also outside the ship or only inside.");
        LethalRegenerationBase.Logger.LogInfo("Configuration Set");
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
    public static void InitializeLocalPlayer()
    {
        if (IsHost)
        {
            MessageManager.RegisterNamedMessageHandler("LethalRegeneration_OnRequestConfigSync", OnRequestSync);
            Synced = true;

            return;
        }
        Synced = false;
        MessageManager.RegisterNamedMessageHandler("LethalRegeneration_OnReceiveConfigSync", OnReceiveSync);
        RequestSync();
    }
}