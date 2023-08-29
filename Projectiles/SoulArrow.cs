using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles;


public class SoulArrow : ModProjectile
{
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        Main.projFrames[Projectile.type] = 6;
    }

    public override void SetDefaults()
    {
        Projectile.width = 8;
        Projectile.height = 8;
        Projectile.alpha = 180;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 300;
    }
    int soularrowanimtimer = 0;
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        Lighting.AddLight(Projectile.Center, .300f, .300f, .450f);

        //ANIMATION

        soularrowanimtimer++;

        if (soularrowanimtimer > 24)
        {
            soularrowanimtimer = 0;
        }

        if (++Projectile.frameCounter >= 4) //ticks spent on each frame
        {
            Projectile.frameCounter = 0;

            if (++Projectile.frame == 6)
            {
                Projectile.frame = 0;
            }
        }

        //int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 68, 0f, 0f, 30, default(Color), 1f);
        //Main.dust[dust].noGravity = true;

        if (Projectile.velocity.X > 0) //if going right
        {
            for (int d = 0; d < 6; d++)
            {
                int num44 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y - 2), Projectile.width, Projectile.height, 68, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
                Main.dust[num44].noGravity = true;
                Main.dust[num44].velocity *= 0f;
            }

            for (int d = 0; d < 6; d++)
            {
                int num45 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y - 2), Projectile.width - 4, Projectile.height, 68, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), .5f);
                Main.dust[num45].noGravity = true;
                Main.dust[num45].velocity *= 0f;
                Main.dust[num45].fadeIn *= 1f;
            }
        }
        else //if going left
        {
            for (int d = 0; d < 6; d++)
            {
                int num44 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 68, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
                Main.dust[num44].noGravity = true;
                Main.dust[num44].velocity *= 0f;
            }

            for (int d = 0; d < 6; d++)
            {
                int num45 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width - 4, Projectile.height, 68, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), .5f);
                Main.dust[num45].noGravity = true;
                Main.dust[num45].velocity *= 0f;
                Main.dust[num45].fadeIn *= 1f;
            }
        }

        int? closestEnemy = UsefulFunctions.GetClosestEnemyNPC(Projectile.Center);
        if (closestEnemy.HasValue && Projectile.timeLeft < 270)
        {
            UsefulFunctions.SmoothHoming(Projectile, Main.npc[closestEnemy.Value].Center, 0.05f, 5, Main.npc[closestEnemy.Value].velocity, false);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];

        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, Projectile.frame * 26, 14, 26), Color.White, Projectile.rotation, new Vector2(8, 6), Projectile.scale, SpriteEffects.None, 0);

        return false;
    }
    public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
    {
        // change the hitbox size, centered about the original projectile center. This makes the projectile have small aoe.
        Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
        Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
        Projectile.width = 40;
        Projectile.height = 40;
        Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
        Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);

        Projectile.timeLeft = 2;
    }
    public override void Kill(int timeLeft)
    {
        for (int d = 0; d < 20; d++)
        {
            int dust = Dust.NewDust(Projectile.Center, 8, 8, 68, Projectile.velocity.X * 1f, Projectile.velocity.Y * 1f, 30, default(Color), 1.5f);
            Main.dust[dust].noGravity = true;
        }
        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.45f }, Projectile.position);
    }
}