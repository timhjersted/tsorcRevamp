using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.VFX
{
    /// <summary>Networked one-shot visual used when a teleport illusion expires.</summary>
    public class TeleportIllusionDissolve : ModProjectile
    {
        private const int GoreHoldTicks = 30;
        private const int GoreFadeTicks = 30;
        private const int TotalLifetimeTicks = GoreHoldTicks + GoreFadeTicks + 4;

        private readonly int[] spawnedGoreIndices = { -1, -1, -1 };
        private readonly int[] spawnedGoreTypes = new int[3];
        private int elapsedTicks;

        public override string Texture => "tsorcRevamp/NPCs/Invaders/PuppetPlaceholder";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = TotalLifetimeTicks;
        }

        public override void AI()
        {
            if (Main.dedServ)
            {
                return;
            }

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;

                for (int i = 0; i < 3; i++)
                {
                    int goreType = Mod.Find<ModGore>($"Cloud Gore {i + 1}").Type;
                    Vector2 goreVelocity = Main.rand.NextVector2Circular(2.2f, 2.2f) + new Vector2(0f, -1.5f);
                    int goreIndex = Gore.NewGore(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        goreVelocity,
                        goreType,
                        Main.rand.NextFloat(0.8f, 1.15f));

                    if (goreIndex >= 0 && goreIndex < Main.maxGore)
                    {
                        // Cap vanilla gore lifetime to 30 ticks as a fallback if this projectile clears early.
                        Main.gore[goreIndex].timeLeft = GoreHoldTicks;
                        spawnedGoreIndices[i] = goreIndex;
                        spawnedGoreTypes[i] = goreType;
                    }
                }

                for (int i = 0; i < 32; i++)
                {
                    bool blue = Main.rand.NextBool();
                    int dustType = blue ? DustID.BlueTorch : DustID.Cloud;
                    Color dustColor = blue ? Color.LightBlue : Color.White;
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(14f, 24f),
                        dustType,
                        Main.rand.NextVector2Circular(3.2f, 3.2f),
                        120,
                        dustColor,
                        Main.rand.NextFloat(1.1f, 1.8f));
                    dust.noGravity = true;
                }
            }

            elapsedTicks++;

            // Vanilla sticky gores fade at 1 alpha/tick (~4.25s) after timeLeft hits 0; fade smoothly over 30 ticks starting at tick 30.
            if (elapsedTicks >= GoreHoldTicks)
            {
                float fadeFraction = MathHelper.Clamp((elapsedTicks - GoreHoldTicks) / (float)GoreFadeTicks, 0f, 1f);
                int targetAlpha = (int)MathHelper.Lerp(0f, 255f, fadeFraction);

                for (int i = 0; i < spawnedGoreIndices.Length; i++)
                {
                    int goreIndex = spawnedGoreIndices[i];
                    if (goreIndex < 0 || goreIndex >= Main.maxGore)
                    {
                        continue;
                    }

                    Gore cloudGore = Main.gore[goreIndex];
                    if (!cloudGore.active || cloudGore.type != spawnedGoreTypes[i])
                    {
                        spawnedGoreIndices[i] = -1;
                        continue;
                    }

                    cloudGore.alpha = Math.Max(cloudGore.alpha, targetAlpha);
                    if (cloudGore.alpha >= 255)
                    {
                        cloudGore.active = false;
                        spawnedGoreIndices[i] = -1;
                    }
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < spawnedGoreIndices.Length; i++)
            {
                int goreIndex = spawnedGoreIndices[i];
                if (goreIndex < 0 || goreIndex >= Main.maxGore)
                {
                    continue;
                }

                Gore cloudGore = Main.gore[goreIndex];
                if (cloudGore.active && cloudGore.type == spawnedGoreTypes[i])
                {
                    cloudGore.alpha = 255;
                    cloudGore.active = false;
                }

                spawnedGoreIndices[i] = -1;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
        public override bool? CanDamage() => false;
    }
}
