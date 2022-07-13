using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class DworcFleshhunter : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.HitSound = SoundID.NPCHit29;
            NPC.DeathSound = SoundID.NPCDeath29;
            NPC.damage = 30;
            NPC.lifeMax = 25;
            NPC.defense = 12;
            NPC.value = 250;
            NPC.width = 18;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.knockBackResist = 0.1f;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DworcFleshhunterBanner>();

            AnimationType = NPCID.Skeleton;
            Main.npcFrameCount[NPC.type] = 15;
        }

        public override void OnKill()
        {
            if (Main.rand.NextBool(100)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>());
            if (Main.rand.NextBool(20)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ManaRegenerationPotion);
            if (Main.rand.NextBool(10)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.MagicPowerPotion);
            if (Main.rand.NextBool(20)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.RegenerationPotion);
            if (Main.rand.NextBool(3)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ShinePotion);
            if (Main.rand.NextBool(20)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.SpelunkerPotion);
            if (Main.rand.NextBool(20)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.SwiftnessPotion);
            if (Main.rand.NextBool(20)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.WaterWalkingPotion);
            if (Main.rand.NextBool(20)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.BattlePotion);
            if (Main.rand.NextBool(10))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Armors.Magic.RedClothHat>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Armors.Magic.RedClothTunic>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Armors.Magic.RedClothPants>());
            }
        }

        //Spawns in the Jungle, mostly Underground and in the Cavern.

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0f;

            if (spawnInfo.Player.ZoneDungeon)
            {
                return 0f;
            }
            else if (!Main.hardMode && spawnInfo.Player.ZoneJungle && spawnInfo.Player.ZoneOverworldHeight)
            {
                return 0.125f;
            }
            else if (Main.dayTime && !Main.hardMode && spawnInfo.Player.ZoneJungle && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight))
            {
                return 0.17f;
            }
            else if (!Main.dayTime && !Main.hardMode && spawnInfo.Player.ZoneJungle && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight))
            {
                return 0.2f;
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
