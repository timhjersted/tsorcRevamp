using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class SmallFlameJet : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/ThrowingAxe";

        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.height = 10;
            Projectile.penetrate = -1;
            Projectile.width = 10;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.ai[0] == 0)
            {
                target.AddBuff(BuffID.OnFire, 300);
            }
            if (Projectile.ai[0] == 1)
            {
                target.AddBuff(BuffID.ShadowFlame, 8 * 60);
                target.AddBuff(BuffID.Darkness, 15 * 60);

            }
        }

        public override void AI()
        {
            Projectile.alpha = 255;

            if (Main.rand.NextBool(4) && Projectile.ai[0] == 0) //Fire
            {
                int z = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 6, 0f, 0f, 120, default(Color), Main.rand.NextFloat(1.5f, 3.5f));
                Main.dust[z].noGravity = true;
            }

            if (Main.rand.NextBool(4) && Projectile.ai[0] == 1) //Shadowflame
            {
                int z = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 120, default(Color), Main.rand.NextFloat(1.5f, 2.5f));
                Main.dust[z].noGravity = true;
            }
        }

    }
}
