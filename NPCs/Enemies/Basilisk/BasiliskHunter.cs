using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Materials.Souls;

namespace tsorcRevamp.NPCs.Enemies.Basilisk
{
    public class BasiliskHunter : BasiliskPuppet
    {
        protected override FamilyKind Kind => FamilyKind.Hunter;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.width = 38;
            NPC.lifeMax = 2500;
            NPC.defense = 80;
            NPC.value = 10000;
            NPC.npcSlots = 5;
            Banner = Type;
            BannerItem = ModContent.ItemType<Banners.BasiliskHunterBanner>();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;

            if (!BasiliskSpawnRules.MeetsSharedRequirements(spawnInfo, Type))
            {
                return 0f;
            }
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

            if (tsorcRevampWorld.SuperHardMode)
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

            return 0f;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CursedSoul>(), 1, 3, 6));
            npcLoot.Add(new CommonDrop(ItemID.GreaterHealingPotion, 100, 1, 1, 8));
        }
    }
}
