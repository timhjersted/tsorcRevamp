using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy
{
    class SmallWeaponSlash : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/ThrowingAxe"; //invis so doesnt matter

        public override void SetDefaults()
        {
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.width = 24;
            projectile.height = 24;
            projectile.alpha = 255; //invis
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 12;
            projectile.melee = true;
        }

        int difference;
        public override void AI()
        {
            NPC owner = Main.npc[(int)projectile.ai[0]];

            if (projectile.ai[1] < 1)
            {
                ++projectile.ai[1];
                difference = (int)projectile.position.X - (int)owner.position.X;
            }

            if (projectile.ai[1] >= 1)
            {
                //Create a new Vector2 with length offsetDistance, and then rotate it toward the correct direction
                //Add that to the npc's position
                if (owner.direction == 1)
                {
                    projectile.position.X = owner.Center.X + difference - 10;
                }
                else
                {
                    projectile.position.X = owner.Center.X - difference - 42;
                }
            }
            
        }
    }
}