using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Gwyn
{
    /// <summary>
    /// Presentation-only Lord's Embrace orb. It follows the captured player and is explicitly
    /// routed over players so the First-Flame grip cannot disappear behind its target.
    /// ai[0] = Gwyn NPC index; ai[1] = captured player index.
    /// </summary>
    class GwynEmbraceOrb : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        int OwnerIndex => (int)Projectile.ai[0];
        int TargetIndex => (int)Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 58;
            Projectile.height = 58;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (OwnerIndex < 0 || OwnerIndex >= Main.maxNPCs
                || TargetIndex < 0 || TargetIndex >= Main.maxPlayers)
            {
                Projectile.Kill();
                return;
            }

            NPC owner = Main.npc[OwnerIndex];
            Player target = Main.player[TargetIndex];
            if (!owner.active || owner.ModNPC is not NPCs.Bosses.SuperHardMode.Gwyn gwyn
                || !gwyn.LordEmbraceOrbActive || !target.active || target.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = target.Center;
            Projectile.rotation = (target.Center - gwyn.GraspHandPosition)
                .SafeNormalize(new Vector2(owner.direction, 0f)).ToRotation();
            Projectile.timeLeft = 2;
            Lighting.AddLight(Projectile.Center, 1.2f, 0.36f, 0.08f);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles,
            List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
            List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs
                && Main.npc[OwnerIndex].ModNPC is NPCs.Bosses.SuperHardMode.Gwyn gwyn)
            {
                gwyn.GetLordEmbraceOrbDraw(out float scale, out float opacity);
                GwynFlameGrasp.DrawHandAura(Projectile.Center, Projectile.rotation,
                    scale, opacity, drawUnblockableOutline: true);
            }
            return false;
        }
    }
}
