using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>
    /// Presentation-only staff held by the Acolyte of Grand Occultist. It deliberately shares the Grand
    /// Occultist's sprite (a lesser copy of the master's staff is the minion read), drawn at 0.65 scale by
    /// the acolyte. 60x64 sprite; the shaft runs butt (0,63) to gem (55,0), an 83.6px diagonal.
    /// </summary>
    public class EnemyOolacileAcolyteStaff : ModItem
    {
        // Intentional, permanent texture share with the Grand Occultist's staff (not a placeholder).
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyOccultistStaff));

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
