using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gwyn's Cinder Nova blast: an expanding ring of the First Flame (FlameRing.png, 3 frames). The
    ///flame ring is DRAWN at growing scale while a true annulus hitbox tracks its leading edge — so
    ///standing inside the ring after it passes, or rolling through it, is safe. ai[0] = max radius (px).
    ///</summary>
    class GwynCinderNova : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/GwynCinderNova";

        const float ExpandSpeed = 10f;
        const float RingHalfThickness = 30f;
        const float SpriteRingRadius = 190f; // the flame ring sits ~190px out in the 400px art

        float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 420f;
        float Radius => Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 900;  // broadphase; real collision is the annulus in Colliding()
            Projectile.height = 900;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 60;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = (int)(MaxRadius / ExpandSpeed) + 4;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.2f }, Projectile.Center); // fiery whoosh
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0] += ExpandSpeed;
            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 3;
            }
            //Ember supplement on the leading edge
            for (int i = 0; i < 6; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + ang.ToRotationVector2() * Radius;
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(pos, 4, 4, type, 0f, 0f, 60, default, 1.4f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = ang.ToRotationVector2() * 2f;
            }
            Lighting.AddLight(Projectile.Center, 1.1f, 0.6f, 0.2f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            float distClosest = Vector2.Distance(Projectile.Center, closest);
            float distFarthest = 0f;
            distFarthest = Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Top)));
            distFarthest = Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Top)));
            distFarthest = Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Bottom)));
            distFarthest = Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Bottom)));
            return distClosest <= Radius + RingHalfThickness && distFarthest >= Radius - RingHalfThickness;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 6 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            float scale = Radius / SpriteRingRadius;
            Color col = Color.White * MathHelper.Clamp(Projectile.timeLeft / 12f, 0.35f, 1f); // fade out as it dissipates
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, col, 0f, new Vector2(texture.Width / 2f, frameHeight / 2f), scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
