namespace LethalRegeneration.config;

using BepInEx.Configuration;



public class Configuration
{
    public static readonly int defaultTicksPerRegeneration = 2;
    public static readonly int defaultRegenerationPower = 5;
    public static readonly bool defaultregenerationOutsideShip = false;
    public static ConfigEntry<int> ticksPerRegeneration;
    public static ConfigEntry<int> regenerationPower;
    public static ConfigEntry<bool> regenerationOutsideShip;
    public static void SetConfig()
    {
        regenerationPower = LethalRegenerationBase.Instance.Config.Bind("Values", "Regeneration Power", defaultRegenerationPower, "Amount of life regenerated each time triggered (Between 0 an 100)");
        ticksPerRegeneration = LethalRegenerationBase.Instance.Config.Bind("Values", "Ticks Per Regeneration", defaultTicksPerRegeneration, "Number of ticks until the regeneration is triggered (1 tick equals each time the minutes of the clock are changed)");
        regenerationOutsideShip = LethalRegenerationBase.Instance.Config.Bind("Values", "Regeneration Outside Ship", defaultregenerationOutsideShip, "Whether health is regenerated also outside the ship or only inside.");
        LethalRegenerationBase.mls.LogInfo("Configuration Set");
        ConfigurationSync.BuildDefaultConfig();
    }
}