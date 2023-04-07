using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{
    public class OrbOfDeceptionFlame : ModProjectile
    {
        public float angularSpeed = 0.3f;
        public float circleRad1 = 50f;

        public float currentAngle;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 9;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }
        public override void SetDefaults()
        {
			Projectile.width = 16;
            Projectile.height = 16;
            Projectile.scale = 1.5f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 900;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Vector2 visualplayercenter = player.Center;
            float maxDetectRadius = 500f;
            float projSpeed = 5f;


            NPC closestNPC = FindClosestNPC(maxDetectRadius);
            if (closestNPC == null)
            {
                currentAngle += angularSpeed / (circleRad1 / 15);

                Vector2 offset = new Vector2(MathF.Sin(currentAngle), MathF.Cos(currentAngle)) * circleRad1;

                Projectile.position = visualplayercenter + offset;

                return;
            }

            Visuals();
            Projectile.velocity = (closestNPC.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * projSpeed;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public NPC FindClosestNPC(float maxDetectDistance)
        {
            NPC closestNPC = null;

            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

            for (int k = 0; k < Main.maxNPCs; k++)
            {
                NPC target = Main.npc[k];
                if (target.CanBeChasedBy())
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);

                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestNPC = target;
                    }
                }
            }
            return closestNPC;
        }
        private void Visuals()
        {
            Lighting.AddLight(Projectile.Center, Color.Firebrick.ToVector3() * 0.78f);
            Dust.NewDust(Projectile.TopLeft, 16, 16, DustID.HeatRay, 0, 0, 200, Color.Firebrick, 1f);
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            Player player = Main.player[Projectile.owner];
            player.statMana += player.GetManaCost(player.HeldItem) / 2;
        }
    }
}