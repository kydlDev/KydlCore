// KyNetworkRules (D-104): the pure part of KydlCore's network check (no Jotunn) - which level needs the mod on every player, which tag means admin-only.
using System;
using System.Collections.Generic;
using KydlCore.Core;

namespace KydlCore.Tests.Unit;

public class KyNetworkRulesTests
{
    enum Level { ClientOnly = 0, EveryoneMustHave = 1 }

    sealed class TagWithField { public bool? IsAdminOnly; }
    sealed class TagWithProperty { public bool IsAdminOnly { get; set; } }
    sealed class OtherTag { public bool? Browsable = false; }

    [Test] public void ModRequired_OnlyForEveryoneMustHave()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(KyNetworkRules.ModRequired(0), Is.False);
            Assert.That(KyNetworkRules.ModRequired(1), Is.True);
            Assert.That(KyNetworkRules.ModRequired(2), Is.False, "an unknown level never forces the mod on every player");
            Assert.That(KyNetworkRules.ModRequired(Level.ClientOnly), Is.False);
            Assert.That(KyNetworkRules.ModRequired(Level.EveryoneMustHave), Is.True);
            Assert.That(KyNetworkRules.ModRequired((Enum)null), Is.False);
        }
    }

    [Test] public void IsAdminOnly_ReadsFieldOrPropertyOfAnyTag()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(KyNetworkRules.IsAdminOnly(new object[] { new TagWithField { IsAdminOnly = true } }), Is.True);
            Assert.That(KyNetworkRules.IsAdminOnly(new object[] { new TagWithProperty { IsAdminOnly = true } }), Is.True);
            Assert.That(KyNetworkRules.IsAdminOnly(new object[] { new OtherTag(), new TagWithField { IsAdminOnly = true } }), Is.True, "any tag of the list counts");
            Assert.That(KyNetworkRules.IsAdminOnly(new object[] { new TagWithField { IsAdminOnly = false } }), Is.False);
            Assert.That(KyNetworkRules.IsAdminOnly(new object[] { new TagWithField() }), Is.False, "unset = not admin-only");
            Assert.That(KyNetworkRules.IsAdminOnly(new object[] { new OtherTag(), "text", null }), Is.False);
            Assert.That(KyNetworkRules.IsAdminOnly(new List<object>()), Is.False);
            Assert.That(KyNetworkRules.IsAdminOnly(null), Is.False);
        }
    }
}
