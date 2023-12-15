namespace LethalRegeneration;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalRegeneration.patches;
using LethalRegeneration.config;

[BepInPlugin(modGUID, modName, modVersion)]

public class LethalRegenerationBase : BaseUnityPlugin
{
    private const string modGUID = "Toskan4134.LethalRegeneration";
    private const string modName = "LethalRegeneration";
    private const string modVersion = "1.0.1";

    private readonly Harmony harmony = new Harmony(modGUID);

    public static LethalRegenerationBase Instance;
    public static ManualLogSource mls;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        mls = BepInEx.Logging.Logger.CreateLogSource(modName);
        mls.LogInfo(modName + " Awaken");
        Configuration.SetConfig();
        harmony.PatchAll(typeof(HUDManagerPatch));
        // harmony.PatchAll(typeof(ConfigurationSync));
    }

}
