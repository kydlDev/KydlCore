using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KydlCore.Tests.Unit;

/// <summary>
/// One row-2 tab per config category, for every mod (user, 2026-11-16): ConfigGroups names the tabs from a mod's config sections, and
/// HubModel.SplitTab shows a mod's "Settings" tab as those tabs. Both are internal: reached in the BUILT mod DLL by reflection.
/// </summary>
[TestFixture]
public class ConfigTabsTests
{
    const string A = "com.kydldev.Alpha";
    const BindingFlags Any = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Type Groups = ModAssembly.Type("KydlCore.Features.Hub.Contracts.ConfigGroups");
    static readonly Type Model = ModAssembly.Type("KydlCore.Ui.HubModel");

    // ---- ConfigGroups ----------------------------------------------------------------------------------------------------
    static string NameOf(string section) { return (string)Groups.GetMethod("NameOf", Any).Invoke(null, new object[] { section }); }

    /// <summary>The groups as "Name: section, section" lines.</summary>
    static string[] Of(params string[] sections)
    {
        var list = (IEnumerable)Groups.GetMethod("Of", Any).Invoke(null, new object[] { sections });
        var lines = new List<string>();
        foreach (object g in list)
        {
            string name = (string)g.GetType().GetProperty("Name", Any).GetValue(g);
            var parts = (List<string>)g.GetType().GetProperty("Sections", Any).GetValue(g);
            lines.Add(name + ": " + string.Join(", ", parts.ToArray()));
        }
        return lines.ToArray();
    }

    [TestCase("Staff of Protection - Appearance", "Staff of Protection")]
    [TestCase("Staff of Protection - Player: Bjorn - the second", "Staff of Protection")]
    [TestCase("Auto-fade", "Auto-fade")]
    [TestCase("General", "General")]
    [TestCase(" - Odd", " - Odd")]
    [TestCase("", "")]
    [TestCase(null, null)]
    public void NameOf_IsTheTextBeforeTheFirstSeparator(string section, string expected)
    {
        Assert.That(NameOf(section), Is.EqualTo(expected));
    }

    [Test]
    public void Of_OneTabPerGroup_GeneralFirst_DebugLogLast_TheRestInTheModsOrder()
    {
        string[] tabs = Of("DebugLog", "Staff of Protection - Appearance", "General", "Staff of Protection - Yourself", "Auto-fade",
            "Goblin Shaman shield - Appearance", "Staff of Protection - Player: Bjorn", "Goblin Shaman shield - Other creatures");
        Assert.That(tabs, Is.EqualTo(new[]
        {
            "General: General",
            "Staff of Protection: Staff of Protection - Appearance, Staff of Protection - Yourself, Staff of Protection - Player: Bjorn",
            "Auto-fade: Auto-fade",
            "Goblin Shaman shield: Goblin Shaman shield - Appearance, Goblin Shaman shield - Other creatures",
            "DebugLog: DebugLog",
        }));
    }

    [Test]
    public void Of_WithoutGeneralOrDebugLog_KeepsTheModsOrder()
    {
        Assert.That(Of("Glow", "Arrow", "Notifications"), Is.EqualTo(new[] { "Glow: Glow", "Arrow: Arrow", "Notifications: Notifications" }));
    }

    [Test]
    public void Of_OnlyDebugLogAndGeneral_GeneralStillFirst()
    {
        Assert.That(Of("DebugLog", "General"), Is.EqualTo(new[] { "General: General", "DebugLog: DebugLog" }));
    }

    [Test]
    public void Of_ASectionTwice_OrBlank_IsListedOnce_OrNotAtAll()
    {
        Assert.That(Of("General", "", null, "General", "Drain", "Drain"), Is.EqualTo(new[] { "General: General", "Drain: Drain" }));
    }

    [Test]
    public void Of_NoSections_NoTabs()
    {
        Assert.That(Of(), Is.Empty);
        Assert.That((ICollection)Groups.GetMethod("Of", Any).Invoke(null, new object[] { null }), Is.Empty);
    }

    static string[] Shown(string[] sections, Func<string, bool> replaced)
    {
        var shown = (List<string>)Groups.GetMethod("Shown", Any).Invoke(null, new object[] { sections, replaced });
        return shown.ToArray();
    }

