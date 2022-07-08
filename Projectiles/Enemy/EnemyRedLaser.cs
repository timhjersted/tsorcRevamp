using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{

    public class EnemyRedLaser : EnemyGenericLaser
    {


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Laser");
        }

        public override string Texture => base.Texture;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.hide = true;

            FollowHost = true;
            LaserOrigin = Main.npc[HostIdentifier].Center;
            TelegraphTime = 90;
            FiringDuration = 60;
            MaxCharge = 90;
            LaserLength = 3000;
            LaserColor = Color.Red;
            TileCollide = true;
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
            NPC owner = Main.npc[(int)Projectile.ai[1]];
            if (owner == null || owner.active == false)
            {
                Projectile.active = false;
                return;
            }

            if (Charge < MaxCharge)
            {
                Player target = Main.player[(int)Projectile.ai[0]];
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
                    Projectile.velocity = UsefulFunctions.GenerateTargetingVector(Projectile.Center, target.Center, 1).RotatedBy(rotDirection * MathHelper.Pi / 3);
                }
            }
            else
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(rotDirection * -MathHelper.PiOver2 / 60f);
            }
            base.AI();
        }
    }
}
