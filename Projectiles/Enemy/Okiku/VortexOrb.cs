using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{
    public class VortexOrb : Projectiles.VFX.DynamicTrail
    {

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Vortex Orb");
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/VortexOrb", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        Vector2 originPoint = Vector2.Zero;
        float radius;
        float angle;
        int chargeTime;
        Vector2 attraidiesPoint = Vector2.Zero;
        public override void AI()
        {
            

            chargeTime++;
            angle += 0.02f;
            float maxRadius = 1000;

            base.AI();
            Lighting.AddLight(Projectile.Center, Color.Teal.ToVector3());

            if (Projectile.ai[1] >= 2)
            {
                maxRadius = 1300;
            }
            if(radius < maxRadius)
            {
                radius += 5;
            }
            if(originPoint == Vector2.Zero)
            {
                SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Thunder_0"), Projectile.Center);
                if (Projectile.ai[1] == 2)
                {
                    Projectile.timeLeft = 1200;
                }
                if (Projectile.ai[1] == 3)
                {
                    Projectile.timeLeft = 99999999;
                }
                originPoint = Projectile.Center;
                chargeTime = (int)(Projectile.ai[0] * getChargeLimit() / 4f);

                //Make them in sync in the final phase
                if (Projectile.ai[1] == 3)
                {
                    chargeTime = 0;
                }
            }

            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>()))
            {
                Projectile.Kill();
            }
            else
            {
                originPoint = Main.npc[UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>()).Value].Center;
            }


            Projectile.Center = originPoint + new Vector2(radius, 0).RotatedBy(angle + Projectile.ai[0] * MathHelper.PiOver2);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (chargeTime > getChargeLimit())
                {
                    Player closestPlayer = UsefulFunctions.GetClosestPlayer(Projectile.Center);
                    if (closestPlayer != null)
                    {
                        Vector2 projVel = UsefulFunctions.Aim(Projectile.Center, closestPlayer.Center, 1);
                        if (Projectile.ai[1] == 3)
                        {
                            if (attraidiesPoint == Vector2.Zero)
                            {
                                int? index = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>());
                                {
                                    if (index.HasValue)
                                    {
                                        attraidiesPoint = Main.npc[index.Value].Center;
                                    }
                                }
                            }
                            projVel = UsefulFunctions.Aim(Projectile.Center, attraidiesPoint, 1);
                        }
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.Marilith.MarilithLightning>(), Projectile.damage, Projectile.knockBack, Main.myPlayer, 1);
                        chargeTime = 0;
                    }
                }
            }
        }

        int timeFactor;
        public override void SetEffectParameters(Effect effect)
        {
            collisionEndPadding = (int)(trailPositions.Count * 23f / 32f);
            collisionPadding = trailPositions.Count / 8;
            visualizeTrail = false;
            timeFactor++;
            if (CalculateLength() < 500)
            {
                trailWidth = (int)trailCurrentLength / 2;
            }
            else
            {
                trailWidth = 50;
            }
            trailWidth = 70;
            trailPointLimit = 2000;
            trailMaxLength = 2250;
            if(Projectile.ai[1] >= 2)
            {
                trailPointLimit = 50;
                trailMaxLength = 200;
                trailWidth = 100;
            }
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/VortexOrb", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTextureTurbulent);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(timeFactor);
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return base.PreDraw(ref lightColor);
        }

        int getChargeLimit()
        {
            if (Projectile.ai[1] == 0)
            {
                return 180;
            }
            if (Projectile.ai[1] == 1)
            {
                return 280;
            }
            if (Projectile.ai[1] == 2)
            {
                return 300;
            }
            if (Projectile.ai[1] == 3)
            {
                return 314;
            }

            return 0;
        }

        public override bool? CanDamage()
        {
            return false;
        }
    }
}
