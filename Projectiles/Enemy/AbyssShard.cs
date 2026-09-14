using System.Collections.Generic;
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
    // Attack spec — Abyss Shard / PuppetNPC.AbyssShardFire / AbyssShard.
    // Tell: 60t at the exact snapped tile surface; a 2x2-pixelated abyss breach grows 1-3 tiles high
    // while dark/bright purple motes rise from that surface. Birth: the five-frame shard rises from
    // 52px below ground during the final 14t. Life: the original 20 damaging ticks remain fully
    // exposed, followed by an 18t non-damaging retreat and fade back to that depth. Draw order:
    // portal and shard behind solid tiles. Multiplayer: projectile
    // spawn/damage remains server authoritative; shader phase derives from identity and dust is cosmetic.
    class AbyssShard : ModProjectile
    {
        const int TelegraphTicks = 60;
        const int PopHoldTicks   = 20;
        const int RetreatTicks   = 18;
        const int DamageTicks    = 20;
        const int TotalTicks     = TelegraphTicks + PopHoldTicks + RetreatTicks;
        const int EmergenceTicks = 14;
        const float EmergenceDepth = 52f;
        static readonly Vector2 PortalSize = new(80f, 52f);

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
                if (!Main.dedServ && elapsed < TelegraphTicks - EmergenceTicks && elapsed % 4 == 0)
                {
                    float telegraphProgress = elapsed / (float)TelegraphTicks;
                    SpawnSurfaceDust(true, telegraphProgress,
                        1 + (int)(telegraphProgress * 2f));
                }
                return;
            }

            if (!_popped)
            {
                _popped = true;
                Pop();
            }

            Lighting.AddLight(Projectile.Center, new Color(138, 40, 214).ToVector3() * 0.82f);

            int sincePop = elapsed - TelegraphTicks;
            if (!Main.dedServ && sincePop % 2 == 0)
            {
                SpawnSurfaceDust(false, 1f - sincePop / (float)(PopHoldTicks + RetreatTicks), 2);
            }

            if (sincePop < PopHoldTicks)
            {
                Projectile.alpha = 0;
            }
            else
            {
                float retreat = MathHelper.Clamp((sincePop - PopHoldTicks) / (float)RetreatTicks, 0f, 1f);
                float fade = MathHelper.Clamp((retreat - 0.18f) / 0.82f, 0f, 1f);
                Projectile.alpha = (int)MathHelper.SmoothStep(0f, 255f, fade);
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

        void SpawnSurfaceDust(bool upward, float intensity, int count)
        {
            Vector2 surface = Projectile.Bottom - new Vector2(0f, 2f);
            float direction = upward ? -1f : 1f;
            for (int i = 0; i < count; i++)
            {
                bool bright = Main.rand.NextBool(3);
                int dustType = bright ? DustID.PurpleTorch : DustID.ShadowbeamStaff;
                Color tint = bright ? new Color(194, 62, 255) : new Color(34, 5, 62);
                float speed = upward
                    ? Main.rand.NextFloat(0.65f, 1.65f + intensity * 0.45f)
                    : Main.rand.NextFloat(1.4f, 3.5f);
                Vector2 velocity = new(Main.rand.NextFloat(-0.55f, 0.55f), direction * speed);
                float scale = bright ? Main.rand.NextFloat(0.46f, 0.76f) : Main.rand.NextFloat(0.54f, 0.86f);
                Dust dust = Dust.NewDustPerfect(
                    surface + new Vector2(Main.rand.NextFloat(-30f, 30f), Main.rand.NextFloat(-2f, 2f)),
                    dustType, velocity, bright ? 80 : 145, tint, scale);
                dust.noGravity = true;
                dust.noLight = !bright;
                if (upward && !bright)
                {
                    // fadeIn is a grow-to-scale target. Using the post-jitter scale guarantees this
                    // dark background layer actually billows before it naturally shrinks away.
                    dust.fadeIn = dust.scale + Main.rand.NextFloat(0.22f, 0.38f);
                }
            }
        }

        /// <summary>The shard is buried in terrain at both ends of its lifecycle, so it belongs in
        /// the behind-NPCs-and-tiles pass. Solid blocks then occlude the submerged portion naturally.</summary>
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int elapsed = TotalTicks - Projectile.timeLeft;
            if (elapsed < TelegraphTicks)
            {
                float telegraph = elapsed / (float)TelegraphTicks;
                Vector2 portalCenter = Projectile.Bottom - new Vector2(0f, PortalSize.Y * 0.5f - 2f);
                float phase = (Projectile.identity * 0.173f) % 1f;
                ArtoriasVFX.DrawAbyssShardPortal(portalCenter, PortalSize, telegraph,
                    0.52f + telegraph * 0.38f, phase);

                float emergence = MathHelper.Clamp(
                    (elapsed - (TelegraphTicks - EmergenceTicks)) / (float)EmergenceTicks, 0f, 1f);
                if (emergence > 0f)
                {
                    float eased = MathHelper.SmoothStep(0f, 1f, emergence);
                    DrawShardSprite(Projectile.Bottom + new Vector2(0f,
                        MathHelper.Lerp(EmergenceDepth, 0f, eased)),
                        new Color(200, 140, 255) * emergence);
                }
                return false;
            }

            int sincePop = elapsed - TelegraphTicks;
            float retreat = MathHelper.Clamp((sincePop - PopHoldTicks) / (float)RetreatTicks, 0f, 1f);
            float sink = MathHelper.SmoothStep(0f, 1f, retreat);
            Vector2 retreatBottom = Projectile.Bottom + new Vector2(0f, EmergenceDepth * sink);
            DrawShardSprite(retreatBottom, GetAlpha(lightColor) ?? lightColor);
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

            SpawnSurfaceDust(false, 1f, 14);
        }

        public override bool CanHitPlayer(Player target)
        {
            int elapsed = TotalTicks - Projectile.timeLeft;
            int sincePop = elapsed - TelegraphTicks;
            return sincePop >= 0 && sincePop < DamageTicks;
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
