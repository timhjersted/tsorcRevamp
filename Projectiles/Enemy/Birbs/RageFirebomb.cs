﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Enemy.Birbs
{
    class RageFirebomb : DynamicTrail
    {
        public override string Texture => base.Texture;
        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.hostile = true;
            Projectile.timeLeft = 240;
            Projectile.penetrate = -1;
            Projectile.knockBack = 9;
            Projectile.DamageType = DamageClass.Throwing;
            Projectile.scale = .8f;
            Projectile.light = 1f;

            // These 2 help the projectile hitbox be centered on the projectile sprite.
            DrawOffsetX = -5;
            DrawOriginOffsetY = -5;

            trailWidth = 35;
            trailPointLimit = 15;
            trailYOffset = 30;
            trailMaxLength = 75;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 2;
            trailCollision = false;
            normalCollision = true;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/FuriousEnergy", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        float baseNoiseUOffset;
        public override void SetEffectParameters(Effect effect)
        {
            if (baseNoiseUOffset == 0)
            {
                baseNoiseUOffset = Main.rand.NextFloat();
            }

            effect.Parameters["baseNoise"].SetValue(tsorcRevamp.NoiseSmooth);
            effect.Parameters["baseNoiseUOffset"].SetValue(baseNoiseUOffset);
            //effect.Parameters["secondaryNoise"].SetValue(noiseTexture);

            visualizeTrail = false;

            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            Color shaderColor = new Color(1.0f, 0.1f, 0.1f, 1.0f);
            effect.Parameters["slashCenter"].SetValue(Color.White.ToVector4());
            effect.Parameters["slashEdge"].SetValue(shaderColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
            collisionEndPadding = trailPositions.Count / 3;
            collisionPadding = trailPositions.Count / 8;
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            Projectile.timeLeft = 2; //sets it to 2 frames, to let the explosion ai kick in. Setdefaults is -1 pen, this allows it to only hit one npc, then run explosion ai.
            Projectile.netUpdate = true;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 1) //the one frame make the explosion only deal damage once.
            {
                Projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 1 frames
                Projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 120;
                Projectile.height = 120;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.damage = 18;
                Projectile.knockBack = 9f;
                Projectile.DamageType = DamageClass.Throwing;
                Projectile.netUpdate = true;
            }
        }


        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.timeLeft = 2;
            return false;
        }
        public override void AI()
        {
            base.AI();

            if (Projectile.timeLeft <= 2)
            {
                Projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 2 frames
                Projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 120;
                Projectile.height = 120;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.damage = 18; //DAMAGE OF EXPLOSION when fuse runs out, not when collidew/npc
                if (Main.hardMode)
                {
                    Projectile.damage = 28;
                }
                if (tsorcRevampWorld.SuperHardMode)
                {
                    Projectile.damage = 38;
                }
                Projectile.knockBack = 9f;
                Projectile.Damage();
            }
            else
            {
                // Smoke and fuse dust spawn.
                if (Main.rand.NextBool(2))
                {
                    int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 6, 0f, 0f, 100, default(Color), 1f);
                    Main.dust[dustIndex].scale = 0.1f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].fadeIn = .5f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].noGravity = true;

                    for (int i = 0; i < 2; i++)
                    {
                        int pink = Dust.NewDust(Projectile.position, Projectile.width * 2, Projectile.height, DustID.CrystalSerpent, Projectile.velocity.X, Projectile.velocity.Y, Scale: 0.3f);
                        Main.dust[pink].noGravity = true;
                    }
                }
            }

            Projectile.ai[0] += 1f;

            if (Projectile.ai[0] > 5f)
            {
                Projectile.ai[0] = 10f;
                // Roll speed dampening.
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X = Projectile.velocity.X * 0.97f;
                    //if (projectile.type == 29 || projectile.type == 470 || projectile.type == 637)
                    {
                        Projectile.velocity.X = Projectile.velocity.X * 0.99f;
                    }
                    if ((double)Projectile.velocity.X > -0.01 && (double)Projectile.velocity.X < 0.01)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y = Projectile.velocity.Y + 0.2f;
            }

            // Rotation increased by velocity.X 
            Projectile.rotation += Projectile.velocity.X * 0.08f;
            return;
        }

        public override void OnKill(int timeLeft)
        {
            // Play explosion sound
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { PitchVariance = 0.5f }, Projectile.Center);
            Projectile.Damage();

            // Fire Dust spawn
            for (int i = 0; i < 200; i++)
            {
                int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X + 36, Projectile.position.Y + 36), Projectile.width - 74, Projectile.height - 74, 6, Main.rand.Next(-6, 6), Main.rand.Next(-6, 6), 100, default(Color), 2f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 3.5f;
            }
            // Large Smoke Gore spawn
            for (int g = 0; g < 2; g++)
            {
                if (!Main.dedServ)
                {
                    int goreIndex = Gore.NewGore(Projectile.GetSource_Death(), new Vector2(Projectile.position.X + (float)(Projectile.width / 2) - 24f, Projectile.position.Y + (float)(Projectile.height / 2) - 24f), default(Vector2), Main.rand.Next(61, 64), .8f);
                    Main.gore[goreIndex].scale = 1f;
                    Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X + 1f;
                    Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1f;
                    goreIndex = Gore.NewGore(Projectile.GetSource_Death(), new Vector2(Projectile.position.X + (float)(Projectile.width / 2) - 24f, Projectile.position.Y + (float)(Projectile.height / 2) - 24f), default(Vector2), Main.rand.Next(61, 64), .8f);
                    Main.gore[goreIndex].scale = 1f;
                    Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X - 1f;
                    Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1f;
                }
            }
        }
    }
}
