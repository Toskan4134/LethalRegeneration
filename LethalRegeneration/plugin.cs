namespace LethalRegeneration;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalRegeneration.config;
using LethalRegeneration.patches;

[BepInPlugin(modGUID, modName, modVersion)]

public class LethalRegenerationBase : BaseUnityPlugin
{
    private const string modGUID = "Toskan4134.LethalRegeneration";
    private const string modName = "LethalRegeneration";
    private const string modVersion = "1.0.3";

    private readonly Harmony patcher = new Harmony(modGUID);
    public static new Configuration Config { get; internal set; }
    public static LethalRegenerationBase Instance;
    public static new ManualLogSource Logger { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        Logger = BepInEx.Logging.Logger.CreateLogSource(modName);
        Logger.LogInfo(base.Config);
        Config = new(base.Config);
        try
        {
            patcher.PatchAll(typeof(HUDManagerPatch));
            patcher.PatchAll(typeof(Configuration));

            Logger.LogInfo(modName + " Awaken");
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }

}
