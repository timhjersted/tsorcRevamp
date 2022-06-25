using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class DraxEX : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Drax EX");
            Tooltip.SetDefault("Congratulations on beating the final secret boss of the game!" +
                                "\nYou are truly a hero of the ages!;" +
                                "\nThis game was over 10 months in development." +
                                "\nIf you really enjoyed the game and want to say thanks" +
                                "\nyou can donate to the non-profit I work for:" +
                                "\nwww.filmsforaction.org/donate" +
                                "\nAs always, I'd love to hear your comments and feedback, too." +
                                "\ntimhjersted@gmail.com");
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 1;
            Item.useTime = 1;
            Item.damage = 500;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Blue;
            Item.shootSpeed = 1;
            Item.value = 20000;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.MegaDrill>();
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-8, 0);
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if ((player.direction == -1) && ((Main.mouseX + Main.screenPosition.X) > (player.position.X + player.width * 0.5f)))
            {
                player.direction = 1;
            }
            if ((player.direction == 1) && ((Main.mouseX + Main.screenPosition.X) < (player.position.X + player.width * 0.5f)))
            {
                player.direction = -1;
            }

            if (player.direction == 1)
            {
                player.itemRotation = (float)Math.Atan2((Main.mouseY + Main.screenPosition.Y) - (player.position.Y + player.height * 0.5f),
                (Main.mouseX + Main.screenPosition.X) - (player.position.X + player.width * 0.5f));
            }
            else player.itemRotation = (float)Math.Atan2((player.position.Y + player.height * 0.5f) - (Main.mouseY + Main.screenPosition.Y), (player.position.X + player.width * 0.5f) - (Main.mouseX + Main.screenPosition.X));

            float targetrotation = (float)Math.Atan2(((Main.mouseY + Main.screenPosition.Y) - player.position.Y), ((Main.mouseX + Main.screenPosition.X) - player.position.X));
            for (int j = 0; j < 6; j++)
            {
                int shot = Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center.X + (float)Math.Cos(targetrotation) * 60, player.Center.Y + (float)Math.Sin(targetrotation) * 60, 0, 0, ModContent.ProjectileType<Projectiles.MegaDrill>(), 500, 1f, player.whoAmI);
                Main.projectile[shot].timeLeft = 100;
                Main.projectile[shot].scale = 5f;
                Main.projectile[shot].position.X += Main.rand.Next(-16, 16);
                Main.projectile[shot].position.Y += Main.rand.Next(-16, 16);
                Main.projectile[shot].rotation = targetrotation;
            }
            return false;
        }
    }
}
