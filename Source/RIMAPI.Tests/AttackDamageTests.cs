using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The per-hit damage the combat read gained (fox ticket #3451): a warg
    /// killed a two-colonist colony in five exchanges while the read said only
    /// "unarmed", so a run could not see that standing and trading blows was a
    /// loss.
    /// </summary>
    public class AttackDamageTests
    {
        [Fact]
        public void A_melee_verb_reports_its_blow()
        {
            AttackDamage.Hit hit = AttackDamage.Of(range: 1.5f, meleeDamage: 12f, projectileDamage: 0f);

            Assert.Equal(12f, hit.Damage);
            Assert.False(hit.Ranged);
            Assert.Equal("melee", hit.Kind);
        }

        [Fact]
        public void A_ranged_verb_reports_its_shot_not_its_stock()
        {
            // A rifle's verb reaches 25 cells and its damage is the bullet's;
            // the 9 a stock hit does is the melee figure, not this one.
            AttackDamage.Hit hit = AttackDamage.Of(range: 25f, meleeDamage: 9f, projectileDamage: 10f);

            Assert.Equal(10f, hit.Damage);
            Assert.True(hit.Ranged);
            Assert.Equal("ranged", hit.Kind);
        }

        [Fact]
        public void A_verb_without_damage_reads_zero_so_the_tool_says_nothing()
        {
            AttackDamage.Hit hit = AttackDamage.Of(range: 0f, meleeDamage: 0f, projectileDamage: 0f);

            Assert.Equal(0f, hit.Damage);
            Assert.Equal("melee", hit.Kind);
        }
    }
}
