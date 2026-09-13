using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// The damaging 100px-radius burst at the center of the returning Abyss Storm.
    /// Its damage is inherited unchanged from the outgoing/returning ring.
    /// </summary>
    class EnemySpellAbyssStormExplosion : ModProjectile
    {
        private const int FrameTicks = 5;
        private const int ActiveDamageTicks = 8;
        private const int ExplosionRadius = 100;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Explosion_WhiteBlueCloud";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = ExplosionRadius * 2;
            Projectile.height = ExplosionRadius * 2;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Main.projFrames[Type] * FrameTicks;
            // Native frames are 98x98; this places the visible cloud just outside the 100px radius.
            Projectile.scale = 2.1f;
        }

        public override bool? CanDamage() =>
            Projectile.timeLeft > Main.projFrames[Type] * FrameTicks - ActiveDamageTicks
                ? null
                : false;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item74 with
                    {
                        Volume = 0.8f,
                        Pitch = -0.25f,
                        PitchVariance = 0.08f,
                    }, Projectile.Center);
                    SpawnBurstDust();
                }
            }

            Lighting.AddLight(Projectile.Center, 0.18f, 0.45f, 0.9f);
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= FrameTicks)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = Math.Min(
                    Projectile.frame + 1,
                    Main.projFrames[Projectile.type] - 1);
            }
        }

        private void SpawnBurstDust()
        {
            const int DustCount = 60;
            for (int i = 0; i < DustCount; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / DustCount
                    + Main.rand.NextFloat(-0.08f, 0.08f)).ToRotationVector2();
                float speed = Main.rand.NextFloat(3.5f, 10f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction * Main.rand.NextFloat(4f, 28f),
                    DustID.BlueCrystalShard,
                    direction * speed,
                    90,
                    default,
                    Main.rand.NextFloat(1.1f, 2f));
                dust.noGravity = true;
                dust.fadeIn = 0.35f;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 nearest = Vector2.Clamp(
                Projectile.Center,
                targetHitbox.TopLeft(),
                targetHitbox.BottomRight());
            return Vector2.DistanceSquared(Projectile.Center, nearest)
                <= ExplosionRadius * ExplosionRadius;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, Main.expertMode ? 225 : 450, false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            Rectangle source = new Rectangle(
                0,
                Projectile.frame * frameHeight,
                texture.Width,
                frameHeight);
            Vector2 origin = source.Size() * 0.5f;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                source,
                Color.White,
                0f,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0);
            return false;
        }
    }
}
