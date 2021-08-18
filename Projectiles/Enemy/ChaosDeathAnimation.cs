using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class ChaosDeathAnimation: ModProjectile
	{
		public override void SetDefaults()
		{
			Main.projFrames[projectile.type] = 14;
			projectile.height = 1;
			projectile.tileCollide = false;
			projectile.width = 1;
			projectile.scale = 2;
		}

		int chaosdacount1 = 0;
        public override void AI()
        {
			projectile.frameCounter++;
			if (projectile.frameCounter > 3)
			{
				projectile.frame++;
				projectile.frameCounter = 0;
			}
			if (projectile.frame >= 14)
			{
				projectile.Kill();
				return;
			}
		}
        /**
		#region Frames
		public override void FindFrame(int currentFrame)
		{
			int num = 1;
			if (!Main.dedServ)
			{
				num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
			}
			if (npc.velocity.X < 0)
			{
				npc.spriteDirection = -1;
			}
			else
			{
				npc.spriteDirection = 1;
			}
			npc.rotation = npc.velocity.X * 0.08f;
			npc.frameCounter += 1.0;
			if (npc.frameCounter >= 4.0)
			{
				npc.frame.Y = npc.frame.Y + num;
				npc.frameCounter = 0.0;
				chaosdacount1 += 1;
			}
			if (npc.frame.Y >= num * Main.npcFrameCount[npc.type])
			{
				npc.frame.Y = 0;
			}
			if (chaosdacount1 >= 13)
			{
				npc.life = 0;
			}
		}
		#endregion **/
    }
}