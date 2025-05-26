using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;
using tsorcRevamp.NPCs.Bosses;
using tsorcRevamp.NPCs.Bosses.JungleWyvern;
using tsorcRevamp.NPCs.Bosses.WyvernMage;
using tsorcRevamp.Projectiles.Pets;
using System.Collections;
using rail;
using Steamworks;
using tsorcRevamp.NPCs.Bosses.Serris;
using tsorcRevamp.NPCs.Bosses.PrimeV2;

namespace tsorcRevamp.NPCs.Friendly
{
    // Increases the likelihood of every dialogue option offered after the first by 10% stacking, this way more relevant dialogue is usually said first.
    public class AutoWeightedDialogue
    {
        public List<Tuple<List<string>, double>> elements;
        public readonly UnifiedRandom random;
        public bool needsRefresh = true;
        private double _totalWeight;
        private double _weight;
        private double _weightIncrease;

        public AutoWeightedDialogue(double weight = 1, double weightIncrease = 0.1)
        {
            random = new UnifiedRandom();
            elements = new List<Tuple<List<string>, double>>();
            _weight = weight;
            _weightIncrease = weightIncrease;
        }

        public int Count { get { return elements.Count; } }

        public AutoWeightedDialogue Add(string element, double weight = 0)
        {
            List<string> strList = new List<string>();
            strList.Add(element);
            Add(strList, weight);
            return this;
        }

        public AutoWeightedDialogue Remove(int index)
        {
            elements.RemoveAt(index);
            return this;
        }

        public AutoWeightedDialogue Add(List<string> element, double weight = 0)
        {
            if (weight == 0)
            {
                weight = _weight;
            }

            elements.Add(new Tuple<List<string>, double>(element, weight));
            needsRefresh = true;
            _weight += _weightIncrease;

            return this;
        }


        public string Get()
        {
            List<string> element = GetDialogue();
            string message = "";

            if (element.Count > 0)
                message = element[0];

            return message;
        }

        // Returns a random dialogue option from the options provided
        public List<string> GetDialogue()
        {
            if (needsRefresh)
                CalculateTotalWeight();

            double num = random.NextDouble();
            num *= _totalWeight;
            foreach (Tuple<List<string>, double> element in elements)
            {
                if (num > element.Item2)
                {
                    num -= element.Item2;
                    continue;
                }

                return element.Item1;
            }

            return new List<string>();
        }

        public void CalculateTotalWeight()
        {
            _totalWeight = 0.0;
            foreach (Tuple<List<string>, double> element in elements)
            {
                _totalWeight += element.Item2;
            }

            needsRefresh = false;
        }

        public AutoWeightedDialogue Clear()
        {
            elements.Clear();
            return this;
        }

        public static implicit operator string(AutoWeightedDialogue weightedRandom)
        {
            return weightedRandom.Get();
        }

        public static implicit operator List<string>(AutoWeightedDialogue weightedRandom)
        {
            return weightedRandom.GetDialogue();
        }
    }

    // Resets dialogue when acessed. This is meant for multipart messages that need to be added as a single list<string>
    class DialogueHandler
    {
        List<string> _dialogueInternal = null;
        List<string> _dialogue
        {
            get
            {
                List<string> dialogue = _dialogueInternal;
                _dialogueInternal = null;

                if (dialogue == null)
                    dialogue = new List<string>();

                return dialogue;
            }
        }

        public DialogueHandler() { }

        public static implicit operator List<string>(DialogueHandler dialogue)
        {
            return dialogue._dialogue;
        }

        public DialogueHandler Add(string message)
        {
            if (_dialogueInternal == null)
                _dialogueInternal = new List<string>();

            _dialogueInternal.Add(message);

            return this;
        }
    }

    [AutoloadHead]
    class MiakodaNPC : ModNPC
    {
        // Miakoda Full moon form maybe increase life regen as well as flat heal
        // Miakoda Cresent moon form maybe increase crit chance as well as flat dmg buff

