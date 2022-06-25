using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class ChaosDeathAnimation : ModProjectile
    {
        public override void SetDefaults()
        {
            Main.projFrames[Projectile.type] = 14;
            Projectile.height = 1;
            Projectile.tileCollide = false;
            Projectile.width = 1;
            Projectile.scale = 2;
        }

        //int chaosdacount1 = 0;
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 14)
            {
                Projectile.Kill();
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