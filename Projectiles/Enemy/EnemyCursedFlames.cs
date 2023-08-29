using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class EnemyCursedFlames : ModProjectile
{

    public override string Texture => "tsorcRevamp/Projectiles/Enemy/Okiku/PoisonSmog";

    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 16;
        Projectile.alpha = 150;
        Projectile.aiStyle = 8;
        Projectile.timeLeft = 600;
        Projectile.damage = 70;
        Projectile.light = 0.8f;
        Projectile.penetrate = 1;
        Projectile.hostile = true;
        AIType = 96;
    }

    public override bool PreKill(int timeLeft)
    {
        Projectile.type = 95;
        return true;
    }

    int timer = 0;
    bool initialSetup = false;
    bool delayedMode = false;
    public override void PostAI()
    {

        Projectile.rotation += 3f;

        int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 75, 0, 0, 50, Color.Chartreuse, 3.0f);
        Main.dust[dust].noGravity = true;

        if (Projectile.velocity.X <= 4 && Projectile.velocity.Y <= 4 && Projectile.velocity.X >= -4 && Projectile.velocity.Y >= -4)
        {
            float accel = 2f + (Main.rand.Next(10, 30) * 0.001f);
            Projectile.velocity.X *= accel;
            Projectile.velocity.Y *= accel;
        }

        if (!initialSetup)
        {
            initialSetup = true;
            if (Projectile.ai[0] != 0)
            {
                delayedMode = true;
                timer = 120;
                Projectile.tileCollide = false;
            }
            else
            {
                Projectile.tileCollide = true;
            }
        }

        if (delayedMode && timer > 0)
        {
            timer--;
            Projectile.velocity = Vector2.Zero;
            if (timer == 0)
            {
                float velocity = 8;
                if (Main.tile[(int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16].LiquidAmount != 0)
                {
                    velocity = 5;
                }
                Projectile.velocity = UsefulFunctions.GenerateTargetingVector(Projectile.Center, Main.player[(int)Projectile.ai[1]].Center, velocity);
            }
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        delayedMode = true;
        timer = 120;
        Projectile.timeLeft = 600;
        Projectile.tileCollide = false;


        return false;
    }

    public override void OnHitPlayer(Player target, int damage, bool crit)
    {
        int buffLengthMod = 1;
        if (Main.expertMode)
        {
            buffLengthMod = 2;
        }
        target.AddBuff(BuffID.BrokenArmor, 180 / buffLengthMod, false);
        target.AddBuff(BuffID.Bleeding, 1800 / buffLengthMod, false);
    }
}
