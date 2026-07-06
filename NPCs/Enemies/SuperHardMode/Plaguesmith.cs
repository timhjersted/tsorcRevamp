using Terraria.Audio;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    // Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
    public class Plaguesmith : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 9;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
        }
        public override void SetDefaults()
        {
            NPC.width = 50;
            NPC.height = 80;
            NPC.damage = 80;
            NPC.defense = 45;
            NPC.lifeMax = 8250;
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 20000;
            NPC.lavaImmune = true;
            NPC.npcSlots = 5;
            NPC.aiStyle = 0;
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (tsorcRevampWorld.SuperHardMode)
            {
                if (spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneOverworldHeight && Main.rand.NextBool(25))
                {
                    return 1;
                }
                else return 0;
            }
            else return 0;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.WitheredArmor, 30 * 60, false);

            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.Venom, 3 * 60, false);

            }
        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.6f, 0.05f, canPounce: false, canDodgeroll: false);

            Player player = Main.player[NPC.target];

            float radius = 650f;
            UsefulFunctions.DustRing(NPC.Center, radius, DustID.JungleTorch, 5, 2.1f);

            if (NPC.Distance(player.Center) < radius)
            {
                player.AddBuff(BuffID.BrokenArmor, 30, false);
                player.AddBuff(BuffID.Poisoned, 30, false);
            }

            // Buff enemies when in the aura
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];

                if (other.active &&
                    !other.friendly &&
                    other.whoAmI != NPC.whoAmI &&
                    Vector2.Distance(other.Center, NPC.Center) < radius)
                {
                    other.AddBuff(ModContent.BuffType<PlaguesmithBuff>(), 2);
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (NPC.velocity.Y != 0f)
            {
                NPC.frame.Y = 0;
                return;
            }

            if (NPC.velocity.X == 0)
            {
                NPC.frame.Y = frameHeight;
                return;
            }

            NPC.frameCounter += Math.Abs(NPC.velocity.X) * 0.5;
            if (NPC.frameCounter >= 7)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type])
                    NPC.frame.Y = frameHeight * 2;
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BlueTitanite>(), 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WhiteTitanite>(), 1, 1, 2));
        }
        public override void OnKill()
        {
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("PlaguesmithGore1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("PlaguesmithGore2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("PlaguesmithGore3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("PlaguesmithGore4").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("PlaguesmithGore5").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("PlaguesmithGore6").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("PlaguesmithGore6").Type, 1f);
                }
            }
        }
    }
}

