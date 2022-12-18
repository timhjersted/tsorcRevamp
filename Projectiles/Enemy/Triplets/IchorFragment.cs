using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triplets
{
    class IchorFragment : ModProjectile
    {
        
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ichor Fragment");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3;
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
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triplets/HomingStarStar";

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }


        float[] trailRotations = new float[6] { 0, 0, 0, 0, 0, 0 };
        bool playedSound = false;
        bool spawnedTrail = false;
        public override void AI()
        {

            Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Ichor, Scale: 4).noGravity = true;
        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.5f}, Projectile.Center);
        }

        float Progress(float progress)
        {
            float percent = 1f;
            float lerpValue = Utils.GetLerpValue(0f, 0.6f, progress, clamped: true);
            percent *= 1f - (1f - lerpValue) * (1f - lerpValue);
            return MathHelper.Lerp(0f, 30f, percent);
        }

        Color ColorValue(float progress)
        {
            float timeFactor = (float)Math.Sin(Math.Abs(progress - Main.GlobalTimeWrappedHourly * 3));
            Color result = Color.Lerp(Color.Cyan, Color.DeepPink, (timeFactor + 1f) / 2f);
            //Main.NewText(timeFactor + 1);
            //result = ;
            result.A = 0;

            return result;
        }

        Texture2D texture;
        Texture2D starTexture;
        float starRotation;
        public override bool PreDraw(ref Color lightColor)
        {
            
            return false;
        }
    }
}
