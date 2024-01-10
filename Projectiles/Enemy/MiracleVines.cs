using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class MiracleVines : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = 75; //this projectile actually uses its timeLeft value. the final set of vines lasts longer, so we need to do the kill check differently
            Projectile.netUpdate = true;
        }

        private const int AI_Split_Counter_Slot = 0;
        private const int AI_Timer_Slot = 1;

        private const int AI_Max_Splits = 2; //the exponent. projectile can split this many times. this counter is remembered for a projectile's children.
        private const int AI_Projectile_Split_Rate = 2; //the base. projectile will split into this many children each time it splits.
        private const int AI_Split_Time = 15; //when does the projectile split, in frames
        private const int AI_Split_Angle = 11; //the spread angle for the child projectiles

        //all of this stuff before the AI makes the AI pretty
        //now instead of saying projectile.ai[0] i can say AI_Split_Count
        public float AI_Split_Count
        {
            get => Projectile.ai[AI_Split_Counter_Slot];
            set => Projectile.ai[AI_Split_Counter_Slot] = value;
        }

        public float AI_Timer
        {
            get => Projectile.ai[AI_Timer_Slot];
            set => Projectile.ai[AI_Timer_Slot] = value;
        }

        public override void AI()
        {

            Lighting.AddLight(Projectile.position, 0.1f, .35f, 1f); //1f was .25f

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncProjectile, number: this.Projectile.whoAmI);
            }

            Projectile.rotation = (float)Math.Atan2((double)this.Projectile.velocity.Y, (double)this.Projectile.velocity.X) + 1.57f;
            Vector2 speedMod = new Vector2(Projectile.velocity.X, Projectile.velocity.Y);
            int z2 = Main.rand.Next(-1, 2);
            speedMod = RotateAboutOrigin(speedMod, (float)((Math.PI * z2) / 38f)); //the 38f controls the curve strength. too low and the projectile spins in circles
            Projectile.velocity = speedMod;

            AI_Timer++;

            if (AI_Timer == AI_Split_Time && AI_Split_Count < AI_Max_Splits)
            {
                float rotation = MathHelper.ToRadians(AI_Split_Angle);
                for (int i = 0; i < AI_Projectile_Split_Rate; i++)
                {
                    Vector2 shiftSpeed = new Vector2(Projectile.velocity.X, Projectile.velocity.Y).RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (AI_Projectile_Split_Rate - 1))); //evenly divide the projectiles among the spread angle
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int projIndex = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position, shiftSpeed, ModContent.ProjectileType<MiracleVines>(), Projectile.damage, Projectile.knockBack, Projectile.owner, AI_Split_Count + 1, 0); //the AI_Split_Count+1 here is what makes the recursion work. child projectiles inherit their parent's AI_Split_Count, plus one.
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projIndex);
                        }
                    }
                }

                Projectile.Kill(); //only kill the projectile if we split. timeLeft will kill the final set automatically
                Projectile.active = false;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, this.Projectile.whoAmI);
                }
            }

            if (AI_Timer % 4 == 0)
            { //spawn a trail vine every 3 frames
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), new Vector2(Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2)), new Vector2(Projectile.velocity.X * 0.01f, Projectile.velocity.Y * 0.01f), ModContent.ProjectileType<MiracleVinesTrail>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                for (int j = -1; j < 2; j++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 18, Projectile.velocity.X * 1.1f, Projectile.velocity.Y * 1.1f, 170, default, 0.7f); //emit dust sideways when trail vines are spawned
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            Projectile.active = false;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.KillProjectile, -1, -1, null);
            }
        }

        public Vector2 RotateAboutOrigin(Vector2 point, float rotation)
        {
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
