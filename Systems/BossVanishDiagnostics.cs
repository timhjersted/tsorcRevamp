using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Bases;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Systems
{
    // Diagnostic for the "bosses vanish while holding Plasma Whirlwind / Nightbringer" report (single-player, a
    // player's laptop only). Every tick it remembers which boss NPCs were active; when one disappears without
    // OnKill having run, it writes the NPC's last state, its last few hits and every player's state to client.log.
    // NPCDespawnHandler also calls LogHandlerDespawn when IT decides to despawn, so the log names that path too.
    public class BossVanishDiagnostics : ModSystem
    {
        private struct TrackedNPC
        {
            public bool Tracked;
            public int Type;
            public string Name;
            public int Life;
            public int LifeMax;
            public int TimeLeft;
            public int Target;
            public Vector2 Center;
            public Vector2 Velocity;
        }

        private readonly TrackedNPC[] lastTick = new TrackedNPC[Main.maxNPCs];

        public override void OnWorldLoad()
        {
            Array.Clear(lastTick);
        }

        public override void PostUpdateNPCs()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                TrackedNPC previous = lastTick[i];

                // Gone = was a tracked boss last tick, and now the slot is inactive or holds a different type.
                bool goneThisTick = previous.Tracked && (!npc.active || npc.type != previous.Type);
                if (goneThisTick)
                {
                    BossVanishTracker tracker = npc.GetGlobalNPC<BossVanishTracker>();
                    if (!tracker.KilledNormally)
                    {
                        LogVanish(i, npc, previous, tracker);
                    }
                }

                lastTick[i] = Capture(npc);
            }
        }

        private static TrackedNPC Capture(NPC npc)
        {
            TrackedNPC snapshot = new TrackedNPC();
            if (!npc.active)
            {
                return snapshot;
            }

            // Boss bodies, plus segments/parts that share a boss's health pool (worms, The Machine's limbs).
            bool sharesBossLife = npc.realLife >= 0 && npc.realLife < Main.maxNPCs && Main.npc[npc.realLife].boss;
            snapshot.Tracked = npc.boss || sharesBossLife;
            snapshot.Type = npc.type;
            snapshot.Name = npc.FullName;
            snapshot.Life = npc.life;
            snapshot.LifeMax = npc.lifeMax;
            snapshot.TimeLeft = npc.timeLeft;
            snapshot.Target = npc.target;
            snapshot.Center = npc.Center;
            snapshot.Velocity = npc.velocity;
            return snapshot;
        }

        private void LogVanish(int index, NPC npc, TrackedNPC previous, BossVanishTracker tracker)
        {
            // Best guess at the path, from what vanilla/our code leaves behind when it removes an NPC.
            string likelyCause = "unknown (something set active=false directly)";
            if (npc.active && npc.type != previous.Type)
            {
                likelyCause = "slot now holds a different NPC type (overwritten / transformed)";
            }
            else if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().DespawnedByHandler)
            {
                likelyCause = "NPCDespawnHandler fade completed";
            }
            else if (npc.life <= 0)
            {
                likelyCause = "life <= 0 without a kill (vanilla UpdateNPC silent removal)";
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine($"[BossVanish] {previous.Name} (type {previous.Type}, slot {index}) vanished on tick {Main.GameUpdateCount}. Likely cause: {likelyCause}");
            report.AppendLine($"  netMode={Main.netMode} modVersion={Mod.Version}");
            report.AppendLine($"  last tick: life={previous.Life}/{previous.LifeMax} timeLeft={previous.TimeLeft} target={previous.Target} center={previous.Center} velocity={previous.Velocity}");
            report.AppendLine($"  now: active={npc.active} type={npc.type} life={npc.life} timeLeft={npc.timeLeft} center={npc.Center}");

            if (tracker.RecentHits == null)
            {
                report.AppendLine("  last hits: none recorded");
            }
            else
            {
                report.AppendLine($"  last hits ({tracker.RecentHits.Count}):");
                foreach (string hit in tracker.RecentHits)
                {
                    report.AppendLine("    " + hit);
                }
            }

            for (int p = 0; p < Main.maxPlayers; p++)
            {
                Player player = Main.player[p];
                if (!player.active)
                {
                    continue;
                }

                NPC dashTarget = player.GetModPlayer<RuneterraKatanaPlayer>().SweepingBladeTarget;
                string dashTargetText = "none";
                if (dashTarget != null)
                {
                    dashTargetText = dashTarget.whoAmI.ToString();
                }

                float distance = player.Center.Distance(previous.Center);
                report.AppendLine($"  player {p}: dead={player.dead} ghost={player.ghost} immune={player.immune} held={player.HeldItem.Name} (type {player.HeldItem.type}) center={player.Center} velocity={player.velocity} distToBoss={distance:0} dashTarget={dashTargetText}");
            }

            Mod.Logger.Warn(report.ToString());
        }

        /// <summary>Called by NPCDespawnHandler the tick it starts a despawn, with the reason and each tracked
        /// player's alive/active state, so a handler despawn is distinguishable from everything else.</summary>
        public static void LogHandlerDespawn(int npcID, string reason, int[] targetIDs, bool[] targetAlive, int targetCount)
        {
            NPC npc = Main.npc[npcID];
            StringBuilder report = new StringBuilder();
            report.AppendLine($"[BossVanish] NPCDespawnHandler started despawning {npc.FullName} (type {npc.type}, slot {npcID}) on tick {Main.GameUpdateCount}: {reason}");
            report.AppendLine($"  tracked players: {targetCount}");

            for (int i = 0; i < targetCount; i++)
            {
                Player player = Main.player[targetIDs[i]];
                report.AppendLine($"    player {targetIDs[i]}: trackedAlive={targetAlive[i]} active={player.active} dead={player.dead} center={player.Center} held={player.HeldItem.Name}");
            }

            ModContent.GetInstance<tsorcRevamp>().Logger.Warn(report.ToString());
        }
    }

    // Per-NPC companion to BossVanishDiagnostics: marks real kills and keeps the last few hits for the report.
    public class BossVanishTracker : GlobalNPC
    {
        private const int MaxRecentHits = 6;

        public override bool InstancePerEntity => true;

        public bool KilledNormally;
        // Created on first hit, not in a field initializer: per-entity globals can be shallow-cloned from a template,
        // which would make every NPC share one queue.
        public Queue<string> RecentHits;

        public override void OnKill(NPC npc)
        {
            KilledNormally = true;
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            RecordHit(npc, $"item {item.Name}", hit, damageDone);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            RecordHit(npc, $"projectile {projectile.Name} (type {projectile.type})", hit, damageDone);
        }

        private void RecordHit(NPC npc, string source, NPC.HitInfo hit, int damageDone)
        {
            // Bosses and boss-life-sharing parts only; ordinary enemies would just churn strings on every hit.
            bool sharesBossLife = npc.realLife >= 0 && npc.realLife < Main.maxNPCs && Main.npc[npc.realLife].boss;
            if (!npc.boss && !sharesBossLife)
            {
                return;
            }

            if (RecentHits == null)
            {
                RecentHits = new Queue<string>();
            }

            string entry = $"tick {Main.GameUpdateCount}: {source} damage={damageDone} crit={hit.Crit} lifeAfter={npc.life}/{npc.lifeMax}";
            RecentHits.Enqueue(entry);

            if (RecentHits.Count > MaxRecentHits)
            {
                RecentHits.Dequeue();
            }
        }
    }
}
