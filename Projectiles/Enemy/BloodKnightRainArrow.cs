using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>One delayed lane in the Blood Knight's deterministic overhead arrow curtain.</summary>
    public class BloodKnightRainArrow : ModProjectile
    {
        private bool _impactSpawned;
        private bool Armed => Projectile.ai[1] <= 0f;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/BloodArrow";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
        }

        public override bool ShouldUpdatePosition() => Armed;

        public override bool? CanDamage() => Armed ? null : false;

        public override void AI()
        {
            // Once spawned, a rain arrow persists until player or tile collision.
            Projectile.timeLeft = 2;
            if (!Armed)
            {
                Projectile.ai[1]--;
                if (Projectile.ai[1] <= 0f)
                {
                    Projectile.hide = false;
                    Projectile.tileCollide = true;
                    Projectile.netUpdate = true;
                }
                return;
            }

            Projectile.velocity.Y += BloodKnightArrow.Gravity;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.Pi * 0.75f;

            bool blackfire = Projectile.ai[2] > 0.5f;
            if (!Main.dedServ && Main.rand.NextBool(blackfire ? 4 : 7))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center,
                    blackfire ? DustID.Shadowflame : DustID.Blood,
                    -Projectile.velocity * 0.05f, 90, default, blackfire ? 0.75f : 0.65f);
                dust.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            DarkBloodKnight.ApplyBloodArrowDebuffs(target, Projectile.ai[2] > 0.5f);
            SpawnImpactDustOnce(Projectile.velocity);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnImpactDustOnce(oldVelocity);
            return true;
        }

        private void SpawnImpactDustOnce(Vector2 travelVelocity)
        {
            if (_impactSpawned)
                return;

            _impactSpawned = true;
            BloodKnightArrow.SpawnImpactDust(Projectile.Center, travelVelocity);
        }
    }
}
