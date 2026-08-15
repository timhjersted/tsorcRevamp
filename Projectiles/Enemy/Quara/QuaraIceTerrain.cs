using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.Projectiles.Enemy.Quara
{
    /// <summary>One 1x1 floor tile of Tide Rush water. It is a gameplay marker, not a dust budget-dependent tell.</summary>
    class QuaraWaterResidue : ModProjectile
    {
        const int IgniteDelay = 3;

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.timeLeft = 5 * 60;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
        }

        public override bool? CanDamage() => false;

        public void Ignite()
        {
            if (Projectile.ai[0] > 0f)
            {
                return;
            }
            Projectile.ai[0] = IgniteDelay;
            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            if (Projectile.ai[0] > 0f)
            {
                Projectile.ai[0]--;
                if (Projectile.ai[0] <= 0f)
                {
                    ReleaseFrostColumn();
                    Projectile.Kill();
                }
                return;
            }

            // A single low water mote stays directly above this floor tile for the residue's full
            // five-second mechanical lifetime. It is intentionally not the collision source.
            if (Main.netMode != NetmodeID.Server && Projectile.timeLeft % 10 == 0)
            {
                Dust water = Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-3f, 3f), -2f),
                    DustID.Water, new Vector2(0f, -0.16f), 105, default, Main.rand.NextFloat(0.62f, 0.88f));
                water.noGravity = true;
            }
        }

        void ReleaseFrostColumn()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center - new Vector2(0f, 64f), Vector2.Zero,
                ModContent.ProjectileType<QuaraFrostColumn>(), Projectile.damage, 0f, Main.myPlayer);

            // Each fired tile wakes only its immediate neighbours. Their three-tick ignition delay
            // makes the one-tile columns visibly race along the Tide Rush path rather than all flash at once.
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.type != Type || other.whoAmI == Projectile.whoAmI)
                {
                    continue;
                }
                if (Math.Abs(other.Center.X - Projectile.Center.X) <= 17f
                    && Math.Abs(other.Center.Y - Projectile.Center.Y) <= 17f)
                {
                    (other.ModProjectile as QuaraWaterResidue)?.Ignite();
                }
            }
        }
    }

    /// <summary>Eight tiles of rapidly rising, damaging frost. The growing collision column matches its dust front.</summary>
    class QuaraFrostColumn : ModProjectile
    {
        const int Lifetime = 12;

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        int ActiveTiles => Math.Min(8, Lifetime - Projectile.timeLeft + 1);

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 128;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
        }

        public override bool? CanDamage() => true;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            int activeHeight = ActiveTiles * 16;
            Rectangle liveColumn = new Rectangle(projHitbox.X, projHitbox.Bottom - activeHeight, projHitbox.Width, activeHeight);
            return liveColumn.Intersects(targetHitbox);
        }

        public override void AI()
        {
            int tileIndex = ActiveTiles - 1;
            Vector2 frostPoint = new Vector2(Projectile.Center.X, Projectile.Bottom.Y - tileIndex * 16f - 8f);
            for (int i = 0; i < 3; i++)
            {
                Dust frost = Dust.NewDustPerfect(frostPoint + Main.rand.NextVector2Circular(5f, 4f), DustID.Frost,
                    new Vector2(Main.rand.NextFloat(-0.7f, 0.7f), Main.rand.NextFloat(-2.2f, -0.7f)), 70, default,
                    Main.rand.NextFloat(0.78f, 1.28f));
                frost.noGravity = true;
            }
            Dust glint = Dust.NewDustPerfect(frostPoint + Main.rand.NextVector2Circular(4f, 3f), DustID.IceTorch,
                new Vector2(Main.rand.NextFloat(-0.45f, 0.45f), Main.rand.NextFloat(-1.6f, -0.6f)), 95, default,
                Main.rand.NextFloat(0.48f, 0.78f));
            glint.noGravity = true;
            Lighting.AddLight(frostPoint, 0.14f, 0.28f, 0.44f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frozen, 3 * 60);
            target.AddBuff(BuffID.Chilled, 9 * 60);
        }
    }

    /// <summary>One delayed aerial frost sprite for the final ice attack; it only emits visuals, then releases shards.</summary>
    class QuaraFrostSprite : ModProjectile
    {
        const int DustDuration = 3 * 60;
        const int DetonationDelay = 15;

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.timeLeft = 360;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            int spawnDelay = (int)Projectile.ai[0];
            int age = 360 - Projectile.timeLeft;
            if (age < spawnDelay)
            {
                return;
            }
            int activeAge = age - spawnDelay;
            if (activeAge < DustDuration)
            {
                if (Main.netMode != NetmodeID.Server && activeAge % 2 == 0)
                {
                    Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2Circular(11f, 11f);
                    Dust frost = Dust.NewDustPerfect(dustPosition, DustID.Frost,
                        (Projectile.Center - dustPosition) * 0.08f,
                        90, default, Main.rand.NextFloat(0.72f, 1.16f));
                    frost.noGravity = true;
                    if (activeAge % 10 == 0)
                    {
                        Dust glint = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(7f, 7f), DustID.IceTorch,
                            Main.rand.NextVector2Circular(0.5f, 0.5f), 100, default, Main.rand.NextFloat(0.45f, 0.72f));
                        glint.noGravity = true;
                    }
                }
                Lighting.AddLight(Projectile.Center, 0.10f, 0.22f, 0.42f);
                return;
            }

            if (activeAge == DustDuration + DetonationDelay)
            {
                ReleaseShardClump();
                Projectile.Kill();
            }
        }

        void ReleaseShardClump()
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.55f, Pitch = 0.25f }, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            int target = (int)Projectile.ai[1];
            if (target < 0 || target >= Main.maxPlayers || !Main.player[target].active || Main.player[target].dead)
            {
                return;
            }
            Vector2 aim = (Main.player[target].Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = aim.RotatedBy((i - 1) * 0.16f) * (7.5f + i * 0.45f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
                    ModContent.ProjectileType<GigasIceShard>(), Projectile.damage, 1f, Main.myPlayer,
                    0f, 3f, target);
            }
        }
    }
}
