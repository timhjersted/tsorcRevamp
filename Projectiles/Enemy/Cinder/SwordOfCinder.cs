using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Cinder
{
    class SwordOfCinder : ModProjectile
    {
        const int Lifetime = 120;
        const int OutboundTicks = 34;
        const float ReturnSpeed = 20f;
        const float BladeLength = 88f;
        const float BladeWidth = 16f;
        const float SpinSpeed = 0.34f;
        static readonly Vector2 HandleNorm = new Vector2(0.14f, 0.87f);

        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Broadswords/SeveringDusk";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
        }

        internal int OwnerIndex => (int)Projectile.ai[0];
        internal int Timer => Lifetime - Projectile.timeLeft;

        public override void AI()
        {
            Projectile.friendly = false;

            if (OwnerIndex < 0 || OwnerIndex >= Main.maxNPCs || !Main.npc[OwnerIndex].active)
            {
                Projectile.Kill();
                return;
            }

            NPC owner = Main.npc[OwnerIndex];
            if (Timer == 0)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            }

            float spinDirection = Math.Sign(Projectile.velocity.X);
            if (spinDirection == 0f)
            {
                spinDirection = owner.direction == 0 ? 1f : owner.direction;
            }
            Projectile.rotation += SpinSpeed * spinDirection;

            if (Timer < OutboundTicks)
            {
                Projectile.velocity *= 0.985f;
            }
            else
            {
                Vector2 toOwner = owner.Center - Projectile.Center;
                if (toOwner.LengthSquared() < 30f * 30f)
                {
                    Projectile.Kill();
                    return;
                }

                Vector2 returnVelocity = toOwner.SafeNormalize(Vector2.UnitX) * ReturnSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, returnVelocity, 0.14f);
            }

            Lighting.AddLight(Projectile.Center, new Color(150, 45, 220).ToVector3() * 0.9f);
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Shadowflame,
                    -Projectile.velocity * 0.08f,
                    90,
                    new Color(195, 70, 255),
                    0.95f);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = new Vector2(texture.Width * HandleNorm.X, texture.Height * HandleNorm.Y);
            Color drawColor = Projectile.GetAlpha(lightColor);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + Projectile.rotation.ToRotationVector2().RotatedBy(-MathHelper.PiOver4) * BladeLength,
                BladeWidth,
                ref point);
        }
    }
}
