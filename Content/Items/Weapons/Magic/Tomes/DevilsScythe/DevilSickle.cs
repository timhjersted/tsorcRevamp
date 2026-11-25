using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Tomes.DevilsScythe
{
    public class DevilSickle : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.scale = 1.05f;
            Projectile.alpha = 200;
            Projectile.timeLeft = 300;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            //Projectile.usesIDStaticNPCImmunity = true; used to be local but honestly this works best with normal/global iframes
            //Projectile.idStaticNPCHitCooldown = 22; would be insanely op with local now
        }
        public override void AI()
        {
            bool isStickingToTile = Projectile.velocity == Vector2.Zero;
            int dust = Dust.NewDust(
            Projectile.position - (isStickingToTile ? new Vector2(Projectile.width * 4, Projectile.height * 4) : new Vector2(Projectile.width * 2, Projectile.height * 2)),
            Projectile.width * (isStickingToTile ? 8 : 4),
            Projectile.height * (isStickingToTile ? 8 : 4),
            DustID.Torch,
            Projectile.velocity.X,
            Projectile.velocity.Y,
            90,
            default,
            2f
            );
            Main.dust[dust].noGravity = true;
            Lighting.AddLight(Projectile.Center, 0.9f, 0.5f, 0f);

            if (!isStickingToTile)
            {
                Projectile.rotation += Projectile.direction * 0.8f;
            }

            if (Projectile.velocity.X <= 12f && Projectile.velocity.Y <= 12f && Projectile.velocity.X >= -12f && Projectile.velocity.Y >= -12f)
            {
                Projectile.velocity.X *= 1.05f;
                Projectile.velocity.Y *= 1.05f;
            }
        }

        public bool collided;
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = Vector2.Zero;
            return false;
        }

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            hitbox.Inflate(Projectile.width * 2, Projectile.height * 2);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White, 
                Projectile.rotation,
                frame.Size() / 2f,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f); 
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 1; i <= 5; i++)  
            {
                Vector2 offset = Projectile.velocity * -i * 0.7f;  
                float alpha = 0.6f * (1f - (i / 6f));  

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    null,
                    lightColor * alpha,  
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None,
                    0
                );
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(5))
            {
                target.AddBuff(BuffID.OnFire3, 5 * 60); //420 blaze it 
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.rand.NextBool(5) && info.PvP)
            {
                target.AddBuff(BuffID.OnFire, 7 * 60);
            }
        }
    }
}
