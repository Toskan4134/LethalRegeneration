namespace LethalRegeneration.config;

using System;
using System.IO;
using System.Text.Json;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Collections;
using Unity.Netcode;

[Serializable]
[HarmonyPatch]
public class ConfigurationSync
{
    public static ConfigurationSync defaultConfig;
    public static ConfigurationSync instance;
    public static PlayerControllerB localPlayerController;
    public static bool isSynced = false;
    public int regenerationPower;
    public int ticksPerRegeneration;
    public bool regenerationOutsideShip;
    public static void BuildDefaultConfig()
    {
        if (defaultConfig == null)
        {
            defaultConfig = new ConfigurationSync
            {
                ticksPerRegeneration = Configuration.ticksPerRegeneration.Value,
                regenerationPower = Configuration.regenerationPower.Value,
                regenerationOutsideShip = Configuration.regenerationOutsideShip.Value
            };
            instance = defaultConfig;
        }
        LethalRegenerationBase.mls.LogInfo("ConfigurationSync Built");
    }
    [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
    [HarmonyPostfix]
    public static void InitializeLocalPlayer(PlayerControllerB __instance)
    {
        localPlayerController = __instance;
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("BetterStaminaOnRequestConfigSync", OnReceiveConfigSyncRequest);
            OnLocalClientConfigSync();
        }
        else
        {
            isSynced = false;
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("BetterStaminaOnReceiveConfigSync", OnReceiveConfigSync);
            RequestConfigSync();
        }
    }

    public static void RequestConfigSync()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            LethalRegenerationBase.mls.LogInfo("Sending config sync request to server.");
            FastBufferWriter messageStream = new FastBufferWriter(4, Allocator.Temp);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("BetterStaminaOnRequestConfigSync", 0uL, messageStream);
        }
        else
        {
            LethalRegenerationBase.mls.LogError("Failed to send config sync request.");
        }
    }

    public static void OnReceiveConfigSyncRequest(ulong clientId, FastBufferReader reader)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            LethalRegenerationBase.mls.LogInfo("Receiving config sync request from client with id: " + clientId + ". Sending config sync to client.");
            byte[] array = SerializeConfigToByteArray(instance);
            FastBufferWriter messageStream = new FastBufferWriter(array.Length + 4, Allocator.Temp);
            int value = array.Length;
            messageStream.WriteValueSafe(in value, default);
            messageStream.WriteBytesSafe(array);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("BetterStaminaOnReceiveConfigSync", clientId, messageStream);
        }
    }

    public static void OnReceiveConfigSync(ulong clientId, FastBufferReader reader)
    {
        if (reader.TryBeginRead(4))
        {
            reader.ReadValueSafe(out int value, default);
            if (reader.TryBeginRead(value))
            {
                LethalRegenerationBase.mls.LogInfo("Receiving config sync from server.");
                byte[] value2 = new byte[value];
                reader.ReadBytesSafe(ref value2, value);
                instance = DeserializeFromByteArray(value2);
                OnLocalClientConfigSync();
            }
            else
            {
                LethalRegenerationBase.mls.LogError("Error receiving sync from server.");
            }
        }
        else
        {
            LethalRegenerationBase.mls.LogError("Error receiving bytes length.");
        }
    }

    public static void OnLocalClientConfigSync()
    {
        isSynced = true;
    }

    public static byte[] SerializeConfigToByteArray(ConfigurationSync config)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            JsonSerializer.Serialize(memoryStream, config, options);
            return memoryStream.ToArray();
        }
    }

    public static ConfigurationSync DeserializeFromByteArray(byte[] data)
    {
        using (MemoryStream memoryStream = new MemoryStream(data))
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            return JsonSerializer.Deserialize<ConfigurationSync>(memoryStream, options);
        }
    }
}