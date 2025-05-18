using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;
using tsorcRevamp.Projectiles.Pets;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class MiakodaNPC : ModNPC
    {
        public Vector2 lastPlayerPos;
        public static Vector2 offSet = new Vector2(30.2f, -19f);
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.AttackType[NPC.type] = -1; // Miakoda shouldn't attack
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
            //NPC.townNPC = true;
            NPC.width = 18;
            NPC.height = 16;
            NPC.damage = 0;
            NPC.defense = 1000;
            NPC.noGravity = true;
            NPC.lifeMax = 10000;
            NPC.lavaImmune = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.scale = 0.7f;

            lastPlayerPos = Main.player[Main.myPlayer].position;

            SetPos();
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote1"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote2Part1") + Main.LocalPlayer.name + Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote2Part2"));
            /*
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote3"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote4"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote5"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote6"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote7"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Quote8"));
            */
            return chat;
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Button1");
            button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.MiakodaNPC.Button2");
        }

        public override void OnChatButtonClicked(bool button, ref string shopName)
        {
            if (button)
            {
                Main.npcChatText = GetChat();
            }
            else
            {
                Main.npcChatText = GetChat(); // Placeholder
                // TODO: Logic for Miakoda hints
            }
        }
        public override bool CanChat()
        {
            return true;
        }

        // Unfortunately Proj Array positions are not guarunteed to remain the same so this loop has to run every time.
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