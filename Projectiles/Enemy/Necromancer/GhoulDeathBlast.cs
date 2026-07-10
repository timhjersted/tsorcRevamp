using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///The Spellbound Ghoul's death detonation: the necromantic binding snaps where it fell. Red and
    ///purple dreamfire swirls inward over the full blast radius for ai[0] telegraph ticks (the swirl
    ///IS the radius warning), then the disc detonates — 150px radius (300px across), damage only in
    ///the short blast window. Circular collision. Also triggered by the Necromancer's sacrifice.
    ///</summary>
    class GhoulDeathBlast : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const float BlastRadius = 150f; //300px diameter
        const int BlastTicks = 8;

        int TelegraphTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 40;
        bool Blasting => Projectile.localAI[0] > TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = (int)(BlastRadius * 2f);
            Projectile.height = (int)(BlastRadius * 2f);
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TelegraphTicks + BlastTicks;
        }

        public override bool? CanDamage()
        {
            return Blasting;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.Distance(Projectile.Center, closest) <= BlastRadius;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            if (!Blasting)
            {
                //Red and purple flames swirling inward across the whole radius — the warning IS the area
                float progress = Projectile.localAI[0] / (float)TelegraphTicks;
                int count = 3 + (int)(progress * 4f);
                for (int i = 0; i < count; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius = Main.rand.NextFloat(BlastRadius);
                    Vector2 pos = Projectile.Center + angle.ToRotationVector2() * radius;
                    int type = Main.rand.NextBool() ? DustID.RedTorch : DustID.Shadowflame;
                    int dust = Dust.NewDust(pos, 4, 4, type, 0f, 0f, (int)(120 - progress * 50f), default, 1.1f);
                    Main.dust[dust].noGravity = true;
                    //Swirl, tightening toward the corpse as detonation nears
                    Main.dust[dust].velocity = angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 1.8f - angle.ToRotationVector2() * progress * 1.5f;
                }
                //Bright rim so the edge reads
                if (Main.rand.NextBool(2))
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = Projectile.Center + angle.ToRotationVector2() * BlastRadius;
                    int rim = Dust.NewDust(pos, 4, 4, DustID.RedTorch, 0f, 0f, 80, default, 1f);
                    Main.dust[rim].noGravity = true;
                    Main.dust[rim].velocity *= 0.2f;
                }
                Lighting.AddLight(Projectile.Center, 0.4f * progress, 0.1f * progress, 0.35f * progress);

                if (Projectile.localAI[0] >= TelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = -0.3f }, Projectile.Center);
                    UsefulFunctions.ScreenShake(Projectile.Center, 4f, 12);
                }
                return;
            }

            //Detonation: the disc erupts in dreamfire
            for (int i = 0; i < 16; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = Main.rand.NextFloat(BlastRadius);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * radius;
                int type = Main.rand.NextBool() ? DustID.RedTorch : DustID.Shadowflame;
                int dust = Dust.NewDust(pos, 4, 4, type, 0f, 0f, 40, default, Main.rand.NextFloat(1.4f, 2f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = angle.ToRotationVector2() * Main.rand.NextFloat(2f, 6f);
            }
            Lighting.AddLight(Projectile.Center, 0.9f, 0.25f, 0.7f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 3 * 60);
        }
    }
}
