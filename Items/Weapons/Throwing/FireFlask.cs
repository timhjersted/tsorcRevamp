using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Throwing
{
    class FireFlask : ModItem
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/FireFlask";

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Blue;
            Item.width = 18;
            Item.height = 22;
            Item.damage = 14;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 3f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = 1800;
            Item.shoot = ModContent.ProjectileType<global::tsorcRevamp.Projectiles.Ranged.FireFlask>();
            Item.shootSpeed = 7.5f;
            Item.useAnimation = 44;
            Item.useTime = 44;
            Item.UseSound = SoundID.Item1;
            Item.DamageType = DamageClass.Ranged;
            Item.mana = 10;
            Item.autoReuse = false;
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                Vector2 flaskVelocity = UsefulFunctions.BallisticTrajectory(position, Main.MouseWorld, Item.shootSpeed, 0.18f, highAngle: false, fallback: true);
                Projectile.NewProjectile(source, position, flaskVelocity, type, damage, knockback, player.whoAmI);
            }

            return false;
        }
    }
}
