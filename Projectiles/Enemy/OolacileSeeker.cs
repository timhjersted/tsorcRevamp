using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	public class OolacileSeeker : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Oolacile Seeker");
		}
		public override void SetDefaults()
		{
			projectile.aiStyle = 0;
			projectile.hostile = true;
			projectile.height = 34;
			projectile.tileCollide = false;
			projectile.width = 34;
			projectile.timeLeft = 750;
			Main.projFrames[projectile.type] = 4;
			projectile.light = 1;
		}

		Vector2[] lastpos = new Vector2[20];
		int lastposindex = 0;
		public override void AI()
		{
			projectile.rotation++;

			this.projectile.rotation = (float)Math.Atan2((double)this.projectile.velocity.Y, (double)this.projectile.velocity.X);

			if (this.projectile.timeLeft < 100)
			{
				this.projectile.scale *= 0.9f;
				this.projectile.damage = 0;
			}

			if (this.projectile.timeLeft > 200 && this.projectile.timeLeft < 500)
			{
				this.projectile.velocity.X -= (this.projectile.position.X - Main.player[(int)this.projectile.ai[0]].position.X) / 1000f;
				this.projectile.velocity.Y -= (this.projectile.position.Y - Main.player[(int)this.projectile.ai[0]].position.Y) / 1000f;

				this.projectile.rotation = (float)Math.Atan2((double)this.projectile.velocity.Y, (double)this.projectile.velocity.X);
				this.projectile.velocity.Y = (float)Math.Sin(this.projectile.rotation) * 8;
				this.projectile.velocity.X = (float)Math.Cos(this.projectile.rotation) * 8;
			}




			lastpos[lastposindex] = this.projectile.position;
			lastposindex++;
			if (lastposindex > 19) lastposindex = 0;



			int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 54, 0, 0, 100, Color.Black, 1.0f);
			Main.dust[dust].noGravity = true;

			projectile.frameCounter++;
			if (projectile.frameCounter > 2)
			{
				projectile.frame++;
				projectile.frameCounter = 3;
			}
			if (projectile.frame >= 4)
			{
				projectile.frame = 0;
			}
		}

        public override bool PreKill(int timeLeft)
        {
			projectile.type = 41;
            return base.PreKill(timeLeft);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.expertMode)
			{
				Main.player[Main.myPlayer].AddBuff(BuffID.Darkness, 9000, false);
				Main.player[Main.myPlayer].AddBuff(BuffID.Poisoned, 1800, false);
				Main.player[Main.myPlayer].AddBuff(BuffID.Bleeding, 9000, false);
				Main.player[Main.myPlayer].AddBuff(BuffID.BrokenArmor, 300, false);
			}
			else
			{
				Main.player[Main.myPlayer].AddBuff(BuffID.Darkness, 18000, false);
				Main.player[Main.myPlayer].AddBuff(BuffID.Poisoned, 3600, false);
				Main.player[Main.myPlayer].AddBuff(BuffID.Bleeding, 18000, false);
				Main.player[Main.myPlayer].AddBuff(BuffID.BrokenArmor, 600, false);
			}
		}
    }
}