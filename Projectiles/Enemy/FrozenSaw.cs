using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class FrozenSaw : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Frozen Orb");

        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.hostile = true;
            Projectile.height = 34;
            Projectile.tileCollide = false;
            Projectile.width = 34;
            Projectile.timeLeft = 150;
            Projectile.light = .3f;
            Main.projFrames[Projectile.type] = 4;
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 44;
            return true;
        }

        public override void AI()
        {
            Projectile.rotation++;
            if (Projectile.velocity.X <= 10 && Projectile.velocity.Y <= 10 && Projectile.velocity.X >= -10 && Projectile.velocity.Y >= -10)
            {
                Projectile.velocity.X *= 1.01f;
                Projectile.velocity.Y *= 1.01f;
            }

            if (Main.rand.Next(2) == 0)
            {

                Lighting.AddLight((int)Projectile.position.X / 16, (int)Projectile.position.Y / 16, 0f, 0.3f, 0.8f);
                return;

            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 2)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 4)
            {
                Projectile.frame = 0;
            }

        }
        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int buffLengthMod = 1;
            if (Main.expertMode)
            {
                buffLengthMod = 2;
            }

            target.AddBuff(BuffID.BrokenArmor, 300 / buffLengthMod);
            if (Main.rand.Next(10) == 0)
            {
                target.AddBuff(BuffID.Silenced, 180 / buffLengthMod);
                target.AddBuff(BuffID.Slow, 300 / buffLengthMod);
            }
        }
    }
}
