using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {

    public class EnemyRedLaser : EnemyGenericLaser {

       
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Laser");
        }

        public override string Texture => base.Texture;

        public override void SetDefaults() {
            projectile.width = 10;
            projectile.height = 10;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.magic = true;
            projectile.hide = true;

            FollowHost = true;
            LaserOrigin = Main.npc[HostIdentifier].Center;            
            TelegraphTime = 90;
            FiringDuration = 60;
            MaxCharge = 90;
            LaserLength = 3000;
            LaserColor = Color.Red;
            TileCollide = false;
            LaserDust = DustID.OrangeTorch;
            LineDust = true;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.RedLaserTransparent;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            LaserSize = 1f;
        }


        //This laser sweeps across its target, which you give it with ai[0]
        int rotDirection = 0;
        public override void AI()
        {
            NPC owner = Main.npc[(int)projectile.ai[1]];
            if(owner == null || owner.active == false)
            {
                projectile.active = false;
                return;
            }

            if (Charge < MaxCharge)
            {
                Player target = Main.player[(int)projectile.ai[0]];
                if (target != null)
                {
                    if (rotDirection == 0) //Only set this once, so no flipping
                    {
                        if (target.Center.X > owner.Center.X)
                        {
                            rotDirection = -1;
                        }
                        else
                        {
                            rotDirection = 1;
                        }
                    }
                    projectile.velocity = UsefulFunctions.GenerateTargetingVector(projectile.Center, target.Center, 1).RotatedBy(rotDirection * MathHelper.Pi / 3);
                }
            }
            else
            {
                projectile.velocity = projectile.velocity.RotatedBy(rotDirection * -MathHelper.PiOver2 / 60f);
            }
            base.AI();
        }
    }
}
