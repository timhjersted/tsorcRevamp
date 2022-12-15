using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    public class DebugTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("You should not have this" +
                "\nDev item used for testing purposes only" +
                "\nUsing this may cause irreversible effects on your world");
        }

        public override void SetDefaults()
        {
            Item.damage = 999999;
            Item.knockBack = 4;
            Item.crit = 4;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item11;
            Item.useTurn = true;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Magic;
            Item.autoReuse = true;
            Item.value = 10000;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = 24f;
            Item.shoot = ModContent.ProjectileType<Projectiles.BlackFirelet>();
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {            
            Main.NewText(player.position / 16);            
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center + new Vector2(10, 0), speed, ModContent.ProjectileType<Projectiles.Enemy.Triplets.RetOmegaLaser>(), damage, knockBack, Main.myPlayer);

            //Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, speed, ModContent.ProjectileType<Projectiles.Enemy.EnemyLightningStrike>(), damage, knockBack, Main.myPlayer);
            //NPC.NewNPC(source, 707 * 16, 1194 * 16, ModContent.NPCType<NPCs.Special.GwynBossVision>());
            //Terraria.Audio.SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.4f, Pitch = 0.0f });
            //Rectangle screenRect = new Rectangle((int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100, Main.screenWidth + 100, Main.screenHeight + 100);
            //speed.Normalize();
            //Main.NewText(Projectiles.Enemy.EnemyGenericLaser.Intersections(screenRect, position, speed));


            /*
			 tsorcRevampWorld.SuperHardMode = true;

			int projType = ModContent.ProjectileType<Projectiles.IdealArrow>();
			for (float i = 0.1f; i < 19; i *= 1.01f)
			{
				Vector2 trajectory = UsefulFunctions.BallisticTrajectory(player.Center, Main.MouseWorld, i, 0.07f, false, false);
				if (trajectory != Vector2.Zero)
				{
					trajectory += player.velocity;
					 Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, trajectory, projType, damage, knockBack, Main.myPlayer);
					i++; //Just to keep this from getting out of hand
				}
				trajectory = UsefulFunctions.BallisticTrajectory(player.Center, Main.MouseWorld, i, 0.07f, true, false);
				if (trajectory != Vector2.Zero)
				{
					trajectory += player.velocity;
					 Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, trajectory, projType, damage, knockBack, Main.myPlayer);
					i++;
				}
			}*/

            return false;
        }

        //For multiplayer testing, because I only have enough hands for one keyboard. Makes the player holding it float vaguely near the next other player.
        public override void UpdateInventory(Player player)
        {
            if (player.name == "MPTestDummy")
            {
                if (player.whoAmI == 0)
                {
                    if (Main.player[1] != null && Main.player[1].active)
                    {
                        player.position = Main.player[1].position;
                        player.position.X += 300;
                        player.position.Y += 300;
                    }
                }
                else
                {
                    if (Main.player[0] != null && Main.player[0].active)
                    {
                        player.position = Main.player[0].position;
                        player.position.X += 300;
                        player.position.Y += 300;
                    }
                }
            }
        }

        public override bool CanUseItem(Player player)
        {
            //if (player.name == "Zeodexic" || player.name.Contains("Sam") || player.name == "Chroma TSORC test")
            {
                return true;
            }
            //return false;
        }
    }
}