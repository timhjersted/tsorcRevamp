using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>Ground fracture + abyss eruption for Artorias's physical landings. ai[0] = footprint width, ai[1] = eruption
    /// height (px), ai[2] = 1 for the persistent column, 0 for the quick 18t fracture.
    /// Quick (Flip Slash, Cursebreaker): flashes up and fades in 18t.
    /// Column (Jump Slash, Ground Pound / Hollow Pounce, Skyward Lunge): rises over 10t, holds for 90t total with dark-blue
    /// smoke rolling up through it, and gives Weight of Shadow to any player standing in it.
    /// Damage 0 = no hit; damage > 0 = the quick fracture hits through its first 58%, while the persistent column
    /// hits throughout its rise and hold. Both stop dealing damage when their final fade begins.</summary>
    class ArtoriasLandingImpactVFX : ModProjectile
    {
        const int QuickLifetime = 18;
        // The quick fracture starts fading at progress 0.58 (see PreDraw); damage stops there so fading wisps never hit.
        const float DamageStartProgress = 0.05f;
        const float DamageEndProgress = 0.58f;

        const int ColumnLifetime = 90;
        // Rise: the shader's Progress eases 0 -> 1 over ColumnRiseTicks; the column's top is fully revealed by ~7t.
        const int ColumnRiseTicks = 10;
        // The wide base flashes bright on impact, then settles over ColumnSettleTicks after the rise.
        const int ColumnSettleTicks = 20;
        const int ColumnFadeTicks = 24;
        // One dark-blue cloud gore every ColumnSmokeInterval ticks (~25 per column), stopping as the column fades.
        const int ColumnSmokeInterval = 3;
        // 3s, like the shadow floor wave: Weight of Shadow is not a LongerExpertDebuff, so this is the real duration.
        // Re-applied every tick the player stands in the column, so it runs 3s past stepping out.
        const int WeightOfShadowTicks = 3 * 60;

        // The eruption shader's rise mask reveals a pixel once Progress >= (height fraction from the bottom) * 0.72.
        const float EruptionRiseProgress = 0.72f;
        // The eruption's solid core spans |uv.x - 0.5| < 0.39 of its quad (ArtoriasAbyssEruption.fx exactCore).
        const float EruptionCoreHalfWidth = 0.39f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = QuickLifetime;
        }

        bool ColumnMode => Projectile.ai[2] >= 0.5f;

        int Lifetime => ColumnMode ? ColumnLifetime : QuickLifetime;

        // Ticks since spawn: 0 on the first AI tick.
        int Age => Lifetime - Projectile.timeLeft;

        float Progress => 1f - Projectile.timeLeft / (float)Lifetime;

        // Progress handed to the eruption / rift shaders. Quick: the whole life is one 0 -> 1 sweep. Column: an ease-out
        // rise over ColumnRiseTicks, then held at 1 (the shader's noise keeps scrolling upward, so it still moves).
        float ShaderProgress
        {
            get
            {
                if (!ColumnMode)
                {
                    return Progress;
                }

                float riseTime = MathHelper.Clamp(Age / (float)ColumnRiseTicks, 0f, 1f);
                float inverse = 1f - riseTime;
                return 1f - inverse * inverse;
            }
        }

        // 1 while solid, 0 when gone. Quick: fades over the last 42% of its life. Column: over its last ColumnFadeTicks.
        float Fade
        {
            get
            {
                if (!ColumnMode)
                {
                    return 1f - MathHelper.Clamp((Progress - 0.58f) / 0.42f, 0f, 1f);
                }

                return MathHelper.Clamp(Projectile.timeLeft / (float)ColumnFadeTicks, 0f, 1f);
            }
        }

        public override bool? CanDamage()
        {
            if (Projectile.damage <= 0)
            {
                return false;
            }

            if (ColumnMode)
            {
                return Age >= 1 && Projectile.timeLeft > ColumnFadeTicks;
            }

            float progress = Progress;
            return progress >= DamageStartProgress && progress <= DamageEndProgress;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return OverlapsEruption(targetHitbox);
        }

        // Two rising rectangles: the tall centre column and the wide low one PreDraw layers over it. Each is its quad's
        // core width, growing up from the quad's bottom edge as the shader's rise mask reveals it. Used for the hit
        // and for the column's debuff, so both match what is drawn.
        bool OverlapsEruption(Rectangle targetHitbox)
        {
            float width = Projectile.ai[0] * ColumnWidthScale;
            float height = Projectile.ai[1];
            float floorY = Projectile.Center.Y - 5f;
            float riseFraction = MathHelper.Clamp(ShaderProgress / EruptionRiseProgress, 0f, 1f);

            // Persistent column: top rises from 1.01h to 2.02h above the floor; bottom stays 0.11h below it.
            float tallQuadHeight = height * TallHeightScale;
            float tallQuadBottom = floorY - height * TallCenterHeightScale + tallQuadHeight * 0.5f;
            float tallHalfWidth = width * 0.62f * EruptionCoreHalfWidth;
            float tallHeight = tallQuadHeight * riseFraction;
            Rectangle tallColumn = new Rectangle((int)(Projectile.Center.X - tallHalfWidth), (int)(tallQuadBottom - tallHeight),
                (int)(tallHalfWidth * 2f), (int)tallHeight);

            // Wide column: quad centre floor - 0.30h, size 0.92w x 0.72h.
            float wideQuadBottom = floorY - height * 0.30f + height * 0.36f;
            float wideHalfWidth = width * 0.92f * EruptionCoreHalfWidth;
            float wideHeight = height * 0.72f * riseFraction;
            Rectangle wideColumn = new Rectangle((int)(Projectile.Center.X - wideHalfWidth), (int)(wideQuadBottom - wideHeight),
                (int)(wideHalfWidth * 2f), (int)wideHeight);

            return tallColumn.Intersects(targetHitbox) || wideColumn.Intersects(targetHitbox);
        }

        // The column thins to 80% width as it fades out, so it dissolves inward instead of just going transparent.
        float ColumnWidthScale => ColumnMode ? MathHelper.Lerp(0.8f, 1f, Fade) : 1f;

        // Share draw/collision geometry: double only the persistent column's upward reach, keeping its floor anchor.
        float TallHeightScale => ColumnMode ? 2.13f : 1.12f;

        float TallCenterHeightScale => ColumnMode ? 0.955f : 0.45f;

        public override void AI()
        {
            // Column lifetime: timeLeft spawns at QuickLifetime (SetDefaults runs before ai[] arrives), so stretch it
            // on the first tick. Every machine runs this from the synced ai[2]. localAI[1] = "lifetime set", unsynced.
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                if (ColumnMode)
                {
                    Projectile.timeLeft = ColumnLifetime;
                }
            }

            if (Main.dedServ)
            {
                return;
            }

            // Ground-slam thud, once per impact: the same Hollow Knight landing OwlFather's LeapSlam plays. Every
            // Artorias slam that hits the ground spawns exactly one of these, and each client runs this AI, so all
            // players hear it. localAI[0] = "already played", unsynced on purpose.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time")
                        with { Volume = 0.42f, Pitch = 0.18f },
                    Projectile.Center);
            }

            Vector2 floor = Projectile.Center - new Vector2(0f, 5f);
            float halfFootprint = Projectile.ai[0] * 0.44f;

            if (ColumnMode)
            {
                float fade = Fade;
                float height = Projectile.ai[1];
                float coreHalfWidth = Projectile.ai[0] * ColumnWidthScale * 0.62f * EruptionCoreHalfWidth;
                float riseFraction = MathHelper.Clamp(ShaderProgress / EruptionRiseProgress, 0f, 1f);
                float tallQuadHeight = height * TallHeightScale;
                float tallQuadBottom = floor.Y - height * TallCenterHeightScale + tallQuadHeight * 0.5f;
                float columnTop = tallQuadBottom - tallQuadHeight * riseFraction;

                // Light the column top to bottom: three points along it, dimming with the fade.
                for (int i = 0; i < 3; i++)
                {
                    float lightY = MathHelper.Lerp(floor.Y, columnTop, (i + 0.5f) / 3f);
                    Lighting.AddLight(new Vector2(floor.X, lightY), new Vector3(0.36f, 0.12f, 0.56f) * fade);
                }

                // Dark-blue smoke rolling up the column: the ChaosImpactCloud gores the shadow floor wave throws, born near
                // the floor across the core width and drifting up. Gores are decoration only and never sync.
                bool smoking = Projectile.timeLeft > ColumnFadeTicks;
                if (smoking && Age % ColumnSmokeInterval == 0)
                {
                    int variant = Main.rand.Next(1, 4);
                    int goreType = ModContent.Find<ModGore>($"tsorcRevamp/ChaosImpactCloud{variant}").Type;
                    // Gore.NewGore places the sprite's top-left corner, so offset by half its size to centre it on the column.
                    Vector2 goreHalfSize = TextureAssets.Gore[goreType].Value.Size() * 0.5f;
                    float bornX = floor.X + Main.rand.NextFloat(-coreHalfWidth, coreHalfWidth);
                    float bornY = floor.Y - Main.rand.NextFloat(0f, height * 0.35f * riseFraction);
                    Vector2 smokeVelocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-3.4f, -1.8f));
                    Gore.NewGore(Projectile.GetSource_FromThis(), new Vector2(bornX, bornY) - goreHalfSize, smokeVelocity, goreType,
                        Main.rand.NextFloat(0.85f, 1.25f));
                }

                // Grey-blue smoke dust up the whole visible column, so it stays dense between the gores.
                if (Main.rand.NextBool(2))
                {
                    float dustY = MathHelper.Lerp(floor.Y, columnTop, Main.rand.NextFloat());
                    Vector2 dustPosition = new Vector2(floor.X + Main.rand.NextFloat(-coreHalfWidth, coreHalfWidth), dustY);
                    Dust smokeDust = Dust.NewDustPerfect(dustPosition, DustID.Smoke, new Vector2(0f, Main.rand.NextFloat(-2.6f, -1.2f)),
                        120, new Color(52, 72, 132), Main.rand.NextFloat(1.1f, 1.6f) * fade);
                    smokeDust.noGravity = true;
                }

                // The curse in the smoke: standing in the column weighs the player down. Each machine applies it to its own
                // player (player buffs are client-owned). Starts once the column has visibly risen, stops as it fades.
                Player localPlayer = Main.LocalPlayer;
                bool debuffLive = Age >= 2 && fade > 0.25f;
                bool localPlayerInside = localPlayer.active && !localPlayer.dead && OverlapsEruption(localPlayer.Hitbox);
                if (debuffLive && localPlayerInside)
                {
                    localPlayer.AddBuff(ModContent.BuffType<WeightOfShadow>(), WeightOfShadowTicks);
                }
            }

            if (!Main.rand.NextBool(2))
            {
                return;
            }

            // Motes scatter across the whole footprint, so a 3x impact still reads as one burst, not a thin puff.
            int type = DustID.PurpleTorch;
            if (Main.rand.NextBool(5))
            {
                type = DustID.SilverFlame;
            }
            else if (Main.rand.NextBool(3))
            {
                type = DustID.ShadowbeamStaff;
            }

            Vector2 velocity = new Vector2(Main.rand.NextFloat(-3.8f, 3.8f),
                Main.rand.NextFloat(-6.4f, -1.2f));
            Dust dust = Dust.NewDustPerfect(floor + new Vector2(Main.rand.NextFloat(-halfFootprint, halfFootprint), 0f),
                type, velocity, 90, new Color(150, 46, 218), Main.rand.NextFloat(0.72f, 1.18f) * Fade);
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = ShaderProgress;
            float fade = Fade;
            float width = Projectile.ai[0] * ColumnWidthScale;
            float height = Projectile.ai[1];
            Vector2 floor = Projectile.Center - new Vector2(0f, 5f);

            // Column only: the tall core breathes +-10% (period ~25t), and the wide base flashes at 0.62 on impact,
            // settling to 0.32 over ColumnSettleTicks after the rise. The quick fracture keeps its fixed 0.72 / 0.42.
            float tallOpacity = 0.72f;
            float wideOpacity = 0.42f;
            if (ColumnMode)
            {
                float breathe = 0.9f + 0.1f * (float)Math.Sin(Age * 0.25f);
                tallOpacity = 0.72f * breathe;
                float settle = MathHelper.Clamp((Age - ColumnRiseTicks) / (float)ColumnSettleTicks, 0f, 1f);
                wideOpacity = MathHelper.Lerp(0.62f, 0.32f, settle);
            }

            // Rift thickness is half the eruption height: 34px at the default 68 height.
            ArtoriasVFX.DrawGroundRift(floor, new Vector2(width, height * 0.5f), progress, 0.70f * fade);
            ArtoriasVFX.DrawEruption(floor - new Vector2(0f, height * TallCenterHeightScale),
                new Vector2(width * 0.62f, height * TallHeightScale), progress, tallOpacity * fade);
            ArtoriasVFX.DrawEruption(floor - new Vector2(0f, height * 0.30f),
                new Vector2(width * 0.92f, height * 0.72f), progress, wideOpacity * fade);
            return false;
        }
    }
}
