using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellLightning4Ball : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Bolt1Ball";
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = 1;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 60;//was 250
            Projectile.aiStyle = 0;
        }
        public override void AI()
        {
            if (Projectile.ai[0] != 0)
            {
                Projectile.timeLeft = (int)Projectile.ai[0];
                Projectile.ai[0] = 0;
            }
            if (Projectile.soundDelay == 0 && Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) > 2f)
            {
                Projectile.soundDelay = 10;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item105 with { Volume = 0.3f }, Projectile.Center);//was 9
            }
            int num47 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 2f);
            Dust expr_2684 = Main.dust[num47];
            expr_2684.velocity *= 0.3f;
            Main.dust[num47].position.X = Projectile.position.X + (float)(Projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].position.Y = Projectile.position.Y + (float)(Projectile.height / 2) + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].noGravity = true;


            if (Projectile.velocity.X != 0f || Projectile.velocity.Y != 0f)
            {
                Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) - 2.355f;
            }

            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
                return;
            }
        }
        public override void OnKill(int timeLeft)
        {
           

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2), 0, 0, ModContent.ProjectileType<EnemySpellLightning4Bolt>(), (int)(this.Projectile.damage), 8f, Projectile.owner);

            }
        }
    }
}
