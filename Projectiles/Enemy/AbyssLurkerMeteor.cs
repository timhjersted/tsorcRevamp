using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class AbyssLurkerMeteor : ModProjectile
    {
        const int Lifetime = 6 * 60;
        const int OrbDelay = 2 * 60;
        const int PreExplosionDustTime = 2 * 60;
        const int ExplosionHitboxSize = 120;
        bool expandedForExplosion;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/AbyssMeteor";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 64;
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.9f;
            Projectile.timeLeft = Lifetime;
            Projectile.rotation = MathHelper.Pi;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                SpawnMeteorSpawnDust();
            }
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => expandedForExplosion ? null : false;

        public override void AI()
        {
            Animate();
            EmitIdleDust();

            if (Projectile.timeLeft == Lifetime - OrbDelay)
            {
                FireFlameOrbs();
            }

            if (Projectile.timeLeft <= 3 && !expandedForExplosion)
            {
                ExpandForExplosion();
            }
        }

        void Animate()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 8)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }
        }

        void EmitIdleDust()
        {
            Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * 0.9f);

            if (Main.dedServ)
            {
                return;
            }

            if (Main.rand.NextBool(3))
            {
                Dust fire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(22f, 28f), DustID.PinkTorch, Main.rand.NextVector2Circular(0.6f, 0.6f), 120, default, Main.rand.NextFloat(1.1f, 1.7f));
                fire.noGravity = true;
            }

            if (Main.rand.NextBool(5))
            {
                Dust smoke = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 26f), DustID.Smoke, Main.rand.NextVector2Circular(0.3f, 0.3f), 190, new Color(65, 45, 35), Main.rand.NextFloat(1.0f, 1.6f));
                smoke.noGravity = true;
            }

            EmitPreExplosionDust();
        }

        void EmitPreExplosionDust()
        {
            if (Projectile.timeLeft > PreExplosionDustTime)
            {
                return;
            }

            float progress = 1f - Projectile.timeLeft / (float)PreExplosionDustTime;
            int dustCount = 1 + (int)(progress * 3f);
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(24f, 32f);
                Vector2 velocity = offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.8f, 2.2f) + Main.rand.NextVector2Circular(0.4f, 0.4f);
                Dust fire = Dust.NewDustPerfect(Projectile.Center + offset, DustID.PinkTorch, velocity, 80, default, Main.rand.NextFloat(1.2f, 1.9f + progress));
                fire.noGravity = true;
                fire.fadeIn = 0.6f + progress * 0.5f;
            }
        }

        void FireFlameOrbs()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
            if (!target.active || target.dead)
            {
                return;
            }

            Vector2 velocity = UsefulFunctions.Aim(Projectile.Center, target.Center, 2.1f);
            int orbDamage = (int)Projectile.ai[0];
            int orbType = ModContent.ProjectileType<AbyssLurkerFlameOrb>();
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity.RotatedBy(MathHelper.ToRadians(-40)), orbType, orbDamage, 0, Main.myPlayer, -1, -3 * 60);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity.RotatedBy(MathHelper.ToRadians(40)), orbType, orbDamage, 0, Main.myPlayer, -1, -3 * 60);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity.RotatedBy(MathHelper.ToRadians(10)), orbType, orbDamage, 0, Main.myPlayer, -1, -3 * 60);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity.RotatedBy(MathHelper.ToRadians(-10)), orbType, orbDamage, 0, Main.myPlayer, -1, -3 * 60);
        }

        void ExpandForExplosion()
        {
            expandedForExplosion = true;
            Projectile.alpha = 255;
            Projectile.Resize(ExplosionHitboxSize, ExplosionHitboxSize);
            Projectile.netUpdate = true;

            if (Main.netMode != NetmodeID.Server)
            {
                SpawnExplosionDust();
                SoundEngine.PlaySound(SoundID.Item74 with { PitchVariance = 0.25f }, Projectile.Center);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.AbyssTeleportBlast>(), 0, 0, Main.myPlayer);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.AbyssTeleportMistLinger>(), 0, 0, Main.myPlayer, 1, 60);
            }
        }

        void SpawnMeteorSpawnDust()
        {
            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 3.2f);
                Dust smoke = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 26f), DustID.Smoke, velocity, 130, new Color(95, 78, 68), Main.rand.NextFloat(1.9f, 3.2f));
                smoke.noGravity = true;
                smoke.fadeIn = 1.0f;
            }

            for (int i = 0; i < 22; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 7f);
                Dust fire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(16f, 20f), DustID.PinkTorch, velocity, 60, default, Main.rand.NextFloat(1.4f, 2.3f));
                fire.noGravity = true;
                fire.fadeIn = 0.8f;
            }
        }

        void SpawnExplosionDust()
        {
            for (int i = 0; i < 30; i++)
            {
                Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 30f);
                Dust smoke = Dust.NewDustPerfect(Projectile.Center + dir * Main.rand.NextFloat(4f, 18f), DustID.Smoke, dir * Main.rand.NextFloat(7f, 11f), 80, new Color(145, 105, 82), Main.rand.NextFloat(1.9f, 3.2f));
                smoke.noGravity = true;
                smoke.fadeIn = 0.8f;
            }

            for (int i = 0; i < 24; i++)
            {
                Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 24f + MathHelper.Pi / 24f);
                Dust fire = Dust.NewDustPerfect(Projectile.Center + dir * Main.rand.NextFloat(3f, 14f), DustID.PinkTorch, dir * Main.rand.NextFloat(9f, 13f), 50, default, Main.rand.NextFloat(1.7f, 2.8f));
                fire.noGravity = true;
                fire.fadeIn = 0.9f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<AbyssInferno>(), 4 * 60, false);
        }
    }
}
