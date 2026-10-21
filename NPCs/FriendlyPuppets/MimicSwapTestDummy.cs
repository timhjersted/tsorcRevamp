using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.FriendlyPuppets
{
    /// <summary>
    /// Step-0 go/no-go rig for the Mimic Tear swap (tsorcDocs/MimicTear_FriendlyPuppet_Design.md §11).
    /// A motionless reflective copy of the player that uses hotbar slot 1 or 2 through the owner's real
    /// ItemCheck. Spawned and steered with /mimictest. Not obtainable in normal play.
    ///
    /// ai[0] = owner, ai[1] = aim mode (0 nearest target, 1 owner's cursor), ai[2] = hotbar slot (0 or 1),
    /// ai[3] = melee clock (0 native, 1 eased — design §12.6, the hostile invader's swing prototype).
    /// </summary>
    public class MimicSwapTestDummy : FriendlyPuppetNPC
    {
        public const int AimModeNearestTarget = 0;
        public const int AimModeCursor = 1;

        /// <summary>Max distance (px) to a target in nearest-target mode: 50 tiles.</summary>
        private const float TargetRange = 50f * 16f;

        /// <summary>Hotbar slots 1 and 2, cloned on the owner's client the first tick it runs.</summary>
        private Item[] _loadout;

        /// <summary>A/B toggle for the eased melee clock (<c>/mimictest ease on|off</c>). Lives in ai[3] so vanilla
        /// syncs it from the server, where the command runs, to the owner client, where the clock runs.</summary>
        protected override bool UsesEasedMeleeClock => NPC.ai[3] != 0f;

        /// <summary>One log line per swing, eased or native, so the two can be compared against §12.6.</summary>
        protected override bool LogsMeleeClock => true;

        protected override Item ActiveWeapon => TestRigWeapon(ref _loadout, NPC, OwnerIndex);

        /// <summary>Both test rigs: snapshot the owner's hotbar slots 1+2 on first use, return the one ai[2] selects.</summary>
        internal static Item TestRigWeapon(ref Item[] loadout, NPC npc, int ownerIndex)
        {
            if (loadout == null)
            {
                Player owner = Main.player[ownerIndex];
                loadout = new Item[] { owner.inventory[0].Clone(), owner.inventory[1].Clone() };
            }

            int slot = (int)npc.ai[2];
            bool slotValid = slot >= 0 && slot < loadout.Length;

            if (!slotValid)
            {
                return null;
            }

            return loadout[slot];
        }

        public override void SetDefaults()
        {
            base.SetDefaults();

            // Test rig stands exactly where it was spawned.
            NPC.noGravity = true;
            NPC.noTileCollide = true;
        }

        protected override bool ChooseAttack(Player owner, out Vector2 aimWorld)
        {
            int aimMode = (int)NPC.ai[1];

            // Cursor mode: fire continuously at the owner's real cursor, for side-by-side comparison
            // with the player using the same weapon.
            if (aimMode == AimModeCursor)
            {
                aimWorld = Main.MouseWorld;
                return true;
            }

            NPC closestTarget = null;
            float closestDistance = TargetRange;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC candidate = Main.npc[i];

                if (!candidate.active || candidate.whoAmI == NPC.whoAmI)
                {
                    continue;
                }

                bool attackable = candidate.CanBeChasedBy() || candidate.type == NPCID.TargetDummy;

                if (!attackable)
                {
                    continue;
                }

                float distance = Vector2.Distance(NPC.Center, candidate.Center);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = candidate;
                }
            }

            if (closestTarget == null)
            {
                aimWorld = NPC.Center + new Vector2(NPC.direction * 100f, 0f);
                return false;
            }

            aimWorld = closestTarget.Center;
            return true;
        }
    }

    /// <summary>
    /// Step-3 rig for the hostile mirror damage path (design §12.3–12.4): a motionless, immortal copy of the owner that
    /// attacks the nearest living player (you, in single player) with hotbar slot 1 or 2 — eased melee strikes that
    /// hurt, flipped hostile projectiles, the per-hit cap and the death message — through the step-4 use wrapper
    /// (tells, aim cap + lock, bursts, recovery, ranged minimum distance; one [MirrorUse] log line per release and per
    /// burst). Not the invader: no movement, rolls or Estus (step 5). Debug: the last strike sweep is drawn in red and
    /// the wrapper phase is written above its head.
    /// Spawned with /mimictest hostile; 1/2/ease/clear work on it like on the friendly dummy.
    ///
    /// ai[0] = owner (the host that runs its swap), ai[2] = hotbar slot (0 or 1), ai[3] = melee clock (1 eased, default).
    /// </summary>
    public class MirrorInvaderTestDummy : HostileMirrorPuppetNPC
    {
        /// <summary>Max distance (px) to the player it attacks: 50 tiles.</summary>
        private const float TargetRange = 50f * 16f;

        private Item[] _loadout;

        protected override Item ActiveWeapon => MimicSwapTestDummy.TestRigWeapon(ref _loadout, NPC, OwnerIndex);

        protected override bool UsesEasedMeleeClock => NPC.ai[3] != 0f;

        protected override bool LogsMeleeClock => true;

        public override void SetDefaults()
        {
            base.SetDefaults();

            // Stands where it was spawned and can be hit (damage numbers) but never dies.
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.immortal = true;
            NPC.lifeMax = 10000;
            NPC.HitSound = SoundID.NPCHit4;
        }

        public override void AI()
        {
            base.AI();

            if (DespawnIfOwnerGone())
            {
                return;
            }

            // Face the nearest player on every machine. Between uses the swap takes its facing from NPC.direction.
            Player target = FindNearestLivingPlayer();

            if (target != null)
            {
                NPC.direction = target.Center.X < NPC.Center.X ? -1 : 1;
                NPC.spriteDirection = NPC.direction;
            }
        }

        protected override bool ChooseTarget(Player owner, out Vector2 targetCenter)
        {
            // Test-rig shortcut: the target is picked on the host's client (the invader will get it from the server).
            Player target = FindNearestLivingPlayer();

            if (target == null)
            {
                targetCenter = NPC.Center;
                return false;
            }

            targetCenter = target.Center;
            return true;
        }

        private Player FindNearestLivingPlayer()
        {
            Player nearest = null;
            float nearestDistance = TargetRange;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player candidate = Main.player[i];

                if (!candidate.active || candidate.dead)
                {
                    continue;
                }

                float distance = Vector2.Distance(NPC.Center, candidate.Center);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);

            // Wrapper phase above the head (screen coords), plus "too close" when a shot is refused for distance.
            string phaseLabel = CurrentUsePhase.ToString();

            if (TooCloseToFire)
            {
                phaseLabel += " (too close)";
            }

            Vector2 labelPosition = NPC.Top - screenPos + new Vector2(-30f, -28f);
            Utils.DrawBorderString(spriteBatch, phaseLabel, labelPosition, Color.OrangeRed, 0.8f);

            if (LastStrikeShowTicks <= 0)
            {
                return;
            }

            // The last strike sweep, so the live window can be judged by eye: the blade where the sweep began (dim red),
            // where it ended (bright red), and the arc its tip covered between them (orange). Utils.DrawLine takes
            // world coords. Everything inside that wedge is what TryStrikeLocalPlayer tested.
            Color startColor = Color.Red * 0.45f;
            Color endColor = Color.Red * 0.9f;
            Color tipPathColor = Color.Orange * 0.8f;
            Utils.DrawLine(spriteBatch, LastSweepStartHilt, LastSweepStartTip, startColor, startColor, 2f);
            Utils.DrawLine(spriteBatch, LastSweepEndHilt, LastSweepEndTip, endColor, endColor, 2f);
            Utils.DrawLine(spriteBatch, LastSweepStartTip, LastSweepEndTip, tipPathColor, tipPathColor, 2f);
        }
    }

    /// <summary>
    /// /mimictest — spawn and steer the swap test rig. World command: runs on the server in multiplayer,
    /// which owns NPC spawning; settings go through ai[] so they sync to the owner's client.
    /// </summary>
    public class MimicSwapTestCommand : ModCommand
    {
        public override CommandType Type => CommandType.World;

        public override string Command => "mimictest";

        public override string Usage => "/mimictest [spawn|hostile|clear|cursor|target|1|2|ease on|ease off]";

        public override string Description => "Mimic Tear swap test rig: spawn a reflective copy that uses hotbar slot 1/2 through your own item code.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            Player player = caller.Player;

            if (player == null)
            {
                caller.Reply("Must be run by a player.", Color.Orange);
                return;
            }

            string action = "spawn";

            if (args.Length > 0)
            {
                action = args[0].ToLowerInvariant();
            }

            // Second word of "ease on|off".
            string easeSetting = "";

            if (args.Length > 1)
            {
                easeSetting = args[1].ToLowerInvariant();
            }

            int dummyType = ModContent.NPCType<MimicSwapTestDummy>();

            if (action == "spawn")
            {
                // Three tiles in front of the player, feet level with theirs.
                int spawnX = (int)(player.Center.X + player.direction * 48f);
                int spawnY = (int)player.Bottom.Y;
                NPC.NewNPC(player.GetSource_FromThis(), spawnX, spawnY, dummyType, 0, player.whoAmI, MimicSwapTestDummy.AimModeNearestTarget, 0f);
                caller.Reply("Mimic test dummy spawned (slot 1, nearest-target mode). Hotbar 1+2 are snapshotted now.", Color.Lime);
                return;
            }

            int hostileDummyType = ModContent.NPCType<MirrorInvaderTestDummy>();

            if (action == "hostile")
            {
                // Five tiles in front of the player (out of first-swing reach), feet level with theirs; eased clock on.
                int spawnX = (int)(player.Center.X + player.direction * 80f);
                int spawnY = (int)player.Bottom.Y;
                NPC.NewNPC(player.GetSource_FromThis(), spawnX, spawnY, hostileDummyType, 0, player.whoAmI, 0f, 0f, 1f);
                caller.Reply("Hostile mirror test dummy spawned (slot 1, eased). It attacks the nearest player. Hotbar 1+2 are snapshotted now.", Color.OrangeRed);
                return;
            }

            int affected = 0;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                bool ownedPuppet = npc.active && npc.ModNPC is MirrorPuppetNPC && (int)npc.ai[0] == player.whoAmI;
                bool isDummy = npc.type == dummyType || npc.type == hostileDummyType;

                // "clear" removes every mirror puppet you own (both dummies and the Soul Mimic); the other settings
                // only apply to the dummies, whose ai[1]/ai[2] mean something different from the Soul Mimic's.
                if (!ownedPuppet || (!isDummy && action != "clear"))
                {
                    continue;
                }

                if (action == "clear")
                {
                    npc.active = false;
                }
                else if (action == "cursor")
                {
                    npc.ai[1] = MimicSwapTestDummy.AimModeCursor;
                }
                else if (action == "target")
                {
                    npc.ai[1] = MimicSwapTestDummy.AimModeNearestTarget;
                }
                else if (action == "1")
                {
                    npc.ai[2] = 0f;
                }
                else if (action == "2")
                {
                    npc.ai[2] = 1f;
                }
                else if (action == "ease" && easeSetting == "on")
                {
                    npc.ai[3] = 1f;
                }
                else if (action == "ease" && easeSetting == "off")
                {
                    npc.ai[3] = 0f;
                }
                else
                {
                    caller.Reply("Usage: " + Usage, Color.Orange);
                    return;
                }

                // Sync immediately: a deactivated NPC never reaches the netUpdate send in its own update.
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                }

                affected++;
            }

            string reply = $"/mimictest {action}: {affected} dummy(s) updated.";

            if (action == "ease")
            {
                reply += " Eased clock shapes swing/thrust melee only; one [MeleeClock] line per swing goes to client.log.";
            }

            caller.Reply(reply, Color.Lime);
        }
    }
}
