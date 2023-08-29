using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy.Okiku;

class EnemyBlackFireVisual : ModProjectile
{
    public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemyBlackFire";
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Black Fire");

    }
    public override void SetDefaults()
    {
        Projectile.width = 12;
        Projectile.height = 12;
        Projectile.scale = 1.5f;
        Projectile.alpha = 50;
        Projectile.aiStyle = -1;
        Projectile.timeLeft = 360;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.penetrate = 1;
        Projectile.light = 0.8f;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.damage = 85;
        Projectile.knockBack = 9;
    }
    public override void AI()
    {
        //projectile.AI(true);


        // Get the length of last frame's velocity
        float lastLength = (float)Math.Sqrt((Projectile.velocity.X * Projectile.velocity.X + Projectile.velocity.Y * Projectile.velocity.Y));

        // Align projectile facing with velocity normal
        Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) - 2.355f;
        // Render fire particles [every frame]
        int particle = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 54, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 160, default(Color), 3f);
        Main.dust[particle].noGravity = true;
        Main.dust[particle].velocity *= 1.4f;
        int lol = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 58, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 160, default(Color), 3f);
        Main.dust[lol].noGravity = true;
        Main.dust[lol].velocity *= 1.4f;


        // Render smoke particles [every other frame]
        if (Projectile.timeLeft % 10 == 0)
        {
            int particle2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 1, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f - 1f, 180, default(Color), 1f + (float)Main.rand.Next(2));
            Main.dust[particle2].noGravity = true;
            Main.dust[particle2].noLight = true;
            Main.dust[particle2].fadeIn = 3f;
        }
    }

    public override void OnHitPlayer(Player target, int damage, bool crit)
    {
        target.AddBuff(ModContent.BuffType<DarkInferno>(), 240, false);
    }
}