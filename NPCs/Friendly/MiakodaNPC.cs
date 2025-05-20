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

// Increases the likelihood of every dialogue option offered after the first by 10% stacking, this way more relevant dialogue is usually said first.
public class WeightedDialogue
{
    public readonly List<Tuple<string, double>> elements;
    public readonly UnifiedRandom random;
    public bool needsRefresh = true;
    private double _totalWeight;
    private double _weight;
    private double _weightIncrease;

    public WeightedDialogue(double weight = 1, double weightIncrease = 0.1)
    {
        random = new UnifiedRandom();
        elements = new List<Tuple<string, double>>();
        _weight = weight;
        _weightIncrease = weightIncrease;
    }

    public void Add(string element, double weight = 0)
    {
        if (weight == 0)
        {
            weight = _weight;
        }

        elements.Add(new Tuple<string, double>(element, weight));
        needsRefresh = true;
        _weight += _weightIncrease;
    }

    public string Get()
    {
        if (needsRefresh)
            CalculateTotalWeight();

        double num = random.NextDouble();
        num *= _totalWeight;
        foreach (Tuple<string, double> element in elements)
        {
            if (num > element.Item2)
            {
                num -= element.Item2;
                continue;
            }

            return element.Item1;
        }

        return default(string);
    }

    public List<string> GetList()
    {
        List<string> list = new List<string>();

        foreach (Tuple<string, double> element in elements)
        {
            list.Add(element.Item1);
        }

        return list;
    }

    public static explicit operator List<string> (WeightedDialogue dialogue)
    {
        return dialogue.GetList();
    }

    public void CalculateTotalWeight()
    {
        _totalWeight = 0.0;
        foreach (Tuple<string, double> element in elements)
        {
            _totalWeight += element.Item2;
        }

        needsRefresh = false;
    }

    public void Clear()
    {
        elements.Clear();
    }

    public static implicit operator string(WeightedDialogue weightedRandom)
    {
        return weightedRandom.Get();
    }
}

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class MiakodaNPC : ModNPC
    {
        // Miakoda Full moon form maybe increase life regen as well as flat heal
        // Miakoda Cresent moon form maybe increase crit chance as well as flat dmg buff

        public Vector2 lastPlayerPos;
        public static Vector2 offSet = new Vector2(30.2f, -19f);
        public List<string> alreadySaid;

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

            lastPlayerPos = Main.player[Main.myPlayer].position;

            SetPos();
        }

        public string PickMessage(List<string> chat)
        {
            string message = "Error: Something went wrong when retrieving dialogue.";
            WeightedDialogue uniqueMsg = new WeightedDialogue();

            foreach (string msg in chat)
            {
                if (!alreadySaid.Contains(msg))
                {
                    uniqueMsg.Add(msg);
                }
            }

            if (uniqueMsg.GetList().Count > 0)
            {
                message = uniqueMsg;
                alreadySaid.Add(message);
            }
            else 
            {
                Player player = Main.player[Main.myPlayer];
                message = Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.ExhaustedDialogue", player.name);
                alreadySaid.Clear();
            }

            return message;
        }

        // The order is important here, the messages added later on in the function are weighted higher than the ones added first
        // Add them later on if you want them to be more likely to be immediately seen by the player upon talking with Miakoda
        public override string GetChat()
        {
            List<string> chat = new List<string>();
            Player player = Main.player[Main.myPlayer];
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            // TODO: Focus on adding lore behind biomes, Attraides, and bosses.
            // Also talk about hidden sub areas that may be difficult to find.

            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote1"));

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

            if (!BossDefeated(NPCID.CultistBoss) && Main.hardMode)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.LunaticCultist"));
            }

            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote2", player.name));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote3"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote4"));

            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.ProjectileHiddenPathsHint"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.BreakableBlocksHint"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.SolidWallsHiddenPathsHint"));

            if (tsorcRevampWorld.TalkedToAraz && tsorcRevampWorld.HardModeNotSHM)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.ArazSuspicion"));
            }

            if (!player.HasItemInAnyInventory(ItemID.ShadowKey) && Main.hardMode)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.ShadowKeyHint"));
            }

            // The hunter guards the gate to your wife
            if (!BossDefeated(ModContent.NPCType<TheHunter>()) && tsorcRevampWorld.HardModeNotSHM)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.HMWifeNotRescued", player.name));
            }
            // Foreshadow the consequences of killing Attraides
            else if (tsorcRevampWorld.HardModeNotSHM)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.WifeRescued"));
            }

            return PickMessage(chat);
        }

        public static bool BossDefeated(int npcType)
        {
            return tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(npcType));
        }

        public string GetHelp()
        {
            List<string> chat = new List<string>();
            Player player = Main.player[Main.myPlayer];
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            // Progressive, each if statement is mutually exclusive and tied to different areas of the story.
            // The player just freed Miakoda
            if (!BossDefeated(ModContent.NPCType<JungleWyvernHead>()))
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
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.WallofFleshBiomeHint"));

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
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.RageEscapeHell", player.name));

                if (!player.ZoneHallow)
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.RageBiomeHint"));
                }
                else
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.RageDeeperHint"));
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.SolidWallsHiddenPathsHint"));
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.BreakableBlocksHint"));
                }
            }
            // The player is on their way to the Frozen ocean, either through the secret path from the hallow or through the Wyvern Mage's fortress
            else if (!BossDefeated(ModContent.NPCType<TheSorrow>()))
            {
                if (player.ZoneHallow || player.ZoneSnow)
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.SolidWallsHiddenPathsHint"));
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.BreakableBlocksHint"));
                }

                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.ShadowKeyHint"));

                // Player is at frozen ocean
                if (player.position.X > 7510)
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.SorrowDeeperHint"));
                }
                // The player has not arrived at the frozen ocean yet nor defeated the wyvern mage
                else if (!BossDefeated(ModContent.NPCType<WyvernMage>()))
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.SorrowBiomeHint", player.name));
                }
                else 
                {
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.SorrowEastHint", player.name));
                }
            }
            // The player has to head over to the western side of the world to find The Hunter.
            else if (!BossDefeated(ModContent.NPCType<TheHunter>()))
            {

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
            button = Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Button1");
            button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Button2");
        }

        public override void OnChatButtonClicked(bool button, ref string shopName)
        {
            string message;

            if (button)
            {
                message = GetHelp();
            }
            else
            {
                message = GetChat();
            }

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