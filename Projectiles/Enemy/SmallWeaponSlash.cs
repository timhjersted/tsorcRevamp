using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class SmallWeaponSlash : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/ThrowingAxe"; //invis so doesnt matter

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.alpha = 255; //invis
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 12;
            //Projectile.DamageType = DamageClass.Melee;
        }

        int difference;
        public override void AI()
        {
            NPC owner = Main.npc[(int)Projectile.ai[0]];

            if (Projectile.ai[1] < 1)
            {
                ++Projectile.ai[1];
                difference = (int)Projectile.position.X - (int)owner.position.X;
            }

            if (Projectile.ai[1] >= 1)
            {
                //Create a new Vector2 with length offsetDistance, and then rotate it toward the correct direction
                //Add that to the npc's position
                if (owner.direction == 1)
                {
                    Projectile.position.X = owner.Center.X + difference - 10;
                }
                else
                {
                    Projectile.position.X = owner.Center.X - difference - 42;
                }
            }

        }
    }
}