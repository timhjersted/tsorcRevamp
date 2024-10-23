using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class ScrewAttack : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.height = 34;
            Projectile.width = 34;
            Projectile.timeLeft = 800;
            Projectile.scale = 2f;
        }
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, .5f, .2f, .7f);
            Projectile.rotation += 0.5f;

            if (Main.player[(int)Projectile.ai[0]].position.X < Projectile.position.X)
            {
                if (Projectile.velocity.X > -10) Projectile.velocity.X -= 0.1f;
            }

            if (Main.player[(int)Projectile.ai[0]].position.X > Projectile.position.X)
            {
                if (Projectile.velocity.X < 10) Projectile.velocity.X += 0.1f;
            }

            if (Main.player[(int)Projectile.ai[0]].position.Y < Projectile.position.Y)
            {
                if (Projectile.velocity.Y > -10) Projectile.velocity.Y -= 0.1f;
            }

            if (Main.player[(int)Projectile.ai[0]].position.Y > Projectile.position.Y)
            {
                if (Projectile.velocity.Y < 10) Projectile.velocity.Y += 0.1f;
            }

            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 5, 0, 0, 50, Color.White, 1.0f);
                Main.dust[dust].noGravity = false;
            }
            Lighting.AddLight(Projectile.position, 0.7f, 0.2f, 0.2f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 2)
            {
                Projectile.frame++;
                Projectile.frameCounter = 3;
            }
            if (Projectile.frame >= 4)
            {
                Projectile.frame = 0;
            }
        }
        public override bool PreKill(int timeLeft)
        {
            Projectile.type = ProjectileID.DemonScythe;
            return true;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int buffMod = 1;
            if (Main.expertMode)
            {
                buffMod = 2;
            }
            target.AddBuff(BuffID.WitheredArmor, 600 / buffMod);
            target.AddBuff(BuffID.WitheredWeapon, 300 / buffMod);
        }

    }
}
