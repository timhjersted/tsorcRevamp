using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // Dust-rendered short cone:36t, drag.975, last8t harmless. No invisible long-range Gigas hitbox.
    public class CrystalSentryBreath : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));
        public override void SetDefaults()
        {
            Projectile.width = 20; Projectile.height = 20; Projectile.timeLeft = 36;
            Projectile.hostile = true; Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1; Projectile.tileCollide = true; Projectile.ignoreWater = true;
        }
        public override bool? CanDamage() => Projectile.timeLeft > 8 && Projectile.ai[2] == 0;
        public override void AI()
        {
            Projectile.velocity *= 0.975f;
            int owner = (int)Projectile.ai[0];
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.ai[2] == 0
                && (owner < 0 || owner >= Main.maxNPCs || !Main.npc[owner].active
                || Main.npc[owner].ModNPC is not CrystalSentry sentry || !sentry.BreathActive((int)Projectile.ai[1])))
            {
                Projectile.ai[2] = 1; Projectile.timeLeft = System.Math.Min(8, Projectile.timeLeft); Projectile.netUpdate = true;
            }
            if (Main.dedServ) return;
            float fade = MathHelper.Clamp(Projectile.timeLeft / 8f, 0, 1);
            Vector2 axis = Projectile.velocity.SafeNormalize(Vector2.UnitX), side = new(-axis.Y, axis.X);
            // Body particles stay within the20px collider; matching slow velocities prevents wall overshoot.
            for (int i = 0; i < 3; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + side * Main.rand.NextFloat(-8, 8), DustID.Frost,
                    Projectile.velocity * 0.2f + side * Main.rand.NextFloat(-0.4f, 0.4f),
                    (int)MathHelper.Lerp(220, 100, fade), Color.LightCyan, Main.rand.NextFloat(0.8f, 1.5f) * fade);
                dust.noGravity = true;
            }
            if (Projectile.timeLeft % 2 == 0) CrystalKnightShard.FrostDust(Projectile.Center, Projectile.velocity * 0.1f, 0.65f * fade);
            if (Projectile.timeLeft % 6 == 0)
            {
                Dust snow = Dust.NewDustPerfect(Projectile.Center, DustID.Snow, Projectile.velocity * 0.2f, 100, default, 0.6f * fade);
                snow.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.2f, 0.28f) * fade);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 180);
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.TornWings>(), 540);
            Buffs.Debuffs.FrostBuildup.Apply(target);
        }
        public override void OnKill(int timeLeft) => CrystalKnightShard.Shatter(Projectile.Center, 10);
        public override bool PreDraw(ref Color lightColor) => false;
    }
}
