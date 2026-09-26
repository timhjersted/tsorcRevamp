using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Melee.Flails;
using tsorcRevamp.NPCs.Enemies;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Great Black Knight's flail head — same ball+chain rig as the Invader's EnemyDiamondCrusherBall (see
    /// EnemyFlailProjectileBase), anchored to the knight's own rigged hand position via GreatBlackKnight
    /// implementing IFlailAnchor.
    /// <para/>
    /// Each throw rolls once (on spawn) whether it's "empowered" — a plain throw is just the chain+head hit, an
    /// empowered one also periodically pulses a wide curse AOE (a scaled-down version of the player Berserker's
    /// Nightmare flail's frost-pulse gimmick, themed to the knight's existing curse/broken-spirit debuff kit) and
    /// flicks a handful of blue embers off the tip on release that stick to whatever they land on and keep burning.
    /// </summary>
    public class GreatBlackKnightFlail : EnemyFlailProjectileBase
    {
        // Separate from the shared base's 0-9 modes: the same head owns this knight's
        // non-damaging orbit and its outward throw. No second projectile appears at release.
        public const float OrbitMode = 10f;
        private const float ReleasedMode = 11f;
        public const float OrbitRadius = 60f;
        private const int OrbitTicks = 40;
        private const int ThrowOutTicks = 26; // 60 + 26 * 16 = 476px from the hand.
        private const int ReleasedLifetime = 110;

        // The knight's orbit throw starts 60px from the hand, travels 26 ticks at 16px/tick,
        // and reaches 476px before reeling back. The inherited legacy throw keeps its 30-tick leg.
        protected override float OutwardTicks => 30f;
        protected override float ReturnSpeed => 15f;
        protected override int Lifetime => 190; // orbit plus a possible LOS pause; reset on release

        protected override string ChainTexturePath => UsefulFunctions.RefactorableFilepath(typeof(BerserkerNightmareBall)) + "_Chain";

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(BerserkerNightmareBall));

        private const int PulseInterval = 24; // ~0.4s between AOE pulses while the flail is out
        private const float EmpoweredChance = 0.5f;
        private const int EmberCount = 7;

        // ai[] is already spoken for (owner index, spin flag) — localAI[0] is the base class's own tick counter,
        // empowerment is instead a private bit synchronized through SendExtraAI/ReceiveExtraAI. The server rolls
        // it once in OnSpawn, keeping the gameplay and the client-side shader presentation in agreement.
        private bool empowered;
        private Vector2 visualMotion;
        private bool Empowered => empowered;

        public bool TryReleaseToward(Vector2 target, float speed)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || Projectile.ai[1] != OrbitMode
                || Projectile.localAI[0] < OrbitTicks - 1 || Owner is not NPC owner || !owner.active
                || owner.ModNPC is not GreatBlackKnight)
                return false;

            Vector2 hand = GetKnightHand(owner);
            Vector2 radial = (Projectile.Center - hand).SafeNormalize(new Vector2(owner.direction, 0f));
            Vector2 towardTarget = (target - hand).SafeNormalize(radial);
            if (Vector2.Dot(radial, towardTarget) < 0.996f) // within about half an orbit step
                return false;

            // Correct the final few pixels of the orbit so the visible chain and the launch
            // velocity share an exact bearing, even at the 480px edge of the attack's reach.
            Projectile.Center = hand + towardTarget * OrbitRadius;
            Projectile.ai[1] = ReleasedMode;
            Projectile.localAI[1] = 0f;
            Projectile.velocity = towardTarget * speed;
            Projectile.timeLeft = ReleasedLifetime;
            Projectile.netUpdate = true;
            return true;
        }

        private static Vector2 GetKnightHand(NPC owner)
            => ((GreatBlackKnight)owner.ModNPC).GetFlailAnchor();

        public override bool? CanDamage()
            => Projectile.ai[1] == OrbitMode ? false : base.CanDamage();

        public override void OnSpawn(IEntitySource source)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                empowered = Main.rand.NextFloat() < EmpoweredChance;
                Projectile.netUpdate = true;
            }
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(empowered);

        public override void ReceiveExtraAI(BinaryReader reader) => empowered = reader.ReadBoolean();

        public override void AI()
        {
            Vector2 previousCenter = Projectile.Center;
            if (Projectile.ai[1] == OrbitMode || Projectile.ai[1] == ReleasedMode)
            {
                NPC owner = Owner;
                if (owner == null || !owner.active || owner.ModNPC is not GreatBlackKnight knight
                    || (Main.netMode != NetmodeID.MultiplayerClient && Projectile.ai[1] == OrbitMode
                        && !knight.FlailWindupActive))
                {
                    Projectile.Kill();
                    return;
                }

                Vector2 hand = GetKnightHand(owner);
                if (Projectile.ai[1] == OrbitMode)
                {
                    Projectile.localAI[0]++;
                    float angle = Projectile.ai[2] + MathHelper.TwoPi * Projectile.localAI[0] / OrbitTicks;
                    Projectile.Center = hand + new Vector2(OrbitRadius, 0f).RotatedBy(angle);
                    Projectile.velocity = Vector2.Zero;
                }
                else
                {
                    int throwTick = (int)++Projectile.localAI[1];
                    if (throwTick == 1)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with
                            { Volume = 0.7f, PitchVariance = 0.2f }, Projectile.Center);
                        OnLaunch(owner, Projectile.velocity);
                    }

                    if (throwTick > ThrowOutTicks)
                    {
                        Vector2 toHand = hand - Projectile.Center;
                        if (toHand.Length() < 18f)
                        {
                            Projectile.Kill();
                            return;
                        }
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                            toHand.SafeNormalize(Vector2.Zero) * ReturnSpeed, 0.18f);
                    }
                    OnFlailTick(owner, hand);
                }

                Projectile.rotation += Projectile.velocity.X * 0.08f + owner.direction * 0.25f;
            }
            else
            {
                base.AI();
            }
            visualMotion = Projectile.Center - previousCenter;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            EnemyVFX.DrawGreatBlackKnightFlail(Projectile.Center, visualMotion, Empowered);
            return base.PreDraw(ref lightColor);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            NPC owner = Owner;
            bool awayFromHand = owner == null || !owner.active || owner.ModNPC is not GreatBlackKnight knight
                || Vector2.Distance(Projectile.Center, knight.GetFlailAnchor()) > 24f;
            if (!awayFromHand)
                return; // normal retract ends naturally in the knight's hand

            for (int i = 0; i < 20; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Wraith, 0f, 0f, 100, default, 0.8f);
                dust.noGravity = true;
            }
        }

        protected override void OnFlailTick(NPC owner, Vector2 hand)
        {
            if (!Empowered || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            int pulseTick = Projectile.ai[1] == ReleasedMode
                ? (int)Projectile.localAI[1] : (int)Projectile.localAI[0];
            if (pulseTick % PulseInterval != 0)
            {
                return;
            }

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<GreatBlackKnightFlailPulse>(),
                Projectile.damage / 3,
                0f,
                Main.myPlayer,
                Projectile.whoAmI);
        }

        protected override void OnLaunch(NPC owner, Vector2 launchVelocity)
        {
            if (!Empowered || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < EmberCount; i++)
            {
                Vector2 spread = Main.rand.NextVector2Circular(2.5f, 2.5f);
                Vector2 emberVelocity = launchVelocity * Main.rand.NextFloat(0.3f, 0.6f) + spread;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, emberVelocity, ModContent.ProjectileType<GreatBlackKnightFlailEmber>(), Projectile.damage / 2, 1f, Main.myPlayer);
            }
        }
    }
}
