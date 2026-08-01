using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
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
        protected override string ChainTexturePath => "tsorcRevamp/Projectiles/Melee/Flails/BerserkerNightmareChain";

        public override string Texture => "tsorcRevamp/Projectiles/Melee/Flails/BerserkerNightmareBall";

        private const int PulseInterval = 24; // ~0.4s between AOE pulses while the flail is out
        private const float EmpoweredChance = 0.5f;
        private const int EmberCount = 7;

        // ai[] is already spoken for (owner index, spin flag) — localAI[0] is the base class's own tick counter,
        // empowerment is instead a private bit synchronized through SendExtraAI/ReceiveExtraAI. The server rolls
        // it once in OnSpawn, keeping the gameplay and the client-side shader presentation in agreement.
        private bool empowered;
        private Vector2 visualMotion;
        private bool Empowered => empowered;

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
            base.AI();
            visualMotion = Projectile.Center - previousCenter;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            EnemyVFX.DrawGreatBlackKnightFlail(Projectile.Center, visualMotion, Empowered);
            return base.PreDraw(ref lightColor);
        }

        protected override void OnFlailTick(NPC owner, Vector2 hand)
        {
            if (!Empowered || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if ((int)Projectile.localAI[0] % PulseInterval != 0)
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
