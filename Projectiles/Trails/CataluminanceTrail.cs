using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Trails
{
    class CataluminanceTrail : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Illuminant Trail");
        }
        public override void SetDefaults()
        {
            Projectile.damage = 0;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 99999999;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            trailWidth = 45;
            trailPointLimit = 900;
            trailMaxLength = 9999999;
           
            trailCollision = true;
            collisionFrequency = 5;
        }

        float timer = 0;
        public override void AI()
        {
            timer++;
            //A phase is 900 seconds long
            //Once that is over, stop adding new positions
            if (timer < 899)
            {
                base.AI();
            }

            //Once the boss is all the way back to that stage again, then start removing the old positions
            if(timer > 2700)
            {
                if (trailPositions.Count > 3)
                {
                    trailPositions.RemoveAt(0);
                    trailRotations.RemoveAt(0);
                }
                else
                {
                    Projectile.Kill();
                }
            }
        }

        public override void SetEffectParameters(Effect effect)
        {
            base.SetEffectParameters(effect);
        }
    }
}