using Terraria.Audio;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles{
	public class OmnirsEnemySpellFire3Flame2 : ModProjectile
	{
		public override void SetDefaults()
        {
            
            Projectile.width = 62;
            Projectile.height = 62;
            Projectile.penetrate = 8;
            Projectile.timeLeft = 75;
            //Projectile.alpha = 100;
            Projectile.light = 1f;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Main.projFrames[Projectile.type] = 3;
        }
        public override void AI()
        {
            Projectile.frameCounter++;

            if (Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 3)
            {
                Projectile.frame = 0;
                return;
            }
            return;
        }
    }
}