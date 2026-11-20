using System;
using KydlCore.Core;

namespace KydlCore.Features.Items.Domain;

/// <summary>
/// The rules of a KY-Items contribution id (spec 5, MIGRATION_DECISIONS B13): "<modid>:<item>", lowercase, where <modid> is the owning mod's GUID
/// after "com.kydldev." ("com.kydldev.SteveTheTroll" -> "stevethetroll"), e.g. "stevethetroll:slippers". Pure: tested in tests/Unit.
/// </summary>
internal static class KyItemRules
{
    internal const string GuidPrefix = "com.kydldev.";
    internal const string NoId = "no id", NoMod = "no owning mod GUID", NotUserMod = "the owner is not a KydlDev mod (com.kydldev.*)",
                          BadShape = "the id must look like '<modid>:<item>'", BadChars = "the item part may only use a-z, 0-9, '_', '-' and '.'";

    /// True for "com.kydldev.*" GUIDs with a name after the prefix (case ignored).
    internal static bool IsUserGuid(string guid)
    {
        if (CoreLog.On) CoreLog.Every("KyItemRules.IsUserGuid", 2d, $"KyItemRules.IsUserGuid(guid={guid})");
        { var returnedAt20 = guid != null && guid.Length > GuidPrefix.Length && guid.StartsWith(GuidPrefix, StringComparison.OrdinalIgnoreCase); if (CoreLog.On) CoreLog.Every("KyItemRules.IsUserGuid:20", 2d, $"KyItemRules.IsUserGuid: returns {returnedAt20} (line 20)"); return returnedAt20; }
    }

    /// The id namespace of a mod: its GUID after the prefix, lowercase ("" for a GUID that is not the user's).
    internal static string ModId(string guid) { return IsUserGuid(guid) ? guid.Substring(GuidPrefix.Length).ToLowerInvariant() : ""; }

    /// Expected outcome: Success with the id in lowercase; Rejected (with the reason) for a missing / foreign owner, a wrong shape, another
    /// mod's namespace or characters outside a-z 0-9 _ - . in the item part.
    internal static OperationResponse<string> ValidateId(string id, string modGuid)
    {
        if (CoreLog.On) CoreLog.Verbose($"KyItemRules.ValidateId(id={id}, modGuid={modGuid})");
        if (string.IsNullOrEmpty(id)) { var returnedAt31 = OperationResponse<string>.Rejected(NoId); if (CoreLog.On) CoreLog.Verbose($"KyItemRules.ValidateId: returns {returnedAt31} (line 31)"); return returnedAt31; }
        if (string.IsNullOrEmpty(modGuid)) { var returnedAt32 = OperationResponse<string>.Rejected(NoMod); if (CoreLog.On) CoreLog.Verbose($"KyItemRules.ValidateId: returns {returnedAt32} (line 32)"); return returnedAt32; }
        if (!IsUserGuid(modGuid)) { var returnedAt33 = OperationResponse<string>.Rejected(NotUserMod); if (CoreLog.On) CoreLog.Verbose($"KyItemRules.ValidateId: returns {returnedAt33} (line 33)"); return returnedAt33; }
        string lower = id.Trim().ToLowerInvariant();
        int colon = lower.IndexOf(':');
        if (colon <= 0 || colon == lower.Length - 1 || lower.IndexOf(':', colon + 1) >= 0) { var returnedAt36 = OperationResponse<string>.Rejected(BadShape); if (CoreLog.On) CoreLog.Verbose($"KyItemRules.ValidateId: returns {returnedAt36} (line 36)"); return returnedAt36; }
        string ns = lower.Substring(0, colon), item = lower.Substring(colon + 1);
        string expected = ModId(modGuid);
        if (ns != expected) { var returnedAt39 = OperationResponse<string>.Rejected("the id namespace '" + ns + "' is not the owning mod '" + expected + "'"); if (CoreLog.On) CoreLog.Verbose($"KyItemRules.ValidateId: returns {returnedAt39} (line 39)"); return returnedAt39; }
        foreach (char ch in item)
            if (!((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_' || ch == '-' || ch == '.')) { var returnedAt41 = OperationResponse<string>.Rejected(BadChars); if (CoreLog.On) CoreLog.Verbose($"KyItemRules.ValidateId: returns {returnedAt41} (line 41)"); return returnedAt41; }
        { var returnedAt42 = OperationResponse<string>.Success(lower); if (CoreLog.On) CoreLog.Verbose($"KyItemRules.ValidateId: returns {returnedAt42} (line 42)"); return returnedAt42; }
    }
}
