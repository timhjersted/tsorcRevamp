
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Projectiles
{
    class SoulDrop : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Dark Soul");
        }
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 9999;
            playerRespawned = false;
        }

        Vector2[] soulPositions;
        float animationProgress = 0;
        bool playerRespawned = false;
        public override void AI()
        {
            //Just keep it alive indefinitely
            Projectile.timeLeft = 9999;

            if (Main.GameUpdateCount % 5 == 0)
            {
                animationProgress++;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i] != null && Main.player[i].active)
                {
                    if ((int)Projectile.ai[1] == Main.player[i].whoAmI)
                    {
                        if (!Main.player[i].dead)
                        {
                            playerRespawned = true;
                            float distance = Main.player[i].DistanceSQ(Projectile.Center);
                            if (distance < 48 * 48)
                            {
                                if (Main.player[i] != null && Main.player[i].active && !Main.player[i].dead)
                                {
                                    if (Main.myPlayer == Main.player[i].whoAmI)
                                    {
                                        Main.player[i].QuickSpawnItem(Main.player[i].GetSource_DropAsItem(), ModContent.ItemType<DarkSoul>(), (int)Projectile.ai[0]);

                                    }
                                    Projectile.Kill();
                                    //CombatText.NewText(player.Hitbox, Color.Purple, itemText);
                                }
                            }
                            else if (distance < 200 * 200)
                            {
                                Projectile.velocity = UsefulFunctions.Aim(Projectile.Center, Main.player[i].Center, 7);
                            }
                        }
                        else
                        {
                            if (playerRespawned && ModContent.GetInstance<tsorcRevampConfig>().DeleteDroppedSoulsOnDeath)
                            {
                                Projectile.Kill();
                            }
                        }

                    }
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (soulPositions == null)
            {
                //Generate a list of fake soul positions
                soulPositions = new Vector2[(int)Math.Log10((int)Projectile.ai[0]) * 2];
                if (soulPositions.Length < 1)
                {
                    soulPositions = new Vector2[1];
                }

                for (int i = 0; i < soulPositions.Length; i++)
                {
                    soulPositions[i] = Main.rand.NextVector2Circular(48, 48);
                }
            }


            Texture2D texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Materials/SoulOfDark", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            int frameHeight = texture.Height / 4;


            for (int i = 0; i < soulPositions.Length; i++)
            {
                int startY = frameHeight * (((int)animationProgress + i) % 4);
                Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
                Vector2 origin = sourceRectangle.Size() / 2f;

                float easedAnimation = 1;
                if (animationProgress < 30)
                {
                    easedAnimation = (float)Math.Sin((animationProgress / 30f) * MathHelper.PiOver2);
                }

                Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + (soulPositions[i] * easedAnimation),
                sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            return false;
        }

        public override bool? CanDamage()
        {
            return false;
        }
    }
}