    [Test]
    public void Shown_LeavesOutTheSectionsAPanelReplaces_KeepsTheOrder()
    {
        string[] sections = { "Staff - Appearance", "Staff - Yourself", "Staff - Player: Bjorn" };
        Assert.That(Shown(sections, s => !s.EndsWith("Appearance")), Is.EqualTo(new[] { "Staff - Appearance" }));
        Assert.That(Shown(sections, s => false), Is.EqualTo(sections));
        Assert.That(Shown(sections, s => true), Is.Empty, "everything replaced: nothing is drawn as a plain entry");
    }

    [Test]
    public void Shown_WithoutAPanel_IsEveryoneOfTheSections()
    {
        Assert.That(Shown(new[] { "A", "B" }, null), Is.EqualTo(new[] { "A", "B" }));
        Assert.That(Shown(null, s => true), Is.Empty);
    }

    // ---- HubModel.SplitTab -------------------------------------------------------------------------------------------------
    sealed class Hub
    {
        readonly object _o = Activator.CreateInstance(Model);
        public int Changes;
        public Hub() { Model.GetEvent("Changed").AddEventHandler(_o, (Action)(() => Changes++)); }
        object Call(string name, params object[] args) { return Model.GetMethod(name, Any).Invoke(_o, args); }
        public string SelectedTab { get { return (string)Model.GetProperty("SelectedTab").GetValue(_o); } }
        public bool AddMod(string g) { return (bool)Call("AddMod", g, g); }
        public bool AddTab(string g, string id, string title, int order) { return (bool)Call("AddTab", g, id, title, order); }
        public bool RemoveTab(string g, string id) { return (bool)Call("RemoveTab", g, id); }
        public string[] TabIds(string g) { return (string[])Call("TabIds", g); }
        public string TabTitle(string g, string id) { return (string)Call("TabTitle", g, id); }
        public bool HasTab(string g, string id) { return (bool)Call("HasTab", g, id); }
        public bool Split(string g, string id, params string[] parts) { return (bool)Call("SplitTab", g, id, parts); }
        public bool SplitNull(string g, string id) { return (bool)Call("SplitTab", g, id, null); }
        public bool Select(string g, string id) { return (bool)Call("Select", g, id); }
    }

    static string Part(string tab, string part) { return (string)Model.GetMethod("PartId", Any).Invoke(null, new object[] { tab, part }); }
    static string PartOf(string id) { return (string)Model.GetMethod("PartOf", Any).Invoke(null, new object[] { id }); }

