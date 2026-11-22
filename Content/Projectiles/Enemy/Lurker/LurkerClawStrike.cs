using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies;

namespace tsorcRevamp.Content.Projectiles.Enemy.Lurker
{
    // The Lurker's Ember Rake and Leap Swipe hit through this invisible box; the visible part is the
    // puppet's own arms and ember dust. One hit per player per strike, debuffs from the owning variant.
    public class LurkerClawStrike : ModProjectile
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
            Projectile.timeLeft = 240;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            LurkerPuppet owner = ResolveOwner();
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

        private LurkerPuppet ResolveOwner()
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs)
            {
                return null;
            }

            NPC ownerNPC = Main.npc[ownerIndex];
            if (!ownerNPC.active || ownerNPC.ModNPC is not LurkerPuppet owner || owner.StrikeSequence != (int)Projectile.ai[1])
            {
                return null;
            }

            return owner;
        }

        public override bool CanHitPlayer(Player target)
        {
            return !hitPlayers.Contains(target.whoAmI);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            LurkerPuppet owner = ResolveOwner();
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
            LurkerPuppet owner = ResolveOwner();
            if (owner != null)
            {
                owner.ApplyMeleeDebuffs(target);
            }
        }
    }
}
