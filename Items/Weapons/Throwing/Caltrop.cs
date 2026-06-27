using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Throwing
{
    class Caltrop : ModItem
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/Caltrop";

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Blue;
            Item.width = 18;
            Item.height = 18;
            Item.damage = 9;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 1.5f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = 1500;
            Item.shoot = ModContent.ProjectileType<global::tsorcRevamp.Projectiles.Melee.Caltrop>();
            Item.shootSpeed = 8f;
            Item.useAnimation = 36;
            Item.useTime = 36;
            Item.UseSound = SoundID.Item1;
            Item.DamageType = DamageClass.Melee;
            Item.mana = 8;
            Item.autoReuse = false;
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                global::tsorcRevamp.Projectiles.Melee.Caltrop.ThrowSpread(source, position, Main.MouseWorld, damage, knockback, player.whoAmI);
            }

            return false;
        }
    }
}
