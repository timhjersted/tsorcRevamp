using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.VanillaItems;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    public class YianBlaze2 : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 42;
            Projectile.scale = 1.2f;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            AIType = ProjectileID.NebulaBlaze2; 
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 600;
            Projectile.extraUpdates = 2;
        }
        public override void AI()
        {
            UsefulFunctions.HomeOnEnemy(Projectile, 300, 15f, false, 1.1f, true);

            Projectile.frameCounter++;
            int frameSpeed = 12;
            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Type])
                {
                    Projectile.frame = 0;
                }
            }
            Lighting.AddLight(Projectile.Center, 0.5f, 0.5f, 0.5f);

            int dust = Dust.NewDust(
                Projectile.Center,
                8, 
                8, 
                109, 
                Projectile.velocity.X * 0.2f, 
                Projectile.velocity.Y * 0.2f,
                0, 
                default, 
                1.6f 
            );
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0.3f;
            {
                float wave = (float)Math.Sin((Main.GlobalTimeWrappedHourly * 8f) + 0.6f) * 16f;

                Vector2 perpendicular = Projectile.velocity.SafeNormalize(Vector2.UnitX)
                .RotatedBy(MathHelper.PiOver2);

                Vector2 wavyOffset = perpendicular * wave;

                Dust dust2 = Dust.NewDustPerfect(
                    Projectile.Center + wavyOffset,
                    54,
                    Vector2.Zero,
                    0,
                    Color.Black,
                    1.3f
                );

                dust2.noGravity = true;
                dust2.velocity *= 0.1f;
            }
            {
                float wave = (float)Math.Sin((Main.GlobalTimeWrappedHourly * 8f) + 0.6f) * 16f;

                Vector2 perpendicular = Projectile.velocity.SafeNormalize(Vector2.UnitX)
                .RotatedBy(MathHelper.PiOver2);

                Vector2 wavyOffset = perpendicular * -wave; 

                Dust dust3 = Dust.NewDustPerfect(
                    Projectile.Center + wavyOffset,
                    54,
                    Vector2.Zero,
                    0,
                    Color.Black,
                    1.3f
                );

                dust3.noGravity = true;
                dust3.velocity *= 0.1f;
            }
        }
        public override bool PreDraw(Player player, ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White, 
                Projectile.rotation,
                frame.Size() / 2f,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
        public override void PostDraw(Player player, Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

            Vector2 origin = frame.Size() / 2f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 1; i <= 6; i++)
            {
                Vector2 offset = Projectile.velocity * -i * 2.1f;
                float alpha = 0.3f * (1f - (i / 6f));

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    frame,                    
                    lightColor * alpha,
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None,
                    0
                );
            }
        }
        public override void OnKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

            for (int i = 0; i < 33; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(10f, 10f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(26f, 26f),
                    54,
                    velocity,
                    0,
                    default,
                    Main.rand.NextFloat(1.5f, 2.2f)
                );
                dust.noGravity = true;
            }
        }
    }

    public class YianBlaze : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 30;
            Projectile.scale = 1.1f;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            AIType = ProjectileID.NebulaBlaze1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 600;
            Projectile.extraUpdates = 2;
        }
        public override void AI()
        {
            UsefulFunctions.HomeOnEnemy(Projectile, 175, 11f, false, 1f, true);

            Projectile.frameCounter++;
            int frameSpeed = 14;
            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Type])
                {
                    Projectile.frame = 0;
                }
            }
            Lighting.AddLight(Projectile.Center, 0.2f, 0.2f, 0.2f);

            int dust4 = Dust.NewDust(
                Projectile.Center - new Vector2(5, 5),
                8, 
                8, 
                DustID.GemDiamond, 
                Projectile.velocity.X * 0.2f, 
                Projectile.velocity.Y * 0.2f,
                0, 
                Color.White, 
                0.9f 
            );
            Main.dust[dust4].noGravity = true;
            Main.dust[dust4].velocity *= 0.2f;
            {
                float wave = (float)Math.Sin((Main.GlobalTimeWrappedHourly * 8f) + 0.6f) * 12f;

                Vector2 perpendicular = Projectile.velocity.SafeNormalize(Vector2.UnitX)
                .RotatedBy(MathHelper.PiOver2);

                Vector2 wavyOffset = perpendicular * wave;

                Dust dust5 = Dust.NewDustPerfect(
                    Projectile.Center + wavyOffset,
                    DustID.GemDiamond,
                    Vector2.Zero,
                    0,
                    Color.White,
                    0.7f
                );

                dust5.noGravity = true;
                dust5.velocity *= 0.1f;
            }
            {
                float wave = (float)Math.Sin((Main.GlobalTimeWrappedHourly * 8f) + 0.6f) * 12f;

                Vector2 perpendicular = Projectile.velocity.SafeNormalize(Vector2.UnitX)
                .RotatedBy(MathHelper.PiOver2);

                Vector2 wavyOffset = perpendicular * -wave; 

                Dust dust6 = Dust.NewDustPerfect(
                    Projectile.Center + wavyOffset,
                    DustID.GemDiamond,
                    Vector2.Zero,
                    0,
                    Color.White,
                    0.7f
                );

                dust6.noGravity = true;
                dust6.velocity *= 0.1f;
            }
        }
        public override bool PreDraw(Player player, ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White, 
                Projectile.rotation,
                frame.Size() / 2f,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
        public override void PostDraw(Player player, Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

            Vector2 origin = frame.Size() / 2f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 1; i <= 4; i++)
            {
                Vector2 offset = Projectile.velocity * -i * 1.2f;
                float alpha = 0.2f * (1f - (i / 6f));

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    frame,                     
                    lightColor * alpha,
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None,
                    0
                );
            }
        }
        public override void OnKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

            for (int i = 0; i < 22; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(8f, 8f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(22f, 22f),
                    DustID.GemDiamond,
                    velocity,
                    0,
                    default,
                    Main.rand.NextFloat(1.1f, 1.5f)
                );
                dust.noGravity = true;
            }
        }
    }
}