using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.Items
{
    class EssenceOfTerraria : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Essence of Terraria");
            // Tooltip.SetDefault("Summons almost every boss at once." + "\nYou will never survive this.");
        }

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Lime;
        }

        public override bool? UseItem(Player player)
        { //todo keep adding bosses to this
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.KingSlime);
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.EyeofCthulhu);
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.EaterofWorldsHead);
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.BrainofCthulhu);
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.QueenBee);
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Slogra>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Gaibon>());
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.SkeletronHead);
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.TheRage>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.TheHunter>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.TheSorrow>());
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.Retinazer);
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.Spazmatism);
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.TheDestroyer);
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.SkeletronPrime);
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.Plantera);
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.Golem);
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.CultistBoss);
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Death>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<FireFiendMarilith>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<EarthFiendLich>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<WaterFiendKraken>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>());
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>());

            return true;
        }
    }
}
