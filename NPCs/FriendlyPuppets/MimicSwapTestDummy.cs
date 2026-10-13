using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.FriendlyPuppets
{
    /// <summary>
    /// Step-0 go/no-go rig for the Mimic Tear swap (tsorcDocs/MimicTear_FriendlyPuppet_Design.md §11).
    /// A motionless reflective copy of the player that uses hotbar slot 1 or 2 through the owner's real
    /// ItemCheck. Spawned and steered with /mimictest. Not obtainable in normal play.
    ///
    /// ai[0] = owner, ai[1] = aim mode (0 nearest target, 1 owner's cursor), ai[2] = hotbar slot (0 or 1).
    /// </summary>
    public class MimicSwapTestDummy : FriendlyPuppetNPC
    {
        public const int AimModeNearestTarget = 0;
        public const int AimModeCursor = 1;

        /// <summary>Max distance (px) to a target in nearest-target mode: 50 tiles.</summary>
        private const float TargetRange = 50f * 16f;

        /// <summary>Hotbar slots 1 and 2, cloned on the owner's client the first tick it runs.</summary>
        private Item[] _loadout;

        protected override Item ActiveWeapon
        {
            get
            {
                if (_loadout == null)
                {
                    Player owner = Main.player[OwnerIndex];
                    _loadout = new Item[] { owner.inventory[0].Clone(), owner.inventory[1].Clone() };
                }

                int slot = (int)NPC.ai[2];
                bool slotValid = slot >= 0 && slot < _loadout.Length;

                if (!slotValid)
                {
                    return null;
                }

                return _loadout[slot];
            }
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
    /// /mimictest — spawn and steer the swap test rig. World command: runs on the server in multiplayer,
    /// which owns NPC spawning; settings go through ai[] so they sync to the owner's client.
    /// </summary>
    public class MimicSwapTestCommand : ModCommand
    {
        public override CommandType Type => CommandType.World;

        public override string Command => "mimictest";

        public override string Usage => "/mimictest [spawn|clear|cursor|target|1|2]";

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

            int affected = 0;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                bool ownedPuppet = npc.active && npc.ModNPC is FriendlyPuppetNPC && (int)npc.ai[0] == player.whoAmI;
                bool isDummy = npc.type == dummyType;

                // "clear" removes every friendly puppet you own (dummy and Mimic Tear); the other settings only
                // apply to the dummy, whose ai[1]/ai[2] mean something different from the Mimic Tear's.
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

            caller.Reply($"/mimictest {action}: {affected} dummy(s) updated.", Color.Lime);
        }
    }
}
