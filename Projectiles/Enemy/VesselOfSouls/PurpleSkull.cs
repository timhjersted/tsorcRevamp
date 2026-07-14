using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.VesselOfSouls
{
    ///<summary>
    ///The Vessel of Souls' universal hostile projectile: a spinning purple skull, spat from the mouth in
    ///fans, cones, novas, sentinel volleys and the death fountain. 4-frame animation. Optional weak homing
    ///lets a lazy fan punish standing still. Hostile projectiles deal 2× the passed damage on hit — author
    ///half-values in the boss.
    ///  ai[0] = homing factor (0 = straight; ~0.02–0.05 = gentle curve toward the nearest player)
    ///</summary>
    class PurpleSkull : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/VesselOfSouls/PurpleSkull";

        float Homing => Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 360;
        }

        public override void AI()
        {
            // Spin animation
            if (++Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            // Gentle homing toward the nearest living player (keeps the same speed, just curves)
            if (Homing > 0f)
            {
                Player target = FindTarget();
                if (target != null)
                {
                    float speed = Projectile.velocity.Length();
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity) * speed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, Homing);
                }
            }

            Projectile.rotation = 0f; // the skull sprite is upright; flip (not rotate) so it never inverts

            // Purple soul-trail
            if (Main.rand.NextBool(2))
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch, 0f, 0f, 120, default, 1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.2f;
            }
            Lighting.AddLight(Projectile.Center, 0.35f, 0.08f, 0.45f);
        }

        // Drawn UPRIGHT (no rotation) so it never appears upside down; just mirrored to face travel L/R.
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            int frameH = tex.Height / Main.projFrames[Type];
            Rectangle src = new Rectangle(0, Projectile.frame * frameH, tex.Width, frameH);
            SpriteEffects fx = Projectile.velocity.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, src, lightColor,
                0f, src.Size() / 2f, Projectile.scale, fx, 0);
            return false;
        }

        Player FindTarget()
        {
            Player closest = null;
            float bestSq = 900f * 900f;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead) continue;
                float dSq = Vector2.DistanceSquared(p.Center, Projectile.Center);
                if (dSq < bestSq)
                {
                    bestSq = dSq;
                    closest = p;
                }
            }
            return closest;
        }
    }
}
