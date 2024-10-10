using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Summon.Whips.SearingLash
{
    class SearingLashBat : ModProjectile
    {
        public const int ExtraUpdates = 2;
        public const int Frames = 5;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
            Main.projFrames[Projectile.type] = Frames;
        }
        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 22;
            Projectile.timeLeft = 600 * ExtraUpdates;
            Projectile.extraUpdates = ExtraUpdates - 1;
            Projectile.penetrate = 5;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            Projectile.scale = 0.75f;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            NPC target;
            if (player.HasMinionAttackTargetNPC)
            {
                target = Main.npc[player.MinionAttackTargetNPC];
                bool lineOfSight = Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, target.position, target.width, target.height);
                float between = Vector2.Distance(target.Center, Projectile.Center);
                if (lineOfSight && between < 600f)
                {
                    Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 3.5f;
                }
                else
                {
                    NPC closestNPC = FindClosestNPC(600);

                    if (closestNPC != null)
                    {
                        lineOfSight = Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, closestNPC.position, closestNPC.width, closestNPC.height);
                        if (lineOfSight)
                        {
                            Projectile.velocity = (closestNPC.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 3.5f;
                        }
                    }
                }
            }
            else
            {
                NPC closestNPC = FindClosestNPC(600);

                if (closestNPC != null)
                {
                    bool lineOfSight = Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, closestNPC.position, closestNPC.width, closestNPC.height);
                    if (lineOfSight)
                    {
                        Projectile.velocity = (closestNPC.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 3.5f;
                    }
                }
            }
            Projectile.spriteDirection = Projectile.direction;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.velocity.X < 0)
            {
                Projectile.rotation -= MathHelper.Pi;
            }

            int frameSpeed = 6 * ExtraUpdates;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
                Dust.NewDust(Projectile.TopLeft, Projectile.width, Projectile.height, DustID.Firefly, Main.rand.NextFloat(), Main.rand.NextFloat());
            }
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
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = oldVelocity * -1;
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, ModdedWhipProjectile.DefaultWhipDebuffDuration * 60);
        }
    }
}