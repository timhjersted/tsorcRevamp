using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class ShatteredMoonlight : ModItem //Great earlygame boomerang, faster but shorter range than enchanted boomerang. Less KB than other boomerangs.
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("The broken blade of Leonhard" +
                               "\nLuckily serves well as a boomerang" +
                               "\nA soft glow like that of the moon seeps through the cracks");
        }
        public override void SetDefaults()
        {
            item.rare = ItemRarityID.Blue;
            item.damage = 22;
            item.height = 24;
            item.width = 24;
            item.knockBack = 6f;
            item.melee = true;
            item.useAnimation = 12;
            item.useTime = 12;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = 20000;
            item.noUseGraphic = true;
            item.noMelee = true;
            item.shootSpeed = 18;
            item.shoot = ModContent.ProjectileType<Projectiles.ShatteredMoonlight>();
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            if (Main.rand.Next(20) == 0)
            {
                Dust dust2 = Main.dust[Dust.NewDust(new Vector2(item.position.X, item.position.Y), item.width, item.height, 89, 0, 0, 50, default(Color), .8f)];
                dust2.velocity *= 0;
                dust2.noGravity = true;
                dust2.fadeIn = 1f;
            }
        }

        public override bool CanUseItem(Player player)
        {
            // Ensures no more than one boomerang can be thrown out
            return player.ownedProjectileCounts[item.shoot] < 1;
        }
    }
}