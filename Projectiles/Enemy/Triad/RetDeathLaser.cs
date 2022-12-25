using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triad
{
    class RetDeathLaser : ModProjectile
    {
        
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Death Laser");
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.timeLeft = 600;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        bool playedSound = false;
        public override void AI()
        {
            if (!playedSound)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.5f}, Projectile.Center); 
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<Projectiles.Trails.RetDeathLaserTrail>(), 0, 0, Main.myPlayer, 0, Projectile.whoAmI);
                playedSound = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
