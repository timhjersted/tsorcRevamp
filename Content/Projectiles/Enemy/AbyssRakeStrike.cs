using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // The Abyss Elemental's Abyss Rake hits through this invisible box; the visible part is the puppet's claw
    // arm and shadow dust. Lives only while its owner's strike (by sequence) runs. One hit per player per strike.
    public class AbyssRakeStrike : ModProjectile
    {
        private readonly HashSet<int> hitPlayers = new HashSet<int>();

        // Dust-only hitbox: intentionally shares the empty texture.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            CorruptedElemental owner = ResolveOwner();
            if (owner == null || !owner.StrikeRunning)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.Kill();
                }

                return;
            }

            Projectile.Center = owner.NPC.Center;
        }

        private CorruptedElemental ResolveOwner()
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs)
            {
                return null;
            }

            NPC ownerNPC = Main.npc[ownerIndex];
            bool sameStrike = ownerNPC.active && ownerNPC.ModNPC is CorruptedElemental candidate && candidate.StrikeSequence == (int)Projectile.ai[1];
            if (!sameStrike)
            {
                return null;
            }

            return (CorruptedElemental)ownerNPC.ModNPC;
        }

        public override bool CanHitPlayer(Player target)
        {
            return !hitPlayers.Contains(target.whoAmI);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            CorruptedElemental owner = ResolveOwner();
            if (owner == null)
            {
                return false;
            }

            return owner.StrikeCollides(targetHitbox);
        }

        // Runs on the hit player's machine. Never Kill() here: the server owns the strike's lifetime.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            hitPlayers.Add(target.whoAmI);
            CorruptedElemental.ApplyAbyssDebuffs(target);
        }
    }
}
