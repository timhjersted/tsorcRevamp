using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;

namespace tsorcRevamp.NPCs.Enemies
{
    public class DworcFleshhunter : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Poisoned,
                    BuffID.OnFire
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.HitSound = SoundID.NPCHit29;
            NPC.DeathSound = SoundID.NPCDeath29;
            NPC.damage = 30;
            NPC.lifeMax = 45;
            NPC.defense = 12;
            NPC.value = 220;
            NPC.width = 18;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.knockBackResist = 0.1f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DworcFleshhunterBanner>();

            AnimationType = NPCID.Skeleton;
        }

        //oh sweet jesus why do you drop so many potions - lol
        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.BattlePotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.WaterWalkingPotion, 30));
            npcLoot.Add(ItemDropRule.Common(ItemID.SwiftnessPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.SpelunkerPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.ShinePotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.RegenerationPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.CrimsonPotion>(), 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 20, 2, 3));

        }

        //Spawns in the Jungle, mostly Underground and in the Cavern.

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0f;
            if (spawnInfo.Water) return 0f;

            if (spawnInfo.Player.ZoneDungeon)
            {
                return 0f;
            }
            else if (!Main.hardMode && spawnInfo.Player.ZoneJungle && spawnInfo.Player.ZoneOverworldHeight)
            {
                return 0.365f;
            }
            else if (Main.dayTime && !Main.hardMode && spawnInfo.Player.ZoneJungle && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight))
            {
                return 0.37f;
            }
            else if (!Main.dayTime && !Main.hardMode && spawnInfo.Player.ZoneJungle && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight))
            {
                return 0.25f;
            }

            return chance;
        }

        #endregion

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 3.2f, 0.1f);
            tsorcRevampAIs.LeapAtPlayer(NPC, 2, 5, 0.01f, 64);
        }

        #region Gore
        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                }
            }
        }
        #endregion
    }
}
