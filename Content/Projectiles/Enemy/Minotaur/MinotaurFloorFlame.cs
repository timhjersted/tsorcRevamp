using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Minotaur
{
    // Each server-placed module rests on its own terrain column. ai[0] is warning duration;
    // ai[2] is synced age. 360 damaging ticks, then a harmless 20t extinguish.
    class MinotaurFloorFlame : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));
        const int BurnTicks = 360;
        const int FadeTicks = 20;
        int WarningTicks => (int)Projectile.ai[0];
        int Age => (int)Projectile.ai[2];
        bool Burning => Age > WarningTicks && Age <= WarningTicks + BurnTicks;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 500;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => Burning ? null : false;

        public override void AI()
        {
            Projectile.ai[2]++;
            if (Age > WarningTicks + BurnTicks + FadeTicks)
            {
                Projectile.Kill();
                return;
            }
            if (Main.dedServ)
            {
                return;
            }

            if (Age == WarningTicks + 1)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.18f, Pitch = 0.3f }, Projectile.Center);
                MinotaurFireVFX.Extinguish(Projectile.Bottom - Vector2.UnitY * 8f, 16);
            }

            float fade = MathHelper.Clamp((WarningTicks + BurnTicks + FadeTicks - Age) / (float)FadeTicks, 0f, 1f);
            if (Age <= WarningTicks)
            {
                // Ramp simmering embers from 1 to 3/tick as ignition nears so the danger footprint is unmistakable.
                float warn = MathHelper.Clamp(Age / (float)System.Math.Max(1, WarningTicks), 0f, 1f);
                int warnCount = 1 + (int)(warn * 2f);
                MinotaurFireVFX.Embers(Projectile.Bottom, 9f, warnCount, 0.65f + warn * 0.4f);
            }
            else
            {
                // Active burning and extinguish fade: full 3-layer fire column every tick.
                MinotaurFireVFX.BurnColumn(Projectile.Bottom, 9f, fade, Age % 3 == 0);
            }

            float lightIntensity = 0.3f;
            if (Burning)
            {
                lightIntensity = fade;
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.65f, 0.26f, 0.05f) * lightIntensity);
        }

        public override bool PreDraw(ref Color lightColor) => false;
        public override void PostDraw(Color lightColor)
        {
            float fade = MathHelper.Clamp((WarningTicks + BurnTicks + FadeTicks - Age) / (float)FadeTicks, 0f, 1f);
            float warn = MathHelper.Clamp(Age / (float)System.Math.Max(1, WarningTicks), 0f, 1f);
            Vector2 size = new Vector2(36f, 48f);
            float opacity = fade * 0.9f;
            if (Age <= WarningTicks)
            {
                size = new Vector2(30f, 14f + warn * 10f);
                opacity = 0.25f + warn * 0.22f;
            }

            MinotaurFireVFX.Draw(Projectile.Bottom - new Vector2(0f, size.Y * 0.44f), size,
                opacity, Projectile.identity * 0.17f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.OnFire, 120);
        public override void OnKill(int timeLeft) => MinotaurFireVFX.Extinguish(Projectile.Center, 12);
    }
}
