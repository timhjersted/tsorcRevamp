using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class LonginusPoke : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 100;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3600;
            Projectile.friendly = true; //can hit enemies
            Projectile.hostile = false; //can hit player / friendly NPCs
            Projectile.ownerHitCheck = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.scale = 1.2f;

        }
        public float moveFactor
        { //controls spear speed
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public override void AI()
        {
            //DrawOffsetX = -35;
            Player pOwner = Main.player[Projectile.owner];
            Projectile.Center = pOwner.RotatedRelativePoint(pOwner.MountedCenter, true);
            Projectile.direction = pOwner.direction;
            pOwner.heldProj = Projectile.whoAmI;
            pOwner.itemTime = pOwner.itemAnimation;

            if (!pOwner.frozen)
            {
                if (moveFactor == 0f)
                { //when initially thrown
                    moveFactor = 4.1f; //move forward
                    Projectile.netUpdate = true;
                }
                if (pOwner.itemAnimation < pOwner.itemAnimationMax / 2)
                { //after x animation frames, return
                    moveFactor -= 2.85f;
                }
                else
                { //extend spear
                    moveFactor += 4.1f;
                }

            }

            if (pOwner.itemAnimation == 0)
            {
                Projectile.Kill();
            }

            Projectile.Center += Projectile.velocity * moveFactor;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.ToRadians(45f);

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 90, Projectile.velocity.X * -0.2f, Projectile.velocity.Y * -0.2f, 70, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }

        public static Texture2D texture;
        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture);
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = sourceRectangle.Size() / 2f;

            Vector2 offset = Projectile.velocity * 25;

            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(DrawOffsetX, Projectile.gfxOffY) - offset,
                sourceRectangle, lightColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            return false;
        }

    }

}
