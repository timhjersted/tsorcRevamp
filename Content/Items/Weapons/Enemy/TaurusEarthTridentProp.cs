using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only Earth Trident that Taurus Knight swaps into his hand for the javelin throw.
    /// The thrown projectile is TaurusJavelin; this item only exists so the puppet can hold the sprite.</summary>
    public class TaurusEarthTridentProp : ModItem
    {
        // Intentional, permanent texture share with the thrown projectile, so the held prop and the javelin
        // in flight are the same sprite. 16x108, tip at the TOP (8, 0), butt at the bottom (8, 107).
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EarthTrident));

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 108;
            Item.damage = 1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.DamageType = DamageClass.Magic;
            Item.value = 0;
        }
    }
}
