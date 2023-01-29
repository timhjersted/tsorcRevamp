using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    class RetDeathLaserTrail : DynamicTrail
    {
        public override void SetDefaults()
        {
            Projectile.tileCollide = false;
        }

        
    }
}