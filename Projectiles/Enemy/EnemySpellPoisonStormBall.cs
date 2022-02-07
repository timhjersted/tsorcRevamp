using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemySpellPoisonStormBall : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Enemy Spell Great Poison Strike Ball");
        }
        public override void SetDefaults()
        {
            projectile.aiStyle = 23;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.width = 16;
            projectile.height = 16;
            projectile.light = 0.8f;
            projectile.penetrate = 1;
            projectile.magic = true;
            projectile.scale = 1f;
            projectile.tileCollide = true;
			projectile.timeLeft = 0;
        }

		public override void AI()
		{		
			
		}

		public override void Kill(int timeLeft)
        {
            Projectile.NewProjectile(projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStorm>(), projectile.damage, 8f, projectile.owner);
            //Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 110, 0.3f, -0.01f); //crystal serpent split, paper, thud, faint high squeel 
        }

	}
}