namespace LethalRegeneration.patches;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TerminalApi;

[HarmonyPatch(typeof(Terminal))]
public class TerminalPatch
{
    [HarmonyPatch("Start")]
    [HarmonyPrefix]
    public static void StartTerminalPatch(Terminal __instance)
    {
        LethalRegenerationBase.Logger.LogInfo("PUTA");
        Item loadedAsset = new Item()
        {
            itemName = "Healer",
            creditsWorth = 700
        };
        List<Item> list = __instance.buyableItemsList.ToList();
        string nombresConcatenados = string.Join(", ", __instance.terminalNodes.allKeywords.Select(item => item.word));
        LethalRegenerationBase.Logger.LogInfo(nombresConcatenados);
        Array.Resize(ref __instance.buyableItemsList, list.Count());
        list.Add(loadedAsset);

        int buyItemIndex = list.IndexOf(loadedAsset);
        __instance.buyableItemsList = list.ToArray();
        TerminalKeyword terminalKeyword = TerminalApi.CreateTerminalKeyword("healer", true, (TerminalNode)null);
        TerminalKeyword terminalKeyword2 = terminalKeyword.defaultVerb = TerminalApi.GetKeyword("buy");
        terminalKeyword.isVerb = false;
        TerminalExtenstionMethods.AddCompatibleNoun(terminalKeyword2, terminalKeyword, "You have requested to order the healer.\r\nTotal cost of items: [totalCost].\r\n\r\nPlease CONFIRM or DENY.\r\n\r\n", false);
        terminalKeyword2.compatibleNouns.Last().noun.name = "Healer";
        terminalKeyword2.compatibleNouns.Last().result.name = "buyHealer";
        terminalKeyword2.compatibleNouns.Last().result.buyItemIndex = buyItemIndex;
        terminalKeyword2.compatibleNouns.Last().result.isConfirmationNode = true;
        terminalKeyword2.compatibleNouns.Last().result.overrideOptions = true;
        terminalKeyword2.compatibleNouns.Last().result.clearPreviousText = true;
        TerminalKeyword keyword = TerminalApi.GetKeyword("confirm");
        CompatibleNoun compatibleNoun = new()
        {
            noun = keyword,
            result = new TerminalNode
            {
                buyItemIndex = buyItemIndex,
                itemCost = loadedAsset.creditsWorth,
                name = loadedAsset.itemName,
                clearPreviousText = true,
                playSyncedClip = 0,
                displayText = "You have requested to order the pito. Your new balance is [playerCredits].\r\n\r\nOur contractors enjoy fast, free shipping while on the job! Any purchased items will arrive hourly at your approximate location.\r\n\r\n"
            }
        };
        TerminalKeyword keyword2 = TerminalApi.GetKeyword("deny");
        CompatibleNoun compatibleNoun2 = new CompatibleNoun
        {
            noun = keyword2,
            result = new TerminalNode
            {
                displayText = "Cancelled order.\r\n"
            }
        };
        terminalKeyword2.compatibleNouns.Last().result.terminalOptions = [compatibleNoun, compatibleNoun2];
        TerminalApi.AddTerminalKeyword(terminalKeyword);
    }
}