using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Winter's Grasp: frost mist floods a disc around where the player was standing, then
    ///crystallizes. The crystallize window damages and briefly FREEZES anyone caught in the disc —
    ///unless they are moving fast at that instant (they "shatter free" untouched). Counterplay is
    ///motion, not position. ai[0] = telegraph ticks. Circular collision via Colliding().
    ///</summary>
    class GigasFrostZone : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const float ZoneRadius = 160f; //10 tiles
        const int CrystallizeTicks = 8;
        const float EscapeSpeed = 5.5f; //moving at least this fast at the crystallize tick = safe

        int TelegraphTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 25;
        bool Crystallizing => Projectile.localAI[0] > TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = (int)(ZoneRadius * 2f);
            Projectile.height = (int)(ZoneRadius * 2f);
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TelegraphTicks + CrystallizeTicks;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = -0.4f }, Projectile.Center);
        }

        public override bool? CanDamage()
        {
            return Crystallizing;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.Distance(Projectile.Center, closest) <= ZoneRadius;
        }

        public override bool CanHitPlayer(Player target)
        {
            return target.velocity.Length() < EscapeSpeed; //fast movers shatter free
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            if (!Crystallizing)
            {
                //Mist flooding the disc, swirling inward as the freeze approaches
                float progress = Projectile.localAI[0] / (float)TelegraphTicks;
                int count = 4 + (int)(progress * 5f);
                for (int i = 0; i < count; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius = Main.rand.NextFloat(ZoneRadius);
                    Vector2 pos = Projectile.Center + angle.ToRotationVector2() * radius;
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, 0f, (int)(140 - progress * 60f), default, 1.2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 1.5f - angle.ToRotationVector2() * progress; //swirl, tightening
                }
                if (Projectile.localAI[0] >= TelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.9f, Pitch = -0.6f }, Projectile.Center);
                }
                return;
            }

            //Crystallize: the whole disc flashes into ice crystal
            for (int i = 0; i < 14; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = Main.rand.NextFloat(ZoneRadius);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * radius;
                int dust = Dust.NewDust(pos, 4, 4, DustID.IceRod, 0f, 0f, 40, default, Main.rand.NextFloat(0.9f, 1.4f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.1f;
            }
            //Bright rim so the edge reads
            for (int i = 0; i < 6; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * ZoneRadius;
                int rim = Dust.NewDust(pos, 4, 4, DustID.IceTorch, 0f, 0f, 60, default, 1.3f);
                Main.dust[rim].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.5f, 0.8f, 1.1f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frozen, 36); //brief freeze — they were warned to keep moving
            target.AddBuff(BuffID.Chilled, 3 * 60);
        }
    }
}
