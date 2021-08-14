using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemyCrystalKnightBolt : ModProjectile
    {
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Crystal Bolt");
		}
		public override void SetDefaults()
        {
            projectile.aiStyle = 1;
            projectile.hostile = true;
            projectile.friendly = false;
            projectile.height = 16;
            projectile.light = 1;
            projectile.ranged = true;
            projectile.penetrate = 8;
            projectile.scale = 1.3f;
            projectile.tileCollide = true;
            aiType = 4;
            projectile.width = 16;
            projectile.timeLeft = 300;
            projectile.ignoreWater = true;
        }



        public override void AI()
        {
            int num40 = Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.height, 15, projectile.velocity.X, projectile.velocity.Y, 250, default(Color), 1f);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 600, false);
            if (Main.expertMode)
            {
                Main.player[Main.myPlayer].AddBuff(BuffID.Frozen, 15, false); //slowed
                Main.player[Main.myPlayer].AddBuff(32, 300, false); //normal slow
            } else
            {
                Main.player[Main.myPlayer].AddBuff(BuffID.Frozen, 30, false); //slowed
                Main.player[Main.myPlayer].AddBuff(32, 600, false); //normal slow
            }
        }

        public override bool PreKill(int timeLeft)
        {
            projectile.type = 30;
            return true;
        }

    }
}