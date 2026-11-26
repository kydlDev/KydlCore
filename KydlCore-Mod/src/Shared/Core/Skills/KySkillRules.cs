namespace KydlCore.Core;

/// <summary>The pure part of KySkills (unit-tested): the skill number of an identifier = the game's stable string hash (Jotunn's rule, so saved levels keep their skill).</summary>
public static class KySkillRules
{
    /// <summary>Valheim's GetStableHashCode, copied (pure): same text, same number on every machine. Expected outcome: the number (null is read as "").</summary>
    public static int TypeNumber(string identifier)
    {
        string text = identifier ?? "";
        int number;
        unchecked
        {
            int h1 = 5381, h2 = h1;
            for (int i = 0; i < text.Length && text[i] != '\0'; i += 2)
            {
                h1 = ((h1 << 5) + h1) ^ text[i];
                if (i == text.Length - 1 || text[i + 1] == '\0') break;
                h2 = ((h2 << 5) + h2) ^ text[i + 1];
            }
            number = h1 + h2 * 1566083941;
        }
        if (CoreLog.On) CoreLog.Verbose($"KySkillRules.TypeNumber: '{identifier}' -> {number}");
        return number;
    }
}
