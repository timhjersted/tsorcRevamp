using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only axe the Tibian Valkyrie carries in her back hand. Deliberately shares the staged
    /// 80x80 Gilded Dual-Bladed Axe art (permanent texture sharing with the future player weapon, not a placeholder);
    /// no player class owns that art yet, so the path is a literal. Haft butt (0.7, 78.3), top of the head (71.7, 7.3),
    /// grip 25% up the haft at (18.5, 60.5). Double-bladed, so it never mirrors on reversed arcs.</summary>
    public class EnemyGildedDualBladedAxe : ModItem
    {
        public const string ArtPath = "tsorcRevamp/Content/Items/Weapons/Melee/Axes/GildedDualBladedAxe";

        public override string Texture => ArtPath;

        public override void SetDefaults()
        {
            Item.width = 80;
            Item.height = 80;
            Item.damage = 1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.DamageType = DamageClass.Melee;
        }
    }
}
