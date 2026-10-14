using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace tsorcRevamp.NPCs.FriendlyPuppets
{
    /// <summary>
    /// Lets enemies fight a friendly puppet (the Mimic Tear) instead of its owner.
    ///
    /// Every enemy AI in Terraria — vanilla, FighterAI, SmartFighter4, PuppetNPC — targets a PLAYER through
    /// <c>Main.player[npc.target]</c>. Rewriting all of them to target an NPC is impossible, so this uses the same
    /// idea as the weapon swap: for an enemy that has picked the puppet (the "decoy"), its AI runs with its target
    /// player temporarily standing where the puppet is. It walks toward, aims at and swings at the puppet. Anything
    /// that AI would do to that player is redirected: damage goes to the puppet (<see cref="FriendlyPuppetAggroPlayer"/>)
    /// and buffs are dropped. The real player is restored before anything else runs.
    ///
    /// Bosses can be drawn onto a puppet too, but only once they are 5 seconds old (BossAggroDelayTicks): their AIs
    /// set up arena bounds, intro positions and despawn anchors from the player at spawn. Later boss logic that
    /// reads the player (teleports, arena leashes, despawn-when-far) will read the PUPPET while decoyed — watch for it.
    /// Damage that never goes through the AI already reaches the puppet the vanilla way: hostile projectiles and
    /// contact hit friendly NPCs (Projectile.Damage / NPC.GetHurtByOtherNPCs).
    ///
    /// Authority: the server picks decoys (<see cref="FriendlyPuppetAggroSystem.PostUpdateNPCs"/>) and syncs them in
    /// the NPC's extra AI; every machine runs the swap around that NPC's AI, so client-side prediction agrees.
    /// </summary>
    public class FriendlyPuppetAggroGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        /// <summary>NPC index of the friendly puppet this enemy is fighting instead of its target player, or -1.</summary>
        public int DecoyPuppetIndex = -1;

        /// <summary>Ticks this NPC has existed. Bosses only become decoy-able after
        /// <see cref="FriendlyPuppetAggroSystem.BossAggroDelayTicks"/>, so their opening setup runs against the real player.</summary>
        public int TicksAlive;

        public override void PostAI(NPC npc)
        {
            TicksAlive++;
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bool hasDecoy = DecoyPuppetIndex >= 0;
            bitWriter.WriteBit(hasDecoy);

            if (hasDecoy)
            {
                binaryWriter.Write((short)DecoyPuppetIndex);
            }
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            bool hasDecoy = bitReader.ReadBit();
            DecoyPuppetIndex = -1;

            if (hasDecoy)
            {
                DecoyPuppetIndex = binaryReader.ReadInt16();
            }
        }
    }

    public class FriendlyPuppetAggroSystem : ModSystem
    {
        /// <summary>How often the server re-decides who each enemy is fighting.</summary>
        private const int AggroIntervalTicks = 30;

        /// <summary>Puppets further than this from an enemy (30 tiles) are never picked.</summary>
        private const float AggroRange = 30f * 16f;

        /// <summary>The puppet must be this much closer than the player (3 tiles) to steal attention, and the player
        /// this much closer to steal it back — hysteresis, so enemies don't flip targets every check. ESTIMATE.</summary>
        private const float AggroSwitchMargin = 3f * 16f;

        /// <summary>A boss can't be drawn onto a puppet for its first 5 seconds: bosses set up arenas, intro positions
        /// and despawn anchors from their target player when they spawn, and that must be the real player.</summary>
        internal const int BossAggroDelayTicks = 5 * 60;

        /// <summary>True only while a decoyed enemy's AI is running.</summary>
        internal static bool DecoyActive;
        internal static Player DecoyPlayer;
        internal static FriendlyPuppetNPC DecoyPuppet;

        private static readonly List<FriendlyPuppetNPC> EligiblePuppets = new List<FriendlyPuppetNPC>();

        public override void Load()
        {
            On_NPC.AI += RunAIWithDecoy;
            On_Player.AddBuff += BlockDecoyBuffs;
        }

        /// <summary>Server (and single player): every <see cref="AggroIntervalTicks"/>, each enemy (bosses only once 5 s old) picks either
        /// its target player or the nearest aggro-drawing puppet, whichever is meaningfully closer.</summary>
        public override void PostUpdateNPCs()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (Main.GameUpdateCount % AggroIntervalTicks != 0)
            {
                return;
            }

            EligiblePuppets.Clear();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC candidate = Main.npc[i];

                if (candidate.active && candidate.ModNPC is FriendlyPuppetNPC puppet && puppet.DrawsAggro && puppet.CanDrawAggroNow)
                {
                    EligiblePuppets.Add(puppet);
                }
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC enemy = Main.npc[i];

                if (!enemy.active)
                {
                    continue;
                }

                FriendlyPuppetAggroGlobalNPC aggro = enemy.GetGlobalNPC<FriendlyPuppetAggroGlobalNPC>();
                int newDecoy = -1;

                // Worm bodies and other segments follow their head; only the head decides.
                bool isFollowerSegment = enemy.realLife >= 0 && enemy.realLife != enemy.whoAmI;
                bool targetIndexValid = enemy.target >= 0 && enemy.target < Main.maxPlayers;
                bool bossStillSettingUp = enemy.boss && aggro.TicksAlive < BossAggroDelayTicks;
                bool canBeDecoyed = EligiblePuppets.Count > 0
                    && UsefulFunctions.IsHostileThreat(enemy)
                    && !bossStillSettingUp
                    && !isFollowerSegment
                    && targetIndexValid;

                if (canBeDecoyed)
                {
                    Player targetPlayer = Main.player[enemy.target];
                    float playerDistance = float.MaxValue;

                    if (targetPlayer.active && !targetPlayer.dead)
                    {
                        playerDistance = Vector2.Distance(enemy.Center, targetPlayer.Center);
                    }

                    FriendlyPuppetNPC nearestPuppet = null;
                    float nearestDistance = AggroRange;

                    foreach (FriendlyPuppetNPC puppet in EligiblePuppets)
                    {
                        float puppetDistance = Vector2.Distance(enemy.Center, puppet.NPC.Center);

                        if (puppetDistance < nearestDistance)
                        {
                            nearestDistance = puppetDistance;
                            nearestPuppet = puppet;
                        }
                    }

                    if (nearestPuppet != null)
                    {
                        // Sticky: once on the puppet, it stays until the player is a margin closer.
                        bool alreadyOnPuppet = aggro.DecoyPuppetIndex == nearestPuppet.NPC.whoAmI;
                        float margin = AggroSwitchMargin;

                        if (alreadyOnPuppet)
                        {
                            margin = -AggroSwitchMargin;
                        }

                        if (nearestDistance + margin < playerDistance)
                        {
                            newDecoy = nearestPuppet.NPC.whoAmI;
                        }
                    }
                }

                if (newDecoy != aggro.DecoyPuppetIndex)
                {
                    aggro.DecoyPuppetIndex = newDecoy;
                    enemy.netUpdate = true;
                }
            }
        }

        /// <summary>Runs a decoyed enemy's whole AI (PreAI + AI + PostAI) with its target player standing at the puppet.</summary>
        private static void RunAIWithDecoy(On_NPC.orig_AI orig, NPC npc)
        {
            int decoyIndex = npc.GetGlobalNPC<FriendlyPuppetAggroGlobalNPC>().DecoyPuppetIndex;
            bool decoyIndexValid = decoyIndex >= 0 && decoyIndex < Main.maxNPCs;
            bool targetIndexValid = npc.target >= 0 && npc.target < Main.maxPlayers;

            if (!decoyIndexValid || !targetIndexValid || DecoyActive || FriendlyPuppetSwap.Active)
            {
                orig(npc);
                return;
            }

            NPC decoyNpc = Main.npc[decoyIndex];
            FriendlyPuppetNPC puppet = null;

            if (decoyNpc.active)
            {
                puppet = decoyNpc.ModNPC as FriendlyPuppetNPC;
            }

            Player targetPlayer = Main.player[npc.target];
            bool usable = puppet != null && puppet.DrawsAggro && puppet.CanDrawAggroNow && targetPlayer.active && !targetPlayer.dead;

            if (!usable)
            {
                orig(npc);
                return;
            }

            Vector2 savedPosition = targetPlayer.position;
            Vector2 savedVelocity = targetPlayer.velocity;

            DecoyActive = true;
            DecoyPlayer = targetPlayer;
            DecoyPuppet = puppet;

            try
            {
                targetPlayer.position = decoyNpc.Bottom - new Vector2(targetPlayer.width * 0.5f, targetPlayer.height);
                targetPlayer.velocity = decoyNpc.velocity;
                orig(npc);
            }
            finally
            {
                targetPlayer.position = savedPosition;
                targetPlayer.velocity = savedVelocity;
                DecoyActive = false;
                DecoyPlayer = null;
                DecoyPuppet = null;
            }
        }

        /// <summary>An enemy fighting the puppet must not debuff the real player through its AI.</summary>
        private static void BlockDecoyBuffs(On_Player.orig_AddBuff orig, Player self, int type, int timeToAdd, bool quiet, bool foodHack)
        {
            if (DecoyActive && self == DecoyPlayer)
            {
                return;
            }

            orig(self, type, timeToAdd, quiet, foodHack);
        }
    }

    /// <summary>
    /// Damage a decoyed enemy's AI deals to its (relocated) target player lands on the puppet instead. Pre-defense
    /// damage is used so only the puppet's own defense applies. Rate-limited like the player's post-hit immunity,
    /// because the player never gets i-frames from a dodged hit.
    /// </summary>
    public class FriendlyPuppetAggroPlayer : ModPlayer
    {
        /// <summary>Matches the player's 40-tick post-hit immunity (attack-timing-design §2).</summary>
        private const int DecoyHitCooldownTicks = 40;

        public override bool FreeDodge(Player.HurtInfo info)
        {
            bool hitWhileDecoyed = FriendlyPuppetAggroSystem.DecoyActive && FriendlyPuppetAggroSystem.DecoyPlayer == Player;

            if (!hitWhileDecoyed)
            {
                return false;
            }

            FriendlyPuppetNPC puppet = FriendlyPuppetAggroSystem.DecoyPuppet;

            // dontTakeDamage covers the puppet's own i-frames (the Mimic Tear's dodge roll, its summon rise): the hit
            // is still swallowed, it just lands on nothing.
            if (puppet.DecoyHitCooldown <= 0 && !puppet.NPC.dontTakeDamage)
            {
                int damage = Math.Max(1, info.SourceDamage);
                puppet.NPC.SimpleStrikeNPC(damage, info.HitDirection);
                puppet.DecoyHitCooldown = DecoyHitCooldownTicks;
            }

            return true;
        }
    }
}
