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

    public class EnemyLingeringLaser : EnemyGenericLaser {

       
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
            projectile.timeLeft = 999;

            FollowHost = true;
            LaserOrigin = Main.npc[HostIdentifier].Center;
            TelegraphTime = 180;
            FiringDuration = 120;
            MaxCharge = 180;
            LaserLength = 5000;
            LaserColor = Color.Red;
            TileCollide = false;
            LaserSize = 1.3f;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.RedLaserTransparent;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            softFlicker = true;
            LaserSound = SoundID.Item12.WithVolume(0.5f);

            CastLight = Main.rand.NextBool();

            Additive = true;
            //ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.LivingOceanDye), Main.LocalPlayer);

            //Static bool that toggles. Ensures only 50% of projectiles survive, and that they're in order from first to last spawned
            flipBool = !flipBool;
            if (flipBool)
            {
            //    projectile.Kill();
            }
        }

        public static bool flipBool = false;

        //This laser does almost nothing unique. As such its class is extremely simple: Just some parameters in its SetDefaults, and code in its AI to despawn half of them instead of firing (to avoid looking janky)

        Vector2 target;
        Vector2 initialTarget;
        Vector2 initialPosition;
        Player targetPlayer;
        bool aimLeft = false;
        bool decided = false;
        public override void AI()
        {
            //Track the player and aim tangental 300 units next to them. Constrains their movement.
            if (projectile.ai[0] >= 0)
            {
                if (targetPlayer == null)
                {
                    targetPlayer = Main.player[(int)projectile.ai[0]];
                    aimLeft = Main.rand.NextBool();
                }

                

                TelegraphTime = 180;
                FiringDuration = 100;
                MaxCharge = 180;

                //All this is to say: Aim 300 units to the left or right of the player, no matter what angle it's shooting at them from
                //Also, only track the player while it's charging. Once it starts firing its target point is locked in.
                if (Charge <= MaxCharge - 1)
                {
                    initialTarget = targetPlayer.Center;
                   
                }

                target = UsefulFunctions.GenerateTargetingVector(projectile.Center, initialTarget, 1);
                target.Normalize();
                target *= 300;

                if (aimLeft)
                {
                    target = target.RotatedBy(MathHelper.PiOver2);
                }
                else
                {
                    target = target.RotatedBy(-MathHelper.PiOver2);
                }

                target += initialTarget;
                projectile.velocity = UsefulFunctions.GenerateTargetingVector(projectile.Center, target, 1);

                //Failsafe. If the boss charges *through* the circle, it causes the lasers to go haywire. This turns them off if that happens.
                if (projectile.Distance(target) < 400 || projectile.Distance(targetPlayer.Center) < 400)
                {
                    projectile.Kill();
                }
            }
            
            //Projectile stays where it's spawned, and either fires at a point or at a small range around it
            if (projectile.ai[0] == -1 || projectile.ai[0] == -2 || projectile.ai[0] == -3)
            {
                if (projectile.ai[0] == -1)
                {
                    if (Main.rand.NextBool() && !decided)
                    {
                        projectile.Kill();
                    }
                    decided = true;
                    TelegraphTime = 80;
                    FiringDuration = 95;
                    MaxCharge = 80;
                }
                if (projectile.ai[0] == -2)
                {
                    if (Main.rand.NextBool() && !decided)
                    {
                        projectile.Kill();
                    }
                    TelegraphTime = 210;
                    FiringDuration = 45;
                    MaxCharge = 240;
                    decided = true;
                }
                if (projectile.ai[0] == -3)
                {
                    TileCollide = false;
                    TelegraphTime = 60;
                    FiringDuration = 15;
                    MaxCharge = 60;
                }

                //Make it sit where it spawned
                FollowHost = false;
                if(initialPosition == Vector2.Zero)
                {
                    initialPosition = projectile.Center;
                }
                projectile.Center = initialPosition;
                LaserOrigin = initialPosition;
                if (Main.rand.NextBool())
                {
                    Dust.NewDustPerfect(LaserOrigin, DustID.OrangeTorch, Main.rand.NextVector2Circular(-3, 3), Scale: 7).noGravity = true;
                }
            }

            //Projectile sticks to NPC, but only lasts a short time
            //Disabled for reasons explained in VanillaChanges
            /*
            if (projectile.ai[0] == -4)
            {
                TelegraphTime = 240;
                FiringDuration = 60;
                MaxCharge = 240;
                if (target == Vector2.Zero)
                {
                    target = Main.player[(int)projectile.ai[0]].Center;
                }
                projectile.velocity = UsefulFunctions.GenerateTargetingVector(projectile.Center, target, 1);
            }*/


            base.AI();
            if(Charge == MaxCharge - 1)
            {
                LaserSize = 0;
                LaserAlpha = 0;
            }

            if(LaserSize < 1.3f)
            {
                LaserSize += (1.3f / 30f);
                LaserAlpha += 1f / 30f;
            }
        }
    }
}
