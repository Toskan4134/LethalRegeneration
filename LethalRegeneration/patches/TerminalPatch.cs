using HarmonyLib;
using LethalRegeneration.config;
using LethalRegeneration.utils;
using UnityEngine;

namespace LethalRegeneration.patches;

[HarmonyPatch(typeof(Terminal))]
public class TerminalPatch
{

    private static Item item;
    private static TerminalNode buyNode1;
    private static TerminalNode buyNode2;
    private static TerminalNode CannotAfford;
    private static TerminalNode AlreadyUnlocked;

    private static CompatibleNoun CreateCompatibleNoun()
    {
        var type = typeof(CompatibleNoun);
        var instance = (CompatibleNoun)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
        return instance;
    }

    [HarmonyPatch("Start")]
    [HarmonyPrefix]
    public static void StartTerminalPatch(Terminal __instance)
    {
        if (!Configuration.Instance.HealingUpgradeEnabled) return;
        item = new Item()
        {
            itemName = "Natural Regeneration",
            creditsWorth = Configuration.Instance.HealingUpgradePrice,
            saveItemVariable = true,
        };

        TerminalKeyword buyKeyword = TerminalUtils.GetTerminalKeyword(__instance, "buy");
        TerminalNode cancelPurchaseNode = buyKeyword.compatibleNouns[0].result.terminalOptions[1].result;
        CannotAfford = ScriptableObject.CreateInstance<TerminalNode>();
        CannotAfford.name = "LethalRegeneration_CannotAfford";
        CannotAfford.displayText = $"You could not afford these items!\nYour balance is [playerCredits]. Total cost of these items is ${item.creditsWorth}\r\n\r\n";
        CannotAfford.clearPreviousText = false;
        CannotAfford.maxCharactersToType = 25;
        CannotAfford.playSyncedClip = 1;

        AlreadyUnlocked = ScriptableObject.CreateInstance<TerminalNode>();
        AlreadyUnlocked.name = "LethalRegeneration_AlreadyUnlocked";
        AlreadyUnlocked.displayText = $"This has already been unlocked for your ship!\r\n\r\n";
        AlreadyUnlocked.clearPreviousText = true;
        AlreadyUnlocked.maxCharactersToType = 25;
        AlreadyUnlocked.playSyncedClip = 1;

        TerminalKeyword infoKeyword = TerminalUtils.GetTerminalKeyword(__instance, "info");
        string itemName = item.itemName;
        TerminalKeyword keyword3 = TerminalUtils.CreateTerminalKeyword(itemName.ToLowerInvariant().Replace(" ", "-"), isVerb: false, null, null, buyKeyword);
        if (__instance.terminalNodes.allKeywords.Any((TerminalKeyword kw) => kw.word == keyword3.word))
        {
            LethalRegenerationBase.Logger.LogInfo("Keyword " + keyword3.word + " already registed, skipping.");
        }
        int itemIndex = StartOfRound.Instance.unlockablesList.unlockables.FindIndex((UnlockableItem unlockable) => unlockable.unlockableName == item.itemName);
        buyNode2 = ScriptableObject.CreateInstance<TerminalNode>();
        buyNode2.name = "LethalRegeneration_" + itemName.Replace(" ", "-") + "BuyNode2";
        buyNode2.displayText = "Ordered the " + itemName + ". Your new balance is [playerCredits].\n\nFrom now on you will regenerate health as time goes by.\r\n\r\n";
        buyNode2.clearPreviousText = true;
        buyNode2.maxCharactersToType = 15;
        buyNode2.buyItemIndex = -1;
        buyNode2.shipUnlockableID = itemIndex;
        buyNode2.buyUnlockable = true;
        buyNode2.creatureName = itemName;
        buyNode2.isConfirmationNode = false;
        buyNode2.itemCost = item.creditsWorth;
        buyNode2.playSyncedClip = 0;

        buyNode1 = ScriptableObject.CreateInstance<TerminalNode>();
        buyNode1.name = "LethalRegeneration_" + itemName.Replace(" ", "-") + "BuyNode1";
        buyNode1.displayText = "You have requested to order the " + itemName + " upgrade.\nTotal cost of items: " + item.creditsWorth + ".\n\nPlease CONFIRM or DENY.\r\n\r\n";
        buyNode1.clearPreviousText = true;
        buyNode1.maxCharactersToType = 35;
        buyNode1.buyItemIndex = -1;
        buyNode1.shipUnlockableID = itemIndex;
        buyNode1.creatureName = itemName;
        buyNode1.isConfirmationNode = true;
        buyNode1.overrideOptions = true;
        buyNode1.itemCost = item.creditsWorth;
        var confirmNoun = CreateCompatibleNoun();
        confirmNoun.noun = __instance.terminalNodes.allKeywords.First((TerminalKeyword keyword2) => keyword2.word == "confirm");
        confirmNoun.result = buyNode2;
        var denyNoun = CreateCompatibleNoun();
        denyNoun.noun = __instance.terminalNodes.allKeywords.First((TerminalKeyword keyword2) => keyword2.word == "deny");
        denyNoun.result = cancelPurchaseNode;
        buyNode1.terminalOptions = new CompatibleNoun[2] { confirmNoun, denyNoun };

        List<TerminalKeyword> allKeywords = __instance.terminalNodes.allKeywords.ToList();
        allKeywords.Add(keyword3);
        __instance.terminalNodes.allKeywords = allKeywords.ToArray();
        List<CompatibleNoun> nouns = buyKeyword.compatibleNouns.ToList();
        var cn = CreateCompatibleNoun();
        cn.noun = keyword3;
        cn.result = buyNode1;
        nouns.Add(cn);
        buyKeyword.compatibleNouns = nouns.ToArray();
        TerminalNode itemInfo = ScriptableObject.CreateInstance<TerminalNode>();
        itemInfo.name = "LethalRegeneration_" + itemName.Replace(" ", "-") + "InfoNode";
        if (Configuration.Instance.RegenerationOutsideShip)
        {
            itemInfo.displayText = $"Your health regenerates inside and outside the ship\n\nINSIDE THE SHIP\nThe healing is activated each {Configuration.Instance.TicksPerRegeneration} ticks\nThe healing power is {Configuration.Instance.RegenerationPower}hp per tick\n\nOUTSIDE THE SHIP\nThe healing is activated each {Configuration.Instance.TicksPerRegenerationOutsideShip} ticks\nThe healing power is {Configuration.Instance.RegenerationPowerOutsideShip}hp per tick\nThe regeneration limit per player is {Configuration.Instance.RegenerationLimitPerPlayer}hp per round\n\n";
        }
        else
        {
            itemInfo.displayText = $"Your health regenerates only inside the ship\n\nThe healing is activated each {Configuration.Instance.TicksPerRegeneration} ticks\nThe healing power is {Configuration.Instance.RegenerationPower}hp per tick\nThe regeneration limit per player is {Configuration.Instance.RegenerationLimitPerPlayer}hp per round\n\n";

        }
        itemInfo.clearPreviousText = true;
        itemInfo.maxCharactersToType = 25;

        __instance.terminalNodes.allKeywords = allKeywords.ToArray();
        List<CompatibleNoun> itemInfoNouns = infoKeyword.compatibleNouns.ToList();
        var cn2 = CreateCompatibleNoun();
        cn2.noun = keyword3;
        cn2.result = itemInfo;
        itemInfoNouns.Add(cn2);
        infoKeyword.compatibleNouns = itemInfoNouns.ToArray();
        LethalRegenerationBase.Logger.LogInfo("Registered " + itemName);

    }

