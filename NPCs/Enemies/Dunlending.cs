using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;
using static tsorcRevamp.oSpawnHelper;

namespace tsorcRevamp.NPCs.Enemies
{
    public class Dunlending : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.PossessedArmor];
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 1;
            NPC.knockBackResist = 0.4f;
            NPC.aiStyle = -1;
            NPC.damage = 20;
            NPC.defDamage = 2;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 45;
            NPC.value = 150;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            AnimationType = NPCID.PossessedArmor;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DunlendingBanner>();
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }

                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dunlending Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dunlending Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dunlending Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dunlending Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dunlending Gore 3").Type, 1f);
            }
        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.5f, 0.05f);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            var p = spawnInfo.Player;

            float chance = 0;

            if (spawnInfo.Invasion)
            {
                chance = 0;
                return chance;
            }

            if (spawnInfo.Player.townNPCs > 1f) return 0f;


            if (p.ZoneOverworldHeight)
            {
                if (Main.dayTime) chance = 0.067f;
                else chance = 0.125f;
            }
            if (oUnderSurfaceByTile(p) || oUndergroundByTile(p) || oCavernByTile(p))
            {
                if (Main.dayTime && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson) chance = 0.067f;
                if (!Main.dayTime && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson) chance = 0.1f;
            }
            if (Main.hardMode)
            {
                return chance /= 2;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                return chance /= 4;
            }
            return chance;
        }
        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Torch, 1);
            Player player = Main.player[NPC.target];

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && Main.rand.Next(10) == 0)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.Lifegem>(), Main.rand.Next(20) == 0 ? 3 : 1); // 1/5 chance of 3, else 1
            }
            else
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.HealingPotion, Main.rand.Next(20) == 0 ? 6 : 1); // 1/5 chance of 6, else 1
            }

            if (Main.rand.NextFloat() < 0.6f)
            { //60%
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ShinePotion, Main.rand.Next(1, 3));
            }

            if (Main.rand.NextFloat() < 0.1f)
            { //10%
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<BoostPotion>());
            }
            if (Main.rand.Next(10) == 0)
            { //8%
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.DunlendingAxe>(), 1, false, -1);
            }

            if (Main.rand.Next(20) == 0)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ManaRegenerationPotion);
            }
            if (Main.rand.Next(20) == 0)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.SpelunkerPotion);
            }
            if (Main.rand.Next(20) == 0)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.SwiftnessPotion);
            }
            if (Main.rand.Next(20) == 0)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.BattlePotion);
            }
            if (Main.rand.Next(50) == 0)
            { //2%
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.RegenerationPotion, Main.rand.Next(1, 5));
            }
            if (Main.rand.Next(100) == 0)
            { //1%
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<CrimsonPotion>());
            }

            if (Main.rand.Next(10) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("CharcoalPineResin").Type);
        }
    }
}
