using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Materials.Souls;

namespace tsorcRevamp.NPCs.Enemies.Basilisk
{
    public class BasiliskWalker : BasiliskPuppet
    {
        protected override FamilyKind Kind => FamilyKind.Walker;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.width = 24;
            NPC.lifeMax = 100;
            NPC.defense = 8;
            NPC.value = 500;
            NPC.npcSlots = 3;
            Banner = Type;
            BannerItem = ModContent.ItemType<Banners.BasiliskWalkerBanner>();

            if (Main.hardMode)
            {
                NPC.lifeMax = 250;
                NPC.defense = 20;
                NPC.value = 1250;
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;

            if (!BasiliskSpawnRules.MeetsSharedRequirements(spawnInfo, Type))
            {
                return 0f;
            }

            // Basilisks are restricted to evil biomes and the desert. Explicit exclusions keep
            // overlapping biome zones from introducing them into unrelated themed pools.
            if (player.ZoneDungeon || player.ZoneJungle || player.ZoneHallow || player.ZoneMeteor)
            {
                return 0f;
            }

            bool basiliskBiome = player.ZoneCorrupt || player.ZoneCrimson || player.ZoneDesert || player.ZoneUndergroundDesert;
            bool belowSurface = player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight;
            bool surfaceAtNight = player.ZoneOverworldHeight && !Main.dayTime;

            if (!basiliskBiome)
            {
                return 0f;
            }

            if (!Main.hardMode)
            {
                if (belowSurface)
                {
                    return 0.15f;
                }
                if (surfaceAtNight)
                {
                    return 0.15f;
                }
            }
            else if (!tsorcRevampWorld.SuperHardMode)
            {
                if (belowSurface)
                {
                    return 0.05f;
                }
                if (surfaceAtNight)
                {
                    return 0.05f;
                }
            }

            return 0f;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnFailedConditions(new CommonDrop(ItemID.HealingPotion, 15, 1, 1, 3));
            npcLoot.Add(hmCondition);
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BloodredMossClump>(), 3));
            npcLoot.Add(new CommonDrop(ItemID.ManaRegenerationPotion, 25, 1, 1, 3));
        }

        public override void OnKill()
        {
            base.OnKill();
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart);
                }
            }
        }
    }
}
