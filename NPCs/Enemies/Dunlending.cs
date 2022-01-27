using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;
using static tsorcRevamp.oSpawnHelper;

namespace tsorcRevamp.NPCs.Enemies {
    public class Dunlending : ModNPC {
        public override void SetStaticDefaults() {
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.PossessedArmor];
        }
        public override void SetDefaults() {
            npc.npcSlots = 1;
            npc.knockBackResist = 0.4f;
            npc.aiStyle = -1;
            npc.damage = 20;
            npc.defDamage = 2;
            npc.height = 40;
            npc.width = 20;
            npc.lifeMax = 45;
            npc.value = 150;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath2;
            animationType = NPCID.PossessedArmor;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.DunlendingBanner>();
        }

        public override void HitEffect(int hitDirection, double damage) {
            for (int i = 0; i < 5; i++) {
                int dustType = 5;
                int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (npc.life <= 0) {
                for (int i = 0; i < 25; i++) {
                    Dust.NewDust(npc.position, npc.width, npc.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }

                Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dunlending Gore 1"), 1f);
                Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dunlending Gore 2"), 1f);
                Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dunlending Gore 3"), 1f);
                Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dunlending Gore 2"), 1f);
                Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dunlending Gore 3"), 1f);
            }
        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(npc, 1.5f, 0.05f);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            var p = spawnInfo.player;

            float chance = 0;

            if (spawnInfo.invasion)
            {
                chance = 0;
                return chance;
            }

            if (spawnInfo.player.townNPCs > 1f) return 0f;


            if (p.ZoneOverworldHeight) {
                if (Main.dayTime) chance = 0.067f;
                else chance = 0.125f;
            }
            if (oUnderSurfaceByTile(p) || oUndergroundByTile(p) || oCavernByTile(p)) {
                if (Main.dayTime) chance = 0.067f;
                else chance = 0.1f;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                return chance /= 2;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                return chance /= 4;
            }
            return chance;
        }
        public override void NPCLoot() {
            Item.NewItem(npc.getRect(), ItemID.Torch, 1);
            Player player = Main.player[npc.target];

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && Main.rand.Next(10) == 0)
            {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.Lifegem>(), Main.rand.Next(20) == 0 ? 3 : 1); // 1/5 chance of 3, else 1
            }
            else
            {
                Item.NewItem(npc.getRect(), ItemID.HealingPotion, Main.rand.Next(20) == 0 ? 6 : 1); // 1/5 chance of 6, else 1
            }

            if (Main.rand.NextFloat() < 0.6f) { //60%
                Item.NewItem(npc.getRect(), ItemID.ShinePotion, Main.rand.Next(1, 3));
            }

            if (Main.rand.NextFloat() < 0.1f) { //10%
                Item.NewItem(npc.getRect(), ModContent.ItemType<BoostPotion>());
            }
            if (Main.rand.Next(10) == 0) { //8%
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.DunlendingAxe>(), 1, false, -1);
            }

            if (Main.rand.Next(20) == 0 && ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) { //5% Legacy only, we have plenty of other enemies dropping them now
                Item.NewItem(npc.getRect(), ItemID.IronskinPotion, Main.rand.Next(1, 2));
            }
            if (Main.rand.Next(20) == 0) {
                Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion);
            }
            if (Main.rand.Next(20) == 0) {
                Item.NewItem(npc.getRect(), ItemID.SpelunkerPotion);
            }
            if (Main.rand.Next(20) == 0) {
                Item.NewItem(npc.getRect(), ItemID.SwiftnessPotion);
            }
            if (Main.rand.Next(20) == 0) {
                Item.NewItem(npc.getRect(), ItemID.BattlePotion);
            }
            if (Main.rand.Next(50) == 0) { //2%
                Item.NewItem(npc.getRect(), ItemID.RegenerationPotion, Main.rand.Next(1, 5));
            }
            if (Main.rand.Next(100) == 0) { //1%
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrimsonPotion>());
            }

            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode && Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), mod.ItemType("CharcoalPineResin"));
        }
    }
}
