namespace LethalRegeneration.config;

using System;
using System.IO;
using System.Text;
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
    private const string RequestMessageName = "LethalRegenerationOnRequestConfigSync";
    private const string ReceiveMessageName = "LethalRegenerationOnRecieveConfigSync";

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
        LethalRegenerationBase.mls.LogInfo("Jugador Inicializado --------------------");
        localPlayerController = __instance;
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(RequestMessageName, OnReceiveConfigSyncRequest);
            OnLocalClientConfigSync();
        }
        else
        {
            isSynced = false;
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(ReceiveMessageName, OnReceiveConfigSync);
            RequestConfigSync();
        }
    }

    public static void RequestConfigSync()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            LethalRegenerationBase.mls.LogInfo("Sending config sync request to server.");
            using (FastBufferWriter messageStream = new FastBufferWriter(4, Allocator.Temp))
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(RequestMessageName, 0uL, messageStream);
            }
        }
        else
        {
            LethalRegenerationBase.mls.LogWarning("Failed to send config sync request.");
        }
    }



    public static void OnReceiveConfigSyncRequest(ulong clientId, FastBufferReader reader)
    {
        try
        {
            if (NetworkManager.Singleton.IsServer)
            {
                LethalRegenerationBase.mls.LogInfo($"Receiving config sync request from client with id: {clientId}. Sending config sync to client.");
                byte[] array = SerializeConfigToByteArray(instance);
                using (FastBufferWriter messageStream = new FastBufferWriter(array.Length + 4, Allocator.Temp))
                {
                    messageStream.WriteValueSafe(array.Length, default);
                    messageStream.WriteBytesSafe(array);
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(ReceiveMessageName, clientId, messageStream);
                }
            }
        }
        catch (Exception ex)
        {
            LethalRegenerationBase.mls.LogError($"Error handling config sync request: {ex.Message}");
        }
    }

    public static void OnReceiveConfigSync(ulong clientId, FastBufferReader reader)
    {
        try
        {
            if (reader.TryBeginRead(4))
            {
                reader.ReadValueSafe(out int value, default);

                LethalRegenerationBase.mls.LogInfo("Receiving config sync from server.");
                if (reader.TryBeginRead(value))
                {
                    byte[] value2 = new byte[value];
                    reader.ReadBytes(ref value2, value);
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
        catch (Exception ex)
        {
            LethalRegenerationBase.mls.LogError($"Error handling config sync: {ex.Message}");
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

            using (Utf8JsonWriter jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true }))
            {
                JsonSerializer.Serialize(jsonWriter, config, options);
            }

            LethalRegenerationBase.mls.LogError("Serializado: " + Encoding.UTF8.GetString(memoryStream.ToArray()));
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

            ConfigurationSync deserializedConfig = JsonSerializer.Deserialize<ConfigurationSync>(new StreamReader(memoryStream).ReadToEnd(), options);
            LethalRegenerationBase.mls.LogError("Deserializado: " + JsonSerializer.Serialize(deserializedConfig, options));

            return deserializedConfig;
        }
    }

}