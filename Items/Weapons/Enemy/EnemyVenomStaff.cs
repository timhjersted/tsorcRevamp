using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Enemy
{
    /// <summary>
    /// Enemy copy of the vanilla Venom Staff — purely the held-weapon sprite + use-animation timing for
    /// an invader.  The actual shotgun-blast projectiles are spawned by the invader's DoRangedAttack
    /// (see <see cref="NPCs.Invaders.CursedDragonInvader"/>), not by this item.
    /// </summary>
    public class EnemyVenomStaff : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.VenomStaff;

        public override void SetStaticDefaults()
        {
            Item.staff[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.damage = 1;
            Item.knockBack = 2f;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.UseSound = SoundID.Item20;
            Item.shoot = ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyVenomStaffProj>();
            Item.shootSpeed = 11f;
            Item.DamageType = DamageClass.Magic;
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }
    }
}
