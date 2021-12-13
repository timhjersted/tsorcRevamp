using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy
{
    class SpearheadCrimson : ModProjectile          //Same as Spearhead, but also bleeds the player and has red dusts
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/ThrowingAxe"; //invis so doesnt matter

        public override void SetDefaults()
        {
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.width = 20;
            projectile.height = 20;
            projectile.alpha = 255; //invis
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 35;
        }

        Vector2 difference;
        public override void AI()
        {
            NPC owner = Main.npc[(int)projectile.ai[0]];

            for (int d = 0; d < 1; d++)
            {
                if (projectile.position.X > owner.position.X)
                {
                    int dust = Dust.NewDust(new Vector2(projectile.position.X + 8, projectile.position.Y), projectile.width, projectile.height, 60, 16, projectile.velocity.Y, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
                else
                {
                    int dust = Dust.NewDust(new Vector2(projectile.position.X - 18, projectile.position.Y), projectile.width, projectile.height, 60, -16, projectile.velocity.Y, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
            }

            for (int d = 0; d < 2; d++)
            {
                if (projectile.position.X > owner.position.X)
                {
                    int dust = Dust.NewDust(new Vector2(projectile.position.X + 8, projectile.position.Y), projectile.width, projectile.height, 183, 0, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
                else
                {
                    int dust = Dust.NewDust(new Vector2(projectile.position.X - 18, projectile.position.Y), projectile.width, projectile.height, 183, 0, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
            }


            if (projectile.ai[1] < 1)
            {
                ++projectile.ai[1];
                difference = projectile.Center - owner.Center;
            }

            if (projectile.ai[1] >= 1 && projectile.ai[1] < 3)
            {
                //Create a new Vector2 with length offsetDistance, and then rotate it toward the correct direction
                //Add that to the npc's position
                if (owner.direction == 1)
                {
                    projectile.Center = owner.Center + difference;
                }
                else
                {
                    projectile.Center = owner.Center + difference;
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