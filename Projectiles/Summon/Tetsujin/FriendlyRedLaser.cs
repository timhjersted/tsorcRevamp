using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Tetsujin
{

    public class FriendlyRedLaser : GenericLaser
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Laser");
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }

        public override string Texture => base.Texture;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;

            FollowHost = false;
            LaserOrigin = Main.projectile[HostIdentifier].Center;
            TelegraphTime = 30;
            FiringDuration = 60;
            MaxCharge = 90;
            LaserLength = 300;
            LaserColor = Color.Red;
            TileCollide = false;
            LaserDust = DustID.OrangeTorch;
            LineDust = true;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.RedLaserTransparent;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            LaserSize = 1f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
            ProjectileSource = true;
            FollowHost = true;
            DustAmount = 20;
        }


        //This laser sweeps across its target, which you give it with ai[0]
        int rotDirection = 0;
        public override void AI()
        {
            Projectile owner = Main.projectile[(int)Projectile.ai[1]];

            if (owner == null || owner.active == false)
            {
                Projectile.active = false;
                return;
            }

            if (Charge < MaxCharge)
            {
                NPC target = Main.npc[(int)Projectile.ai[0]];
                if (target != null)
                {
                    if (rotDirection == 0) //Only set this once, so no flipping
                    {
                        rotDirection = 1;
                    }
                    Projectile.velocity = UsefulFunctions.Aim(Projectile.Center, target.Center, 1).RotatedBy(rotDirection * MathHelper.Pi / 3);
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
