namespace LethalRegeneration.patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LethalRegeneration.utils;
using TerminalApi;
using UnityEngine;

[HarmonyPatch(typeof(Terminal))]
public class TerminalPatch
{

    private static Item item;

    [HarmonyPatch("Start")]
    [HarmonyPrefix]
    public static void StartTerminalPatch(Terminal __instance)
    {

        LethalRegenerationBase.Logger.LogInfo("PUTA");
        item = new Item()
        {
            itemName = "Healer",
            creditsWorth = 30,
            itemId = 4134,
        };

        TerminalKeyword buyKeyword = __instance.terminalNodes.allKeywords.First((TerminalKeyword keyword) => keyword.word == "buy");
        TerminalNode cancelPurchaseNode = buyKeyword.compatibleNouns[0].result.terminalOptions[1].result;
        TerminalKeyword infoKeyword = __instance.terminalNodes.allKeywords.First((TerminalKeyword keyword) => keyword.word == "info");
        LethalRegenerationBase.Logger.LogInfo((object)$"Adding item to terminal");
        string itemName = item.itemName;
        TerminalKeyword keyword3 = TerminalUtils.CreateTerminalKeyword(itemName.ToLowerInvariant().Replace(" ", "-"), isVerb: false, null, null, buyKeyword);
        if (__instance.terminalNodes.allKeywords.Any((TerminalKeyword kw) => kw.word == keyword3.word))
        {
            LethalRegenerationBase.Logger.LogInfo((object)("Keyword " + keyword3.word + " already registed, skipping."));
        }
        int itemIndex = StartOfRound.Instance.unlockablesList.unlockables.FindIndex((UnlockableItem unlockable) => unlockable.unlockableName == item.itemName);
        StartOfRound wah = StartOfRound.Instance;
        if (wah == null)
        {
            LethalRegenerationBase.Logger.LogDebug("STARTOFROUND INSTANCE NOT FOUND");
        }
        char lastChar = itemName[itemName.Length - 1];
        string itemNamePlural = itemName;
        TerminalNode buyNode2 = ScriptableObject.CreateInstance<TerminalNode>();
        buyNode2.name = itemName.Replace(" ", "-") + "BuyNode2";
        buyNode2.displayText = "Ordered [variableAmount] " + itemNamePlural + ". Your new balance is [playerCredits].\n\nOur contractors enjoy fast, free shipping while on the job! Any purchased items will arrive hourly at your approximate location.\r\n\r\n";
        buyNode2.clearPreviousText = true;
        buyNode2.maxCharactersToType = 15;
        buyNode2.buyItemIndex = -1;
        buyNode2.shipUnlockableID = itemIndex;
        buyNode2.buyUnlockable = true;
        buyNode2.creatureName = itemName;
        buyNode2.isConfirmationNode = false;
        buyNode2.itemCost = item.creditsWorth;
        buyNode2.playSyncedClip = 0;

        TerminalNode buyNode1 = ScriptableObject.CreateInstance<TerminalNode>();
        buyNode1.name = itemName.Replace(" ", "-") + "BuyNode1";
        buyNode1.displayText = "You have requested to order " + itemNamePlural + ". Amount: [variableAmount].\nTotal cost of items: [totalCost].\n\nPlease CONFIRM or DENY.\r\n\r\n";
        buyNode1.clearPreviousText = true;
        buyNode1.maxCharactersToType = 35;
        buyNode1.buyItemIndex = -1;
        buyNode1.shipUnlockableID = itemIndex;
        buyNode1.creatureName = itemName;
        buyNode1.isConfirmationNode = true;
        buyNode1.overrideOptions = true;
        buyNode1.itemCost = item.creditsWorth;
        buyNode1.terminalOptions =
        [
            new CompatibleNoun
            {
                noun = __instance.terminalNodes.allKeywords.First((TerminalKeyword keyword2) => keyword2.word == "confirm"),
                result = buyNode2
            },
            new CompatibleNoun
            {
                noun = __instance.terminalNodes.allKeywords.First((TerminalKeyword keyword2) => keyword2.word == "deny"),
                result = cancelPurchaseNode
            }
        ];

        List<TerminalKeyword> allKeywords = __instance.terminalNodes.allKeywords.ToList();
        allKeywords.Add(keyword3);
        __instance.terminalNodes.allKeywords = allKeywords.ToArray();
        List<CompatibleNoun> nouns = buyKeyword.compatibleNouns.ToList();
        nouns.Add(new CompatibleNoun
        {
            noun = keyword3,
            result = buyNode1
        });
        buyKeyword.compatibleNouns = nouns.ToArray();
        TerminalNode itemInfo = ScriptableObject.CreateInstance<TerminalNode>();
        itemInfo.name = itemName.Replace(" ", "-") + "InfoNode";
        itemInfo.displayText = "Healsalotlol\n\n";
        itemInfo.clearPreviousText = true;
        itemInfo.maxCharactersToType = 25;

        __instance.terminalNodes.allKeywords = allKeywords.ToArray();
        List<CompatibleNoun> itemInfoNouns = infoKeyword.compatibleNouns.ToList();
        itemInfoNouns.Add(new CompatibleNoun
        {
            noun = keyword3,
            result = itemInfo
        });
        infoKeyword.compatibleNouns = itemInfoNouns.ToArray();
        LethalRegenerationBase.Logger.LogInfo("Registered item :)");

    }

    [HarmonyPatch("OnSubmit")]
    [HarmonyPrefix]
    public static void UpdateTerminalPatch(Terminal __instance)
    {
        // LethalRegenerationBase.Logger.LogInfo(__instance.currentText);
        // LethalRegenerationBase.Logger.LogInfo(__instance.currentNode.name);
    }

    [HarmonyPatch("TextPostProcess")]
    [HarmonyPrefix]
    public static void TextPostProcessPatch_Prefix(ref string modifiedDisplayText, TerminalNode node, ref string __result)
    {
        if (modifiedDisplayText.Contains("[buyableItemsList]") && modifiedDisplayText.Contains("[unlockablesSelectionList]"))
        {
            int index = modifiedDisplayText.IndexOf(":");
            string unlockableName = item.itemName;
            int unlockablePrice = item.creditsWorth;
            string newLine = $"\n* {unlockableName}    //    Price: ${unlockablePrice}";
            modifiedDisplayText = modifiedDisplayText.Insert(index + 1, newLine);
            LethalRegenerationBase.Logger.LogInfo(modifiedDisplayText);
        }
    }
}