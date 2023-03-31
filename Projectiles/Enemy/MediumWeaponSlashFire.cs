using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class MediumWeaponSlashFire : ModProjectile //Same as Medium Weapon Slash, but also bleeds the player and has red dusts
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/ThrowingAxe"; //invis so doesnt matter
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Medium Weapon Slash Fire");
        }
        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.alpha = 255; //invis
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 12;
        }

        int difference;
        public override void AI()
        {
            NPC owner = Main.npc[(int)Projectile.ai[0]];

            for (int d = 0; d < 2; d++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 6, Projectile.velocity.X, Projectile.velocity.Y, 30, default(Color), Main.rand.NextFloat(1.2f, 2.5f));
                Main.dust[dust].noGravity = true;
            }


            if (Projectile.ai[1] < 1)
            {
                ++Projectile.ai[1];
                difference = (int)Projectile.Center.X - (int)owner.Center.X;
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

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Crippled>(), 600);
            target.AddBuff(BuffID.OnFire, 300);
        }
    }
}