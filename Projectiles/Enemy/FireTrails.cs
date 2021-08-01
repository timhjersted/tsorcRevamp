using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class FireTrails : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/FireBall";

        public override void SetDefaults() {
            projectile.width = 15;
            projectile.height = 15;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.MaxUpdates = 2;
            projectile.penetrate = 1;
            projectile.hostile = true;
            projectile.alpha -= 50;
            projectile.netUpdate = true;
        }

        private const int AI_Split_Counter_Slot = 0;
        private const int AI_Timer_Slot = 1;

        private const int AI_Max_Splits = 2; //the exponent. projectile can split this many times. this counter is remembered for a projectile's children.
        private const int AI_Projectile_Split_Rate = 2; //the base. projectile will split into this many children each time it splits.
        private const int AI_Split_Time = 30; //when does the projectile split, in frames
        private const int AI_Split_Angle = 15; //the spread angle for the child projectiles

        //all of this stuff before the AI makes the AI pretty
        //now instead of saying projectile.ai[0] i can say AI_Split_Count
        public float AI_Split_Count {
            get => projectile.ai[AI_Split_Counter_Slot];
            set => projectile.ai[AI_Split_Counter_Slot] = value;
        }

        public float AI_Timer {
            get => projectile.ai[AI_Timer_Slot];
            set => projectile.ai[AI_Timer_Slot] = value;
        }

        public override void AI() {

            projectile.rotation += 4f;

            Vector2 speedMod = new Vector2(projectile.velocity.X, projectile.velocity.Y);
            int z2 = Main.rand.Next(-1, 2);
            speedMod = RotateAboutOrigin(speedMod, (float)((Math.PI * z2) / 35f)); //the 28f controls the curve strength. too low and the projectile spins in circles
            projectile.velocity = speedMod;

            AI_Timer++;
            if (AI_Timer == AI_Split_Time) {

                if (AI_Split_Count < AI_Max_Splits) {
                    float rotation = MathHelper.ToRadians(AI_Split_Angle);
                    for (int i = 0; i < AI_Projectile_Split_Rate; i++) {
                        Vector2 shiftSpeed = new Vector2(projectile.velocity.X, projectile.velocity.Y).RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (AI_Projectile_Split_Rate - 1))); //evenly divide the projectiles among the spread angle
                        Projectile.NewProjectile(projectile.position, shiftSpeed, ModContent.ProjectileType<FireTrails>(), projectile.damage, projectile.knockBack, projectile.owner, AI_Split_Count + 1, 0); //the AI_Split_Count+1 here is what makes the recursion work. child projectiles inherit their parent's AI_Split_Count, plus one.
                    }
                }
                projectile.Kill(); //not necessary (we check if ai_timer is *exactly* 12) but to stay true to the rage's original ai, we kill it
            }
            if (AI_Timer % 7 == 0) { //spawn a trail fireball every 4 frames
                Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width / 2), projectile.position.Y + (float)(projectile.height / 2)), new Vector2(0, 0), ModContent.ProjectileType<FireTrail>(), projectile.damage, projectile.knockBack, projectile.owner);
                for (int j = -1; j < 2; j++) {
                    Dust.NewDust(projectile.position, projectile.width, projectile.height, 6, projectile.velocity.X * 2f * j, 0, 170, default, 0.7f); //emit dust sideways when trail fireballs are spawned
                }
            }
        }
        public Vector2 RotateAboutOrigin(Vector2 point, float rotation) {
            if (rotation < 0)
                rotation += (float)(Math.PI * 4);
            Vector2 u = point; //point relative to origin  

            if (u == Vector2.Zero)
                return point;

            float a = (float)Math.Atan2(u.Y, u.X); //angle relative to origin  
            a += rotation;

            //u is now the new point relative to origin  
            u = u.Length() * new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
            return u;
        }
    }
}
