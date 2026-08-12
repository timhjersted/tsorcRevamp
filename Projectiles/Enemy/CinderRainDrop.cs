using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Great Red Knight's airborne-only Cinder Rain drop. Destined Death flame art, blood/wraith
    /// breakup, its matching debuff, and a synchronized expiry altitude distinguish it from poison rain.
    /// </summary>
    public class CinderRainDrop : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/DestinedDeathFlame";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.scale = 1f;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 100;
            Projectile.light = 0.55f;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.24f * (Projectile.velocity.X < 0f ? -1f : 1f);
            Lighting.AddLight(Projectile.Center, new Color(232, 30, 86).ToVector3() * 0.55f);

            // ai[0] is the fixed world-space expiry height chosen when the attack fires. This keeps
            // every lane aerial even when ceiling clearance forced its spawn point downward.
            if (Projectile.ai[0] != 0f && Projectile.velocity.Y > 0f
                && Projectile.Center.Y >= Projectile.ai[0])
            {
                Projectile.Kill();
                return;
            }

            if (Main.dedServ)
            {
                return;
            }

            Dust body = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                DustID.Blood,
                -Projectile.velocity * Main.rand.NextFloat(0.025f, 0.07f)
                    + Main.rand.NextVector2Circular(0.25f, 0.25f),
                95, default, Main.rand.NextFloat(0.75f, 1.2f));
            body.noGravity = true;

            if (Main.rand.NextBool(3))
            {
                Dust spark = Dust.NewDustPerfect(Projectile.Center,
                    DustID.TintableDustLighted,
                    -Projectile.velocity * Main.rand.NextFloat(0.08f, 0.16f)
                        + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    75, new Color(255, 72, 150), Main.rand.NextFloat(0.42f, 0.72f));
                spark.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                Dust soot = Dust.NewDustPerfect(Projectile.Center,
                    DustID.Wraith,
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.1f)
                        + Main.rand.NextVector2Circular(0.3f, 0.3f),
                    145, default, Main.rand.NextFloat(0.55f, 0.9f));
                soot.noGravity = true;
                soot.noLight = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<DestinedDeath>(), 600);
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Bleeding, 1800);
                target.AddBuff(BuffID.Weak, 150);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 10; i++)
            {
                bool wraith = i % 3 == 0;
                Dust mote = Dust.NewDustPerfect(Projectile.Center,
                    wraith ? DustID.Wraith : DustID.Blood,
                    Main.rand.NextVector2Circular(2.2f, 2.2f), wraith ? 145 : 95,
                    default, Main.rand.NextFloat(0.48f, 0.95f));
                mote.noGravity = true;
                mote.noLight = wraith;
            }
        }
    }
}
