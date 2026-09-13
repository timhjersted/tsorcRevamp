using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>A hostile, ballistic Blood Arrow whose clean misses teach its owning knight to lead farther.</summary>
    public class BloodKnightArrow : ModProjectile
    {
        public const float Gravity = 0.075f;

        private bool _reported;
        private bool _tileCollision;
        private bool _impactSpawned;
        private int _age;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/BloodArrow";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // Missed arrows remain physical until they strike a player or terrain.
            Projectile.timeLeft = 2;
            _age++;
            Projectile.velocity.Y += Gravity;
            // The supplied sprite's point faces down-left (135 degrees in texture space).
            // Remove that native angle so the point, rather than the fletching, follows velocity.
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.Pi * 0.75f;

            bool blackfire = Projectile.ai[2] > 0.5f;
            if (!Main.dedServ && Main.rand.NextBool(blackfire ? 4 : 7))
            {
                int dustType = blackfire ? DustID.Shadowflame : DustID.Blood;
                Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType,
                    -Projectile.velocity * 0.06f, 90, default, blackfire ? 0.75f : 0.65f);
                dust.noGravity = true;
            }

            if (_reported || _tileCollision || _age < 6)
                return;

            int targetIndex = (int)Projectile.ai[1];
            if (targetIndex < 0 || targetIndex >= Main.maxPlayers)
                return;

            Player target = Main.player[targetIndex];
            if (target.active && !target.dead
                && Vector2.Dot(target.Center - Projectile.Center, Projectile.velocity) < 0f)
            {
                ReportMiss();
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            _tileCollision = true;
            SpawnImpactDustOnce(oldVelocity);
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            DarkBloodKnight.ApplyBloodArrowDebuffs(target, Projectile.ai[2] > 0.5f);
            SpawnImpactDustOnce(Projectile.velocity);
            if (!_reported)
            {
                _reported = true;
                if (TryGetOwner(out DarkBloodKnight knight))
                    knight.ReportBloodArrowHit();
            }
        }

        private void SpawnImpactDustOnce(Vector2 travelVelocity)
        {
            if (_impactSpawned)
                return;

            _impactSpawned = true;
            SpawnImpactDust(Projectile.Center, travelVelocity);
        }

        public static void SpawnImpactDust(Vector2 center, Vector2 travelVelocity)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = travelVelocity.SafeNormalize(Vector2.UnitX);

            // Most of the blood follows the arrow's momentum, while a smaller radial layer
            // prevents the impact from reading as a narrow projectile trail.
            for (int i = 0; i < 60; i++)
            {
                bool directional = i < 42;
                Vector2 direction = directional
                    ? forward.RotatedBy(Main.rand.NextFloat(-0.95f, 0.95f))
                    : Main.rand.NextVector2Unit();
                float speed = directional
                    ? Main.rand.NextFloat(2.8f, 8.5f)
                    : Main.rand.NextFloat(1.2f, 5.5f);

                Dust blood = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(4f, 4f),
                    DustID.Blood,
                    direction * speed,
                    Main.rand.Next(20, 75),
                    default,
                    Main.rand.NextFloat(0.65f, 1.15f));
                blood.noGravity = false;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 direction = i < 3
                    ? forward.RotatedBy(Main.rand.NextFloat(-0.8f, 0.8f))
                    : Main.rand.NextVector2Unit();
                Dust wood = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(3f, 3f),
                    DustID.WoodFurniture,
                    direction * Main.rand.NextFloat(2f, 6f),
                    40,
                    default,
                    Main.rand.NextFloat(0.72f, 1f));
                wood.noGravity = false;
            }
        }

        private void ReportMiss()
        {
            _reported = true;
            if (TryGetOwner(out DarkBloodKnight knight))
                knight.ReportBloodArrowMiss();
        }

        private bool TryGetOwner(out DarkBloodKnight knight)
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs)
            {
                NPC owner = Main.npc[ownerIndex];
                if (owner.active && owner.ModNPC is DarkBloodKnight bloodKnight)
                {
                    knight = bloodKnight;
                    return true;
                }
            }

            knight = null;
            return false;
        }
    }
}
