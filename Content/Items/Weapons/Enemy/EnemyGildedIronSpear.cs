using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only spear the Tibian Valkyrie wields in her front hand and throws. Deliberately shares
    /// the staged 104x104 Gilded Iron Spear holdout art (permanent texture sharing, not a placeholder); no player class
    /// owns that art yet, so the path is a literal. The art runs head (1, 1) top-left to butt (102, 102) bottom-right:
    /// a -135 degree shaft, 142.8 texels long.</summary>
    public class EnemyGildedIronSpear : ModItem
    {
        public const string ArtPath = "tsorcRevamp/Content/Projectiles/Melee/Spears/GildedIronSpearProj";

        public override string Texture => ArtPath;

        public override void SetDefaults()
        {
            Item.width = 104;
            Item.height = 104;
            Item.damage = 1;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 32;
            Item.useTime = 32;
            Item.DamageType = DamageClass.Melee;
        }
    }
}
