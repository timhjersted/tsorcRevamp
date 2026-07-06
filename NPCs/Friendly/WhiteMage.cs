using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace tsorcRevamp.NPCs.Friendly
{
    // Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
    [AutoloadHead]
    public class WhiteMage : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("White Mage");
            Main.npcFrameCount[NPC.type] = 25;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 9;
            NPCID.Sets.AttackFrameCount[NPC.type] = 4;
            NPCID.Sets.DangerDetectRange[NPC.type] = 400;
            NPCID.Sets.AttackType[NPC.type] = 1;
            NPCID.Sets.AttackTime[NPC.type] = 10;
            NPCID.Sets.AttackAverageChance[NPC.type] = 5;
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = 7;
            NPC.damage = 5;
            NPC.defense = 10;
            NPC.lifeMax = 300;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.knockBackResist = 0.6f;
            AnimationType = NPCID.ArmsDealer;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            int num = NPC.life > 0 ? 1 : 10;
            for (int k = 0; k < num; k++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 5);
            }
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WhiteMageGore1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WhiteMageGore2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WhiteMageGore2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WhiteMageGore3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WhiteMageGore3").Type, 1f);
                }
            }
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            return false; // Disabled natural spawn for now
        }

        public override List<string> SetNPCNameList()
        {
            return new List<string> { "Elia", "Jenica", "KuKu", "Luca", "Mikoto", "Noah", "Ruby", "Sara", "Sarina", "Sherko", "Shiroma" };
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add("Think we could find a rat's tail around here?");
            chat.Add("It's strange; I find problems with swords, but not hammers.");
            chat.Add("Are you here to learn some spells?... or to sell random junk?");
            chat.Add("Welcome!");
            chat.Add("Yes, I'm a healer. That does not mean I'll heal you through your stupid mistakes.");
            chat.Add("Oh, Mysidia...");
            chat.Add("Studying on the ancient spell 'Holy'. It's rather an impressive spell!");
            return chat;
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            // Shop button disabled for now
            button = "";
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            // Shop disabled
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 100;
            knockback = 4f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 20;
            randExtraCooldown = 3;
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = ModContent.ProjectileType<Projectiles.Fireball2>();
            attackDelay = 2;
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = 12f;
            randomOffset = 2f;
        }
    }
}