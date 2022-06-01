using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemyCrystalKnightBoltII : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Freezing Crystal Bolt");
        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 16;
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.light = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 8;
            Projectile.scale = 1.3f;
            Projectile.tileCollide = true;
            Projectile.width = 16;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            int num40 = Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.height, 15, Projectile.velocity.X, Projectile.velocity.Y, 250, default(Color), 1f);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 600, false);
            //Main.player[Main.myPlayer].AddBuff(4, 1200, false); //gills. Which used to make you suffocate out of water. Guess this evil was too deep for modern Terraria...

            if (Main.expertMode)
            {
                Main.player[Main.myPlayer].AddBuff(BuffID.Frozen, 15, false); //slowed
                Main.player[Main.myPlayer].AddBuff(BuffID.Slow, 300, false); //normal slow
            }
            else
            {
                Main.player[Main.myPlayer].AddBuff(BuffID.Frozen, 30, false); //slowed
                Main.player[Main.myPlayer].AddBuff(BuffID.Slow, 600, false); //normal slow
            }
        }
    }
}