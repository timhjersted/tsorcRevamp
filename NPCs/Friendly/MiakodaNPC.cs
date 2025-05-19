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

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class MiakodaNPC : ModNPC
    {
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
            NPC.townNPC = true;
            TownNPCStayingHomeless = true;
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

        public override string GetChat()
        {
            string help = GetHelp();

            if (!alreadySaid.Contains(help))
            {
                alreadySaid.Add(help);
            }

            return help;
        }

        public string GetChat2()
        {
            List<string> chat = new List<string>();
            Player player = Main.player[Main.myPlayer];
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote1"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote2", player.name));

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

            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote3"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote4"));

            if (!BossDefeated(NPCID.CultistBoss))
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.LunaticCultist"));
            }

            // Hardmode only chat from here on
            if (!Main.hardMode)
            {
                return chat[Main.rand.Next(chat.Count)];
            }

            // The hunter guards the gate to your wife
            if (!BossDefeated(ModContent.NPCType<TheHunter>()))
            {
                chat.Add(
                    Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.HMWifeNotRescued", player.name));
            }
            // Foreshadow the consequences of killing Attraides
            else if (!tsorcRevampWorld.SuperHardMode)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.WifeRescued"));
            }

            return chat[Main.rand.Next(chat.Count)];
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

            // The player just freed Miakoda
            if (!BossDefeated(ModContent.NPCType<JungleWyvernHead>()))
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.BrotherNotRescued") +
                    player.name +
                    Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.BrotherNotRescued2")
                    );

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

                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.WallofFleshBiomeHint"));

            }
            else if (!BossDefeated(ModContent.NPCType<TheRage>()))
            {

            }
            else if (!BossDefeated(ModContent.NPCType<WyvernMage>()))
            {
                
            }

            // If the help chat is empty
            if (chat.Count == 0)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.CannotHelp", player.name));
            }

            return chat[Main.rand.Next(chat.Count)];
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
                
                // Try saying something unique
                for (int i = 0; i < 25 && alreadySaid.Contains(message); i++)
                {
                    message = GetHelp();
                }
            }
            else
            {
                message = GetChat2();

                // Try saying something unique
                for (int i = 0; i < 25 && alreadySaid.Contains(message); i++)
                {
                    message = GetChat2();
                }
            }

            if (!alreadySaid.Contains(message))
            {
                alreadySaid.Add(message);
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