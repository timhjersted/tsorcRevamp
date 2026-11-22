using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Halberds;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only halberd the Darkroot Sentry fights with. Deliberately shares the player Darkroot
    /// Halberd's 74x74 art (permanent texture sharing, not a placeholder) so the NPC sequencer never runs the player
    /// weapon's input/channel code. Point (70.5, 1.5), grip (12.5, 61.5), butt (1.5, 72.5).</summary>
    public class EnemyDarkrootHalberd : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(DarkrootHalberd));

        public override void SetDefaults()
        {
            Item.width = 74;
            Item.height = 74;
            Item.damage = 1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 42;
            Item.useTime = 42;
            Item.DamageType = DamageClass.Melee;
        }
    }
}