        public Vector2 lastPlayerPos;
        public static Vector2 offSet = new Vector2(30.2f, -19f);
        public List<string> alreadySaid;
        public List<string> nextMessage;

        // For use when adding a multipart message to an AutoWeightedDialogue
        public DialogueHandler dialogue;
        public bool helpButtonPressed;
        // Don't allow multi-message dialogue for the first chat message or after exit is pressed.
        public bool longDialogueAllowed;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.AttackType[NPC.type] = -1; // Miakoda shouldn't attack
            NPCID.Sets.NoTownNPCHappiness[Type] = true;
        }

        public override bool UsesPartyHat()
        {
            return false;
        }

        public static List<string> Names = new List<string> { "Miakoda" };
        public override List<string> SetNPCNameList()
        {
            return Names;
        }

        public override void SetDefaults()
        {
            NPC.friendly = true;
            // These two variables cause Miakoda to show up on the map when I run the despawn code
            // They don't affect anything related to her function so I've commented them out
            // NPC.townNPC = true;
            // TownNPCStayingHomeless = true;
            NPC.width = 18;
            NPC.height = 16;
            NPC.damage = 0;
            NPC.defense = 1000;
            NPC.noGravity = true;
            NPC.lifeMax = 100;
            NPC.lavaImmune = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.scale = 0.7f;
            alreadySaid = new List<string>();
            nextMessage = new List<string>();
            dialogue = new DialogueHandler();
            helpButtonPressed = false;
            longDialogueAllowed = false;

            lastPlayerPos = Main.player[Main.myPlayer].position;

            SetPos();
        }

        // Picks a message from the dialogue given based on whether or not it was already said
        // Also properly handles when a message needs to be sent in parts by adding the additional messages to nextMessage.
        public string PickMessage(AutoWeightedDialogue chat)
        {
            List<string> message;
            AutoWeightedDialogue uniqueMsg = new AutoWeightedDialogue();

            foreach (Tuple<List<string>, double> msg in chat.elements)
            {
                if (!alreadySaid.Contains(msg.Item1[0]))
                {
                    if (longDialogueAllowed || msg.Item1.Count == 1)
                    {
                        uniqueMsg.Add(msg.Item1);
                    }
                }
            }

            if (uniqueMsg.Count > 0)
            {
                message = uniqueMsg;
                alreadySaid.Add(message[0]);

                // Adds extended dialogue to the list
                for (int i = 1; i < message.Count; i++)
                {
                    nextMessage.Add(message[i]);
                }
            }
            // No unique messages, restart logic
            else 
            {
                Player player = Main.player[Main.myPlayer];
                message = new List<string>();
                message.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.ExhaustedDialogue", player.name));
                alreadySaid.Clear();
            }

            return message[0];
        }

        // The order is important here, the messages added later on in the function are weighted higher than the ones added first
        // Add them later on if you want them to be more likely to be immediately seen by the player upon talking with Miakoda
        public override string GetChat()
        {
            AutoWeightedDialogue chat = new AutoWeightedDialogue();
            Player player = Main.player[Main.myPlayer];
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            // TODO: Focus on adding lore behind biomes, Attraides, and bosses.
            // Also talk about hidden sub areas that may be difficult to find.

            // Least likely quote, have Mikoda complain about the lunatic cultist
            // Might remove or change later.
            if (!BossDefeated(NPCID.CultistBoss))
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.LunaticCultist"));
            }

