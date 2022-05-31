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
            Item.rare = ItemRarityID.Blue;
            Item.damage = 22;
            Item.height = 24;
            Item.width = 24;
            Item.knockBack = 6f;
            Item.melee = true;
            Item.useAnimation = 14;
            Item.useTime = 14;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = 20000;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.shootSpeed = 18;
            Item.shoot = ModContent.ProjectileType<Projectiles.ShatteredMoonlight>();
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            if (Main.rand.Next(20) == 0)
            {
                Dust dust2 = Main.dust[Dust.NewDust(new Vector2(Item.position.X, Item.position.Y), Item.width, Item.height, 89, 0, 0, 50, default(Color), .8f)];
                dust2.velocity *= 0;
                dust2.noGravity = true;
                dust2.fadeIn = 1f;
            }
        }

        public override bool CanUseItem(Player player)
        {
            // Ensures no more than one boomerang can be thrown out
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }
    }
}