using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class HomingFireball : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Enemy Spell Great Fireball Ball");

        }
        public override void SetDefaults()
        {
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.height = 16;
            Projectile.width = 16;
            Projectile.light = 0.8f;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            UsefulFunctions.HomeOnEnemy(Projectile, 99999, 12, false, 3, false);

            int thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.InfernoFork, 0, 0, 100, default, 2f);
            Main.dust[thisDust].noGravity = true;

            Projectile.rotation += 0.25f;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.DefenseEffectiveness /= 2;
            modifiers.DisableCrit();
        }
        public override bool PreKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item100 with { Volume = 0.5f }, Projectile.Center); // cursed flame wall, lasts a bit longer than flame
                                                                                                                         //Terraria.Audio.SoundEngine.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 125, 0.3f, .2f); //phantasmal bolt fire 2
                                                                                                                         //Terraria.Audio.SoundEngine.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X, Projectile.position.Y, 0, 0, ModContent.ProjectileType<Projectiles.FireballInferno1>(), 0, 6f, Projectile.owner);
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(12, 12);
                int thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 6, vel.X, vel.Y, 100, default, 2f);
                Main.dust[thisDust].noGravity = true;
            }
            return true;
        }
    }
}