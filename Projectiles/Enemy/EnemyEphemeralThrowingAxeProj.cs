using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyEphemeralThrowingAxeProj : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 2;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.width = 52;
            Projectile.height = 52;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 100;
            Projectile.scale = 0.8f;
        }

        public override void AI()
        {
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 57, 0f, 0f, 80, default, 1f);
            Main.dust[dust].noGravity = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Ichor, 10 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            if (!Projectile.active)
            {
                return;
            }

            Projectile.timeLeft = 0;
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 57, 0f, 0f, 0, default, 1f);
            }
            Projectile.active = false;
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D glowTexture = ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/EnemyEphemeralThrowingAxeProj_Glowmask").Value;
            Vector2 origin = glowTexture.Size() / 2f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 1; i <= 3; i++)
            {
                Vector2 offset = Projectile.velocity * -i * 0.5f;
                float alpha = 0.2f * (1f - (i / 6f));
                Main.EntitySpriteDraw(glowTexture, drawPosition + offset, null, Color.White * alpha, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(glowTexture, drawPosition, null, Color.White * 0.2f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
        }
    }
}
