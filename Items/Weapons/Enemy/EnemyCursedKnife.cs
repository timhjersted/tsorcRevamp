using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Enemy
{
    /// <summary>
    /// Enemy held-sprite item for the Cursed Dragon's CursedKnives attack — provides the in-hand knife
    /// visual + throw-animation timing.  The actual knives are spawned by the invader's
    /// DoCursedKnivesThrow, not by this item.
    /// </summary>
    public class EnemyCursedKnife : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.ThrowingKnife;

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.damage = 1;
            Item.knockBack = 2f;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 18;
            Item.useTime = 18;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.Enemy.Weapons.CursedDragonKnife>();
            Item.shootSpeed = 11f;
            Item.DamageType = DamageClass.Melee;
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }
    }
}
