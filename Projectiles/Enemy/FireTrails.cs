using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class FireTrails : VFX.DynamicTrail
    {

        public override string Texture => "tsorcRevamp/Projectiles/FireBallDarkCore";

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.MaxUpdates = 2;
            Projectile.penetrate = 1;
            Projectile.hostile = true;
            //Projectile.alpha -= 5;
            Projectile.netUpdate = true;
            Projectile.light = .9f;

            trailWidth = 25;
            trailPointLimit = 150;
            trailYOffset = 30;
            trailMaxLength = 850;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 2;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/DeathLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }
        public override float CollisionWidthFunction(float progress)
        {
            return 10;
        }
        public override void SetEffectParameters(Effect effect)
        {
            collisionEndPadding = trailPositions.Count / 3;
            collisionPadding = trailPositions.Count / 8;
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

            Color shaderColor = new Color(1.0f, 0.4f, 0.1f, 1.0f);
            shaderColor = UsefulFunctions.ShiftColor(shaderColor, (float)Main.timeForVisualEffects, 0.01f);
            effect.Parameters["shaderColor"].SetValue(shaderColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

        private const int AI_Split_Counter_Slot = 0;
        private const int AI_Timer_Slot = 1;

        private const int AI_Max_Splits = 2; //the exponent. projectile can split this many times. this counter is remembered for a projectile's children.
        private const int AI_Split_Time = 110; //when does the projectile split, in frames
        private const int AI_Split_Angle = 15; //the spread angle for the child projectiles

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

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
            return false;
        }

        public override void AI()
        {
            base.AI();

            Projectile.rotation += 4f;

            int rand = Main.rand.Next(-1, 2);
            Projectile.velocity = RotateAboutOrigin(Projectile.velocity, (float)((Math.PI * rand) / 40f)); //the 40f controls the curve strength. too low and the projectile spins in circles

            AI_Timer++;
            if (AI_Timer == AI_Split_Time)
            {
                if (AI_Split_Count < AI_Max_Splits)
                {
                    float rotation = MathHelper.ToRadians(AI_Split_Angle);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int proj = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position, Projectile.velocity.RotatedBy(rotation), ModContent.ProjectileType<FireTrails>(), Projectile.damage, Projectile.knockBack, Projectile.owner, AI_Split_Count + 1, 0); //the AI_Split_Count+1 here is what makes the recursion work. child projectiles inherit their parent's AI_Split_Count, plus one.
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj);
                        }
                    }
                    //New projectile goes one way, existing one goes another
                    Projectile.velocity = Projectile.velocity.RotatedBy(-rotation);
                }
                AI_Timer = 0;
                AI_Split_Count++;

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncProjectile, number: this.Projectile.whoAmI);
                }
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
