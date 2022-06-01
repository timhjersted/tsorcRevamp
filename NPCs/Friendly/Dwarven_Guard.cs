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
using Terraria.GameContent;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class DwarvenGuard : ModNPC
    {
        public static List<string> Names = new List<string> {
            "Urbur", "Bafarm", "Kothurn", "Okjorn", "Rulik", "Norbirn", "Joulni", "Norta", "Biffidor", "Koroin", "Uorin"
        };

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dwarven Guard");
            Main.npcFrameCount[NPC.type] = 25;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 9;
            NPCID.Sets.AttackFrameCount[NPC.type] = 4;
            NPCID.Sets.DangerDetectRange[NPC.type] = 60;
            NPCID.Sets.AttackType[NPC.type] = 3;
            NPCID.Sets.AttackTime[NPC.type] = 18;
            NPCID.Sets.AttackAverageChance[NPC.type] = 100;
            NPCID.Sets.HatOffsetY[NPC.type] = 4;
        }

        public override string TownNPCName()
        {
            string name = Names[Main.rand.Next(Names.Count)]; //pick a random name from the list
            return name;
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = 7;
            NPC.damage = 50;
            NPC.defense = 45;
            NPC.lifeMax = 300;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            AnimationType = NPCID.DyeTrader;
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
            item = (Texture2D)TextureAssets.Item[ModContent.ItemType<Items.Weapons.Melee.ForgottenAxe>()];
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