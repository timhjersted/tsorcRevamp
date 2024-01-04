using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class DarkExplosion2 : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Dark Explosion");
        }

        public override void SetDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
            Projectile.aiStyle = 0;
            Projectile.hostile = true;
            Projectile.alpha = 215;
            Projectile.height = 76;
            Projectile.scale = 2;
            Projectile.tileCollide = false;
            Projectile.width = 76;
            Projectile.timeLeft = 1500;
        }

        public override void AI()
        {
            //if (projectile.frame < 5)
            Projectile.frameCounter++;

            if (Projectile.frameCounter == 30)
            {
                Projectile.frame = 1;
                Projectile.alpha = 200;
            }

            if (Projectile.frameCounter == 60)
            {
                Projectile.frame = 2;
                Projectile.alpha = 175;
            }

            if (Projectile.frameCounter == 90)
            {
                Projectile.frame = 3;
                Projectile.alpha = 150;
            }
            if (Projectile.frameCounter == 120)
            {
                Projectile.frame = 4;
                Projectile.alpha = 125;
            }
            if (Projectile.frameCounter == 180)
            {
                Projectile.frame = 5;
                Projectile.alpha = 50;
                Projectile.hostile = true;
                for (int i = 0; i < 20; i++)
                {
                    int dustDeath = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 55, Main.rand.Next(-6, 6), Main.rand.Next(-6, 6), 200, Color.White, 2f);
                    Main.dust[dustDeath].noGravity = true;
                }
            }
            if (Projectile.frameCounter >= 182)
            {
                Projectile.active = false;
            }

            Projectile.rotation += 0.1f;

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 6, 0, 0, Projectile.alpha, Color.White, 2.0f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<PowerfulCurseBuildup>(), 36000, false);

            if (Main.expertMode)
            {
                target.AddBuff(39, 150, false); //cursed flames
                target.AddBuff(30, 1800, false); //bleeding
                target.AddBuff(33, 1800, false); //weak
            }
            else
            {
                target.AddBuff(39, 300, false); //cursed flames
                target.AddBuff(30, 3600, false); //bleeding
                target.AddBuff(33, 3600, false); //weak
            }
        }
    }
}