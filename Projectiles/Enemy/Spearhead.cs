using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class Spearhead : ModProjectile
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

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.ai[2] == 0) //Lothric enemies
            {
                target.AddBuff(ModContent.BuffType<Crippled>(), 600);
            }
            else if (Projectile.ai[2] == 1) //Hollow Spearman
            {
                target.AddBuff(ModContent.BuffType<Crippled>(), 360);
            }
            else if (Projectile.ai[2] == 2) //Ghost of the Drowned
            {
                SoundEngine.PlaySound(SoundID.Drown, target.Center);
                target.AddBuff(BuffID.Darkness, 10 * 60);
                target.AddBuff(BuffID.BrokenArmor, 10 * 60);
                target.AddBuff(BuffID.Chilled, 10 * 60);
                target.AddBuff(ModContent.BuffType<Gilled>(), 20 * 60);
            }
        }
    }
}