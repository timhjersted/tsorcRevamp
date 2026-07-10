using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///A lightweight sunlight spear — the volley/storm projectile fired by GwynSolarSpearNode. Shares
    ///the LightningSpear art (GwynLightningSpear.png), but flies straight and simply hits, with NONE
    ///of the marquee spear's delayed-bolt payload (that stays unique to Spear of the First Sun).
    ///</summary>
    class GwynSunlightSpear : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/GwynLightningSpear";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 26;
            Projectile.height = 18;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 240;
            Projectile.light = 0.6f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4;
            }
            if (Main.rand.NextBool(2))
            {
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, 0f, 0f, 80, default, 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
            }
            Lighting.AddLight(Projectile.Center, 0.6f, 0.5f, 0.2f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 4 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, vel.X, vel.Y, 40, default, 1.3f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);
            SpriteEffects fx = Projectile.velocity.X < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, origin, 0.45f, fx, 0);
            return false;
        }
    }
}
