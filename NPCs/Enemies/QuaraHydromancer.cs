using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.NPCs.Enemies
{
    class QuaraHydromancer : ModNPC
    {
        int bubbleDamage = 33;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
        }
        public override void SetDefaults()
        {
            AnimationType = 21;
            NPC.aiStyle = 3;
            NPC.damage = 0;
            NPC.defense = 22;
            NPC.height = 45;
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 1500;
            NPC.width = 18;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0.25f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.QuaraHydromancerBanner>();

            if (Main.hardMode) { NPC.lifeMax = 500; NPC.defense = 22; NPC.value = 1500; bubbleDamage = 45; }
            if (tsorcRevampWorld.SuperHardMode) { NPC.lifeMax = 1500; NPC.defense = 50; NPC.value = 3600; bubbleDamage = 55; }

            UsefulFunctions.AddAttack(NPC, 80, ModContent.ProjectileType<Projectiles.Enemy.Bubble>(), bubbleDamage, 6, SoundID.Item87, 0);
            UsefulFunctions.AddAttack(NPC, 300, ModContent.ProjectileType<Projectiles.Enemy.InkGeyser>(), bubbleDamage, 0, condition: (NPC npc) => { return tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WaterFiendKraken>())) && Main.netMode != NetmodeID.MultiplayerClient; });
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;

            if (spawnInfo.Water) return 0f;

            //now spawns in hallow, since jungle was getting crowded
            //spawns more before the rage is defeated

            if (Main.hardMode && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && !Main.dayTime && P.ZoneHallow && P.ZoneOverworldHeight && Main.rand.NextBool(30)) return 1;
            if (Main.hardMode && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && !Main.dayTime && P.ZoneHallow && (P.ZoneRockLayerHeight || P.ZoneDirtLayerHeight) && Main.rand.NextBool(25)) return 1;
            if (Main.hardMode && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && Main.dayTime && P.ZoneHallow && (P.ZoneRockLayerHeight || P.ZoneDirtLayerHeight) && Main.rand.NextBool(35)) return 1;
            if (Main.hardMode && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && P.ZoneHallow && (P.ZoneRockLayerHeight || P.ZoneDirtLayerHeight) && Main.rand.NextBool(10)) return 1;
            if (Main.hardMode && spawnInfo.Lihzahrd && Main.rand.NextBool(45)) return 1;
            if (Main.hardMode && spawnInfo.Player.ZoneDesert && Main.rand.NextBool(45)) return 1;
            if (tsorcRevampWorld.SuperHardMode && P.ZoneHallow && Main.rand.NextBool(10)) return 1;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneGlowshroom && Main.rand.NextBool(5)) return 1;
            return 0;
        }
        #endregion

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 2, 0.05f, canTeleport: false, lavaJumping: true, canDodgeroll: false, canPounce: false);


            //PLAY CREATURE SOUND
            if (Main.rand.NextBool(1000))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.3f, Pitch = -0.3f }, NPC.Center); // water sound
            }


            //JUSTHIT CODE
            Player player = Main.player[NPC.target];
            if (NPC.justHit && NPC.Distance(player.Center) < 100)
            {
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 0f;
            }
            if (NPC.justHit && NPC.Distance(player.Center) < 150 && Main.rand.NextBool(2))
            {
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 40f;
                NPC.velocity.Y = Main.rand.NextFloat(-11f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-4f, -3f);
                NPC.netUpdate = true;
            }
            if (NPC.justHit && NPC.Distance(player.Center) > 200 && Main.rand.NextBool(2))
            {
                NPC.velocity.Y = Main.rand.NextFloat(-11f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(4f, 3f);
                NPC.netUpdate = true;
            }
        }



        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 1").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 2").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 3").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 2").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 3").Type, 1.2f);
            }
            //if (Main.rand.Next(99) < 10) Item.NewItem(NPC.GetSource_Loot(), npc.getRect(), ItemID.HealingPotion, 1);
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Magic.GreatEnergyBeamScroll>(), 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.SoulofLight, 2));
            npcLoot.Add(new CommonDrop(ItemID.GreaterHealingPotion, 100, 1, 1, 8));
        }
    }
}