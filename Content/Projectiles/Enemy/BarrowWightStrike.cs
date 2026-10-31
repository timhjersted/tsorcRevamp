using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // One invisible, NPC-owned hitbox per lunge/plunge. The source's synced strike sequence and
    // active stage terminate it immediately on stagger, recovery or slot reuse.
    public class BarrowWightStrike : ModProjectile
    {
        private readonly bool[] _hitPlayers = new bool[Main.maxPlayers];
        private Vector2 _previousCenter;
        private bool _hasSample;

        // Intentionally invisible; reuse the already-required sword asset for content loading.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyBarrowWightSword));

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 46;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
        }

        private BarrowWightBase Source
        {
            get
            {
                int index = (int)Projectile.ai[0];
                if (index < 0 || index >= Main.maxNPCs)
                {
                    return null;
                }
                NPC npc = Main.npc[index];
                if (!npc.active || npc.type != (int)Projectile.ai[2]
                    || npc.ModNPC is not BarrowWightBase wight || wight.StrikeSequence != (int)Projectile.ai[1])
                {
                    return null;
                }
                return wight;
            }
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void AI()
        {
            BarrowWightBase source = Source;
            if (source == null || !source.BodyStrikeActive)
            {
                Projectile.Kill();
                return;
            }
            _previousCenter = Projectile.Center;
            if (!_hasSample)
            {
                _previousCenter = source.NPC.Center;
                _hasSample = true;
            }
            Projectile.Center = source.NPC.Center;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage()
        {
            return Source != null && Source.BodyStrikeActive;
        }

        public override bool CanHitPlayer(Player target)
        {
            return !_hitPlayers[target.whoAmI];
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            BarrowWightBase source = Source;
            if (source == null || !source.BodyStrikeActive)
            {
                return false;
            }
            if (source.LandingStrikeActive)
            {
                Vector2 center = source.LandingStrikeCenter;
                Vector2 closest = Vector2.Clamp(center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
                return Vector2.DistanceSquared(center, closest) <= 48f * 48f;
            }

            Rectangle sweptBodyTarget = targetHitbox;
            sweptBodyTarget.Inflate(source.NPC.width / 2, source.NPC.height / 2);
            bool body = MeleeBladeCollision.SegmentIntersectsRect(_previousCenter, source.NPC.Center, 0f, sweptBodyTarget);
            bool blade = MeleeBladeCollision.SegmentIntersectsRect(source.StrikeHand, source.StrikeTip, 16f, targetHitbox);
            return body || blade;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            _hitPlayers[target.whoAmI] = true;
            BarrowWightBase source = Source;
            if (source != null)
            {
                source.OnHumanoidMeleeHit(target);
                source.ReportAttackHit();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
