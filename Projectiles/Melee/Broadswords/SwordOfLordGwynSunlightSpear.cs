using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    class SwordOfLordGwynSunlightSpear : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/GwynLightningSpear";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 180;
            Projectile.light = 0.8f;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (Projectile.ai[0] == 1f)
                {
                    Projectile.penetrate = 5;
                    Projectile.scale = 1.12f;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (++Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }
            for (int i = 0; i < 2; i++)
            {
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, 0f, 0f, 50, default, 1.25f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = -Projectile.velocity * 0.08f;
            }
            Lighting.AddLight(Projectile.Center, 0.85f, 0.7f, 0.25f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Daybreak, 4 * 60);
            target.AddBuff(BuffID.OnFire3, 4 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.55f, Pitch = 0.25f }, Projectile.Center);
            if (Projectile.ai[0] == 1f && Main.myPlayer == Projectile.owner)
            {
                for (int j = 0; j < 5; j++)
                {
                    Vector2 vel = (MathHelper.TwoPi * j / 5f).ToRotationVector2() * 5f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel, ModContent.ProjectileType<LordGwynShortFlame>(), Projectile.damage / 3, Projectile.knockBack * 0.5f, Projectile.owner);
                }
            }
            for (int i = 0; i < 20; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4.5f, 4.5f);
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, vel.X, vel.Y, 45, default, 1.45f);
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
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, origin, 0.6f, fx, 0);
            return false;
        }
    }
}
