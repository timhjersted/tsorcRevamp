using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class FlameRain : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            //projectile.pretendType = 85;
            aiType = 79;
            Projectile.alpha = 100;
            Projectile.aiStyle = 1;
            Projectile.timeLeft = 600;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.light = 0.8f;
            //projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;

            //projectile.aiStyle = 0; //2 aistyle declarations? hmm
            //projectile.hostile = true;
            //projectile.height = 34;
            Projectile.tileCollide = false;
            //projectile.width = 34;
            //projectile.timeLeft = 150;
            //projectile.light = .3f;
            Main.projFrames[Projectile.type] = 4;
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 15;
            return true;
        }
    }
}