using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class SpearheadCrimson : ModProjectile          //Same as Spearhead, but also bleeds the player and has red dusts
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/ThrowingAxe"; //invis so doesnt matter

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.alpha = 255; //invis
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 35;
        }

        Vector2 difference;
        public override void AI()
        {
            NPC owner = Main.npc[(int)Projectile.ai[0]];

            for (int d = 0; d < 1; d++)
            {
                if (Projectile.position.X > owner.position.X)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X + 8, Projectile.position.Y), Projectile.width, Projectile.height, 60, 16, Projectile.velocity.Y, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
                else
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X - 18, Projectile.position.Y), Projectile.width, Projectile.height, 60, -16, Projectile.velocity.Y, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
            }

            for (int d = 0; d < 2; d++)
            {
                if (Projectile.position.X > owner.position.X)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X + 8, Projectile.position.Y), Projectile.width, Projectile.height, 183, 0, Projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
                else
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X - 18, Projectile.position.Y), Projectile.width, Projectile.height, 183, 0, Projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
            }


            if (Projectile.ai[1] < 1)
            {
                ++Projectile.ai[1];
                difference = Projectile.Center - owner.Center;
            }

            if (Projectile.ai[1] >= 1 && Projectile.ai[1] < 3)
            {
                //Create a new Vector2 with length offsetDistance, and then rotate it toward the correct direction
                //Add that to the npc's position
                if (owner.direction == 1)
                {
                    Projectile.Center = owner.Center + difference;
                }
                else
                {
                    Projectile.Center = owner.Center + difference;
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 1000);
            target.AddBuff(BuffID.Bleeding, 1000);
        }
    }
}