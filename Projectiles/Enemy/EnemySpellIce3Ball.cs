using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellIce3Ball : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Ice1Ball";
        public override void SetDefaults()
        {
            Projectile.aiStyle = 4;
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.width = 16;
            Projectile.coldDamage = true;
        }
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Enemy Spell Ice 3");

        }
        public override void AI()
        {
            if (Projectile.ai[0] != 0) //Dark Elf Mage version
            {
                Projectile.timeLeft = 80;
                Projectile.aiStyle = 1;
            }

            if (Math.Abs(Main.player[(int)Projectile.ai[1]].Center.X - Projectile.Center.X) < 8)
            {
                Projectile.Kill();
                Projectile.active = false;
            }
        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item30 with { Volume = 0.2f, Pitch = 0.3f }, Projectile.Center); //ice materialize - good
                                                                                                                            //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
            int Icicle = ModContent.ProjectileType<EnemySpellIce3Icicle>();
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height), 0, 5, Icicle, Projectile.damage, 3f, Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * 4), Projectile.position.Y + (float)(Projectile.height * 2), 0, 5, Icicle, Projectile.damage, 3f, Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * -2), Projectile.position.Y + (float)(Projectile.height * 2), 0, 5, Icicle, Projectile.damage, 3f, Projectile.owner);
            Vector2 projectilePos = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
            int num41 = Dust.NewDust(projectilePos, Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 2f);
            Main.dust[num41].noGravity = true;
            Main.dust[num41].velocity *= 2f;
            Dust.NewDust(projectilePos, Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 1f);
        }
    }
}