            // Form change explainations
            if (modPlayer.MiakodaCrescent)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.CrescentMoonForm"));
            }
            else if (modPlayer.MiakodaFull)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.FullMoonForm"));
            }
            else if (modPlayer.MiakodaNew)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.NewMoonForm"));
            }

            // Miakoda introduction as the guide for the mod
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote1"));

            // Dark soul curse lore
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote4"));
            // Form change hint
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote3"));
            // Dark souls item crafting emphasis
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote2", player.name));

            // Projectiles can open hidden paths reminder
            dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.ProjectileHiddenPathsHintPart1"));
            dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.ProjectileHiddenPathsHintPart2"));
            chat.Add(dialogue);

            // Explain conditions to break blocks
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.BreakableBlocksHint"));
            // Explain certain secret passages
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.SolidWallsHiddenPathsHint"));

            // Musings from Miakoda relevant to before the wife is saved
            if (!BossDefeated(ModContent.NPCType<TheHunter>()) && Main.hardMode)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.HMWifeNotRescued", player.name));
            }
            else if (tsorcRevampWorld.HardModeNotSHM)
            {
                // Foreshadow the consequences of killing Attraides
                dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.WifeRescuedPart1"));
                dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.WifeRescuedPart2"));
                chat.Add(dialogue);
            }

            // Lore of the crests, foreshadowing
            if (tsorcRevampWorld.TalkedToAraz && tsorcRevampWorld.HardModeNotSHM)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.ArazSuspicion"));
            }

            // Introducing the awakened bosses from hardmode
            if (tsorcRevampWorld.HardModeNotSHM)
            {
                dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.HardmodeLorePart1"));
                dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.HardmodeLorePart2"));
                chat.Add(dialogue);
            }

            // Lore relevant to the first three hardmode bosses - the Wings of the Awakened
            if (!BossDefeated(ModContent.NPCType<TheHunter>()) && Main.hardMode)
            {
                dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.3WingsLorePart1"));
                dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.3WingsLorePart2"));
                chat.Add(dialogue);
            }
            // Lore relevant to next two hard mode bosses along with the final one - The Machines
            else if (tsorcRevampWorld.HardModeNotSHM)
            {
                dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.3MachinesLorePart1"));
                dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.3MachinesLorePart2"));
                dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.3MachinesLorePart3"));
                dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.3MachinesLorePart4"));
                chat.Add(dialogue);
            }

            // Player doesn't have a shadow key but can get one
            if (!player.HasItemInAnyInventory(ItemID.ShadowKey) && BossDefeated(ModContent.NPCType<TheRage>()))
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.ShadowKeyHint"));
            }

            return PickMessage(chat);
        }

        public static bool BossDefeated(int npcType)
        {
            return tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(npcType));
        }

        // The order is important here, the messages added later on in the function are weighted higher than the ones added first
        // Add them later on if you want them to be more likely to be immediately seen by the player upon talking with Miakoda
        public string GetHelp()
        {
            AutoWeightedDialogue chat = new AutoWeightedDialogue();
            Player player = Main.player[Main.myPlayer];
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            // Progressive, each if statement is mutually exclusive and tied to different areas of the story.
            // The player just freed Miakoda
            if (!BossDefeated(ModContent.NPCType<JungleWyvernHead>()) && !tsorcRevampWorld.EnteredHell)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.BrotherNotRescued", player.name));
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.JungleWyvern"));

                // If the player isn't in The Forgotten City
                if (!player.ZoneDungeon)
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.JungleWyvernBiomeHint"));
                }
            }
            // The player recently spoke with their brother, Elijah, and should be on their way to The Wall of Flesh.
            else if (!Main.hardMode && !tsorcRevampWorld.TheEnd)
            {
                dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.WallofFleshBiomeHintPart1"));
                dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.WallofFleshBiomeHintPart2"));
                chat.Add(dialogue);

                // The player is currently in hell.
                if (player.ZoneUnderworldHeight)
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.WallofFleshHellLeversHint"));
                }
                // The player is above hell, looking for switches.
                else if ((player.ZoneJungle || player.ZoneDungeon) && tsorcRevampWorld.EnteredHell)
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.WallofFleshJungleLeverHint"));
                }
            }
            // The player recently killed the Wall of Flesh and activated HardMode
            else if (!BossDefeated(ModContent.NPCType<TheRage>()))
            {
                // The player is speaking to Miakoda while in hell directly after the wall of flesh
                if (player.ZoneUnderworldHeight && !BossDefeated(NPCID.QueenSlimeBoss))
                {
                    // Tell them about the intended progression escape through the jungle
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.RageEscapeHell", player.name));
                }

                // Generic hint about the hallow if the player isn't within it
                if (!player.ZoneHallow)
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.RageBiomeHint"));
                }
                else
                {
                    // Hidden paths present in the hallow lead to underneath the frozen ocean 
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.SolidWallsHiddenPathsHint"));
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.BreakableBlocksHint"));

                    // The player is within the hallow, hint the direction to go
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.RageDeeperHint"));
                }
            }
            // The player is on their way to the Frozen ocean, either through the secret path from the hallow or through the Wyvern Mage's fortress
            else if (!BossDefeated(ModContent.NPCType<TheSorrow>()))
            {
                // Hidden paths present in the hallow lead to underneath the frozen ocean
                if (player.ZoneHallow || player.ZoneSnow)
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.SolidWallsHiddenPathsHint"));
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.BreakableBlocksHint"));
                }

                // Tell the player about shadow key locations if they don't have one.
                if (!player.HasItemInAnyInventory(ItemID.ShadowKey))
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.ShadowKeyHint"));

                // Player is at frozen ocean
                if ((int)player.position.X / 16 > 7510)
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.SorrowDeeperHint"));
                }
                // The player has not arrived at the frozen ocean yet nor defeated the wyvern mage
                else if (!BossDefeated(ModContent.NPCType<WyvernMage>()))
                {
                    dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.SorrowBiomeHintPart1"));
                    dialogue.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.SorrowBiomeHintPart2", player.name));
                    chat.Add(dialogue);
                }
                else // The player defeated the wyvern mage, only the sorrow is left
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.SorrowEastHint", player.name));
                }
            }
            // The player has to head over to the western side of the world to find The Hunter.
            else if (!BossDefeated(ModContent.NPCType<TheHunter>()))
            {
                // Player is at the Desert
                if ((int)player.position.X / 16 < 2700)
                {
                    // If the player has found the ruins that lead to the hunter
                    if (tsorcRevampWorld.EnteredRuinsOfElengad)
                    {
                        // If the player cannot walk on lava
                        if (!((player.waterWalk && player.lavaImmune) || player.waterWalk2))
                        {
                            // Hint at exploring further west
                            // Tell them about the water walking boots
                            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.FireWalkHint", player.name));
                        }
                        else if (!(player.ZoneJungle || player.ZoneGlowshroom))
                        {
                            // The player is ready to traverse the lava spike area to fight the hunter
                            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.HunterElengadRuinReady", player.name));
                        }
                        else
                        {
                            // The player is very close to the hunter's location
                            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.HunterClose", player.name));
                        }
                    }
                    // The player has yet to find The Ruins of Elengad
                    else
                    {
                        // Sand blocks need to be mined to enter the ruins
                        chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.BreakableBlocksHint"));

                        // Tell the player about the ruins
                        chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.HunterElengadRuinHint"));
                    }
                }
                else // The player is not within the western desert
                {
                    // Tell them to go past the corruption to the desert
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.HunterBiomeHint"));
                }
                // The player has found a secret underground area, but not the ruins leading to the hunter
                if (tsorcRevampWorld.EnteredRuinsOfSerris)
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.TempleOfSerris", player.name));
                }
            }
            // The Destroyer is next, onto the Shadow Temple.
            else if (!BossDefeated(NPCID.TheDestroyer))
            {
                // The player needs to find the shadow temple entrance.
                if (!tsorcRevampWorld.EnteredShadowTemple)
                {
                    // Tell the player about the sandy cave entrance.
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.DestroyerFromHunter"));

                    // The player defeated the optional boss, the entrance to the Shadow temple is closer
                    if (BossDefeated(ModContent.NPCType<SerrisX>()))
                    {
                        // Tell the player about the underwater entrance
                        chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.DestroyerFromSerris"));
                    }
                }
                // Shadow temple progression hints
                else
                {
                    // Sand block paths serve as a shortcut through the entire dungeon
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.BreakableBlocksHint"));

                    // A reminder about puzzle solving for the labrynth
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.DestroyerLabrynthGates"));

                    // Hidden shortcuts hint
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.DestroyerLabrynthSecretPaths"));

                    // Shadow Labrynth Layout
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.DestroyerLabrynthLayout", player.name));
                }
            }
            // Flooded Machine Temple
            else if (!BossDefeated(ModContent.NPCType<TheMachine>()))
            {
                // The player has not found the Flooded Machine Temple
                if (!tsorcRevampWorld.EnteredFloodedMachineTemple)
                {
                    // Hint at the location
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.MachineTempleLocationHint", player.name));
                }
                else // The player has found the Flooded Machine Temple
                {

                }
            }

            // If the help chat is empty
            if (chat.Count == 0)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.CannotHelp", player.name));
            }

            return PickMessage(chat);
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            if (nextMessage.Count > 0)
            {
                // Switch the continue button to be the button the player originally pressed
                if (helpButtonPressed)
                {
                    button = Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.NextButtonContinue");
                    button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.NextButtonExit");
                }
                else
                {
                    button = Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.NextButtonExit");
                    button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.NextButtonContinue");
                }
            }
            else // Default buttons
            {
                button = Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.ButtonHelp");
                button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.ButtonChat");
            }
        }

        public override void OnChatButtonClicked(bool button, ref string shopName)
        {
            string message;

            longDialogueAllowed = true;

            // There is dialogue in the queue
            if (nextMessage.Count > 0)
            {
                // Exit dialogue pressed
                if (button && !helpButtonPressed)
                {
                    nextMessage.Clear();
                    longDialogueAllowed = false;
                    message = GetChat();
                }
                // Continue pressed
                else
                {
                    message = nextMessage[0];
                    nextMessage.RemoveAt(0);
                }
            }
            // Help pressed
            else if (button)
            {
                helpButtonPressed = true;
                message = GetHelp();
            }
            // Chat pressed
            else
            {
                helpButtonPressed = false;
                message = GetChat();
            }

            // Display the message 
            Main.npcChatText = message;
        }
        public override bool CanChat()
        {
            return true;
        }

        public void SetPos()
        {
            Player player = Main.player[Main.myPlayer];
            Vector2 targetLocation = new Vector2(player.Center.X + offSet.X, player.Center.Y + offSet.Y);
            NPC.position = targetLocation;
        }

        public override void PostAI()
        {
            Player player = Main.player[Main.myPlayer];
            NPC.timeLeft = 2;
            //UsefulFunctions.BroadcastText("X: " + NPC.position.X.ToString("0.00") + " Y: " + NPC.position.Y.ToString("0.00"));

            SetPos();

            // Despawn Miakoda
            if (lastPlayerPos != player.position)
            {
                NPC.timeLeft = 0;
                NPC.position.X += 10000;
            }
        }
        
        // Make Miakoda a ghost, should not interact with other NPC's
        // Also disable most vanilla npc behaviour
        #region Ghost

        public override void HitEffect(NPC.HitInfo hit)
        {
            return; // Nothing, Miakoda should not take damage
        }
        public override bool? CanBeHitByItem(Player player, Item item)
        {
            return false;
        }
        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            return false;
        }
        public override bool CanHitNPC(NPC target)
        {
            return false;
        }
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return false;
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }
        public override bool CanBeHitByNPC(NPC attacker)
        {
            return false;
        }
        #endregion
    }
}