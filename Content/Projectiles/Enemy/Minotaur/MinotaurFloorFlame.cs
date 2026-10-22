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
            if (Main.dedServ) return;
            if (Age == WarningTicks + 1)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.18f, Pitch = 0.3f }, Projectile.Center);
                MinotaurFireVFX.Extinguish(Projectile.Bottom - Vector2.UnitY * 8f, 8);
            }
            float fade = MathHelper.Clamp((WarningTicks + BurnTicks + FadeTicks - Age) / (float)FadeTicks, 0f, 1f);
            if (Age % (Burning ? 2 : 5) == 0)
                MinotaurFireVFX.Embers(Projectile.Bottom, 7f, 1, Burning ? fade : 0.6f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.5f, 0.19f, 0.035f) * (Burning ? fade : 0.25f));
        }

        public override bool PreDraw(ref Color lightColor) => false;
        public override void PostDraw(Color lightColor)
        {
            float fade = MathHelper.Clamp((WarningTicks + BurnTicks + FadeTicks - Age) / (float)FadeTicks, 0f, 1f);
            float warn = MathHelper.Clamp(Age / (float)System.Math.Max(1, WarningTicks), 0f, 1f);
            Vector2 size = Age <= WarningTicks ? new Vector2(24f, 10f + warn * 8f) : new Vector2(26f, 40f);
            MinotaurFireVFX.Draw(Projectile.Bottom - new Vector2(0f, size.Y * 0.44f), size,
                Age <= WarningTicks ? 0.2f + warn * 0.18f : fade * 0.85f, Projectile.identity * 0.17f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.OnFire, 120);
        public override void OnKill(int timeLeft) => MinotaurFireVFX.Extinguish(Projectile.Center, 8);
    }
}
