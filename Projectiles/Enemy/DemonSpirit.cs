using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class DemonSpirit : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.width = 38;
        Projectile.height = 46;
        Projectile.aiStyle = 0;
        Projectile.timeLeft = 320;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.damage = 20;
        Projectile.friendly = false;
        Projectile.penetrate = 3;
        Projectile.alpha = 70;
        Projectile.light = .7f;
    }

    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Demon Spirit");
    }

    public override bool PreKill(int timeLeft)
    {
        Projectile.type = 468; //killpretendtype shadowbeem
        return true;
    }

    Player targetPlayer;

    public override void AI()
    {

        Color color = new Color();
        int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y - 10), Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 160, color, 3.0f);
        Main.dust[dust].noGravity = true;

        this.Projectile.ai[0] += 1f;


        Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + 1.57f;
        if (Projectile.velocity.Y > 16f)
        {
            Projectile.velocity.Y = 16f;
            return;
        }

        #region Homing Code
        Vector2 move = Vector2.Zero;
        float distance = 900f;
        bool target = false;
        float speed = 3;
        if (!target)
        {
            int targetIndex = GetClosestPlayer();
            if (targetIndex != -1)
            {
                targetPlayer = Main.player[targetIndex];
                target = true;
            }
        }

        if (target)
        {
            Vector2 newMove = targetPlayer.Center - Projectile.Center;
            float distanceTo = (float)Math.Sqrt(newMove.X * newMove.X + newMove.Y * newMove.Y);
            if (distanceTo < distance)
            {
                move = newMove;
                distance = distanceTo;
            }

            Projectile.velocity.X = (move.X / distance) * speed;
            Projectile.velocity.Y = (move.Y / distance) * speed;
        }
        #endregion
    }

    private int GetClosestPlayer()
    {
        int closest = -1;
        float distance = 9999;
        for (int i = 0; i < Main.player.Length; i++)
        {
            Vector2 diff = Main.player[i].Center - Projectile.Center;
            float distanceTo = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
            if (distanceTo < distance)
            {
                distance = distanceTo;
                closest = i;
            }
        }
        return closest;
    }
}