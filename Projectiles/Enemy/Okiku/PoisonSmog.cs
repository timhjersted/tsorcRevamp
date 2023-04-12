using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{
    public class PoisonSmog : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Poison Smog");
        }
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
        }
        public override void AI()
        {
            Projectile.rotation += 0.1f;
            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 6, 0, 0, 50, Color.Green, 3.0f);
                Main.dust[dust].noGravity = true; 
                Main.dust[dust].velocity = Vector2.Zero;
            }
            Lighting.AddLight((int)(Projectile.position.X / 16f), (int)(Projectile.position.Y / 16f), 0.4f, 0.1f, 0.1f);

            if (Projectile.velocity.X <= 4 && Projectile.velocity.Y <= 4 && Projectile.velocity.X >= -4 && Projectile.velocity.Y >= -4)
            {
                float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
                Projectile.velocity.X *= accel;
                Projectile.velocity.Y *= accel;
            }
        }
        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 44; //killpretendtype
            return true;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 600, false);
            target.AddBuff(BuffID.Tipsy, 1800, false);
        }
    }
}
