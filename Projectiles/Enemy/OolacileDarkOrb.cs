using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

public class OolacileDarkOrb : ModProjectile
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Oolacile's Dark Orb");
    }

    public override void SetDefaults()
    {
        Projectile.aiStyle = 0;
        Projectile.hostile = true;
        Projectile.height = 34;
        Projectile.scale = 2f;
        Projectile.tileCollide = false;
        Projectile.width = 34;
        Projectile.timeLeft = 600;
        Main.projFrames[Projectile.type] = 4;
        Projectile.light = 1;
    }

    public override void AI()
    {

        Projectile.rotation += 0.5f;

        if (Main.player[(int)Projectile.ai[0]].position.X < Projectile.position.X)
        {
            if (Projectile.velocity.X > -10) Projectile.velocity.X -= 0.1f;
        }

        if (Main.player[(int)Projectile.ai[0]].position.X > Projectile.position.X)
        {
            if (Projectile.velocity.X < 10) Projectile.velocity.X += 0.1f;
        }

        if (Main.player[(int)Projectile.ai[0]].position.Y < Projectile.position.Y)
        {
            if (Projectile.velocity.Y > -10) Projectile.velocity.Y -= 0.1f;
        }

        if (Main.player[(int)Projectile.ai[0]].position.Y > Projectile.position.Y)
        {
            if (Projectile.velocity.Y < 10) Projectile.velocity.Y += 0.1f;
        }

        if (Main.rand.NextBool(2))
        {
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 27, 0, 0, 50, Color.Purple, 1.0f);
            Main.dust[dust].noGravity = false;
        }
        Lighting.AddLight((int)(Projectile.position.X / 16f), (int)(Projectile.position.Y / 16f), 0.7f, 0.2f, 0.2f);

        Projectile.frameCounter++;
        if (Projectile.frameCounter > 2)
        {
            Projectile.frame++;
            Projectile.frameCounter = 3;
        }
        if (Projectile.frame >= 4)
        {
            Projectile.frame = 0;
        }
    }

    public override bool PreKill(int timeLeft)
    {
        Projectile.type = 44;
        return base.PreKill(timeLeft);
    }

    public override void OnHitPlayer(Player target, int damage, bool crit)
    {
        if (Main.expertMode)
        {
            target.AddBuff(BuffID.Poisoned, 9000, false);
            target.AddBuff(BuffID.Darkness, 9000, false);
            target.AddBuff(BuffID.Bleeding, 9000, false);
        }
        else
        {
            target.AddBuff(BuffID.Poisoned, 18000, false);
            target.AddBuff(BuffID.Darkness, 18000, false);
            target.AddBuff(BuffID.Bleeding, 18000, false);
        }
    }
}