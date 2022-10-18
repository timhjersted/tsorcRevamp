using Terraria;
using System;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Swords.Runeterra
{
    public class NightbringerWindWall: ModProjectile
    {
        public int soundtimer = 0;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 62;
            Projectile.height = 66;
            Projectile.aiStyle = ProjAIStyleID.TwilightLance;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 800;
            Projectile.DamageType = DamageClass.Melee;
        }
        public override void OnSpawn(IEntitySource source)
        {
            //SoundEngine.PlaySound(SoundID.DD2_BookStaffTwisterLoop, Projectile.Center);
        }
        public override void Kill(int timeLeft)
        {
            SoundEngine.FindActiveSound(SoundID.DD2_BookStaffTwisterLoop);
            SoundEngine.StopTrackedSounds();
        }
        public override void AI()
        {
            Player owner = Main.player[Main.myPlayer];
            Vector2 unitVectorTowardsMouse = owner.Center.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * owner.direction);
            owner.ChangeDir((unitVectorTowardsMouse.X > 0f) ? 1 : (-1));
            if (Main.GameUpdateCount % 15 == 0)
            {
                Projectile.velocity = Vector2.Zero;
            }
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];

                if (i != Projectile.whoAmI && other.active && !other.friendly && Math.Abs(Projectile.position.X - other.position.X) + Math.Abs(Projectile.position.Y - other.position.Y) < Projectile.width)
                {
                    other.Kill();
                }
            }
                    Visuals();
        }
        private void Visuals()
        {
            // So it will lean slightly towards the direction it's moving
            Projectile.rotation = Projectile.velocity.X * 0.05f;

            // This is a simple "loop through all frames from top to bottom" animation
            int frameSpeed = 5;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            // Some visuals here
            //Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.78f);
        }
    }
}