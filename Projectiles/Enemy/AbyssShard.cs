using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Purple/white recolor of Deerclops's ice spike (ProjectileID.DeerclopsIceSpike). Reuses the same
    // shape that made the original read well: spawned at an approximate ground X, it snaps itself down
    // onto the actual solid surface, sits through a one-second dust-only telegraph with no hitbox, then
    // "pops" (extra dust burst + tiny screenshake) into a brief active/damaging window before fading
    // out and dying - entirely self-contained, so the boss side only has to decide where and when to
    // spawn one of these.
    class AbyssShard : ModProjectile
    {
        const int TelegraphTicks = 60;
        const int PopHoldTicks   = 10;
        const int FadeTicks      = 10;
        const int TotalTicks     = TelegraphTicks + PopHoldTicks + FadeTicks;
        const int EmergenceTicks = 14;
        const float EmergenceDepth = 52f;

        bool _grounded;
        bool _popped;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/AbyssShard";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 48;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = TotalTicks;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
        }

        public override void AI()
        {
            if (!_grounded)
            {
                SnapToGround();
                _grounded = true;
            }

            int elapsed = TotalTicks - Projectile.timeLeft;

            if (elapsed < TelegraphTicks)
            {
                Projectile.alpha = 255;
                if (!Main.dedServ && elapsed % 5 == 0)
                {
                    SpawnTelegraphDust();
                }
                return;
            }

            if (!_popped)
            {
                _popped = true;
                Pop();
            }

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.15f);

            int sincePop = elapsed - TelegraphTicks;
            if (sincePop < PopHoldTicks)
            {
                Projectile.alpha = 0;
            }
            else
            {
                float fadeT = (sincePop - PopHoldTicks) / (float)FadeTicks;
                Projectile.alpha = (int)MathHelper.Clamp(fadeT * 255f, 0f, 255f);
            }
        }

        // Deerclops finds ground by walking the target's column; we don't have easy access to a
        // player reference here, so just probe straight down (then up as a fallback) from the
        // spawn point - good enough since the boss side already spawns roughly at ground height.
        void SnapToGround()
        {
            int tileX = (int)(Projectile.Center.X / 16f);
            int tileY = (int)(Projectile.Center.Y / 16f);
            int foundY = -1;

            for (int i = 0; i < 60; i++)
            {
                int y = tileY + i;
                if (WorldGen.InWorld(tileX, y) && WorldGen.SolidTile(tileX, y))
                {
                    foundY = y;
                    break;
                }
            }

            if (foundY < 0)
            {
                for (int i = 1; i < 60; i++)
                {
                    int y = tileY - i;
                    if (WorldGen.InWorld(tileX, y) && WorldGen.SolidTile(tileX, y))
                    {
                        foundY = y;
                        break;
                    }
                }
            }

            if (foundY >= 0)
            {
                Projectile.Bottom = new Vector2(tileX * 16f + 8f, foundY * 16f);
            }
        }

        void SpawnTelegraphDust()
        {
            Vector2 pos = Projectile.Bottom - new Vector2(0f, 3f);
            bool white = Main.rand.NextBool(5);
            int dustType = white ? DustID.SilverFlame
                : Main.rand.NextBool(3) ? DustID.ShadowbeamStaff : DustID.Smoke;
            Color tint = white ? new Color(224, 214, 255)
                : dustType == DustID.Smoke ? new Color(25, 8, 38) : new Color(118, 32, 180);
            Dust d = Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(-26f, 26f), Main.rand.NextFloat(-5f, 3f)),
                dustType, new Vector2(Main.rand.NextFloat(-1.3f, 1.3f), Main.rand.NextFloat(-1.4f, -0.25f)),
                110, tint, Main.rand.NextFloat(0.9f, 1.4f));
            d.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int elapsed = TotalTicks - Projectile.timeLeft;
            if (elapsed < TelegraphTicks)
            {
                float telegraph = elapsed / (float)TelegraphTicks;
                ArtoriasVFX.DrawGroundRift(Projectile.Bottom - new Vector2(0f, 4f),
                    new Vector2(76f, 32f), telegraph, 0.42f + telegraph * 0.42f);

                float emergence = MathHelper.Clamp(
                    (elapsed - (TelegraphTicks - EmergenceTicks)) / (float)EmergenceTicks, 0f, 1f);
                if (emergence > 0f)
                {
                    float eased = MathHelper.SmoothStep(0f, 1f, emergence);
                    Vector2 source = Projectile.Bottom - new Vector2(0f, 5f);
                    ArtoriasVFX.DrawOrb(source, new Vector2(68f, 28f),
                        Main.GlobalTimeWrappedHourly * 1.8f, emergence, 0.58f * emergence);
                    DrawShardSprite(Projectile.Bottom + new Vector2(0f,
                        MathHelper.Lerp(EmergenceDepth, 0f, eased)),
                        new Color(200, 140, 255, (int)(255f * emergence)));
                }
                return false;
            }

            float activeProgress = MathHelper.Clamp((elapsed - TelegraphTicks) / (float)(PopHoldTicks + FadeTicks), 0f, 1f);
            float activeOpacity = 1f - MathHelper.Clamp((activeProgress - 0.55f) / 0.45f, 0f, 1f);
            ArtoriasVFX.DrawGroundRift(Projectile.Bottom - new Vector2(0f, 4f),
                new Vector2(58f, 26f), 1f, 0.65f * activeOpacity);
            ArtoriasVFX.DrawEruption(Projectile.Center, Projectile.Size, activeProgress, 0.80f * activeOpacity);

            DrawShardSprite(Projectile.Bottom, GetAlpha(lightColor) ?? lightColor);
            return false;
        }

        void DrawShardSprite(Vector2 bottom, Color color)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            // Every authored frame points right; the previous alternating assumption inverted
            // odd-numbered frames. Rotating all frames the same way makes every shard rise upward.
            Vector2 origin = new Vector2(0f, source.Height * 0.5f);
            Main.EntitySpriteDraw(texture, bottom - Main.screenPosition, source, color,
                -MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0);
        }

        void Pop()
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = -0.1f }, Projectile.Center);
            UsefulFunctions.ScreenShake(Projectile.Center, strength: 2.5f, frames: 8);

            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 12; i++)
            {
                Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : Color.White;
                Vector2 vel = new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), Main.rand.NextFloat(-6f, -1.4f));
                int type = i % 4 == 0 ? DustID.SilverFlame : DustID.ShadowbeamStaff;
                Dust d = Dust.NewDustPerfect(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-18f, 18f), -2f),
                    type, vel, 70, tint, Main.rand.NextFloat(0.9f, 1.45f));
                d.noGravity = true;
            }
        }

        public override bool CanHitPlayer(Player target)
        {
            int elapsed = TotalTicks - Projectile.timeLeft;
            return elapsed >= TelegraphTicks;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Crippled>(), 3 * 60, false);
            target.AddBuff(BuffID.Bleeding, 16 * 60, false);
            target.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 16 * 60, false);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(200, 140, 255, 255 - Projectile.alpha);
        }
    }
}
