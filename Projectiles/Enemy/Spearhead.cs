using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy;

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

    public override void OnHitPlayer(Player target, int damage, bool crit)
    {
        target.AddBuff(ModContent.BuffType<Crippled>(), 600);
    }
}