using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.DarkrootImpSpells
{
    // One puff of the Darkroot Imp's Cursed Breath sweep. Dust-only (no sprite): the stream reads as green curse fire.
    // Grows from 0.6x to 1.4x over its life at constant speed (reach = speed x LifetimeTicks; the demon gates the breath on it),
    // and thins out over the last FadeTicks instead of popping.
    // Stops on tiles (the breath does not go through walls). Lifetime is fixed here, not patched after spawn (timeLeft isn't synced).
    public class DarkrootImpBreath : ModProjectile
    {
        public const int LifetimeTicks = 44;
        private const int FadeTicks = 14;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            float age = LifetimeTicks - Projectile.timeLeft;
            float growth = MathHelper.Clamp(age / LifetimeTicks, 0f, 1f);
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            float scale = MathHelper.Lerp(0.6f, 1.4f, growth);

            Lighting.AddLight(Projectile.Center, 0.25f * fade, 0.55f * fade, 0.1f * fade);

            if (Main.dedServ)
            {
                return;
            }

            // Body: two green flames per tick while alive, one once it starts thinning out.
            int flameCount = 2;
            if (fade < 1f)
            {
                flameCount = 1;
            }

            for (int i = 0; i < flameCount; i++)
            {
                Vector2 spot = Projectile.Center + Main.rand.NextVector2Circular(8f * scale, 8f * scale);
                Dust flame = Dust.NewDustPerfect(spot, DustID.CursedTorch, Projectile.velocity * 0.35f, 40, default, 2.4f * scale * fade + 0.4f);
                flame.noGravity = true;
            }

            // Plum accent through the green, the same "powerful curse" tint the old breath carried.
            if (Main.rand.NextBool(4))
            {
                PowerfulCurseBuildup.SpawnAccentDust(Projectile.position, Projectile.width, Projectile.height, Projectile.velocity * 0.2f, 1.6f * scale * fade);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            // A small smoky gutter wherever it ends (wall, player or timeout).
            for (int i = 0; i < 6; i++)
            {
                Dust smoke = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.CursedTorch, 0f, 0f, 120, default, 1.2f);
                smoke.noGravity = true;
                smoke.velocity *= 0.6f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Same debuff set the demon's old EnemyCursedBreath applied.
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(ModContent.BuffType<PowerfulCurseBuildup>(), 36000, false);
            }

            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.CursedInferno, 150, false);
                target.AddBuff(BuffID.Bleeding, 1800, false);
                target.AddBuff(BuffID.Weak, 150, false);
            }
        }
    }
}
