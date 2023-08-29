using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

public class EnemySpellPoisonStorm : ModProjectile
{

    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Enemy Spell Poison Storm");
        Main.projFrames[Projectile.type] = 7;
    }
    public override void SetDefaults()
    {

        Projectile.aiStyle = -1;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.width = 190;
        Projectile.height = 190;
        Projectile.light = 1f;
        Projectile.penetrate = 1;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.scale = 2f;
        Projectile.tileCollide = true;
        DrawOriginOffsetY = 95;
        DrawOriginOffsetX = -95;
    }
    float size = 0;
    int dustCount = 0;

    public override void AI()
    {
        if (size < 40 * 16)
        {
            size += ((8 * 16) / 30f); //Increase to its full size (7 blocks) in half a second (30 ticks)
            dustCount = (int)(2 * MathHelper.Pi * size / 10); //Spawn dust according to its size                
        }
        else
        {
            //Fade out after reaching max radius, and then despawn
            dustCount = (int)(dustCount / 1.1f);
            if (dustCount <= 0)
            {
                Projectile.Kill();
                return;
            }
        }

        for (int j = 0; j < dustCount; j++)
        {
            Vector2 dir = Main.rand.NextVector2CircularEdge(size, size);
            Vector2 dustPos = Projectile.Center + dir;
            dir.Normalize();
            Vector2 dustVel = dir;
            Dust.NewDustPerfect(dustPos, DustID.CursedTorch, dustVel, 200).noGravity = true;
        }
    }

    //Circular collision
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float distance = Vector2.Distance(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2());
        if (distance < size && distance > size - 16)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }
    public override void OnHitPlayer(Player target, int damage, bool crit)
    {
        target.AddBuff(BuffID.Poisoned, 900, false);
    }
}