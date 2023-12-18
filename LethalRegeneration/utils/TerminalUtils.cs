namespace LethalRegeneration.utils;
using UnityEngine;

public class TerminalUtils
{
    public static TerminalKeyword CreateTerminalKeyword(string word, bool isVerb = false, CompatibleNoun[] compatibleNouns = null, TerminalNode specialKeywordResult = null, TerminalKeyword defaultVerb = null, bool accessTerminalObjects = false)
    {
        TerminalKeyword keyword = ScriptableObject.CreateInstance<TerminalKeyword>();
        keyword.name = word;
        keyword.word = word;
        keyword.isVerb = isVerb;
        keyword.compatibleNouns = compatibleNouns;
        keyword.specialKeywordResult = specialKeywordResult;
        keyword.defaultVerb = defaultVerb;
        keyword.accessTerminalObjects = accessTerminalObjects;
        return keyword;
    }
}
