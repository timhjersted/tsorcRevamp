using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Winter's Embrace: a fixed point in space (anchored where it was cast — it does NOT track the
    ///giant) that pulls the player toward it for its whole 5-second life, like GigasUndertowZone but
    ///radial around a point instead of a directional band toward the NPC. While pulling, it fires
    ///three sets of eight GigasEmbraceIcicle in a ring around its centre: each set spawns, vibrates
    ///in place for 50 ticks (the telegraph), then launches outward together. The three sets are
    ///rotated 15° from each other so the safe gaps between lanes shift wave to wave — camping one
    ///spot only works for the first burst. Deals no damage itself; the icicles are the payoff, same
    ///split as GigasUndertowZone's pull-vs-exhale.
    ///</summary>
    class GigasFrostZone : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const int Duration = 320; //90 (first suck) + 100*2 (later sets) + 30 tail for the last burst to read
        const int FirstSpawnAge = 41; //spawns here, vibrates 50 more -> fires ~90 ticks after cast
        const int SetInterval = 100; //ticks between each set's spawn (and so each fire, since vibrate is fixed)
        const int SetCount = 3;
        const int VibrateTicks = 50; //must match what's passed as GigasEmbraceIcicle's ai[1]
        const int IciclesPerSet = 8;
        const float RingRadius = 56f; //spawn ring; chord spacing at 8-around clears icicle width easily
        const float SetRotationOffset = MathHelper.PiOver2 / 3f; //45°/3 = 15° shift per set

        //Radial pull, same shape as GigasUndertowZone's per-axis pull but toward a fixed point.
        const float PullPerTick = 0.22f;
        const float PullSpeedCap = 6f;
        const float InnerDeadzone = 60f; //no pull once basically on top of it
        const int PullRangeWidth = 900;
        const int PullRangeHeight = 500;

        //Quarter the old 160px crystallize-disc radius — a small, dense, energetic core rather
        //than a big static sheet, per feedback that the large version read as nothing at all.
        const float VisualRadius = 40f;
        const int QuadSize = 108;
        const int FadeTicks = 12; //opacity ramp at the very start/end so it doesn't pop on/off

        const string TextureRoot = "tsorcRevamp/Textures/Noise/";
        static Asset<Effect> zoneEffect;
        static Asset<Texture2D> cellNoise;
        static Asset<Texture2D> facetNoise;

        //1-indexed: localAI[0] is incremented at the top of AI(), so the first tick already reads
        //1, not 0 — matches the convention the rest of the Ice Gigas kit uses for Timer/Age fields.
        int Age => (int)Projectile.localAI[0];
        //0 at the first set's spawn tick, 100 at the second, 200 at the third; negative during the
        //pure 40-tick pull-only opening before anything has spawned yet.
        int RelativeAge => Age - FirstSpawnAge;
        int IcicleDamage => (int)(Projectile.damage * 0.6f);

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = PullRangeWidth;
            Projectile.height = PullRangeHeight;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = Duration;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Duration;
            SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = -0.6f }, Projectile.Center);
        }

        public override bool? CanDamage()
        {
            return false; //pure pull + spawner — the icicles carry the damage
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            RunPull();

            //First set spawns at FirstSpawnAge (~90 ticks of pure pull before it fires), then one
            //every SetInterval after that.
            if (RelativeAge >= 0 && RelativeAge % SetInterval == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int setIndex = RelativeAge / SetInterval;
                if (setIndex < SetCount)
                {
                    SpawnSet(setIndex);
                }
            }

            RunDustChoreography();
            Lighting.AddLight(Projectile.Center, 0.3f, 0.5f, 0.7f);
        }

        ///<summary>Radial pull toward the zone's fixed centre. Applied on every client so the local
        ///player's movement (authoritative for them) feels it; resisting stays possible by holding
        ///away, same as GigasUndertowZone — the dot-product check only adds pull while the player's
        ///CURRENT speed toward the centre is under the cap.</summary>
        void RunPull()
        {
            Rectangle band = Projectile.Hitbox;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || !player.Hitbox.Intersects(band))
                {
                    continue;
                }
                Vector2 toCenter = Projectile.Center - player.Center;
                float distance = toCenter.Length();
                if (distance < InnerDeadzone)
                {
                    continue;
                }
                Vector2 pullDir = toCenter / distance;
                float towardSpeed = Vector2.Dot(player.velocity, pullDir);
                if (towardSpeed < PullSpeedCap)
                {
                    player.velocity += pullDir * PullPerTick;
                }
            }
        }

        ///<summary>Eight icicles evenly spaced around the ring, rotated `setIndex * 15°` from the
        ///previous set so the dodgeable gaps between lanes shift each wave.</summary>
        void SpawnSet(int setIndex)
        {
            SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = 0.2f + setIndex * 0.15f }, Projectile.Center);
            float rotation = setIndex * SetRotationOffset;
            for (int i = 0; i < IciclesPerSet; i++)
            {
                float angle = rotation + i * (MathHelper.TwoPi / IciclesPerSet);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * RingRadius;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, Vector2.Zero,
                    ModContent.ProjectileType<GigasEmbraceIcicle>(), IcicleDamage, 1f, Main.myPlayer, angle, VibrateTicks);
            }
        }

        ///<summary>Snow pulls IN toward the centre — for the opening 40-tick pure-suck window AND
        ///every set's 50-tick vibrate — then bursts OUT the instant a set launches. One continuous
        ///"gathering" read from the moment it lands, not just during each set's own telegraph.</summary>
        void RunDustChoreography()
        {
            bool burstTick = RelativeAge >= 0 && RelativeAge % SetInterval == VibrateTicks;
            if (burstTick)
            {
                for (int i = 0; i < 16; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(7f, 7f);
                    int dust = Dust.NewDust(Projectile.Center, 1, 1, DustID.Snow, vel.X, vel.Y, 60, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
            }
            else if (Main.rand.NextBool(2))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * Main.rand.NextFloat(20f, 90f);
                int dust = Dust.NewDust(pos, 4, 4, DustID.Snow, 0f, 0f, 90, default, 1.1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = (Projectile.Center - pos) * 0.12f;
            }
            //Ambient core shimmer regardless of phase, so the hub reads as "alive" between beats
            if (Main.rand.NextBool(3))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * Main.rand.NextFloat(0f, VisualRadius);
                int glint = Dust.NewDust(pos, 4, 4, DustID.IceRod, 0f, 0f, 70, default, 0.8f);
                Main.dust[glint].noGravity = true;
                Main.dust[glint].velocity *= 0.1f;
            }
        }

        static void LoadAssets()
        {
            zoneEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/IceGigasWintersGrasp", AssetRequestMode.ImmediateLoad);
            //Flat-shaded polygons, used as ice-slab GEOMETRY rather than as a smooth modulator
            cellNoise ??= ModContent.Request<Texture2D>(TextureRoot + "VoronoiNoise", AssetRequestMode.ImmediateLoad);
            facetNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_Noise_Wo14", AssetRequestMode.ImmediateLoad);
        }

        ///<summary>Ground-hugging sheet: behind tiles and NPCs so it reads as ice hanging in the
        ///air/on the floor, and the existing dust (which draws in a later pass) still sits on top.</summary>
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();
            Texture2D primary = cellNoise.Value;
            //Fade in/out over the first/last 12 ticks instead of popping on/off at full strength —
            //part of why the old version read as "here for a split second and gone".
            float fadeIn = MathHelper.Clamp(Age / (float)FadeTicks, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            float opacity = MathHelper.Min(fadeIn, fadeOut) * 0.9f;
            float time = Main.GlobalTimeWrappedHourly;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = facetNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                Effect effect = zoneEffect.Value;
                effect.CurrentTechnique = effect.Techniques["IceGigasWintersGrasp"];
                effect.Parameters["OuterColor"].SetValue(new Color(18, 40, 68).ToVector3());
                effect.Parameters["MiddleColor"].SetValue(new Color(88, 168, 226).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(205, 236, 250).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["DrawSize"].SetValue(new Vector2(QuadSize));
                effect.Parameters["Radius"].SetValue(VisualRadius);
                effect.Parameters["Progress"].SetValue(1f);
                //No telegraph/crystallize split any more — this is always the energetic core, so
                //Active is pinned to 1 and RimGain/RimCoreGain use their Active=1 values directly.
                effect.Parameters["Active"].SetValue(1f);
                //~4x the original pan speed: a small, constantly churning vortex reads as "lots of
                //movement" the way the old large, nearly-static disc never could.
                effect.Parameters["EdgePan"].SetValue(new Vector2(0.5f + time * 0.016f, 0.5f - time * 0.012f));
                effect.Parameters["ShapePan"].SetValue(new Vector2(0.5f + time * 0.035f, 0.5f - time * 0.028f));
                effect.Parameters["FacetPan"].SetValue(new Vector2(0.5f - time * 0.045f, 0.5f + time * 0.036f));
                effect.Parameters["RimGain"].SetValue(1f);
                effect.Parameters["RimCoreGain"].SetValue(0.8f);
                effect.Parameters["CellScale"].SetValue(1.2f / QuadSize);
                effect.Parameters["FacetScale"].SetValue(3.4f / QuadSize);
                //2px blocks now that the quad is small again (was 3px at the old 420px size).
                effect.Parameters["PixelGrid"].SetValue(new Vector4(
                    QuadSize / 2f, QuadSize / 2f, 2f / QuadSize, 2f / QuadSize));
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(primary, Projectile.Center - Main.screenPosition, null, Color.White, 0f,
                    primary.Size() * 0.5f, QuadSize / (float)primary.Width, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }
    }
}
