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
    class IchorFragment : ModProjectile
    {
        
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ichor Fragment");
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 200;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.Ichor, 300);
        }

        bool spawnedTrail = false;
        public override void AI()
        {
            if (!spawnedTrail)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<Projectiles.VFX.IchorFragmentTrail>(), 0, 0, Main.myPlayer, 0, UsefulFunctions.EncodeID(Projectile));
                spawnedTrail = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.5f}, Projectile.Center);
        }
    }
}
