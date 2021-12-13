using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy
{
    class MediumWeaponSlashCrimson : ModProjectile //Same as Medium Weapon Slash, but also bleeds the player and has red dusts
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/ThrowingAxe"; //invis so doesnt matter
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crimson Medium Weapon Slash");
        }
        public override void SetDefaults()
        {
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.width = 30;
            projectile.height = 30;
            projectile.alpha = 255; //invis
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 12;
        }

        int difference;
        public override void AI()
        {
            NPC owner = Main.npc[(int)projectile.ai[0]];

            for (int d = 0; d < 2; d++)
            {
                int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 60, projectile.velocity.X, projectile.velocity.Y, 30, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }

            for (int d = 0; d < 2; d++)
            {
                int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 183, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }

            if (projectile.ai[1] < 1)
            {
                ++projectile.ai[1];
                difference = (int)projectile.Center.X - (int)owner.Center.X;
            }

            if (projectile.ai[1] >= 1)
            {
                //Create a new Vector2 with length offsetDistance, and then rotate it toward the correct direction
                //Add that to the npc's position
                if (owner.direction == 1)
                {
                    projectile.position.X = owner.Center.X + difference - 10;
                }
                else
                {
                    projectile.position.X = owner.Center.X - difference - 42;
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