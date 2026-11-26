// FeatureRules (D-105): a functionality runs only when Enable Mod AND its own switch are on (rule 11).
using KydlCore.Core;

namespace KydlCore.Tests.Unit;

public class FeatureRulesTests
{
    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    public void IsOn_NeedsBothSwitches(bool enableMod, bool own, bool expected)
    {
        Assert.That(FeatureRules.IsOn(enableMod, own), Is.EqualTo(expected));
    }
}
