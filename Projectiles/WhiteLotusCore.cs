using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class WhiteLotusCore : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.timeLeft = 200;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.knockBack = 4;
            Projectile.damage = 0;
            Projectile.light = 0.4f;
        }

        int count = 5;
        float dir = 0;
        public override void AI()
        {
            if (Projectile.ai[0] == 0)
            {
                dir = (Projectile.ai[1] * 2) - 1;
                for (int i = 0; i < count; i++)
                {
                    int proj = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position, Vector2.Zero, ModContent.ProjectileType<WhiteLotusPetal>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0, i);
                    WhiteLotusPetal petal = Main.projectile[proj].ModProjectile as WhiteLotusPetal;
                    petal.ownerWAI = Projectile.whoAmI;
                    petal.dir = dir;
                    petal.count = count;
                }
                Projectile.ai[0] = 1;
                //its an upgrade to the leaf blower, it has to have the same wiggly effect that the leaf blower does
                Projectile.ai[1] = Main.rand.Next(-5, 5) * 0.001f;
                Projectile.netUpdate = true;
            }
            Projectile.ai[0] += 0.15f;
            Projectile.rotation = Projectile.ai[0] * ((Projectile.ai[1] * 2) - 1);
            Dust.NewDust(Projectile.Center, 1, 1, DustID.WhiteTorch, Projectile.velocity.X * -0.5f, Projectile.velocity.Y * -0.5f);

            //after 20 frames, swerve a bit
            if (Projectile.ai[0] >= 3)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[1]);
            }
            if (Main.GameUpdateCount % 3 != 0)
                return;
            bool hasPetals = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == ModContent.ProjectileType<WhiteLotusPetal>())
                {
                    WhiteLotusPetal h = Main.projectile[i].ModProjectile as WhiteLotusPetal;
                    if (h.ownerWAI == Projectile.whoAmI)
                    {
                        hasPetals = true;
                        break;
                    }
                }
            }
            if (!hasPetals)
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass, Projectile.Center);
            base.OnKill(timeLeft);
        }
    }
}
