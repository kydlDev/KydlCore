// KySkillRules (D-104): a mod skill's number is Valheim's GetStableHashCode of its identifier (Jotunn's rule), so saved skill levels keep their skill.
using KydlCore.Core;

namespace KydlCore.Tests.Unit;

public class KySkillRulesTests
{
    [Test] public void TypeNumber_IsTheGamesStableHash()
    {
        using (Assert.EnterMultipleScope())
        {
            // values computed with the game's algorithm (StringExtensionMethods.GetStableHashCode, Valheim 1.0.21)
            Assert.That(KySkillRules.TypeNumber("Hammer"), Is.EqualTo(200814284));
            Assert.That(KySkillRules.TypeNumber("GunPowdered.Muskets"), Is.EqualTo(-2030942280));
            Assert.That(KySkillRules.TypeNumber(""), Is.EqualTo(371857150));
            Assert.That(KySkillRules.TypeNumber(null), Is.EqualTo(KySkillRules.TypeNumber("")));
            Assert.That(KySkillRules.TypeNumber("a"), Is.Not.EqualTo(KySkillRules.TypeNumber("b")));
        }
    }
}
