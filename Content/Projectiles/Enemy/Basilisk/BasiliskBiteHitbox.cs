using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies.Basilisk;

namespace tsorcRevamp.Content.Projectiles.Enemy.Basilisk
{
    // Only the already-rendered jaw and armor claws are visible. One projectile and hit ledger
    // combine all three contact paths, so overlapping hands cannot multiply a bite's damage.
    public class BasiliskBiteHitbox : ModProjectile
    {
        private readonly HashSet<int> hitPlayers = new HashSet<int>();
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 112;
            Projectile.height = 112;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs || !Main.npc[ownerIndex].active
                || Main.npc[ownerIndex].ModNPC is not BasiliskPuppet owner || owner.StrikeSequence != (int)Projectile.ai[1])
            {
                if (Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient)
                {
                    Projectile.Kill();
                }

                return;
            }

            if (!owner.StrikeRunning)
            {
                if (Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient)
                {
                    Projectile.Kill();
                }

                return;
            }

            Projectile.Center = owner.NPC.Center;
        }

        public override bool CanHitPlayer(Player target)
        {
            return !hitPlayers.Contains(target.whoAmI);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            NPC ownerNPC = Main.npc[(int)Projectile.ai[0]];
            if (ownerNPC.active && ownerNPC.ModNPC is BasiliskPuppet owner && owner.StrikeSequence == (int)Projectile.ai[1])
            {
                return owner.StrikeCollides(targetHitbox);
            }

            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            hitPlayers.Add(target.whoAmI);
            if (Main.npc[(int)Projectile.ai[0]].ModNPC is BasiliskPuppet owner)
            {
                owner.ApplyBiteEffects(target);
            }
        }
    }
}
