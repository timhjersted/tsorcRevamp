using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class Hellwing : ModProjectile
{

  

    public override void SetDefaults()
    {
        Projectile.CloneDefaults(ProjectileID.Hellwing);
        AIType = 485;
        Main.projFrames[Projectile.type] = 5;
        Projectile.width = 24;
        Projectile.height = 26;
        Projectile.aiStyle = 485;
        Projectile.timeLeft = 320;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.damage = 60;
        Projectile.friendly = false;
        Projectile.penetrate = 3;
        Projectile.alpha = 0;
        Projectile.light = .9f;
    }

    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Hellwing");
    }

    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Hellwing;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];

        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, 32, 32), Color.White, Projectile.rotation, new Vector2(16, 16), Projectile.scale, SpriteEffects.None, 0);

        return false;
    }

    public override bool PreKill(int timeLeft)
    {
        Projectile.type = 485; //killpretendtype shadowbeem
        return true;
    }

    Player targetPlayer;

    public override void AI()
    {



        Projectile.frameCounter++;
        if (Projectile.frameCounter > 4)
        {
            Projectile.frame++;
            Projectile.frameCounter = 0;
        }
        //if (Projectile.frame >= 14)
        //{
        //    Projectile.Kill();
        //    return;
       // }


        Color color = new Color();
        int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y - 10), Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 160, color, 3.0f);
        Main.dust[dust].noGravity = true;

        
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
