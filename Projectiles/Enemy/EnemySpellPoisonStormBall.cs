using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemySpellPoisonStormBall : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Enemy Spell Great Poison Strike Ball");
        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 23;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.light = 0.8f;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = 1f;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 0;
        }

        public override void AI()
        {

        }

        public override void OnKill(int timeLeft)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStorm>(), Projectile.damage, 8f, Projectile.owner);
            //Terraria.Audio.SoundEngine.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 110, 0.3f, -0.01f); //crystal serpent split, paper, thud, faint high squeel 



        }

    }
}