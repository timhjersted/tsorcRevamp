using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Weapons.Magic;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Localization;
using System.Collections.Generic;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class DwarvenGuard : ModNPC
    {
        public override bool Autoload(ref string name) => true;

        public static List<string> Names = new List<string> {
            "Urbur", "Bafarm", "Kothurn", "Okjorn", "Rulik", "Norbirn", "Joulni", "Norta", "Biffidor", "Koroin", "Uorin"
        };

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dwarven Guard");
            Main.npcFrameCount[npc.type] = 25;
            NPCID.Sets.ExtraFramesCount[npc.type] = 9;
            NPCID.Sets.AttackFrameCount[npc.type] = 4;
            NPCID.Sets.DangerDetectRange[npc.type] = 60;
            NPCID.Sets.AttackType[npc.type] = 3;
            NPCID.Sets.AttackTime[npc.type] = 18;
            NPCID.Sets.AttackAverageChance[npc.type] = 100;
            NPCID.Sets.HatOffsetY[npc.type] = 4;
        }

        public override string TownNPCName()
        {
            string name = Names[Main.rand.Next(Names.Count)]; //pick a random name from the list
            return name;
        }

        public override void SetDefaults()
        {
            npc.townNPC = true;
            npc.friendly = true;
            npc.width = 18;
            npc.height = 40;
            npc.aiStyle = 7;
            npc.damage = 50;
            npc.defense = 45;
            npc.lifeMax = 300;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.knockBackResist = 0.5f;
            animationType = NPCID.DyeTrader;
        }

        #region Chat
        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add("Here to serve.");
            chat.Add("I could use some ale...");
            chat.Add("Can't wait until the next break.");
            chat.Add("Hi' ho!");
            chat.Add("Nothing to report");
            return chat;
        }
        #endregion

        public override void FindFrame(int frameHeight)
        {
            base.FindFrame(frameHeight);


        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 55;
            knockback = 4f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 30;
            randExtraCooldown = 30;
        }

        public override void DrawTownAttackSwing(ref Texture2D item, ref int itemSize, ref float scale, ref Vector2 offset)
        {
            item = Main.itemTexture[ModContent.ItemType<Items.Weapons.Melee.ForgottenAxe>()];
            itemSize = 36;
        }

        public override void TownNPCAttackSwing(ref int itemWidth, ref int itemHeight)
        {
            itemWidth = 36;
            itemHeight = 30;
        }

        public override bool CanGoToStatue(bool toKingStatue)
        {
            return true;
        }
    }
}