    [Test]
    public void PartId_AndPartOf_AreEachOthersOpposite()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PartOf(Part("settings", "General")), Is.EqualTo("General"));
            Assert.That(Part("settings", "General"), Is.Not.EqualTo("settings").And.StartsWith("settings"));
            Assert.That(PartOf("settings"), Is.Null);
            Assert.That(PartOf(""), Is.Null);
            Assert.That(PartOf(null), Is.Null);
        });
    }

    [Test]
    public void SplitTab_ShowsTheTabAsItsParts_AtItsPlace_TitledWithThePart()
    {
        var hub = new Hub();
        hub.AddMod(A);
        hub.AddTab(A, "bubbles", "Bubbles", 0);
        hub.AddTab(A, "settings", "Settings", 100);
        hub.AddTab(A, "extra", "Extra", 200);
        int before = hub.Changes;

        Assert.That(hub.Split(A, "settings", "General", "Auto-fade", "DebugLog"), Is.True);

        Assert.Multiple(() =>
        {
            // user 2026-11-17: "General" first and "DebugLog" last in row 2, whatever tabs the mod registered around the settings tab
            Assert.That(hub.TabIds(A), Is.EqualTo(new[] { Part("settings", "General"), "bubbles", Part("settings", "Auto-fade"), "extra", Part("settings", "DebugLog") }));
            Assert.That(hub.TabTitle(A, Part("settings", "Auto-fade")), Is.EqualTo("Auto-fade"));
            Assert.That(hub.TabTitle(A, Part("settings", "Nothing")), Is.Null, "a part the tab does not have");
            Assert.That(hub.TabTitle(A, "bubbles"), Is.EqualTo("Bubbles"));
            Assert.That(hub.Changes, Is.EqualTo(before + 1), "one Changed for the split");
        });
    }

    [Test]
    public void TabIds_GeneralIsFirst_DebugLogIsLast_AlsoForTabsTheModRegisteredWithThoseTitles()
    {
        var hub = new Hub();
        hub.AddMod(A);
        hub.AddTab(A, "log", "DebugLog", 0);              // registered first, still last
        hub.AddTab(A, "stats", "Stats", 10);
        hub.AddTab(A, "main", "General", 20);             // registered late, still first
        hub.AddTab(A, "more", "More", 30);

        Assert.That(hub.TabIds(A), Is.EqualTo(new[] { "main", "stats", "more", "log" }));
    }

    [Test]
    public void TabIds_WithoutGeneralOrDebugLog_KeepTheirOrder_AndTheOrderFollowsAChange()
    {
        var hub = new Hub();
        hub.AddMod(A);
        hub.AddTab(A, "b", "Bravo", 20);
        hub.AddTab(A, "a", "Alpha", 10);
        Assert.That(hub.TabIds(A), Is.EqualTo(new[] { "a", "b" }));

        hub.AddTab(A, "a", "DebugLog", 10);               // the same tab gets the title: it moves to the end
        Assert.That(hub.TabIds(A), Is.EqualTo(new[] { "b", "a" }));
    }

    [Test]
    public void TabIds_ATabOfTheModBeforeTheSettingsTab_NoLongerComesBeforeGeneral()
    {
        var hub = new Hub();
        hub.AddMod(A);
        hub.AddTab(A, "bubbles", "Bubbles", 0);
        hub.AddTab(A, "settings", "Settings", 100);
        hub.Split(A, "settings", "Staff", "General", "DebugLog");

        string[] ids = hub.TabIds(A);
        Assert.Multiple(() =>
        {
            Assert.That(ids.First(), Is.EqualTo(Part("settings", "General")));
            Assert.That(ids.Last(), Is.EqualTo(Part("settings", "DebugLog")));
            Assert.That(ids, Is.EqualTo(new[] { Part("settings", "General"), "bubbles", Part("settings", "Staff"), Part("settings", "DebugLog") }));
        });
    }

    [Test]
    public void SplitTab_TheTabItselfStaysRegistered_AndSelectingItSelectsItsFirstPart()
    {
        var hub = new Hub();
        hub.AddMod(A);
        hub.AddTab(A, "bubbles", "Bubbles", 0);
        hub.AddTab(A, "settings", "Settings", 100);
        hub.Split(A, "settings", "General", "DebugLog");

        Assert.Multiple(() =>
        {
            Assert.That(hub.HasTab(A, "settings"), Is.True, "a mod still finds the tab it registered");
            Assert.That(hub.HasTab(A, Part("settings", "DebugLog")), Is.True);
            Assert.That(hub.HasTab(A, "bubbles"), Is.True);
            Assert.That(hub.HasTab(A, "nothing"), Is.False);
            Assert.That(hub.HasTab(A, ""), Is.False);
            Assert.That(hub.HasTab("", "settings"), Is.False);
            Assert.That(hub.Select(A, "settings"), Is.True);
            Assert.That(hub.SelectedTab, Is.EqualTo(Part("settings", "General")));
            Assert.That(hub.Select(A, Part("settings", "DebugLog")), Is.True);
            Assert.That(hub.SelectedTab, Is.EqualTo(Part("settings", "DebugLog")));
            Assert.That(hub.Select(A, "nothing"), Is.False);
            Assert.That(hub.SelectedTab, Is.EqualTo(Part("settings", "DebugLog")), "a failed select changes nothing");
        });
    }

    [Test]
    public void SplitTab_TheAutomaticSettingsTab_IsSplitTheSameWay()
    {
        var hub = new Hub();
        hub.AddMod(A);                      // registered no tab: one automatic "settings" tab
        hub.Split(A, "settings", "General", "Drain", "DebugLog");

        Assert.Multiple(() =>
        {
            Assert.That(hub.TabIds(A), Is.EqualTo(new[] { Part("settings", "General"), Part("settings", "Drain"), Part("settings", "DebugLog") }));
            Assert.That(hub.SelectedTab, Is.EqualTo(Part("settings", "General")), "the selection moved from the whole tab to its first part");
            Assert.That(hub.HasTab(A, "settings"), Is.True);
        });
    }

    [Test]
    public void SplitTab_FewerThanTwoParts_ShowsTheTabWholeAgain()
    {
        var hub = new Hub();
        hub.AddMod(A);
        hub.AddTab(A, "settings", "Settings", 0);
        hub.Split(A, "settings", "General", "DebugLog");
        int before = hub.Changes;

        Assert.That(hub.Split(A, "settings", "General"), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(hub.TabIds(A), Is.EqualTo(new[] { "settings" }));
            Assert.That(hub.SelectedTab, Is.EqualTo("settings"));
            Assert.That(hub.Changes, Is.EqualTo(before + 1));
        });
        hub.Split(A, "settings", "General", "DebugLog");
        Assert.That(hub.SplitNull(A, "settings"), Is.True);
        Assert.That(hub.TabIds(A), Is.EqualTo(new[] { "settings" }), "null parts = whole");
    }

    [Test]
    public void SplitTab_TheSamePartsAgain_OrNothingToUndo_IsSilent()
    {
        var hub = new Hub();
        hub.AddMod(A);
        hub.AddTab(A, "settings", "Settings", 0);
        hub.Split(A, "settings", "General", "DebugLog");
        int before = hub.Changes;

        hub.Split(A, "settings", "General", "DebugLog");
        hub.Split(A, "other", "Only one");
        hub.SplitNull(A, "other");

        Assert.That(hub.Changes, Is.EqualTo(before));
        hub.Split(A, "settings", "DebugLog", "General");
        Assert.That(hub.Changes, Is.EqualTo(before + 1), "another order is a change");
    }

    [Test]
    public void SplitTab_BlankAndRepeatedParts_AreDropped_BlankGuidOrTabIsRefused()
    {
        var hub = new Hub();
        hub.AddMod(A);
        hub.AddTab(A, "settings", "Settings", 0);

        Assert.Multiple(() =>
        {
            Assert.That(hub.Split("", "settings", "General", "DebugLog"), Is.False);
            Assert.That(hub.Split(A, "", "General", "DebugLog"), Is.False);
            Assert.That(hub.Split(null, "settings", "General", "DebugLog"), Is.False);
            Assert.That(hub.Split(A, null, "General", "DebugLog"), Is.False);
            Assert.That(hub.TabIds(A), Is.EqualTo(new[] { "settings" }));
        });
        hub.Split(A, "settings", "General", "", null, "General", "DebugLog");
        Assert.That(hub.TabIds(A), Is.EqualTo(new[] { Part("settings", "General"), Part("settings", "DebugLog") }));
        hub.Split(A, "settings", "General", "", "General");
        Assert.That(hub.TabIds(A), Is.EqualTo(new[] { "settings" }), "one real part = whole");
    }

    [Test]
    public void SplitTab_ARemovedTab_ShowsNoParts_AndSelectingItFails()
    {
        var hub = new Hub();
        hub.AddMod(A);
        hub.AddTab(A, "bubbles", "Bubbles", 0);
        hub.AddTab(A, "settings", "Settings", 1);
        hub.Split(A, "settings", "General", "DebugLog");
        hub.Select(A, Part("settings", "DebugLog"));

        hub.RemoveTab(A, "settings");

        Assert.Multiple(() =>
        {
            Assert.That(hub.TabIds(A), Is.EqualTo(new[] { "bubbles" }));
            Assert.That(hub.SelectedTab, Is.EqualTo("bubbles"));
            Assert.That(hub.Select(A, "settings"), Is.False, "the parts are remembered, but the tab is gone");
            Assert.That(hub.HasTab(A, "settings"), Is.False);
        });
    }

    [Test]
    public void SplitTab_ThePartsOfOneMod_DoNotShowInAnother()
    {
        var hub = new Hub();
        hub.AddMod(A);
        hub.AddMod("com.kydldev.Beta");
        hub.Split(A, "settings", "General", "DebugLog");
        Assert.That(hub.TabIds("com.kydldev.Beta"), Is.EqualTo(new[] { "settings" }));
        Assert.That(hub.TabTitle("com.kydldev.Beta", Part("settings", "General")), Is.Null);
    }
}
