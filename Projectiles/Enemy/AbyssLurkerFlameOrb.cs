using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class AbyssLurkerFlameOrb : ModProjectile
    {
        const int ReleasedTime = 3 * 60;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.scale = 1.1f;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.9f;
            Projectile.timeLeft = 60 * 60;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * 0.75f);
            Animate();

            if (Projectile.ai[1] >= 0)
            {
                OrbitOwner();
                return;
            }

            SeekTarget();
        }

        void Animate()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }
        }

        void OrbitOwner()
        {
            int owner = (int)Projectile.ai[0];
            if (owner < 0 || owner >= Main.maxNPCs || !Main.npc[owner].active)
            {
                Projectile.Kill();
                return;
            }

            NPC npc = Main.npc[owner];
            int slot = (int)Projectile.ai[1];
            Projectile.rotation = 0f;
            Projectile.localAI[0] += 0.025f;
            Vector2 orbitOffset = new Vector2(15.6f, 0).RotatedBy(Projectile.localAI[0] + slot * MathHelper.TwoPi / 3f);
            Projectile.Center = npc.Top + new Vector2(0, -56) + orbitOffset;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 10;

            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PinkTorch, 0, 0, 120, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }

        void SeekTarget()
        {
            Projectile.ai[1]++;
            if (Projectile.ai[1] >= 0)
            {
                Projectile.Kill();
                return;
            }

            Player target = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
            if (target.active && !target.dead)
            {
                Vector2 targetVelocity = Projectile.DirectionTo(target.Center) * 2.1f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, targetVelocity, 0.035f);
            }

            FaceVelocity();

            Projectile.alpha = 0;
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PinkTorch, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
            }
        }

        void FaceVelocity()
        {
            if (Projectile.velocity.LengthSquared() > 0.01f)
            {
                Projectile.spriteDirection = Projectile.velocity.X < 0f ? -1 : 1;
                Projectile.rotation = MathHelper.PiOver2;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<AbyssInferno>(), 4 * 60, false);
            SpawnFlameDust(25, 4.6f, 1.9f);
        }

        public override void OnKill(int timeLeft)
        {
            SpawnFlameDust(15, 3.2f, 1.7f);
        }

        void SpawnFlameDust(int count, float speed, float scale)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(speed, speed);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PinkTorch, velocity.X, velocity.Y, 80, new Color(255, 135, 35), scale);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
