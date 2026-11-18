using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Royal Knight weapon icon and swing clock. The puppet draws the full-length holdout.</summary>
    [LegacyName("RoyalKnightHalberd")]
    public class GreatRoyalHalberd : ModItem
    {
        // One 110x110 holdout frame. The tip points up-right on a 45 degree shaft, with the crescent below-right to lead an overhand cut.
        public const string HeldTexturePath = "tsorcRevamp/Content/Projectiles/Enemy/Weapons/GreatRoyalHalberdProj";
        public const float HeldTextureSize = 110f;
        public static readonly Vector2 HeldPoint = new Vector2(109f, 1f);
        public static readonly Vector2 HeldButt = new Vector2(1f, 109f);

        public override void SetDefaults()
        {
            Item.width = 80;
            Item.height = 80;
            Item.damage = 55;
            Item.knockBack = 6.5f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 27;
            Item.useTime = 27;
            Item.DamageType = DamageClass.Melee;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Lime;
        }
    }
}