    [HarmonyPatch("TextPostProcess")]
    [HarmonyPrefix]
    public static void TextPostProcessPatch_Prefix(ref string modifiedDisplayText, TerminalNode node, ref string __result)
    {
        if (!Configuration.Instance.HealingUpgradeEnabled) return;
        if (modifiedDisplayText.Contains("[buyableItemsList]") && modifiedDisplayText.Contains("[unlockablesSelectionList]"))
        {
            int index = modifiedDisplayText.IndexOf(":");
            string unlockableName = item.itemName;
            int unlockablePrice = item.creditsWorth;
            string newLine = $"\n* {unlockableName}    //    Price: ${unlockablePrice}";
            modifiedDisplayText = modifiedDisplayText.Insert(index + 1, newLine);
        }
    }
    [HarmonyPatch("LoadNewNode")]
    [HarmonyPrefix]
    public static void LoadNewNodePatch_Prefix(ref TerminalNode node, Terminal __instance)
    {
        if (!node.name.StartsWith("LethalRegeneration")) return;
        string nodeName = node.name.Split('_')[1];
        switch (nodeName)
        {
            case "Natural-RegenerationBuyNode2":
                if (Configuration.Instance.HealingUpgradeUnlocked)
                {
                    node = AlreadyUnlocked;
                    return;
                }
                int finalCredits = __instance.groupCredits - item.creditsWorth;
                __instance.SyncGroupCreditsServerRpc(finalCredits, __instance.numberOfItemsInDropship);
                Configuration.Instance.HealingUpgradeUnlocked = true;
                Configuration.Synced = false;
                if (Configuration.IsHost)
                {
                    Configuration.Instance.BroadcastHealingUpgradeStatusToClients();
                }
                else
                {
                    Configuration.Instance.SendHealingUpgradeStatusToHost();
                }
                break;
            case "Natural-RegenerationBuyNode1":
                if (__instance.groupCredits < item.creditsWorth)
                {
                    node = CannotAfford;
                    return;
                }
                break;
        }
    }

}
