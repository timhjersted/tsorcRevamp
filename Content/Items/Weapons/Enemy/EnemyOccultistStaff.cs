using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>
    /// Presentation-only casting staff held by the Grand Occultist of Oolacile in BOTH phases. Every dark
    /// spell in the kit fires from its gem. 60x64 sprite; the shaft runs butt (0,63) to gem (55,0), so the
    /// boss's MagicGripNorm sits ~15% up that diagonal and the tip is ~71px from the hand at draw scale 1.
    /// </summary>
    public class EnemyOccultistStaff : ModItem
    {

        public override void SetStaticDefaults() => Item.staff[Type] = true;

        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 64;
            Item.damage = 1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 26;
            Item.useTime = 26;
            Item.value = 0;
            Item.rare = ItemRarityID.White;
            Item.DamageType = DamageClass.Magic;
        }
    }
}
