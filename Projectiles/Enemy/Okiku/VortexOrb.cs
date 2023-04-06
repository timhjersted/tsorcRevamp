using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{
    public class VortexOrb : Projectiles.VFX.DynamicTrail
    {

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Vortex Orb");
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
        }

        Vector2 originPoint = Vector2.Zero;
        float radius;
        float angle;
        int chargeTime;
        Vector2 attraidiesPoint = Vector2.Zero;
        public override void AI()
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>()))
            {
                Projectile.Kill();
            }

            chargeTime++;
            angle += 0.02f;
            float maxRadius = 1000;
            if(Projectile.ai[1] >= 2)
            {
                maxRadius = 1300;
            }
            if(radius < maxRadius)
            {
                radius += 5;
            }
            if(originPoint == Vector2.Zero)
            {
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
            Projectile.Center = originPoint + new Vector2(radius, 0).RotatedBy(angle + Projectile.ai[0] * MathHelper.PiOver2);
            Dust.NewDust(Projectile.Center, 10, 10, DustID.Vortex);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (chargeTime > getChargeLimit())
                {
                    Player closestPlayer = UsefulFunctions.GetClosestPlayer(Projectile.Center);
                    if (closestPlayer != null)
                    {
                        Vector2 projVel = UsefulFunctions.GenerateTargetingVector(Projectile.Center, closestPlayer.Center, 1);
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
                            projVel = UsefulFunctions.GenerateTargetingVector(Projectile.Center, attraidiesPoint, 1);
                        }
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.Marilith.MarilithLightning>(), Projectile.damage, Projectile.knockBack, Main.myPlayer, 1);
                        chargeTime = 0;
                    }
                }
            }
        }

        public override void SetEffectParameters(Effect effect)
        {
            base.SetEffectParameters(effect);
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
    }
}
