using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellIcestormBall : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Ice1Ball";
        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.width = 16;
            Projectile.tileCollide = true;
            Projectile.aiStyle = 1;
            Projectile.coldDamage = true;
        }

        public override void AI()
        {
            if (Projectile.soundDelay == 0 && Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) > 2f)
            {
                Projectile.soundDelay = 10;
                //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item30 with { Volume = 0.8f }, Projectile.Center); //ice materialize - good
                //Terraria.Audio.SoundEngine.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 120, 0.3f, .1f); //ice mist howl sounds crazy
            }
            int thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 15, 0, 0, 100, default, 2f);
            Main.dust[thisDust].noGravity = true;

            Projectile.rotation += 0.25f;
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ice Storm Shards");

        }


        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            //target.AddBuff(BuffID.Blackout, 360, false); //darkness
            target.AddBuff(ModContent.BuffType<Frostbite>(), 180, false);

        }

        public override void OnKill(int timeLeft)
        {

            for (int i = 0; i < 20; i++)
            {
                int thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 15, 0, 0, 100, default, 2f);
                Main.dust[thisDust].noGravity = true;
                Main.dust[thisDust].velocity.X += Main.rand.Next(-15, 15);
            }
            Vector2 positionOffset = new Vector2(1000, 0);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position + positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(-9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle1>(), Projectile.damage, 3f, Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position + positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(-9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle2>(), Projectile.damage, 3f, Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position + positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(-9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle3>(), Projectile.damage, 3f, Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position + positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(-9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle4>(), Projectile.damage, 3f, Projectile.owner);

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position - positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle1>(), Projectile.damage, 3f, Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position - positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle2>(), Projectile.damage, 3f, Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position - positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle3>(), Projectile.damage, 3f, Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position - positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle4>(), Projectile.damage, 3f, Projectile.owner);


            Vector2 projectilePos = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
            int num41 = Dust.NewDust(projectilePos, Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 2f);
            Main.dust[num41].noGravity = true;
            Main.dust[num41].velocity *= 2f;
            Dust.NewDust(projectilePos, Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 1f);
        }

    }
}
