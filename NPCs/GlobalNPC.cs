using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Buffs.Summon;
using tsorcRevamp.Buffs.Summon.WhipDebuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
using tsorcRevamp.Items.Weapons.Ranged;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Items.Weapons.Throwing;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.Projectiles.Summon.Runeterra;
using tsorcRevamp.Projectiles.Summon.Sentry;
using tsorcRevamp.Projectiles.Summon.Whips;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Armors.Melee;

namespace tsorcRevamp.NPCs
{
    public class tsorcRevampGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        float enemyValue;
        float multiplier = 1f;
        float divisorMultiplier = 1f;
        int DarkSoulQuantity;
        public Player lastHitPlayerSummoner = Main.LocalPlayer;
        public Player lastHitPlayerRanger = Main.LocalPlayer;

        public float SummonTagFlatDamage;
        public float BaseSummonTagCriticalStrikeChance;
        public float SummonTagCriticalStrikeChance;
        public float SummonTagScalingDamage;
        public float SummonTagArmorPenetration;
        public bool markedByCrystalNunchaku;
        public bool markedByDetonationSignal;
        public bool markedByDominatrix;
        public bool markedByDragoonLash;
        public bool markedByEnchantedWhip;
        public bool markedByNightsCracker;
        public bool markedByPolarisLeash;
        public bool markedByPyrosulfate;
        public bool markedBySearingLash;
        public bool markedByTerraFall;
        public bool markedByUrumi;
        public bool markedByLeatherWhip;
        public bool markedBySnapthorn;
        public bool markedBySpinalTap;
        public bool markedByFirecracker;
        public bool markedByCoolWhip;
        public bool markedByDurendal;
        public bool markedByMorningStar;
        public bool markedByDarkHarvest;
        public bool markedByKaleidoscope;

        public float CrystalNunchakuStacks = 10;
        public bool CrystalNunchakuProc = false;
        public float CrystalNunchakuUpdateTick = 0f;
        public float CrystalNunchakuScalingDamage = 0f;

        public float DragoonLashFireBreathTimer = 0f;

        public bool Scorched;
        public bool Shocked;
        public bool Sunburnt;

        //Stores the event this NPC belongs to
        public ScriptedEvent ScriptedEventOwner;

        //Stores which NPC in that event this is
        public int ScriptedEventIndex;

        //Stores the targeting, tracking, and despawning information for a NPC
        NPCDespawnHandler despawnHandler;

        //Whatever custom expert scaling we want goes here. For reference 1 eliminates all expert mode doubling, and 2 is normal expert mode scaling.
        public static double expertScale = 2;

        public bool DarkInferno;
        public bool Ignited;
        public bool CrimsonBurn;
        public bool ToxicCatDrain;
        public bool ResetToxicCatBlobs;
        public bool ViruCatDrain;
        public bool ResetViruCatBlobs;
        public bool BiohazardDrain;
        public bool ResetBiohazardBlobs;
        public bool ElectrocutedEffect;
        public bool PolarisElectrocutedEffect;
        public bool CrescentMoonlight;
        public bool Soulstruck;
        public bool PhazonCorruption;

        public bool Venomized;
        public bool Electrified;
        public bool Irradiated;
        public bool IrradiatedByShroom;


        public override void ResetEffects(NPC npc)
        {
            DarkInferno = false;
            Ignited = false;
            CrimsonBurn = false;
            ToxicCatDrain = false;
            ResetToxicCatBlobs = false;
            ViruCatDrain = false;
            ResetViruCatBlobs = false;
            BiohazardDrain = false;
            ResetBiohazardBlobs = false;
            ElectrocutedEffect = false;
            PolarisElectrocutedEffect = false;
            CrescentMoonlight = false;
            Soulstruck = false;
            PhazonCorruption = false;
            Venomized = false;
            Electrified = false;
            Irradiated = false;
            IrradiatedByShroom = false;
            SummonTagFlatDamage = 0f;
            BaseSummonTagCriticalStrikeChance = 4f;
            SummonTagScalingDamage = 0f;
            SummonTagArmorPenetration = 0f;
            markedByCrystalNunchaku = false;
            markedByDetonationSignal = false;
            markedByDominatrix = false;
            markedByDragoonLash = false;
            markedByEnchantedWhip = false;
            markedByNightsCracker = false;
            markedByPolarisLeash = false;
            markedByPyrosulfate = false;
            markedBySearingLash = false;
            markedByTerraFall = false;
            markedByUrumi = false;
            markedByLeatherWhip = false;
            markedBySnapthorn = false;
            markedBySpinalTap = false;
            markedByFirecracker = false;
            markedByCoolWhip = false;
            markedByDurendal = false;
            markedByMorningStar = false;
            markedByDarkHarvest = false;
            markedByKaleidoscope = false;
            CrystalNunchakuScalingDamage = 0f;
            Scorched = false;
            Shocked = false;
            Sunburnt = false;
        }
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.KingSlime)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>()));
            }
            if (npc.type == NPCID.EyeofCthulhu)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
            }
            if (npc.type == NPCID.BrainofCthulhu)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>(), 1, 1, 2));
            }
            if (npc.type == NPCID.QueenSlimeBoss)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>(), 1, 4, 8));
            }
            if (npc.type == NPCID.Plantera)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 1, 2));
            }
            if (npc.type == NPCID.DukeFishron)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 3, 6));
            }
            if (npc.type == NPCID.Golem)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 2, 4));
            }
            if (npc.type == NPCID.HallowBoss)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 3, 6));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 3, 6));
            }
            if (npc.type == NPCID.CultistBoss)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 4, 8));
            }
            if (npc.type == NPCID.MoonLordCore)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 5, 10));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 5, 10));
            }
        }
        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {

            if (tsorcRevampWorld.TheEnd)
            {
                pool.Clear(); //stop NPC spawns in The End 
            }

            if (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.StarlitHeavenWallpaper)
            {
                pool.Clear();
                pool.Add(ModContent.NPCType<Enemies.HumanityPhantom>(), 10f);
            }

            //VANILLA NPC SPAWN EDITS

            //PRE-HARD MODE
            //jungle
            if (spawnInfo.Player.ZoneJungle && !Main.hardMode)
            {
                //pool.Add(the type of the npc, what chance you want it to spawn with);
                pool.Add(NPCID.LostGirl, 0.005f);
                pool.Add(NPCID.Salamander2, 0.03f);
            }
            //corrupt (not in water)
            if (spawnInfo.Player.ZoneCorrupt && !spawnInfo.Water && !Main.hardMode)
            {
                pool.Add(NPCID.CochinealBeetle, 0.02f);
                pool.Add(NPCID.GiantShelly, 0.02f);
            }
            //corrupt (in water)
            if (spawnInfo.Player.ZoneCorrupt && spawnInfo.Water && !Main.hardMode)
            {
                pool.Add(NPCID.Squid, 0.02f);
            }
            //crimson
            if (spawnInfo.Player.ZoneCrimson && !Main.hardMode)
            {
                pool.Add(NPCID.LacBeetle, 0.02f);
                pool.Add(NPCID.Drippler, 0.1f);
                pool.Add(NPCID.BloodCrawler, 0.002f);
                pool.Add(NPCID.BloodCrawlerWall, 0.002f);
            }
            //meteor
            if (spawnInfo.Player.ZoneMeteor && !Main.hardMode)
            {
                pool.Add(NPCID.GraniteFlyer, 0.4f);
                pool.Add(NPCID.Salamander4, 0.4f);
                pool.Add(NPCID.MeteorHead, 0.01f);
            }

            //HARD MODE SECTION

            //golem temple
            if (spawnInfo.SpawnTileType == TileID.LihzahrdBrick && spawnInfo.Lihzahrd && Main.hardMode)
            {
                pool.Add(NPCID.DesertDjinn, 0.075f); 
                pool.Add(NPCID.DiabolistWhite, 0.02f); //was 0.1
                pool.Add(ModContent.NPCType<Enemies.RingedKnight>(), 0.25f);
                pool.Add(ModContent.NPCType<Enemies.LothricSpearKnight>(), 0.05f);
                pool.Add(ModContent.NPCType<Enemies.LothricKnight>(), 0.05f);

            }

            //desert or underground desert and dungeon(shadow temple)
            if ((spawnInfo.Player.ZoneDesert || spawnInfo.Player.ZoneUndergroundDesert) && spawnInfo.Player.ZoneDungeon && Main.hardMode)
            {
                pool.Add(NPCID.DiabolistRed, 0.01f);
            }

            //machine temple (in water)
            if (spawnInfo.Water && Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.GreenDungeonSlabUnsafe && Main.hardMode)
            {
                if (!pool.ContainsKey(NPCID.GreenJellyfish))
                {
                    pool.Remove(NPCID.GreenJellyfish);
                }
                if (!pool.ContainsKey(ModContent.NPCType<Enemies.MutantToad>()))
                {
                    pool.Remove(ModContent.NPCType<Enemies.MutantToad>());
                }

                pool.Add(NPCID.GreenJellyfish, 8f);
                pool.Add(ModContent.NPCType<Enemies.MutantToad>(), 2f);
            }
            //machine temple (not in water)
            if (!spawnInfo.Water && Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.GreenDungeonSlabUnsafe && Main.hardMode && !tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(ModContent.NPCType<Enemies.SuperHardMode.IceSkeleton>(), 0.2f); 
            }
            //sky
            if (spawnInfo.Player.ZoneSkyHeight && Main.hardMode)
            {
                pool.Add(NPCID.GoblinSummoner, 0.01f);
            }
            //ocean water (outer thirds of the map)
            if (spawnInfo.Water && Main.hardMode && (Math.Abs(spawnInfo.SpawnTileX - Main.spawnTileX) > Main.maxTilesX / 3))
            {
                pool.Add(NPCID.SandsharkHallow, 0.3f);
            }

            //SUPER HARD MODE SECTION
            if (spawnInfo.Player.ZoneJungle && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.BoneLee, 0.05f);
            }

            //mushroom
            if (spawnInfo.Player.ZoneGlowshroom && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.DD2LightningBugT3, 0.25f);
            }
            if (spawnInfo.Player.ZoneUnderworldHeight && !spawnInfo.Player.ZoneDungeon && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.SolarCrawltipedeHead, 0.002f);
                pool.Add(NPCID.SolarSroller, 0.4f); //.5 is 16%
                pool.Add(NPCID.SolarCorite, 0.01f);
                pool.Add(NPCID.SolarSpearman, 0.4f);
                pool.Add(NPCID.SolarDrakomire, 0.4f);
                pool.Add(NPCID.SolarSolenian, 0.6f);
            }
            //catacombs
            if (spawnInfo.SpawnTileType == TileID.BoneBlock && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.NebulaBrain, 0.2f); //.1 is 3%
                pool.Add(NPCID.NebulaHeadcrab, 0.4f); //.1 is 3%
                pool.Add(NPCID.NebulaBeast, 0.6f); //.1 is 3%
                pool.Add(NPCID.NebulaSoldier, 0.5f); //.1 is 3%
            }
            //spaceships
            if (spawnInfo.SpawnTileType == TileID.MartianConduitPlating && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.VortexLarva, 1f); //.1 is 3%
            }
            //one of the outer thirds of the map
            if ((Math.Abs(spawnInfo.SpawnTileX - Main.spawnTileX) > Main.maxTilesX / 3) && tsorcRevampWorld.SuperHardMode)
            {
                //pool.Add(NPCID.GoblinShark, 0.2f); //.1 is 3%
            }
            // molten sky temple
            if (spawnInfo.Player.ZoneUnderworldHeight && spawnInfo.SpawnTileType == TileID.MeteoriteBrick && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.StardustWormHead, 0.1f); //.1 is 3%
                pool.Add(NPCID.StardustCellBig, 0.02f); //.5 is 16%
                pool.Add(NPCID.StardustJellyfishBig, 0.3f);
                pool.Add(NPCID.StardustSpiderBig, 0.6f);
                pool.Add(NPCID.StardustSoldier, 1f);
            }

            Player thisPlayer = spawnInfo.Player;
            bool invasion = Main.invasionType != 0;
            if(thisPlayer.Center.X > 82016 || thisPlayer.Center.X < 74560 || thisPlayer.Center.Y > 16000)
            {
                invasion = false;
            }

            if (spawnInfo.Player.ZoneTowerSolar || spawnInfo.Player.ZoneTowerNebula || spawnInfo.Player.ZoneTowerStardust || spawnInfo.Player.ZoneTowerVortex || spawnInfo.Player.ZoneOldOneArmy || invasion)
            {
                List<int> blockedNPCs = new List<int>();

                foreach (int id in pool.Keys)
                {
                    ModNPC modNPC = NPCLoader.GetNPC(id);

                    if (modNPC != null && modNPC.Mod == ModLoader.GetMod("tsorcRevamp"))
                    {
                        blockedNPCs.Add(id);
                    }
                }

                foreach (int id in blockedNPCs)
                {
                    pool.Remove(id);
                }
            }
        }

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            //reduces max spawns by 40% and rate by 60% until player exceeds 160 health
            if(player.statLifeMax2 <= 160)
            {
                spawnRate = (int)(spawnRate * 1.6f);
                maxSpawns = (int)(maxSpawns * 0.6f);
            }
            //reduces max spawns by 30% and spawn rate by 50% after 160 health
            if (player.statLifeMax2 > 160 && player.statLifeMax2 <= 200)
            {
                spawnRate = (int)(spawnRate * 1.5f);
                maxSpawns = (int)(maxSpawns * 0.7f);
            }
            //reduces max spawns by 20% and spawn rate by 40% from 200-400 health
            if (player.statLifeMax2 > 200 && player.statLifeMax2 <= 400)
            {
                spawnRate = (int)(spawnRate * 1.4f);
                maxSpawns = (int)(maxSpawns * 0.8f);
            }
            //only reduces spawn rate by 30% above 400 health
            if (player.statLifeMax2 > 400)
            {
                spawnRate = (int)(spawnRate * 1.3f);   
            }           

            if (player.GetModPlayer<tsorcRevampPlayer>().BossZenBuff || tsorcRevampWorld.BossAlive || player.HasBuff(ModContent.BuffType<Buffs.Bonfire>()))
            {
                spawnRate = 9999999;//Higher is less spawns
                maxSpawns = 0;
            }


            //Peace candles do not activate if there is a) an invasion and b) the player is near the center of the world.
            if ((Main.invasionType == 0 || player.Center.X > 82016 || player.Center.X < 74560 || player.Center.Y > 16000))
            {
                if (player.HasBuff(BuffID.PeaceCandle))
                {
                    spawnRate = 9999999;
                    maxSpawns = 0;
                }
            }
            else
            {
                if (Main.invasionType == 1)
                {
                    player.buffImmune[BuffID.PeaceCandle] = true;
                    player.ZonePeaceCandle = false;
                    spawnRate /= 2;
                    maxSpawns *= 3;
                }
            }

            if (player.ZoneTowerSolar || player.ZoneTowerNebula || player.ZoneTowerStardust || player.ZoneTowerVortex)
            {
                spawnRate /= 2;
                maxSpawns = (int)(maxSpawns * 1.5);
            }

            if (Main.tile[(int)player.position.X / 16, (int)player.position.Y / 16].WallType == WallID.StarlitHeavenWallpaper)
            {
                spawnRate /= 10; //Origin of the Abyss. All spawns blocked other than Humanity Phantoms
            }

        }

        //vanilla npc changes moved to separate file

        public override void OnKill(NPC npc)
        {
            Player LocalPlayer = Main.LocalPlayer;
            if(npc.active && !npc.friendly && (LocalPlayer.HeldItem.type == ModContent.ItemType<OrbOfDeception>() || LocalPlayer.HeldItem.type == ModContent.ItemType<OrbOfFlame>() || LocalPlayer.HeldItem.type == ModContent.ItemType<OrbOfSpirituality>()) && Main.rand.NextBool(9))
            {
                LocalPlayer.GetModPlayer<tsorcRevampPlayer>().EssenceThief += 1;
            }

            if(npc.type == NPCID.Golem && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                UsefulFunctions.BroadcastText("Somewhere in the sky a forcefield collapses...", Color.Cyan);
            }
            if (npc.boss)
            {
                foreach (Player player in Main.player)
                {
                    if (!player.active) { continue; }
                    player.GetModPlayer<tsorcRevampPlayer>().bossMagnet = true;
                    player.GetModPlayer<tsorcRevampPlayer>().bossMagnetTimer = 300; //5 seconds of increased grab range, check GlobalItem::GrabStyle and GrabRange
                }
            }

            if (Main.LocalPlayer.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < Main.LocalPlayer.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2)
            {
                if (Main.rand.NextBool(2))
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.StaminaDroplet>(), 1);
                }

                if (Main.rand.NextBool(12))
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.StaminaDroplet>(), 1);
                }
            }

            #region Dark Souls & Consumable Souls Drops

            if (Soulstruck)
            {
                divisorMultiplier = 0.9f; //10% increase
            }

            if (npc.lifeMax > 5 && npc.value >= 10f || npc.boss)
            { //stop zero-value souls from dropping (the 'or boss' is for expert mode support)
                if (Main.masterMode)
                {
                    enemyValue = (int)npc.value / (divisorMultiplier * 20);
                }
                else
                if (Main.expertMode)
                { //npc.value is the amount of coins they drop
                    enemyValue = (int)npc.value / (divisorMultiplier * 25); //all enemies drop more money in expert mode, so the divisor is larger to compensate
                }
                else
                {
                    enemyValue = (int)npc.value / (divisorMultiplier * 15);
                }


                multiplier = tsorcRevampPlayer.CheckSoulsMultiplier(Main.LocalPlayer);

                DarkSoulQuantity = (int)(multiplier * enemyValue);

                #region Bosses drop souls once
                if (npc.boss)
                {
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(npc.type)))
                    {
                        DarkSoulQuantity = 0;
                    }
                    else
                    {
                        // check whether the SHM boss was killed
                        if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.WaterFiendKraken>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.FireFiendMarilith>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.EarthFiendLich>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>()) /*|| npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>()) gwyn CLOSES the abyss portal!*/
                        {
                            UsefulFunctions.BroadcastText("The portal to The Abyss widens, and its denizens grow stronger...", Color.Orange);
                        }

                        if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)) && Main.invasionType == 0)
                        {
                            Main.StartInvasion();
                        }
                        tsorcRevampWorld.PopulatePairedBosses();
                        //Paired bosses have to have their slain entries work different
                        if (tsorcRevampWorld.PairedBosses.Contains(npc.type)) {
                            for (int i = 0; i < tsorcRevampWorld.PairedBosses.Count; i++)
                            {
                                if (tsorcRevampWorld.PairedBosses[i] == npc.type)
                                {
                                    int pairedNPCOffset = -1;
                                    if (i % 2 == 0)
                                    {
                                        pairedNPCOffset = 1;
                                    }

                                    //If the other boss is not alive, then add them both. If not, don't.
                                    if (!NPC.AnyNPCs(tsorcRevampWorld.PairedBosses[i + pairedNPCOffset]))
                                    {
                                        tsorcRevampWorld.NewSlain.Add(new NPCDefinition(npc.type), 1);
                                        tsorcRevampWorld.NewSlain.Add(new NPCDefinition(tsorcRevampWorld.PairedBosses[i + pairedNPCOffset]), 1);
                                    }

                                    break;
                                }
                            }
                        }
                        else
                        {
                            tsorcRevampWorld.NewSlain.Add(new NPCDefinition(npc.type), 1);
                        }

                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.WorldData); //Slain only exists on the server. This tells the server to run NetSend(), which syncs this data with clients
                        }

                        if (Main.expertMode && npc.type != ModContent.NPCType<LichKingDisciple>() && npc.type != ModContent.NPCType<LichKingSerpentHead>())
                        {
                            DarkSoulQuantity = 0;
                        }
                        if (npc.type == ModContent.NPCType<LichKingDisciple>() && Main.expertMode)
                        {
                            DarkSoulQuantity = (int)((float)DarkSoulQuantity * 2.5f);
                        }
                    }
                }
                #endregion

                #region EoW drops souls in a unique way
                if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)))
                {

                    DarkSoulQuantity = 24; //*72 for soul drops per eater, 1728 souls per one whole eater

                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
                }
                #endregion

                if (DarkSoulQuantity > 0)
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
                }


                // Consumable Soul drops ahead - Current numbers give aprox. +20% souls

                float chance = 0.01f + (0.0005f * Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().ConsSoulChanceMult);
                //Main.NewText(chance);

                if (!(npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail || npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.Creeper))
                {

                    if ((enemyValue >= 1) && (enemyValue <= 200) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 1 and 200 dropping FadingSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<FadingSoul>(), 1); // Zombies and eyes are 6 and 7 enemyValue, so will only drop FadingSoul
                    }

                    if ((enemyValue >= 15) && (enemyValue <= 2000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 10 and 2000 dropping LostUndeadSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<LostUndeadSoul>(), 1); // Most pre-HM enemies fall into this category
                    }

                    if ((enemyValue >= 55) && (enemyValue <= 10000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 50 and 10000 dropping NamelessSoldierSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<NamelessSoldierSoul>(), 1); // Most HM enemies fall into this category
                    }

                    if ((enemyValue >= 150) && (enemyValue <= 10000) && (Main.rand.NextFloat() < chance) && Main.hardMode) // 1% chance of all enemies between enemyValue 150 and 10000 dropping ProudKnightSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<ProudKnightSoul>(), 1);
                    }
                }
                //End consumable souls drops
            }
            #endregion


            #region Event saving and custom drops code

            if (ScriptedEventOwner != null && ScriptedEventOwner.eventNPCs[ScriptedEventIndex].type == npc.type)
            {
                ScriptedEventOwner.eventNPCs[ScriptedEventIndex].killed = true;

                if (ScriptedEventOwner.eventNPCs[ScriptedEventIndex].extraLootItems != null)
                {
                    for (int i = 0; i < ScriptedEventOwner.eventNPCs[ScriptedEventIndex].extraLootItems.Count; i++)
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.Center, ScriptedEventOwner.eventNPCs[ScriptedEventIndex].extraLootItems[i], ScriptedEventOwner.eventNPCs[ScriptedEventIndex].extraLootAmounts[i]);
                    }
                }

                bool oneAlive = false;
                foreach(EventNPC eventNPC in ScriptedEventOwner.eventNPCs)
                {
                    if (!eventNPC.killed)
                    {
                        oneAlive = true;
                    }
                }

                if (!oneAlive)
                {
                    if (ScriptedEventOwner.FinalNPCCustomDrops != null)
                    {
                        for (int i = 0; i < ScriptedEventOwner.FinalNPCCustomDrops.Count; i++)
                        {
                            Item.NewItem(npc.GetSource_Loot(), npc.Center, ScriptedEventOwner.FinalNPCCustomDrops[i], ScriptedEventOwner.FinalNPCDropAmounts[i]);
                        }
                    }
                }
            }
            #endregion
        }


        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (Ignited)
            {
                modifiers.FlatBonusDamage += MagmaBreastplate.OnHitDmg;
            }
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().ConditionOverload)
            {
                int debuffCounter = 1;
                foreach (int buffType in npc.buffType)
                {
                    if (Main.debuff[buffType] && !(BuffID.Sets.IsAnNPCWhipDebuff[buffType]))
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<MythrilRamDebuff>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<ScorchingDebuff>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<ShockedDebuff>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<SunburnDebuff>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<SunderedDebuff>())
                    {
                        debuffCounter++;
                    }
                }
                double scalar = Math.Pow(1.15, debuffCounter - 1); //was 1.2 before, then 1.1
                modifiers.FinalDamage *= (float)scalar;
            }
            if (markedByCrystalNunchaku && CrystalNunchakuProc)
            {
                CrystalNunchakuScalingDamage += CrystalNunchakuStacks / 20f; //this should still fall into the category of summon tag scaling damage for any bonuses related, but it works on all sources of damage, so it gets treated differently
                modifiers.ScalingBonusDamage += CrystalNunchakuScalingDamage; //in case you want to add an effect that affects summon tag damage, don't forget to include it here
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            Player projectileOwner = Main.player[projectile.owner];
            var modPlayerProjectileOwner = Main.player[projectile.owner].GetModPlayer<tsorcRevampPlayer>();
            float SummonTagDamageMultiplier = ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];
            if (projectile.DamageType == DamageClass.Summon) //minion damage, whip damage unaffected
            {
                modifiers.SourceDamage *= 0.8f;
            }
            #region Individual Whip debuff effects
            #region Modded Whips
            //if(markedByCrystalNunchaku) only has a special effect
            if (markedByDetonationSignal)
            {
                SummonTagScalingDamage += DetonationSignal.SummonTagScalingDamage / 100f;
            }
            if (markedByDominatrix)
            {
                BaseSummonTagCriticalStrikeChance += Dominatrix.SummonTagCrit;
            }
            //if (markedByDragoonLash) only has a special effect
            if (markedByEnchantedWhip)
            {
                SummonTagFlatDamage += EnchantedWhip.SummonTagDamage;
            }
            if (markedByNightsCracker)
            {
                SummonTagFlatDamage += modPlayerProjectileOwner.NightsCrackerStacks * 2f;
                BaseSummonTagCriticalStrikeChance += modPlayerProjectileOwner.NightsCrackerStacks;
                //SearingLashDamageBonus needs to be calculated after all the flat tag damage has been added
            }
            if (markedByPolarisLeash)
            {
                SummonTagFlatDamage += PolarisLeash.SummonTagDamage;
            }
            if (markedByPyrosulfate)
            {
                SummonTagFlatDamage += Pyrosulfate.SummonTagDamage;
                BaseSummonTagCriticalStrikeChance += Pyrosulfate.SummonTagCrit;
            }
            //if (markedBySearingLash) SearingLashDamageBonus needs to be calculated after all the flat tag damage has been added
            if (markedByTerraFall)
            {
                SummonTagFlatDamage += modPlayerProjectileOwner.TerraFallStacks * 5f;
                BaseSummonTagCriticalStrikeChance += modPlayerProjectileOwner.TerraFallStacks * 4f;
            }
            if (markedByUrumi)
            {
                SummonTagArmorPenetration += Urumi.SummonTagArmorPen;
                BaseSummonTagCriticalStrikeChance += Urumi.SummonTagCrit;
            }
            #endregion
            #region Vanilla Whips
            if (markedByLeatherWhip)
            {
                SummonTagFlatDamage += 4f;
            }
            if (markedBySnapthorn)
            {
                SummonTagFlatDamage += 6f;
            }
            if (markedBySpinalTap)
            {
                SummonTagFlatDamage += 7f;
            }
            if(markedByFirecracker)
            {
                SummonTagScalingDamage += SummonerEdits.FirecrackerScalingDamage / 100f;
            }
            if (markedByCoolWhip)
            {
                SummonTagFlatDamage += 6f;
            }
            if (markedByDurendal)
            {
                SummonTagFlatDamage += 9f;
            }
            if (markedByMorningStar)
            {
                SummonTagFlatDamage += SummonerEdits.MorningStarTagDamage;
                BaseSummonTagCriticalStrikeChance += SummonerEdits.MorningStarTagCriticalStrikeChance;
            }
            if (markedByDarkHarvest)
            {
                SummonTagFlatDamage += 10f;
            }
            if (markedByKaleidoscope)
            {
                SummonTagFlatDamage += SummonerEdits.KaleidoscopeTagDamage;
                BaseSummonTagCriticalStrikeChance += SummonerEdits.KaleidoscopeTagCriticalStrikeChance;
            }
            #endregion
            #endregion
            #region Summon Tag Damage Calculation and Special Effects
            if (projectile.IsMinionOrSentryRelated)
            {
                #region Modded Whip Special Effects
                //Crystal Nunchaku Effect located in ModifyIncomingHit
                if (markedByDetonationSignal) //Detonation Signal effect
                {
                    int buffIndex = 0;
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Top, Vector2.Zero, ProjectileID.DD2ExplosiveTrapT2Explosion, 0, 0, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = 1.5f, PitchVariance = 0.3f }, npc.Top);
                    npc.AddBuff(ModContent.BuffType<DetonationSignalBuff>(), 4 * 60);
                    foreach (int buffType in npc.buffType)
                    {
                        if (buffType == ModContent.BuffType<DetonationSignalDebuff>())
                        {
                            npc.DelBuff(buffIndex);
                        }
                        buffIndex++;
                    }
                }
                //Dragoon Lash effect at the bottom
                if (markedByEnchantedWhip)
                {
                    int StarDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(EnchantedWhip.BaseDamage * EnchantedWhip.StarDamageScaling / 100f);
                    Vector2 StarVector1 = new Vector2(-640, -800) + npc.Center;
                    Vector2 StarVector2 = new Vector2(640, -800) + npc.Center;
                    Vector2 StarVector3 = new Vector2(0, -800) + npc.Center;
                    Vector2 StarMove1 = new Vector2(+32, 40);
                    Vector2 StarMove2 = new Vector2(-32, 40);
                    Vector2 StarMove3 = new Vector2(0, 40);
                    if (Main.rand.NextBool(3))
                    {
                        Projectile.NewProjectile(Projectile.GetSource_None(), StarVector1, StarMove1, ModContent.ProjectileType<EnchantedWhipFallingStar>(), StarDamage, 0, Main.myPlayer);
                    }
                    else if (Main.rand.NextBool(3))
                    {
                        Projectile.NewProjectile(Projectile.GetSource_None(), StarVector2, StarMove2, ModContent.ProjectileType<EnchantedWhipFallingStar>(), StarDamage, 0, Main.myPlayer);
                    }
                    else
                    {
                        Projectile.NewProjectile(Projectile.GetSource_None(), StarVector3, StarMove3, ModContent.ProjectileType<EnchantedWhipFallingStar>(), StarDamage, 0, Main.myPlayer);

                    }
                }
                if (markedByNightsCracker)
                {
                    int WhipDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(NightsCracker.BaseDamage);
                    float SearingLashDamageScaling = modPlayerProjectileOwner.NightsCrackerStacks * 8f * 0.01f;
                    if (markedBySearingLash)
                    {
                        SearingLashDamageScaling /= 2f;
                    }
                    if (markedByTerraFall)
                    {
                        SearingLashDamageScaling /= 2f;
                    }
                    SummonTagFlatDamage += (projectile.damage + SummonTagFlatDamage) * SearingLashDamageScaling * WhipDamage * 0.01f;
                }
                if (markedByPolarisLeash)
                {
                    int StarDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(PolarisLeash.BaseDamage * PolarisLeash.StarDamageScaling / 100f);
                    Vector2 StarVector1 = new Vector2(-640, -800) + npc.Center;
                    Vector2 StarVector2 = new Vector2(640, -800) + npc.Center;
                    Vector2 StarVector3 = new Vector2(0, -800) + npc.Center;
                    Vector2 StarMove1 = new Vector2(+32, 40);
                    Vector2 StarMove2 = new Vector2(-32, 40);
                    Vector2 StarMove3 = new Vector2(0, 40);
                    if (Main.rand.NextBool(3))
                    {
                        Projectile.NewProjectile(Projectile.GetSource_None(), StarVector1, StarMove1, ModContent.ProjectileType<PolarisLeashFallingStar>(), StarDamage, 0, Main.myPlayer);
                    }
                    else if (Main.rand.NextBool(3))
                    {
                        Projectile.NewProjectile(Projectile.GetSource_None(), StarVector2, StarMove2, ModContent.ProjectileType<PolarisLeashFallingStar>(), StarDamage, 0, Main.myPlayer);
                    }
                    else
                    {
                        Projectile.NewProjectile(Projectile.GetSource_None(), StarVector3, StarMove3, ModContent.ProjectileType<PolarisLeashFallingStar>(), StarDamage, 0, Main.myPlayer);
                    }
                }
                if (markedBySearingLash)
                {
                    int WhipDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(SearingLash.BaseDamage);
                    SummonTagFlatDamage += (projectile.damage + SummonTagFlatDamage) * 0.66f * WhipDamage * 0.01f;
                }
                if (markedByTerraFall)
                {
                    int WhipDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(TerraFall.BaseDamage);
                    float SearingLashDamageScaling = modPlayerProjectileOwner.TerraFallStacks * 2f * 0.01f;
                    if (markedBySearingLash)
                    {
                        SearingLashDamageScaling /= 2f;
                    }
                    if (markedByNightsCracker)
                    {
                        SearingLashDamageScaling /= 2f;
                    }
                    SummonTagFlatDamage += (int)((projectile.damage + SummonTagFlatDamage) * SearingLashDamageScaling * WhipDamage * 0.01f);
                }
                #endregion
                #region Vanilla Whip Special Effects
                if (markedByFirecracker)
                {
                    int buffIndex = 0;
                    Projectile.NewProjectile(projectile.GetSource_FromThis(), npc.Center, Vector2.Zero, ProjectileID.FireWhipProj, 0, 0f, projectile.owner);
                    foreach (int buffType in npc.buffType)
                    {
                        if (buffType == ModContent.BuffType<FirecrackerDebuff>())
                        {
                            npc.DelBuff(buffIndex);
                        }
                        buffIndex++;
                    }
                }
                if (markedByDarkHarvest)
                {
                    int num6 = Projectile.NewProjectile(projectile.GetSource_FromThis(), npc.Center, Vector2.Zero, ProjectileID.ScytheWhipProj, (int)(10f * SummonTagDamageMultiplier), 0f, projectile.owner);
                    Main.projectile[num6].localNPCImmunity[npc.netID] = -1;
                    Projectile.EmitBlackLightningParticles(npc);
                }
                if (markedByKaleidoscope)
                {
                    ParticleOrchestrator.RequestParticleSpawn(clientOnly: false, ParticleOrchestraType.RainbowRodHit, new ParticleOrchestraSettings
                    {
                        PositionInWorld = npc.Center
                    });
                }
                #endregion
                #region Runeterra effects
                if (Scorched || Shocked || Sunburnt)
                {
                    BaseSummonTagCriticalStrikeChance += 10f;
                }
                #endregion

                modifiers.FlatBonusDamage += SummonTagFlatDamage * ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type] * modPlayerProjectileOwner.SummonTagStrength;
                modifiers.ScalingBonusDamage += SummonTagScalingDamage * ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type] * modPlayerProjectileOwner.SummonTagStrength;
                modifiers.ArmorPenetration += SummonTagArmorPenetration * modPlayerProjectileOwner.SummonTagStrength;
                SummonTagCriticalStrikeChance = (BaseSummonTagCriticalStrikeChance * (1f + (projectileOwner.GetTotalCritChance(DamageClass.Summon) / 100f)));
                int critLevel = (int)(Math.Floor(SummonTagCriticalStrikeChance / 100f));
                if (critLevel >= 1)
                {
                    modifiers.SetCrit();
                }
                if (critLevel > 1)
                {
                    for (int i = 1; i < critLevel; i++)
                    {
                        modifiers.CritDamage *= 2;
                    }
                }
                if (Main.rand.Next(1, 101) <= SummonTagCriticalStrikeChance - (100 * critLevel))
                {
                    modifiers.SetCrit();
                    modifiers.CritDamage *= 2;
                }

            }
            if (markedByDragoonLash && (projectile.IsMinionOrSentryRelated || ProjectileID.Sets.IsAWhip[projectile.type])) //has to be outside of the main if since this is supposed to also be procced on whip-hit
            {
                int WhipDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(DragoonLash.BaseDamage);
                if (DragoonLashFireBreathTimer >= 1)
                {
                    Projectile.NewProjectile(Projectile.GetSource_None(), projectileOwner.Center, (npc.Center - projectileOwner.Center) * 0.1f, ProjectileID.Flamelash, WhipDamage, 1f, Main.myPlayer);
                    DragoonLashFireBreathTimer = 0;
                }
            }
            #endregion

            #region BotC Whip Debuff Damage Scaling
            int WhipDebuffCounter = 0;
            if (projectile.IsMinionOrSentryRelated && Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse &&
                  projectile.type != ProjectileID.DD2BallistraProj && projectile.type != ProjectileID.DD2ExplosiveTrapT1Explosion && projectile.type != ProjectileID.DD2ExplosiveTrapT2Explosion
                && projectile.type != ProjectileID.DD2ExplosiveTrapT3Explosion && projectile.type != ProjectileID.DD2FlameBurstTowerT1Shot && projectile.type != ProjectileID.DD2FlameBurstTowerT2Shot
                && projectile.type != ProjectileID.DD2FlameBurstTowerT3Shot | projectile.type != ProjectileID.DD2LightningAuraT1 && projectile.type != ProjectileID.DD2LightningAuraT2
                && projectile.type != ProjectileID.DD2LightningAuraT3 && projectile.type != ProjectileID.HoundiusShootiusFireball && projectile.type != ProjectileID.SpiderEgg && projectile.type != ProjectileID.BabySpider
                && projectile.type != ProjectileID.FrostBlastFriendly && projectile.type != ProjectileID.MoonlordTurretLaser && projectile.type != ProjectileID.RainbowCrystalExplosion
                && projectile.type != ModContent.ProjectileType<GaleForceProjectile>())
            {
                foreach (int buffType in npc.buffType)
                {
                    if (BuffID.Sets.IsAnNPCWhipDebuff[buffType])
                    {
                        WhipDebuffCounter++;
                    }
                }
                if (markedBySearingLash && modPlayerProjectileOwner.SearingLashStacks >= 4f)
                {
                    WhipDebuffCounter++;
                }
                if (markedByNightsCracker && modPlayerProjectileOwner.NightsCrackerStacks >= 4f)
                {
                    WhipDebuffCounter++;
                }
                if (markedByTerraFall && modPlayerProjectileOwner.TerraFallStacks >= 4f)
                {
                    WhipDebuffCounter++;
                }
                if (npc.HasBuff(ModContent.BuffType<ScorchingDebuff>()))
                {
                    WhipDebuffCounter--;
                }
                if (npc.HasBuff(ModContent.BuffType<ShockedDebuff>()))
                {
                    WhipDebuffCounter--;
                }
                if (npc.HasBuff(ModContent.BuffType<SunburnDebuff>()))
                {
                    WhipDebuffCounter--;
                }
                if (WhipDebuffCounter > Darksign.WhipDebuffCounterCap)
                {
                    WhipDebuffCounter = Darksign.WhipDebuffCounterCap;
                }
                modifiers.FinalDamage *= 0.1f + (WhipDebuffCounter * Darksign.MinionDamageReductionDecrease / 100f);
            }
            #endregion
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
        }

        public override void ModifyGlobalLoot(GlobalLoot globalLoot) {

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                List<IItemDropRule> ruleList = globalLoot.Get();
                for (int i = 0; i < ruleList.Count; i++)
                {
                    string s = ruleList[i].ToString();
                    if (s == "Terraria.GameContent.ItemDropRules.MechBossSpawnersDropRule")
                    {
                        globalLoot.Remove(ruleList[i]);
                    }
                }                
            }
        }


        public override bool PreKill(NPC npc)
        {
            for(int i = 0; i < tsorcRevamp.BannedItems.Count; i++)
            {
                NPCLoader.blockLoot.Add(tsorcRevamp.BannedItems[i]);
            }

            return base.PreKill(npc);
        }
        // public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        // {           

        //     if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
        //     {

        //         List<IItemDropRule> ruleList = npcLoot.Get();

        //         for (int i = 0; i < ruleList.Count; i++)
        //         {
        //             if (ruleList[i] is CommonDrop rodRule && rodRule.itemId == ItemID.RodofDiscord)
        //             {
        //                 //Hacky
        //                 rodRule.chanceNumerator = 0;
        //             }


        //             /* 
        //             if (ruleList[i] is OneFromOptionsDropRule bossRule)
        //             {
        //                 for(int j = 0; j < bossRule.dropIds.Length; j++)
        //                 {
        //                     if (bossRule.dropIds[j] == ItemID.Picksaw)
        //                     {
        //                         //You have to recreate the rule from scratch. I'm just gonna disable the picksaw in CanUseItem instead.
        //                         OneFromOptionsDropRule newRule = new OneFromOptionsDropRule(1, 1, ItemID.Stynger, ItemID.GolemFist, ItemID.GolemMask);

        //                     }
        //                     if (bossRule.dropIds[j] == ItemID.SlimeHook)
        //                     {
        //                         bossRule.RemoveDrop(ItemID.SlimeHook); //No equivalent to this exists. How do you remove an item from a drop list?
        //                     }
        //                 }
        //                 sawRule.chanceNumerator = 0;
        //             }*/
        //         }
        //     }
        // }

        //TODO
        /*
        public override bool PreNPCLoot(NPC npc)
        {
            Player player = Main.LocalPlayer;


            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                

                
            }

            if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2)
            {
                if (Main.rand.NextBool(2))
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.StaminaDroplet>(), 1);
                }

                if (Main.rand.NextBool(12))
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.StaminaDroplet>(), 1);
                }
            }

            return base.PreNPCLoot(npc);
        }
        */

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (Ignited)
            {
                int DoTPerS = 20;
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (Venomized)
            {
                int DoTPerS = (int)lastHitPlayerRanger.GetTotalDamage(DamageClass.Ranged).ApplyTo((float)ToxicShot.BaseDamage) + (int)(lastHitPlayerRanger.GetTotalCritChance(DamageClass.Ranged) / 100f * (float)ToxicShot.BaseDamage);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Electrified)
            {
                int DoTPerS = (int)lastHitPlayerRanger.GetTotalDamage(DamageClass.Ranged).ApplyTo((float)AlienRifle.BaseDamage) + (int)(lastHitPlayerRanger.GetTotalCritChance(DamageClass.Ranged) / 100f * (float)AlienRifle.BaseDamage);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Irradiated)
            {
                int DoTPerS = (int)lastHitPlayerRanger.GetTotalDamage(DamageClass.Ranged).ApplyTo((float)OmegaSquadRifle.BaseDamage) + (int)(lastHitPlayerRanger.GetTotalCritChance(DamageClass.Ranged) / 100f * (float)OmegaSquadRifle.BaseDamage);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (IrradiatedByShroom)
            {
                int DoTPerS = (int)lastHitPlayerRanger.GetTotalDamage(DamageClass.Ranged).ApplyTo((float)OmegaSquadRifle.BaseDamage * 2.28f) + (int)(lastHitPlayerRanger.GetTotalCritChance(DamageClass.Ranged) / 100f * (float)OmegaSquadRifle.BaseDamage * 2.28f);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Scorched)
            {
                int DoTPerS = (int)lastHitPlayerSummoner.GetTotalDamage(DamageClass.Summon).ApplyTo(10);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Shocked)
            {
                int DoTPerS = (int)lastHitPlayerSummoner.GetTotalDamage(DamageClass.Summon).ApplyTo(30);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Sunburnt)
            {
                int DoTPerS = (int)lastHitPlayerSummoner.GetTotalDamage(DamageClass.Summon).ApplyTo(110);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (DarkInferno)
            {
                int DoTPerS = 20;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;

                var N = npc;
                for (int j = 0; j < 6; j++)
                {
                    int dust = Dust.NewDust(N.position, N.width / 2, N.height / 2, 54, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(N.position, N.width / 2, N.height / 2, 58, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust2].noGravity = true;
                }
            }

            if (PhazonCorruption)
            {
                int DoTPerS = 15;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage = DoTPerS;

                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 185, (npc.velocity.X * 0.2f), npc.velocity.Y * 0.2f, 100, default, 1f);
                Main.dust[dust].noGravity = true;

                int dust2 = Dust.NewDust(npc.position, npc.width, npc.height, DustID.FireworkFountain_Blue, (npc.velocity.X * 0.2f), npc.velocity.Y * 0.2f, 100, default, 1f);
                Main.dust[dust2].noGravity = true;
            }

            if (CrimsonBurn)
            {
                int DoTPerS = 12;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= DoTPerS * 2;
                if (Main.hardMode) npc.lifeRegen -= DoTPerS * 2 * 2; //this is additive to the one in pre-hm.....so it's tripled, not doubled
                damage += DoTPerS;
                if (Main.hardMode) damage += DoTPerS * 2;
            }

            if (ToxicCatDrain)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }

                int ToxicCatShotCount = 0;

                for (int i = 0; i < 1000; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.ToxicCatShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI)
                    {
                        ToxicCatShotCount++;
                    }
                }
                if (ToxicCatShotCount >= 4)
                { //this is to make it worth the players time stickying more than 3 times
                    npc.lifeRegen -= ToxicCatShotCount * 3 * 2; //Use 1st N for damage, second N can be used to make it tick faster.
                    if (damage < ToxicCatShotCount * 1)
                    {
                        damage = ToxicCatShotCount * 1;
                    }
                }
                else
                {
                    npc.lifeRegen -= ToxicCatShotCount * 2 * 2;
                    if (damage < ToxicCatShotCount * 1)
                    {
                        damage = ToxicCatShotCount * 1;
                    }
                }
            }

            if (ViruCatDrain)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }

                int ViruCatShotCount = 0;

                for (int i = 0; i < 1000; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.VirulentCatShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI)
                    {
                        ViruCatShotCount++;
                    }
                }
                if (ViruCatShotCount >= 4)
                {
                    npc.lifeRegen -= ViruCatShotCount * 3 * 5; //I use 1st N for damage, second N can be used to make it tick faster.
                    if (damage < ViruCatShotCount * 1)
                    {
                        damage = ViruCatShotCount * 1;
                    }
                }
                else
                {
                    npc.lifeRegen -= ViruCatShotCount * 2 * 5;
                    if (damage < ViruCatShotCount * 1)
                    {
                        damage = ViruCatShotCount * 1;
                    }
                }
            }

            if (BiohazardDrain)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }

                int BiohazardShotCount = 0;

                for (int i = 0; i < 1000; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.BiohazardShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI)
                    {
                        BiohazardShotCount++;
                    }
                }
                if (BiohazardShotCount >= 4)
                {
                    npc.lifeRegen -= BiohazardShotCount * 9 * 4; //I use 1st N for damage, second N can be used to make it tick faster.
                    if (damage < BiohazardShotCount * 1)
                    {
                        damage = BiohazardShotCount * 1;
                    }
                }
                else
                {
                    npc.lifeRegen -= BiohazardShotCount * 6 * 4;
                    if (damage < BiohazardShotCount * 1)
                    {
                        damage = BiohazardShotCount * 1;
                    }
                }
            }

            if (ElectrocutedEffect)
            {
                int DoTPerS = 6;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (PolarisElectrocutedEffect)
            {
                int DoTPerS = 35;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (CrescentMoonlight)
            {
                int DoTPerS = 6;
                if (npc.lifeRegen > 0)
                { 
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
                if (Main.hardMode) 
                {
                    npc.lifeRegen -= DoTPerS * 2;
                    damage += DoTPerS;
                }
            }
        }

        public override void ModifyShop(NPCShop shop) {

            switch (shop.NpcType) {
                case NPCID.Merchant: {
                    shop.Add(ItemID.Bottle);
                    break;
                }
                case NPCID.DyeTrader:
                    {
                        //Basic dyes (most others can be crafted from a combination of these)
                        int price = 5;
                        shop.Add(new Item(ItemID.RedDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.OrangeDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.YellowDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.LimeDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.GreenDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.TealDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.CyanDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.SkyBlueDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.BlueDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.PurpleDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.VioletDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.PinkDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.BlackDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        price = 25;

                        //Special Dyes (Aka the cool ones)
                        shop.Add(new Item(ItemID.FogboundDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.MushroomDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.PurpleOozeDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.ReflectiveDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.ReflectiveObsidianDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.ShadowDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.MirageDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.TwilightDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.BurningHadesDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.ShadowflameHadesDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.PhaseDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.ShiftingSandsDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.GelDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.LivingFlameDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.LivingRainbowDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.LivingOceanDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.MidnightRainbowDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });


                        break;
                    }
                case NPCID.SkeletonMerchant: {
                    shop.Add(new Item(ModContent.ItemType<Firebomb>()) {
                        shopCustomPrice = 5,
                        shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                    });

                    shop.Add(new Item(ModContent.ItemType<EternalCrystal>()) {
                        shopCustomPrice = 2000,
                        shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                    });
                    break;
                }
                case NPCID.GoblinTinkerer: {
                    shop.Add(new Item(ModContent.ItemType<Pulsar>()) {
                        shopCustomPrice = 800,
                        shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                    });


                    shop.Add(new Item(ModContent.ItemType<ToxicCatalyzer>()) {
                        shopCustomPrice = 800,
                        shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                    });
                    break;
                    }
                case NPCID.Mechanic:
                    {
                        foreach (NPCShop.Entry item in shop.ActiveEntries)
                        {
                            item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                        }
                        break;
                    }
                case NPCID.Cyborg:
                    {
                        foreach (NPCShop.Entry item in shop.ActiveEntries)
                        {
                            if (item.Item.type == ItemID.DryRocket)
                            {
                                item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                            }
                            if (item.Item.type == ItemID.WetRocket)
                            {
                                item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                            }
                            if (item.Item.type == ItemID.LavaRocket)
                            {
                                item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                            }
                            if (item.Item.type == ItemID.HoneyRocket)
                            {
                                item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                            }
                        }
                        break;
                    }
                default:break;
            }
        }
        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (!CrystalNunchakuProc && !(CrystalNunchakuStacks == 0) && markedByCrystalNunchaku)
            {
                CrystalNunchakuStacks -= 1;
            }
            if (CrystalNunchakuProc && markedByCrystalNunchaku)
            {
                player.GetModPlayer<tsorcRevampPlayer>().CrystalNunchakuDefenseDamage = 15f - (CrystalNunchakuStacks * 1.5f);
                player.AddBuff(ModContent.BuffType<CrystalShield>(), 4 * 60);
            }
        }
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[projectile.owner];
            var modPlayer = Main.player[projectile.owner].GetModPlayer<tsorcRevampPlayer>();
            #region Vanilla Whips applying their modded counterparts
            if (projectile.type == ProjectileID.BlandWhip)
            {
                npc.AddBuff(ModContent.BuffType<LeatherWhipDebuff>(), (int)(4 * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.ThornWhip)
            {
                npc.AddBuff(ModContent.BuffType<SnapthornDebuff>(), (int)(4 * 60 * modPlayer.SummonTagDuration));
                player.AddBuff(BuffID.ThornWhipPlayerBuff, (int)(4 * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.BoneWhip)
            {
                npc.AddBuff(ModContent.BuffType<SpinalTapDebuff>(), (int)(4 * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.FireWhip)
            {
                npc.AddBuff(ModContent.BuffType<FirecrackerDebuff>(), (int)(4 * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.CoolWhip)
            {
                npc.AddBuff(ModContent.BuffType<CoolWhipDebuff>(), (int)(4 * 60 * modPlayer.SummonTagDuration));
                player.AddBuff(BuffID.CoolWhipPlayerBuff, (int)(4 * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.SwordWhip)
            {
                npc.AddBuff(ModContent.BuffType<DurendalDebuff>(), (int)(4 * 60 * modPlayer.SummonTagDuration));
                player.AddBuff(BuffID.SwordWhipPlayerBuff, (int)(4 * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.MaceWhip)
            {
                npc.AddBuff(ModContent.BuffType<MorningStarDebuff>(), (int)(4 * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.ScytheWhip)
            {
                npc.AddBuff(ModContent.BuffType<DarkHarvestDebuff>(), (int)(4 * 60 * modPlayer.SummonTagDuration));
                player.AddBuff(BuffID.SwordWhipPlayerBuff, (int)(4 * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.RainbowWhip)
            {
                npc.AddBuff(ModContent.BuffType<KaleidoscopeDebuff>(), (int)(4 * 60 * modPlayer.SummonTagDuration));
            }
            #endregion
            #region Crystal Nunchaku effects
            if (!CrystalNunchakuProc && !(CrystalNunchakuStacks == 0) && !projectile.npcProj && !projectile.trap && markedByCrystalNunchaku && projectile.type != ModContent.ProjectileType<CrystalNunchakuProjectile>())
            {
                CrystalNunchakuStacks -= 1;
            }
            if (CrystalNunchakuProc && !projectile.npcProj && !projectile.trap && markedByCrystalNunchaku)
            {
                Main.player[projectile.owner].GetModPlayer<tsorcRevampPlayer>().CrystalNunchakuDefenseDamage = 15f - (CrystalNunchakuStacks * 1.5f);
                Main.player[projectile.owner].AddBuff(ModContent.BuffType<CrystalShield>(), 4 * 60);
            }
            #endregion
            if (hit.DamageType == DamageClass.Ranged && damageDone > npc.life && modPlayer.BoneRing)
            {
                Projectile.NewProjectile(projectile.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(npc.Hitbox), projectile.velocity, ProjectileID.Bone, projectile.damage / 3, projectile.knockBack, projectile.owner);
                Projectile.NewProjectile(projectile.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(npc.Hitbox), projectile.velocity, ProjectileID.Bone, projectile.damage / 3, projectile.knockBack, projectile.owner);
                Projectile.NewProjectile(projectile.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(npc.Hitbox), projectile.velocity, ProjectileID.Bone, projectile.damage / 3, projectile.knockBack, projectile.owner);
                Projectile.NewProjectile(projectile.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(npc.Hitbox), projectile.velocity, ProjectileID.Bone, projectile.damage / 3, projectile.knockBack, projectile.owner);
            }
                if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ToxicCatDrain && (projectile.type == ModContent.ProjectileType<Projectiles.ToxicCatDetonator>() || projectile.type == ModContent.ProjectileType<Projectiles.ToxicCatExplosion>()))
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetToxicCatBlobs = true;
                int tags;

                bool shockwaveCreated = false;
                for (int i = 0; i < 1000; i++)
                {
                    tags = 0;
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.ToxicCatShot>() && p.ai[0] == 1f && p.timeLeft > 2 && p.ai[1] == npc.whoAmI)
                    {
                        for (int q = 0; q < 1000; q++)
                        {
                            Projectile ñ = Main.projectile[q];
                            if (ñ.active && ñ.type == ModContent.ProjectileType<Projectiles.ToxicCatShot>() && ñ.ai[0] == 1f && ñ.ai[1] == npc.whoAmI)
                            {
                                tags++;
                            }
                        }
                        float volume = (tags * 0.3f) + 0.7f;
                        float pitch = tags * 0.08f;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = volume, Pitch = -pitch}, projectile.Center);

                        p.timeLeft = 2;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), p.Center, npc.velocity, ModContent.ProjectileType<Projectiles.ToxicCatExplosion>(), (int)(projectile.damage * 1.8f), projectile.knockBack, projectile.owner, tags, 0);
                        }
                        int buffindex = npc.FindBuffIndex(ModContent.BuffType<Buffs.ToxicCatDrain>());

                        if (buffindex != -1)
                        {
                            npc.DelBuff(buffindex);
                        }
                    }

                    if (tags > 0 && !shockwaveCreated)
                    {
                        shockwaveCreated = true;
                        if (projectile.type == ModContent.ProjectileType<Projectiles.ToxicCatDetonator>())
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 300 * (tags / 12f), 45 * (tags / 12f));
                        }
                    }

                }
            }

            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ViruCatDrain && (projectile.type == ModContent.ProjectileType<Projectiles.VirulentCatDetonator>() || projectile.type == ModContent.ProjectileType<Projectiles.VirulentCatExplosion>()))
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetViruCatBlobs = true;
                int tags;

                bool shockwaveCreated = false;
                for (int i = 0; i < 1000; i++)
                {
                    tags = 0;
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.VirulentCatShot>() && p.ai[0] == 1f && p.timeLeft > 2 && p.ai[1] == npc.whoAmI)
                    {
                        for (int q = 0; q < 1000; q++)
                        {
                            Projectile ñ = Main.projectile[q];
                            if (ñ.active && ñ.type == ModContent.ProjectileType<Projectiles.VirulentCatShot>() && ñ.ai[0] == 1f && ñ.ai[1] == npc.whoAmI)
                            {
                                tags++;
                            }
                        }
                        float volume = (tags * 0.3f) + 0.7f;
                        float pitch = tags * 0.08f;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = volume, Pitch = -pitch }, projectile.Center);

                        //Main.NewText(pitch);
                        p.timeLeft = 2;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), p.Center, npc.velocity, ModContent.ProjectileType<Projectiles.VirulentCatExplosion>(), (projectile.damage * 2), projectile.knockBack, projectile.owner, tags, 0);
                        }
                        int buffindex = npc.FindBuffIndex(ModContent.BuffType<Buffs.ViruCatDrain>());

                        if (buffindex != -1)
                        {
                            npc.DelBuff(buffindex);
                        }
                    }
                    if (tags > 0 && !shockwaveCreated)
                    {
                        shockwaveCreated = true;
                        if (projectile.type == ModContent.ProjectileType<Projectiles.VirulentCatDetonator>())
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 400 * (tags / 12f), 50 * (tags / 12f));
                            }
                        }
                    }
                }
            }

            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().BiohazardDrain && (projectile.type == ModContent.ProjectileType<Projectiles.BiohazardDetonator>() || projectile.type == ModContent.ProjectileType<Projectiles.BiohazardExplosion>()))
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetBiohazardBlobs = true;
                int tags;


                bool shockwaveCreated = false;
                for (int i = 0; i < 1000; i++)
                {
                    tags = 0;
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.BiohazardShot>() && p.ai[0] == 1f && p.timeLeft > 2 && p.ai[1] == npc.whoAmI)
                    {
                        for (int q = 0; q < 1000; q++)
                        {
                            Projectile ñ = Main.projectile[q];
                            if (ñ.active && ñ.type == ModContent.ProjectileType<Projectiles.BiohazardShot>() && ñ.ai[0] == 1f && ñ.ai[1] == npc.whoAmI)
                            {
                                tags++;
                            }
                        }
                        float volume = (tags * 0.3f) + 0.7f;
                        float pitch = tags * 0.08f;

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = volume, Pitch = -pitch }, projectile.Center);

                        p.timeLeft = 2;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), p.Center, npc.velocity, ModContent.ProjectileType<Projectiles.BiohazardExplosion>(), (projectile.damage * 2), projectile.knockBack, projectile.owner, tags, 0);
                        }
                        int buffindex = npc.FindBuffIndex(ModContent.BuffType<Buffs.BiohazardDrain>());

                        if (buffindex != -1)
                        {
                            npc.DelBuff(buffindex);
                        }
                    }
                    if (tags > 0 && !shockwaveCreated)
                    {
                        shockwaveCreated = true;
                        if (projectile.type == ModContent.ProjectileType<Projectiles.BiohazardDetonator>())
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 500 * (tags / 12f), 60 * (tags / 12f));
                        }
                    }
                }
            }
        }

        public override void SetDefaults(NPC npc)
        {
            //Only mess with it if it's one of our bosses
            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp"))
            {
                if (npc.boss && !Main.expertMode)
                {
                    //Bosses are 1.3x weaker in normal mode
                    //Doing it like this means we can simply set npc.lifeMax to exactly value we want their expert mode health to be, saving us a headache.
                    //Rounded, because casting to an int truncates it which causes slight inaccuracies later on
                    npc.lifeMax = (int)Math.Round(npc.lifeMax / 1.3f);
                }
                else
                {
                    if (npc.ModNPC.GetType().Namespace.Contains("SuperHardMode"))
                    {
                        base.SetDefaults(npc);
                        npc.lifeMax = (int)(tsorcRevampWorld.SHMScale * npc.lifeMax);
                        npc.defense = (int)(tsorcRevampWorld.SubtleSHMScale * npc.defense);
                        npc.damage = (int)(tsorcRevampWorld.SubtleSHMScale * npc.damage);
                    }
                }
            }
        }

        //This method lets us scale the stats of NPC's in expert mode.
        public override void ApplyDifficultyAndPlayerScaling(NPC npc, int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp") && npc.boss)
            {
                //Counter expert mode automatic scaling
                npc.lifeMax = (int)Math.Round(npc.lifeMax / 2f);

                //Add 70% to the boss's health per extra player
                //npc.lifeMax = (int)Math.Round(npc.lifeMax * (1f + (0.7f * ((float)bossLifeScale - 1f))));

                //Add our scaling
                npc.lifeMax = (int)(npc.lifeMax * (1f + ((numPlayers - 1f) * .5f)));
                return;
            }
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (ElectrocutedEffect)
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .4f);
                Main.dust[dust].noGravity = true;
            }

            if (PolarisElectrocutedEffect)
            {
                for (int i = 0; i < 2; i++)
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = false;
                }
            }

            if (ToxicCatDrain)
            {
                drawColor = Color.LimeGreen;
                Lighting.AddLight(npc.position, 0.125f, 0.23f, 0.065f);

                if (Main.rand.NextBool(10))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 74, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .8f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (ViruCatDrain)
            {
                drawColor = Color.LimeGreen;
                Lighting.AddLight(npc.position, 0.125f, 0.23f, 0.065f);

                if (Main.rand.NextBool(6))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 74, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .8f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (BiohazardDrain)
            {
                drawColor = Color.LimeGreen;
                Lighting.AddLight(npc.position, 0.125f, 0.23f, 0.065f);

                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 74, npc.velocity.X * 0f, -2f, 100, default(Color), .8f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (CrescentMoonlight)
            {
                drawColor = Color.White;

                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 164, npc.velocity.X * 0f, 0f, 100, default(Color), 1f); ;
                Main.dust[dust].velocity *= 0f;
                Main.dust[dust].noGravity = false;
                Main.dust[dust].velocity += npc.velocity;
            }

            if (Soulstruck)
            {
                Lighting.AddLight(npc.Center, .4f, .4f, .850f);

                if (Main.rand.NextBool(6))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 68, 0, 0, 30, default(Color), 1.25f);
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                }
            }
        }

        //AIWorm(NPC npc, int headType, int[] bodyTypes, int tailType, int wormLength = 3, float partDistanceAddon = 0f, float maxSpeed = 8f, float gravityResist = 0.07f, bool fly = false, bool split = false, bool ignoreTiles = false, bool spawnTileDust = true, bool soundEffects = true)

        /*
                 * A cleaned up (and edited) copy of Worm AI.

                 * headType/tailType : the type of the head, body, and tail of the worm, respectively.
                 * bodyTypes: An array of the body types. NOTE: Array must at least be as long as the body length - 2!
                 * wormLength : the total length of the worm.
                 * partDistanceAddon : and addon to the distance between parts of the worm.
                 * maxSpeed : the fastest the worm can accellerate to.
                 * gravityResist : how much resistance on the X axis the worm has when it is out of tiles. was 0.07f
                    //higher values cause the wvyern's 'gravity' towards the player to increase
                    //lower values basically == longer passes
                 * fly : If true, acts like a Wvyern.
                 * split : If true, worm will split when parts of it die.
                 * ignoreTiles : If true, Allows the worm to move outside of tiles as if it were in them. (ignored if fly is true)
                 * spawnTileDust : If true, worm will spawn tile dust when it digs through tiles.
                 * soundEffects : If true, will produce a digging sound when nearing the player.

                 * that array works like this: say you have a worm that is 5 segments long
                 * you would make the body array have 3 ids in it and they would go in order they would appear on the worm from the head
                 * the array *must* be 2 less than the total length of the worm or it will not work
        */


        //ai[0] = ID of piece behind it
        //ai[1] = ID of piece ahead of it
        //ai[2] = Relates to length of worms
        //ai[3] = ID of worm head
        //npc.localAI[0] = place in the queue to sync itself, used to spread the syncing out
        #region AIWorm
        public static void AIWorm(NPC npc, int headType, int[] bodyTypes, int tailType, int wormLength = 3, float partDistanceAddon = 0f, float maxSpeed = 8f, float gravityResist = 0.07f, bool fly = false, bool split = false, bool ignoreTiles = false, bool spawnTileDust = true, bool soundEffects = true)
        {
            //Flip sprite so it's always facing the right way            
            if (npc.type == headType)
            {
                if (npc.velocity.X < 0f || Math.Abs(npc.velocity.X) < 0.1f)
                {
                    npc.spriteDirection = 1;
                }
                else if (npc.velocity.X > 0f)
                {
                    npc.spriteDirection = -1;
                }
            }
            else
            {
                if (npc.position.X > Main.npc[(int)npc.ai[1]].position.X || Math.Abs(npc.position.X - Main.npc[(int)npc.ai[1]].position.X) < 0.1f)
                {
                    npc.spriteDirection = 1;
                }
                if (npc.position.X < Main.npc[(int)npc.ai[1]].position.X)
                {
                    npc.spriteDirection = -1;
                }
            }
            //If it splits, ignore the health of the head and keep its own healthbar
            //If it doesn't, set its real health to the health of the head
            if (split)
            {
                npc.realLife = -1;
            }
            else if (npc.ai[3] > 0f)
            {
                npc.realLife = (int)npc.ai[3];
            }

            //Don't do *any* spawning if we're a multiplayer client
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Tick down the sync counter, and if it hits 1 then sync them.
                if (npc.localAI[0] == 1 && npc.localAI[0] > 0)
                {
                    npc.netUpdate = true;
                    npc.localAI[0] = -1;
                }
                else
                {
                    npc.localAI[0]--;
                }

                //And the piece behind it does not exist
                if (npc.ai[0] == 0f)
                {
                    //If we're a head and flying type, spawn the rest of the worm
                    if (fly && npc.type == headType)
                    {
                        //Set its the head's head id, actual health, and ID to itself
                        npc.ai[3] = (float)npc.whoAmI;
                        npc.realLife = npc.whoAmI;

                        //Store the head's index in npcID. This will get updated as we go through each piece.
                        int npcID = npc.whoAmI;

                        //Spawn the rest of the worm. For each piece...
                        for (int m = 0; m < wormLength - 1; m++)
                        {
                            //If we're the last piece, make the worm type the tail. If not, make it the body type corrosponding to its position on the list
                            int npcType = (m == wormLength - 2 ? tailType : bodyTypes[m]);

                            //Spawn the npc
                            int newnpcID = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y), npcType, npc.whoAmI);

                            //Set the new piece's Head ID to the head
                            Main.npc[newnpcID].ai[3] = (float)npc.whoAmI;

                            //Set its real health to the head's
                            Main.npc[newnpcID].realLife = npc.whoAmI;

                            //Set its "previous piece id" to the id of the previous spawned piece
                            Main.npc[newnpcID].ai[1] = (float)npcID;

                            //Set the previous piece's "next piece id" to the id of the newly spawned piece
                            Main.npc[npcID].ai[0] = (float)newnpcID;

                            //Set their localAI to a number that grows as each segment is spawned
                            Main.npc[npcID].localAI[0] = 2 + (m * 2);

                            //Ask the server to sync it right away (might be triggering the net spam limit and causing the issues!!)
                            //NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, newnpcID);

                            //Store the current piece's ID in npcID, so that the next piece can use it
                            npcID = newnpcID;
                        }
                        //Immediately update
                        npc.netUpdate = true;
                    }
                    //If we're a grounded type and not the tail, just spawn the piece behind itself
                    else if (npc.type != tailType)
                    {
                        if (npc.type == headType)
                        {
                            if (!split)
                            {
                                npc.ai[3] = (float)npc.whoAmI;
                                npc.realLife = npc.whoAmI;
                            }
                            npc.ai[2] = (float)(wormLength - 2);
                            int nextPiece = (bodyTypes.Length == 0 ? tailType : bodyTypes[0]);
                            npc.ai[0] = (float)NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y), nextPiece, npc.whoAmI);
                        }
                        else
                        if ((npc.type != headType && npc.type != tailType) && npc.ai[2] > 0f)
                        {
                            npc.ai[0] = (float)NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y), bodyTypes[wormLength - 3 - (int)npc.ai[2]], npc.whoAmI);
                        }
                        else
                        {
                            npc.ai[0] = (float)NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y), tailType, npc.whoAmI);
                        }
                        if (!split)
                        {
                            Main.npc[(int)npc.ai[0]].ai[3] = npc.ai[3];
                            Main.npc[(int)npc.ai[0]].realLife = npc.realLife;
                        }
                        Main.npc[(int)npc.ai[0]].ai[1] = (float)npc.whoAmI;
                        Main.npc[(int)npc.ai[0]].ai[2] = npc.ai[2] - 1f;
                        npc.netUpdate = true;
                    }
                }

                //if npc can split, check if pieces are dead and if so split.
                if (split)
                {
                    //If the piece in front and behind it are dead, then die too
                    if (!Main.npc[(int)npc.ai[1]].active && !Main.npc[(int)npc.ai[0]].active)
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);
                        npc.active = false;
                    }

                    //If it's a head and the piece behind it dies, then die
                    if (npc.type == headType && !Main.npc[(int)npc.ai[0]].active)
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);
                        npc.active = false;
                    }

                    //If it's a tail and the piece in front of it dies, then die
                    if (npc.type == tailType && !Main.npc[(int)npc.ai[1]].active)
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);
                        npc.active = false;
                    }

                    //If the piece isn't a head or tail, and the piece in front of it dies, then become a head
                    if ((npc.type != headType && npc.type != tailType) && !Main.npc[(int)npc.ai[1]].active)
                    {
                        npc.type = headType;
                        int npcID = npc.whoAmI;
                        float lifePercent = (float)npc.life / (float)npc.lifeMax;
                        float lastPiece = npc.ai[0];
                        npc.SetDefaults(npc.type);
                        npc.life = (int)((float)npc.lifeMax * lifePercent);
                        npc.ai[0] = lastPiece;
                        npc.netUpdate = true;
                        npc.whoAmI = npcID;
                    }

                    //If the piece isn't a head or tail, and the piece behind it dies, then become a head
                    else if ((npc.type != headType && npc.type != tailType) && !Main.npc[(int)npc.ai[0]].active)
                    {
                        npc.type = tailType;
                        int npcID = npc.whoAmI;
                        float lifePercent = (float)npc.life / (float)npc.lifeMax;
                        float lastPiece = npc.ai[1];
                        npc.SetDefaults(npc.type);
                        npc.life = (int)((float)npc.lifeMax * lifePercent);
                        npc.ai[1] = lastPiece;
                        npc.netUpdate = true;
                        npc.whoAmI = npcID;
                    }
                }

                //If it can't split, die if it is incomplete 
                else
                {
                    //If it's not a head and the piece in front of it is dead (or the wrong aiStyle, just in-case a new npc took its slot) then die
                    if (npc.type != headType && (!Main.npc[(int)npc.ai[1]].active || Main.npc[(int)npc.ai[1]].aiStyle != npc.aiStyle))
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);

                        npc.active = false;
                    }

                    //If it's not a tail and the piece behind it is dead then die
                    if (npc.type != tailType && (!Main.npc[(int)npc.ai[0]].active || Main.npc[(int)npc.ai[0]].aiStyle != npc.aiStyle))
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);

                        npc.active = false;
                    }
                }
                /**
                if (!npc.active && Main.netMode == NetmodeID.Server) 
                { 
                    NetMessage.SendData(28, -1, -1, "", npc.whoAmI, 1, 0f, 0f, -1); 
                }**/
            }
            int tileX = (int)(npc.position.X / 16f) - 1;
            int tileCenterX = (int)((npc.Center.X) / 16f) + 2;
            int tileY = (int)(npc.position.Y / 16f) - 1;
            int tileCenterY = (int)((npc.Center.Y) / 16f) + 2;
            if (tileX < 0) { tileX = 0; }
            if (tileCenterX > Main.maxTilesX) { tileCenterX = Main.maxTilesX; }
            if (tileY < 0) { tileY = 0; }
            if (tileCenterY > Main.maxTilesY) { tileCenterY = Main.maxTilesY; }
            bool canMove = false;
            if (fly || ignoreTiles) { canMove = true; }


            if (!canMove || spawnTileDust)
            {
                for (int tX = tileX; tX < tileCenterX; tX++)
                {
                    for (int tY = tileY; tY < tileCenterY; tY++)
                    {
                        if (Main.tile[tX, tY] != null && ((Main.tile[tX, tY].HasTile && (Main.tileSolid[(int)Main.tile[tX, tY].TileType] || (Main.tileSolidTop[(int)Main.tile[tX, tY].TileType] && Main.tile[tX, tY].TileFrameY == 0))) || Main.tile[tX, tY].LiquidAmount > 64))
                        {
                            Vector2 tPos;
                            tPos.X = (float)(tX * 16);
                            tPos.Y = (float)(tY * 16);
                            if (npc.position.X + (float)npc.width > tPos.X && npc.position.X < tPos.X + 16f && npc.position.Y + (float)npc.height > tPos.Y && npc.position.Y < tPos.Y + 16f)
                            {
                                canMove = true;
                                if (spawnTileDust && (Main.rand.Next(100)) == 0 && Main.tile[tX, tY].HasTile)
                                {
                                    WorldGen.KillTile(tX, tY, true, true, false);
                                }
                            }
                        }
                    }
                }
            }


            if (!canMove && npc.type == headType)
            {
                Rectangle rectangle = new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height);
                int playerCheckDistance = 1000;
                bool canMove2 = true;
                for (int m3 = 0; m3 < 255; m3++)
                {
                    if (Main.player[m3].active)
                    {
                        Rectangle rectangle2 = new Rectangle((int)Main.player[m3].position.X - playerCheckDistance, (int)Main.player[m3].position.Y - playerCheckDistance, playerCheckDistance * 2, playerCheckDistance * 2);
                        if (rectangle.Intersects(rectangle2))
                        {
                            canMove2 = false;
                            break;
                        }
                    }
                }
                if (canMove2) { canMove = true; }
            }



            Vector2 npcCenter = npc.Center;
            float playerCenterX = Main.player[npc.target].Center.X;
            float playerCenterY = Main.player[npc.target].Center.Y;
            playerCenterX = (float)((int)(playerCenterX / 16f) * 16); playerCenterY = (float)((int)(playerCenterY / 16f) * 16);
            npcCenter.X = (float)((int)(npcCenter.X / 16f) * 16); npcCenter.Y = (float)((int)(npcCenter.Y / 16f) * 16);
            playerCenterX -= npcCenter.X; playerCenterY -= npcCenter.Y;
            float dist = (float)Math.Sqrt((double)(playerCenterX * playerCenterX + playerCenterY * playerCenterY));
            if (npc.ai[1] > 0f && npc.ai[1] < (float)Main.npc.Length)
            {

                npcCenter = npc.Center;
                float offsetX = Main.npc[(int)npc.ai[1]].Center.X - npcCenter.X;
                float offsetY = Main.npc[(int)npc.ai[1]].Center.Y - npcCenter.Y;

                npc.rotation = (float)Math.Atan2((double)offsetY, (double)offsetX) + 1.57f;
                dist = (float)Math.Sqrt((double)(offsetX * offsetX + offsetY * offsetY));
                dist = (dist - (float)npc.width - (float)partDistanceAddon) / dist;
                offsetX *= dist;
                offsetY *= dist;
                npc.velocity = default(Vector2);
                npc.position.X += offsetX;
                npc.position.Y += offsetY;
            }
            else
            {
                if (!canMove)
                {
                    npc.velocity.Y += 0.11f;
                    if (npc.velocity.Y > maxSpeed) { npc.velocity.Y = maxSpeed; }
                    if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)maxSpeed * 0.4)
                    {
                        if (npc.velocity.X < 0f) { npc.velocity.X -= gravityResist * 1.1f; } else { npc.velocity.X += gravityResist * 1.1f; }
                    }
                    else
                    if (npc.velocity.Y == maxSpeed)
                    {
                        if (npc.velocity.X < playerCenterX) { npc.velocity.X += gravityResist; }
                        else
                        if (npc.velocity.X > playerCenterX) { npc.velocity.X -= gravityResist; }
                    }
                    else
                    if (npc.velocity.Y > 4f)
                    {
                        if (npc.velocity.X < 0f) { npc.velocity.X += gravityResist * 0.9f; } else { npc.velocity.X -= gravityResist * 0.9f; }
                    }
                }
                else
                {
                    if (soundEffects && npc.soundDelay == 0)
                    {
                        float distSoundDelay = dist / 40f;
                        if (distSoundDelay < 10f) { distSoundDelay = 10f; }
                        if (distSoundDelay > 20f) { distSoundDelay = 20f; }
                        npc.soundDelay = (int)distSoundDelay;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                    }
                    dist = (float)Math.Sqrt((double)(playerCenterX * playerCenterX + playerCenterY * playerCenterY));
                    float absPlayerCenterX = Math.Abs(playerCenterX);
                    float absPlayerCenterY = Math.Abs(playerCenterY);
                    float newSpeed = maxSpeed / dist;
                    playerCenterX *= newSpeed;
                    playerCenterY *= newSpeed;
                    bool dontFall = false;
                    if (fly)
                    {
                        if (((npc.velocity.X > 0f && playerCenterX < 0f) || (npc.velocity.X < 0f && playerCenterX > 0f) || (npc.velocity.Y > 0f && playerCenterY < 0f) || (npc.velocity.Y < 0f && playerCenterY > 0f)) && Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) > gravityResist / 2f && dist < 300f)
                        {
                            dontFall = true;
                            if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < maxSpeed) { npc.velocity *= 1.1f; }
                        }
                    }
                    if (!dontFall)
                    {
                        if ((npc.velocity.X > 0f && playerCenterX > 0f) || (npc.velocity.X < 0f && playerCenterX < 0f) || (npc.velocity.Y > 0f && playerCenterY > 0f) || (npc.velocity.Y < 0f && playerCenterY < 0f))
                        {
                            if (npc.velocity.X < playerCenterX) { npc.velocity.X += gravityResist; }
                            else
                            if (npc.velocity.X > playerCenterX) { npc.velocity.X -= gravityResist; }
                            if (npc.velocity.Y < playerCenterY) { npc.velocity.Y += gravityResist; }
                            else
                            if (npc.velocity.Y > playerCenterY) { npc.velocity.Y -= gravityResist; }
                            if ((double)Math.Abs(playerCenterY) < (double)maxSpeed * 0.2 && ((npc.velocity.X > 0f && playerCenterX < 0f) || (npc.velocity.X < 0f && playerCenterX > 0f)))
                            {
                                if (npc.velocity.Y > 0f) { npc.velocity.Y += gravityResist * 2f; } else { npc.velocity.Y -= gravityResist * 2f; }
                            }
                            if ((double)Math.Abs(playerCenterX) < (double)maxSpeed * 0.2 && ((npc.velocity.Y > 0f && playerCenterY < 0f) || (npc.velocity.Y < 0f && playerCenterY > 0f)))
                            {
                                if (npc.velocity.X > 0f) { npc.velocity.X += gravityResist * 2f; } else { npc.velocity.X -= gravityResist * 2f; }
                            }
                        }
                        else
                        if (absPlayerCenterX > absPlayerCenterY)
                        {
                            if (npc.velocity.X < playerCenterX) { npc.velocity.X += gravityResist * 1.1f; }
                            else
                            if (npc.velocity.X > playerCenterX) { npc.velocity.X -= gravityResist * 1.1f; }

                            if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)maxSpeed * 0.5)
                            {
                                if (npc.velocity.Y > 0f) { npc.velocity.Y += gravityResist; } else { npc.velocity.Y -= gravityResist; }
                            }
                        }
                        else
                        {
                            if (npc.velocity.Y < playerCenterY) { npc.velocity.Y += gravityResist * 1.1f; }
                            else
                            if (npc.velocity.Y > playerCenterY) { npc.velocity.Y -= gravityResist * 1.1f; }
                            if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)maxSpeed * 0.5)
                            {
                                if (npc.velocity.X > 0f) { npc.velocity.X += gravityResist; } else { npc.velocity.X -= gravityResist; }
                            }
                        }
                    }
                }
                npc.rotation = (float)Math.Atan2((double)npc.velocity.Y, (double)npc.velocity.X) + 1.57f;
                if (npc.type == headType)
                {
                    if (canMove)
                    {
                        if (npc.localAI[0] != 1f) { npc.netUpdate = true; }
                        npc.localAI[0] = 1f;
                    }
                    else
                    {
                        if (npc.localAI[0] != 0f) { npc.netUpdate = true; }
                        npc.localAI[0] = 0f;
                    }
                    if (((npc.velocity.X > 0f && npc.oldVelocity.X < 0f) || (npc.velocity.X < 0f && npc.oldVelocity.X > 0f) || (npc.velocity.Y > 0f && npc.oldVelocity.Y < 0f) || (npc.velocity.Y < 0f && npc.oldVelocity.Y > 0f)) && !npc.justHit)
                    {
                        npc.netUpdate = true;
                        return;
                    }
                }
            }
        }

        #endregion

    }

    ///<summary> 
    ///Handles boss despawning and targeting.
    ///This exists to simplify AI code.
    ///Create an instance of this class in SetDefaults, call targetAndDespawn(npcID) at the start of their AI, and removing any existing targeting or despawning.
    ///</summary>
    public class NPCDespawnHandler
    {
        ///<summary> 
        ///Handles all targeting and despawning.
        ///</summary> 
        ///<param name="despawnFlavorText">The custom text this boss displays when it despawns</param>
        ///<param name="textColor">The color of the despawn text</param>
        ///<param name="DustType">The ID of the dust this NPC should create an explosion of upon despawning</param>
        public NPCDespawnHandler(string despawnFlavorText, Color textColor, int DustType)
        {
            despawnText = despawnFlavorText;
            despawnTextColor = textColor;
            despawnDustType = DustType;
        }

        ///<summary> 
        ///Handles all targeting and despawning.
        ///</summary> 
        ///<param name="DustType">The ID of the dust this NPC should create an explosion of upon despawning</param>
        public NPCDespawnHandler(int DustType)
        {
            despawnDustType = DustType;
        }

        readonly string despawnText;
        readonly Color despawnTextColor;
        readonly int despawnDustType;
        bool hasTargeted = false;
        int targetCount = 0;
        readonly int[] targetIDs = new int[256];
        readonly bool[] targetAlive = new bool[256];
        int despawnTime = -1;

        ///<summary> 
        ///Handles all targeting and despawning.
        ///</summary>         
        ///<param name="npcID">The ID of the NPC in question.</param>
        public bool TargetAndDespawn(int npcID)
        {

            //When despawning, we set timeLeft to 240. If that's been done, we don't need to check for players or target anyone anymore.
            if (despawnTime < 0)
            {
                //Only run this once. Gets all active players and throws them into these arrays so we can track their status.
                if (!hasTargeted)
                {
                    foreach (Player player in Main.player)
                    {
                        //For some reason, Main.player always has 255 entries. This ensures we're only pulling real players from it.
                        if (player.active)
                        {
                            targetIDs[targetCount] = player.whoAmI;
                            targetAlive[targetCount] = true;
                            targetCount++;
                        }
                    }
                    hasTargeted = true;
                }


                //Go through the target list. If everyone has died once, despawn. Else, target the closest one that has not yet died.
                //It's important that it only targets players who haven't died, because otherwise one living player could hide far away while the other repeatedly respawned and fought the boss.
                //With this, it will intentionally seek out those it has not yet killed instead.
                bool viableTarget = false;
                float closestPlayerDistance = float.MaxValue;
                float oldTarget = Main.npc[npcID].target;
                //Iterate through all tracked players in the array
                for (int i = 0; i < targetCount; i++)
                {
                    //For each of them, check if they're dead. If so, mark it down in targetAlive.
                    if (Main.player[targetIDs[i]].dead && targetAlive[i])
                    {
                        targetAlive[i] = false;
                    }
                    else if (targetAlive[i] && Main.player[targetIDs[i]].active)
                    {
                        //If it found a player that hasn't been killed yet, then don't despawn
                        viableTarget = true;
                        //Check if they're the closest one, and if so target them
                        float distance = Vector2.DistanceSquared(Main.player[targetIDs[i]].position, Main.npc[npcID].position);
                        if (distance < closestPlayerDistance)
                        {
                            closestPlayerDistance = distance;
                            Main.npc[npcID].target = targetIDs[i];
                        }
                    }
                }

                //If a npc changes targets, sync it
                if (oldTarget != Main.npc[npcID].target)
                {
                    Main.npc[npcID].netUpdate = true;
                }

                //If there's no player that has not yet died, then despawn.
                if (!viableTarget)
                {
                    if (despawnText != null)
                    {
                        UsefulFunctions.BroadcastText("All players have been defeated at least once!", Color.Yellow);
                        UsefulFunctions.BroadcastText(despawnText, despawnTextColor);
                    }
                    despawnTime = 240;
                }
            }
            else
            {
                //Adios
                if (despawnTime == 0)
                {
                    for (int i = 0; i < 60; i++)
                    {
                        int dustID = Dust.NewDust(Main.npc[npcID].position, Main.npc[npcID].width, Main.npc[npcID].height, despawnDustType, Main.rand.Next(-12, 12), Main.rand.Next(-12, 12), 150, default, 7f);
                        Main.dust[dustID].noGravity = true;
                    }
                    Main.npc[npcID].active = false;
                }
                else
                {
                    int dustID = Dust.NewDust(Main.npc[npcID].position, Main.npc[npcID].width, Main.npc[npcID].height, despawnDustType, Main.rand.Next(-12, 12), Main.rand.Next(-12, 12), 150, default, 1f);
                    Main.dust[dustID].noGravity = true;
                    despawnTime--;
                }

                //The frame before despawning, we return true to let the NPC's AI know it's about to get despawned. This allows it to do anything it needs to with that information (like re-actuating the pyramid)
                if (despawnTime == 1)
                {
                    return true;
                }
            }
            return false;
        }


    }


    static class tsorcRevampAIs
    {
        ///<summary> 
        ///Walking AI that walks toward the player. Can be used with SimpleProjectile to fire projectiles, or LeapAtPlayer to leap when the player is close
        ///Uses up to two ai slots depending on configuration: npc.ai[2] is door break progress (only used if it can break them) and npc.ai[3] is boredom
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="topSpeed">The max speed it can run at</param>
        ///<param name="acceleration">How quickly it can speed up</param>
        ///<param name="brakingPower">How quickly it can slow down</param>
        ///<param name="canTeleport">Lets it teleport near the player when it gets bored instead of walking around randomly</param>
        ///<param name="doorBreakingDamage">Setting this above 0 lets the npc break doors, and sets much damage should it deal when it hits them. Doors have 10 "health"</param>
        ///<param name="hatesLight">Should it run away during daylight?</param>
        ///<param name="randomSound">What sound should it randomly play?</param>
        ///<param name="soundFrequency">How often does it play its sound?</param>
        ///<param name="enragePercent">Accelerates twice as fast when below this % health</param> 
        ///<param name="enrageTopSpeed">Its new top speed when enraged</param>
        ///<param name="lavaJumping">Lets it hop around in lava</param>
        public static void FighterAI(NPC npc, float topSpeed = 1f, float acceleration = .07f, float brakingPower = .2f, bool canTeleport = false, int doorBreakingDamage = 0, bool hatesLight = false, SoundStyle? randomSound = null, int soundFrequency = 1000, float enragePercent = 0, float enrageTopSpeed = 0, bool lavaJumping = false)
        {
            npc.aiStyle = -1;
            BasicAI(npc, topSpeed, acceleration, brakingPower, false, canTeleport, doorBreakingDamage, hatesLight, randomSound, soundFrequency, enragePercent, enrageTopSpeed, lavaJumping);
        }

        ///<summary> 
        ///Special version of the fighter ai, stopping to shoot when the player is within range. Gets bored if it doesn't have line of sight to the player, and if it can teleport it will attempt to warp to a position with a clean shot.
        ///Uses three ai slots: npc.ai[1] is shot cooldown, npc.ai[2] controls the sprite aiming animation, and npc.ai[3] is boredom
        ///</summary>         
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="projectileType">The ID of the projectile you want to shoot</param>
        ///<param name="projectileDamage">Damage of the projectile. Multiplied by 2 by default, and then 2 again in expert mode</param>
        ///<param name="projectileVelocity">Speed of the projectile</param>
        ///<param name="projectileCooldown">Sets the delay (in ticks) between shots</param>
        ///<param name="topSpeed">The max speed it can run at</param>
        ///<param name="acceleration">How quickly it can speed up</param>
        ///<param name="brakingPower">How quickly it can slow down</param>
        ///<param name="canTeleport">Lets it teleport near the player when it gets bored instead of walking around randomly</param>
        ///<param name="hatesLight">Should it run away during daylight? (UNIMPLEMENTED!)</param>
        ///<param name="shootSound">What sound should it play?</param>
        ///<param name="soundFrequency">How often does it play its sound?</param>
        ///<param name="enragePercent">Below this percent health, doubles speed and acceleration</param>
        ///<param name="lavaJumping">Lets it hop around in lava</param>
        ///<param name="projectileGravity">How much is the projectile's y velocity reduced each tick? Set 0 for projectiles with no gravity. If your projectile has custom gravity dropoff, stick that here.</param>
        ///<param name="shootSound">The type of sound to play when it shoots. Defaults to bow.</param>
        public static void ArcherAI(NPC npc, int projectileType, int projectileDamage, float projectileVelocity, int projectileCooldown, float topSpeed = 1f, float acceleration = .07f, float brakingPower = .2f, bool canTeleport = false, bool hatesLight = false, SoundStyle? randomSound = null, int soundFrequency = 1000, float enragePercent = 0, float enrageTopSpeed = 0, bool lavaJumping = false, float projectileGravity = 0.035f, SoundStyle? shootSound = null)
        {
            BasicAI(npc, topSpeed, acceleration, brakingPower, true, canTeleport, 0, hatesLight, randomSound, soundFrequency, enragePercent, enrageTopSpeed, lavaJumping);

            //Set default shoot sound
            if(shootSound == null)
            {
                shootSound = SoundID.Item5;
            }

            //Apply scaling to SHM enemies
            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp"))
            {
                if (!npc.boss)
                {
                    if (npc.ModNPC.GetType().Namespace.Contains("SuperHardMode"))
                    {
                        projectileDamage = (int)(tsorcRevampWorld.SHMScale * projectileDamage);
                        projectileVelocity = (int)(tsorcRevampWorld.SubtleSHMScale * projectileVelocity);
                    }
                }
            }

            npc.aiStyle = -1;
            if (npc.confused)
            {
                npc.ai[2] = 0f; // won't try to stop & aim if confused
            }
            else
            {
                if (npc.ai[1] > 0f)
                    npc.ai[1] -= 1f; // decrement fire & reload counter

                if (npc.justHit || npc.velocity.Y != 0f || npc.ai[1] <= 0f) // was just hit?
                {
                    npc.ai[1] = projectileCooldown; //Reset firing time
                    npc.ai[2] = 0f; //Not aiming
                }

                //Check if we're in range of and can hit the player
                if (Vector2.Distance(npc.Center, Main.player[npc.target].Center) < 700f && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1) && Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1) && npc.velocity.Y == 0)
                {
                    //If so, set boredom to 0
                    npc.ai[3] = 0;

                    //If it's not aiming yet, then slow down, aim, and start its cooldown
                    if (npc.ai[2] == 0)
                    {
                        //Aim at them, and start the shot cooldown
                        npc.velocity.X *= 0.5f;
                        npc.ai[2] = 3f;
                        npc.ai[1] = projectileCooldown;
                    }

                    npc.velocity.X *= 0.9f; // decelerate to stop & shoot
                    npc.spriteDirection = npc.direction; // match animation to facing

                    //Fire at halfway through: first half of delay is aim, 2nd half is cooldown
                    if (npc.ai[1] == (projectileCooldown / 2))
                    {
                        //Calculate the actual ballistic trajectory to aim the projectile on
                        Vector2 projectileVector = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, projectileVelocity, projectileGravity);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center.X, npc.Center.Y, projectileVector.X, projectileVector.Y, projectileType, projectileDamage, 0f, Main.myPlayer);
                        }

                        SoundEngine.PlaySound(shootSound.Value);
                    }

                    //Calculate a vector aiming at the player. This is purely for the npc's sprite visuals, so it can use the much simpler aiming code.
                    Vector2 aimVector = UsefulFunctions.Aim(npc.Center, Main.player[npc.target].Center, projectileVelocity);

                    if (Math.Abs(aimVector.Y) > Math.Abs(aimVector.X) * 2f) // target steeply above/below NPC
                    {
                        if (aimVector.Y > 0f)
                            npc.ai[2] = 1f; // aim downward
                        else
                            npc.ai[2] = 5f; // aim upward
                    }
                    else if (Math.Abs(aimVector.X) > Math.Abs(aimVector.Y) * 2f) // target on level with NPC
                        npc.ai[2] = 3f;  //  aim straight ahead
                    else if (aimVector.Y > 0f) // target is below NPC
                        npc.ai[2] = 2f;  //  aim slight downward
                    else // target is not below NPC
                        npc.ai[2] = 4f;  //  aim slight upward                    
                }
                //If we're out of range of the player, don't aim at them
                else
                {
                    npc.ai[2] = 0;
                }
            }
        }



        //Todo:
        //Upgrade gap-jumping code to scale jump x and  y velocity with gap size, up to a limit
        //Upgrade wall-jumping code to scale jump height with how tall the wall in front of it is. Also let it recognize walls with gaps in them.
        //More complex "bored" check than simple velocity. Right now it can get bored if it takes too long doing things that require it to move slow.
        private static void BasicAI(NPC npc, float topSpeed, float acceleration, float brakingPower, bool isArcher, bool canTeleport = false, int doorBreakingDamage = 0, bool hatesLight = false, SoundStyle? randomSound = null, int soundFrequency = 1000, float enragePercentage = 0, float enrageTopSpeed = 0, bool lavaJumping = false)
        {
            //Apply scaling to SHM enemies
            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp"))
            {
                if (!npc.boss)
                {
                    if (npc.ModNPC.GetType().Namespace.Contains("SuperHardMode"))
                    {
                        topSpeed *= tsorcRevampWorld.SHMScale;
                        acceleration *= tsorcRevampWorld.SubtleSHMScale;
                        enrageTopSpeed *= tsorcRevampWorld.SHMScale;
                    }
                }
            }


            //If it has a sound to play, roll a chance for playing it
            if (randomSound != null && Main.rand.Next(soundFrequency) <= 0)
            {
                Terraria.Audio.SoundEngine.PlaySound(randomSound.Value, npc.Center);
            }

            //If we can enrage, do that
            if (npc.life < (float)npc.lifeMax * enragePercentage)
            {
                acceleration *= 2;
                topSpeed = enrageTopSpeed;
            }

            //If it can jump in lava and is in lava, do that
            if (lavaJumping && npc.lavaWet)
            {
                npc.velocity.Y -= 2;
            }

            //If just hit, then it's not bored
            if (npc.justHit)
            {
                npc.ai[3] = 0;
            }

            //If not fleeing light and not bored, target the closest player
            if (hatesLight && Main.dayTime && (npc.position.Y / 16f) < Main.worldSurface)
            {
                npc.ai[3] = -999;
                npc.timeLeft = 10;
            }

            //If bored, target the closest player it has line of sight to. If it doesn't have los to any, just target the closest one.
            if (npc.ai[3] != 0)
            {
                float distance = 9999999;
                int target = -1;

                //Stopwatch s = new Stopwatch();
                //s.Start();
                for(int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i] != null && Main.player[i].active)
                    {
                        if (Main.player[i].CanHit(npc))
                        {
                            float playerDistance = Main.player[i].Distance(npc.Center);
                            if(playerDistance < distance)
                            {
                                distance = playerDistance;
                                target = i;
                            }
                        }
                    }
                    if(target != -1)
                    {
                        npc.target = target;
                    }                    
                    else
                    {
                        npc.TargetClosest(false);
                    }
                }
                //s.Stop();
                //Main.NewText("Boredom: " + s.Elapsed);
            }
            if (npc.ai[3] >= 0)
            {
                if (Main.player[npc.target].Center.X <= npc.Center.X)
                {
                    npc.direction = -1;
                }
                else
                {
                    npc.direction = 1;
                }
            }

            //If moving more than max speed, then slow down
            if (npc.velocity.X > topSpeed)
            {
                npc.velocity.X -= brakingPower;
                if (npc.velocity.X < 0)
                {
                    npc.velocity.X = 0;
                }
            }
            if (npc.velocity.X < -topSpeed)
            {
                npc.velocity.X += brakingPower;
                if (npc.velocity.X > 0)
                {
                    npc.velocity.X = 0;
                }
            }
            //If not then accelerate (unless the npc is an aiming archer)
            else if (!isArcher || npc.ai[2] == 0)
            {
                if (npc.velocity.X < topSpeed && npc.direction == 1)
                {
                    npc.velocity.X += acceleration;
                    if (npc.velocity.X > topSpeed)
                    {
                        npc.velocity.X = topSpeed;
                    }
                }
                else
                {
                    if (npc.velocity.X > -topSpeed && npc.direction == -1)
                    {
                        npc.velocity.X -= acceleration;
                        if (npc.velocity.X < -topSpeed)
                        {
                            npc.velocity.X = -topSpeed;
                        }
                    }
                }
            }

            //Jumping and platform falling code, copied and edited from Firebomb Hollow
            int x_in_front;
            if (npc.direction == -1)
            {
                x_in_front = (int)(npc.position.X / 16f) - 1;
            }
            else
            {
                x_in_front = (int)((npc.position.X + npc.width) / 16f) + 1;
            }

            int y_above_feet = (int)((npc.position.Y + (float)npc.height - 15f) / 16f); // 15 pix above feet
            int y_below_feet = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
            bool standing_on_solid_tile = false;

            //Check if standing on a solid tile
            int x_left_edge = (int)npc.position.X / 16;
            int x_right_edge = (int)(npc.position.X + (float)npc.width) / 16;
            if (npc.velocity.Y == 0)
            {
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (UsefulFunctions.IsTileReallySolid(l, y_below_feet)) // tile exists and is solid
                    {
                        standing_on_solid_tile = true;
                        break; // one is enough so stop checking
                    }
                }
            }

            if (standing_on_solid_tile)  //  if standing on solid tile
            {
                if ((npc.velocity.X < 0f && npc.spriteDirection == -1) || (npc.velocity.X > 0f && npc.spriteDirection == 1))
                {  //  moving forward
                    ;
                    if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 2))
                    { // 3 blocks above ground level(head height) blocked
                        if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 3))
                        { // 4 blocks above ground level(over head) blocked
                            npc.velocity.Y = -8f; // jump with power 8 (for 4 block steps)
                            npc.netUpdate = true;
                        }
                        else
                        {
                            npc.velocity.Y = -7f; // jump with power 7 (for 3 block steps)
                            npc.netUpdate = true;
                        }
                    } // for everything else, head height clear:
                    else if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1))
                    { // 2 blocks above ground level(mid body height) blocked
                        npc.velocity.Y = -6f; // jump with power 6 (for 2 block steps)
                        npc.netUpdate = true;
                    }
                    else if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet))
                    { // 1 block above ground level(foot height) blocked
                        npc.velocity.Y = -5f; // jump with power 5 (for 1 block steps)
                        npc.netUpdate = true;
                    }
                    else if (npc.directionY < 0 && !UsefulFunctions.IsTileReallySolid(x_in_front, y_below_feet) && !UsefulFunctions.IsTileReallySolid(x_in_front + npc.direction, y_below_feet))
                    { //If player is above npc and no solid tile ahead to step on for 2 spaces
                        npc.velocity.Y = -8f; // jump with power 8
                        npc.velocity.X += 4f * npc.direction; // jump forward hard as well; we're trying to jump a gap
                        npc.netUpdate = true;
                    }

                    //Door breaking
                    //First, it checks if the tile in front of it is solid, a door, and the npc can break it
                    if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1) && Main.tile[x_in_front, y_above_feet - 1].TileType == 10 && (doorBreakingDamage > 0))
                    {
                        npc.ai[3] = 0f; // not bored if working on breaking a door
                        if (Main.GameUpdateCount % 60 == 0)  //  knock once per second
                        {
                            npc.velocity.X = 0.5f * -npc.direction; //  slight recoil from hitting it
                            npc.ai[2] += doorBreakingDamage;  //  increase door damage counter
                            WorldGen.KillTile(x_in_front, y_above_feet - 1, true, true, false);  //  kill door ? when door not breaking too? can fail=true; effect only would make more sense, to make knocking sound
                            if (npc.ai[2] >= 10f && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                npc.ai[2] = 0; //Reset counter

                                //Try to open door
                                if (!WorldGen.OpenDoor(x_in_front, y_above_feet, npc.direction))
                                {
                                    //If the door is stuck set the npc to bored
                                    npc.ai[3] = 999;
                                    npc.velocity.X = 0; // cancel recoil so boredom wall reflection can trigger
                                }
                                else if (Main.netMode == NetmodeID.Server)
                                {
                                    //If it didn't fail sync the door opening
                                    NetMessage.SendData(19, -1, -1, null, 0, (float)x_in_front, (float)y_above_feet, (float)npc.direction, 0); // ??
                                }
                            }
                        }
                    }
                }
            }

            //Main.NewText("Boredom: " + npc.ai[3]);
            bool lineOfSight = Main.player[npc.target].CanHit(npc);
            bool belowTopSpeed = (Math.Abs(npc.velocity.X) <= topSpeed * 0.9f);

            if (npc.ai[3] >= 0)
            {
                //Increase boredom if it's stuck on a wall it can't pass through, walking back and forth above the player, or can teleport but can't see the player
                if (belowTopSpeed || (canTeleport && !lineOfSight))
                {
                    npc.ai[3]++;

                    //Time it takes to get bored scales with how long it takes to accelerate
                    if (npc.ai[3] > 100 + (4 * (topSpeed / acceleration)))
                    {
                        if (!canTeleport)
                        {
                            npc.ai[3] = -180;
                            npc.direction *= -1;
                        }
                        else
                        {
                            //Try to teleport somewhere it has line of sight to the player
                            Teleport(npc, 50, true);
                            npc.ai[3] = -30;
                        }
                    }
                }
                //If it's not stuck not and it's not bored decrease the boredom counter
                else if (npc.ai[3] > 0)
                {
                    npc.ai[3] -= 1;
                    if (npc.ai[3] < 0)
                    {
                        npc.ai[3] = 0;
                    }
                }
            }
            else
            {
                npc.ai[3]++;
            }

            //If it has line of sight and is moving at full speed, and the player is near its level, instantly set boredom to 0
            if (lineOfSight && !belowTopSpeed && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) < 144)
            {
                npc.ai[3] = 0;
            }
        }





        //AI snippits go here! Simply call these in the npc's main AI function to add them
        #region AI Snippets


        ///<summary> 
        ///Fires a projectile with various parameters. Uses any timer variable you give it, and goes in the npc's AI() function
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="timer">The variable used as a timer. This function will increment it automatically, disable incrementTimer to stop that</param>
        ///<param name="timerCap">How high does the timer have to be for it to shoot</param>
        ///<param name="projectileType">The ID of the projectile you want to shoot</param>
        ///<param name="projectileDamage">Damage of the projectile. Multiplied by 2 by default, and then 2 again in expert mode</param>
        ///<param name="projectileVelocity">Speed of the projectile</param>
        ///<param name="actuallyFire">This lets you use a condition to block the projectile from firing unless it is true (such as having line of sight to the player)</param>
        ///<param name="incrementTimer">Should this functoin increase the timer variable by 1 every tick</param>
        ///<param name="soundType">The type of sound to play</param>
        ///<param name="soundStyle">The style of sound to play</param>
        ///<param name="projectileGravity">How much is the projectile's y velocity reduced each tick? Leave blank for default gravity, set to 0 for projectiles with no gravity, set it custom if your projectile has custom gravity</param>
        ///<param name="ai0">Lets you pass a value to the projectile's ai0</param>
        ///<param name="ai1">Lets you pass a value to the projectile's ai1</param>
        public static bool SimpleProjectile(NPC npc, ref float timer, int timerCap, int projectileType, int projectileDamage, float projectileVelocity, bool actuallyFire = true, bool incrementTimer = true, SoundStyle? shootSound = null, float projectileGravity = 0.035f, float ai0 = 0, float ai1 = 0)
        {
            if (npc.ai[3] < 0)
            {
                timer = 0;
            }
            else
            {
                if (incrementTimer && timer < timerCap)
                {
                    timer++;
                }
                if (timer >= timerCap && actuallyFire)
                {
                    timer = 0;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projectileVector = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, projectileVelocity, projectileGravity);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center.X, npc.Center.Y, projectileVector.X, projectileVector.Y, projectileType, projectileDamage, 0f, Main.myPlayer, ai0, ai1);
                    }
                    if (shootSound != null)
                    {
                        SoundEngine.PlaySound(shootSound.Value, npc.Center);
                    }
                    return true;
                }
            }

            return false;
        }

        ///<summary> 
        ///Lets the npc leap at players who are close, does not use any ai slots, and goes in an npc's ai function
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="hopSpeedX">How fast it leaps horizontally</param>
        ///<param name="hopSpeedY">How fast it leaps vertically</param>
        ///<param name="minimumSpeed">How fast it has to be running to be allowed to hop</param>
        ///<param name="hopRange">It leaps at the player when it is this close to them</param>
        public static void LeapAtPlayer(NPC npc, float hopSpeedX, float hopSpeedY, float minimumSpeed, float hopRange = 64)
        {
            //If the player is within range and if the npc is moving fast enough to be allowed to hop, then hop
            if (npc.velocity.Y == 0f && Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) < hopRange && Math.Abs(npc.Center.Y - Main.player[npc.target].Center.Y) < hopRange && ((npc.direction > 0 && npc.velocity.X >= minimumSpeed) || (npc.direction < 0 && npc.velocity.X <= -minimumSpeed)))
            {
                npc.velocity.X = hopSpeedX * npc.direction;
                npc.velocity.Y = -hopSpeedY;
                npc.netUpdate = true;
            }
        }

        ///<summary> 
        ///Teleports the NPC to a random position within a specified range around the player. *No* effects or sound! Does not teleport the enemy if no safe location exists. Will not teleport enemies right next to the player.
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="range">The max range from the player it can teleport. Minimum is 12 blocks.</param>
        ///<param name="requireLineofSight">Try to teleport somewhere that has line of sight to the player</param>
        public static void TeleportNoEffects(NPC npc, int range, bool requireLineofSight = true)
        {
            int target_y_blockpos = (int)Main.player[npc.target].position.Y / 16; // corner not center

            //Do not teleport if the player is way way too far away (stops enemies following you home if you mirror away)
            if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f)
            { // far away from target; 2000 pixels = 125 blocks
                return;
            }

            //Try 100 times at most
            for (int i = 0; i < 100; i++)
            {
                //Pick a random point to target. Make sure it's at least 11 blocks away from the player to avoid cheap hits.
                Vector2 teleportTarget = Vector2.Zero;
                if(range < 12)
                {
                    range = 12;
                }
                teleportTarget.X = Main.rand.Next(11, range);
                if (Main.rand.NextBool())
                {
                    teleportTarget.X *= -1;
                }

                //Add the player's position to it to convert it to an actual tile coordinate
                teleportTarget += Main.player[npc.target].position / 16;

                //Starting from the point we picked, go down one block at a time until we find hit a solid block
                bool odd = false;
                for (int y = 0; Math.Abs(y) < range / 2;)
                {
                    if (odd)
                    {
                        y *= -1;
                        y++;
                        odd = !odd;
                    }
                    else
                    {
                        y *= -1;
                        odd = !odd;
                    }
                    if (UsefulFunctions.IsTileReallySolid((int)teleportTarget.X, (int)teleportTarget.Y + y))
                    {
                        //Skip to the next tile if any of the following is true:

                        // If there are solid blocks in the way, leaving no room to teleport to
                        if (Collision.SolidTiles((int)teleportTarget.X - 1, (int)teleportTarget.X + 1, (int)teleportTarget.Y + y - 4, (int)teleportTarget.Y + y - 1))
                        {
                            //Main.NewText("Fail 1");
                            continue;
                        }          

                        //If it requires line of sight, and there is not a clear path, and it has not tried at least 50 times, then skip to the next try
                        else if (requireLineofSight && !(Collision.CanHit(new Vector2(teleportTarget.X, (int)teleportTarget.Y + y), 2, 2, Main.player[npc.target].Center / 16, 2, 2) && Collision.CanHitLine(new Vector2(teleportTarget.X, (int)teleportTarget.Y + y), 2, 2, Main.player[npc.target].Center / 16, 2, 2)))
                        {
                            //Main.NewText("Fail 3");
                            continue;
                        }

                        //If the selected tile has lava above it, and the npc isn't immune
                        else if (Main.tile[(int)teleportTarget.X, (int)teleportTarget.Y + y - 1].LiquidType == LiquidID.Lava && !npc.lavaImmune)
                        {
                            //Main.NewText("Fail 4");
                            continue;
                        }

                        //Then teleport and return
                        npc.position.X = ((int)teleportTarget.X * 16 - npc.width / 2); //Center npc at target
                        npc.position.Y = (((int)teleportTarget.Y + y) * 16 - npc.height); //Subtract npc.height from y so block is under feet
                        npc.TargetClosest(true);
                        npc.netUpdate = true;
                        return;

                    }
                }
            }
        }


        ///<summary> 
        ///Teleports the NPC to a random position within a specified range around the player, includes effects. Does not teleport the enemy if no safe location exists.
        ///Will not teleport enemies right next to the player. Teleports enemies somewhere with line of sight to the player by default.
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="range">The max range from the player it can teleport. Minimum is 12 blocks.</param>
        ///<param name="requireLineofSight">Try to teleport somewhere that has line of sight to the player</param>
        public static void Teleport(NPC npc, int range, bool requireLineofSight = true)
        {
            Vector2 oldPosition = npc.Center;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, npc.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 100; i++)
                {
                    TeleportNoEffects(npc, range, requireLineofSight);
                    if (!requireLineofSight || (Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1) && Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1)))
                    {
                        break;
                    }
                }
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, npc.Center);

            Vector2 newPosition = npc.Center;

            Vector2 diff = newPosition - oldPosition;
            float length = diff.Length();
            diff.Normalize();
            Vector2 offset = Vector2.Zero;

            for (int i = 0; i < length; i++)
            {
                offset += diff;
                if (Main.rand.NextBool(2))
                {
                    Vector2 dustPoint = offset;
                    dustPoint.X += Main.rand.NextFloat(-npc.width / 2, npc.width / 2);
                    dustPoint.Y += Main.rand.NextFloat(-npc.height / 2, npc.height / 2);
                    if (Main.rand.NextBool())
                    {
                        Dust.NewDustPerfect(oldPosition + dustPoint, 71, diff * 5, 200, default, 0.8f).noGravity = true;
                    }
                    else
                    {
                        Dust.NewDustPerfect(oldPosition + dustPoint, DustID.FireworkFountain_Pink, diff * 5, 200, default, 0.8f).noGravity = true;
                    }
                }
            }
        }

        public static void RedKnightOnHit(NPC npc, bool melee)
        {
            if (melee)
            {
                npc.localAI[1] = 100f;
                npc.knockBackResist = 0.09f;

                //WHEN HIT, CHANCE TO JUMP BACKWARDS 
                if (Main.rand.NextBool(10))
                {
                    npc.TargetClosest(false);

                    npc.velocity.Y = -8f;
                    npc.velocity.X = -4f * npc.direction;


                    npc.localAI[1] = 160f;

                    npc.netUpdate = true;
                }

                //WHEN HIT, CHANCE TO DASH STEP BACKWARDS 
                else if (Main.rand.NextBool(8))//was 10
                {
                    npc.velocity.Y = -4f;
                    npc.velocity.X = -6f * npc.direction;

                    npc.localAI[1] = 160f;

                    //CHANCE TO JUMP AFTER DASH
                    if (Main.rand.NextBool(4))
                    {
                        npc.TargetClosest(true);
                        npc.velocity.Y = -7f;
                        //npc.localAI[1] = 161f;
                    }

                    npc.netUpdate = true;
                }

                //TELEPORT MELEE
                if (Main.rand.NextBool(12))
                {
                    Teleport(npc, 20, true);
                }
            }

            if (!melee && Main.rand.NextBool())
            {
                if (Main.rand.NextBool(4))
                {
                    //customAi1 = 110f;
                    int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Red, 1f);
                    Main.dust[dust].noGravity = true;
                    


                    npc.velocity.Y = -9f; //9
                    npc.velocity.X = 4f * npc.direction; //was -4

                    npc.TargetClosest(true);
                    
                    if ((float)npc.direction * npc.velocity.X > 4)
                    {
                        npc.velocity.X = (float)npc.direction * 4;  //  but cap at top speed
                    }
                    npc.netUpdate = true;
                }

                if (Main.rand.NextBool(6))
                {
                    

                    npc.ai[0] = 0f;
                    npc.velocity.Y = -5f;
                    npc.velocity.X *= 4f; // burst forward
                    npc.TargetClosest(true);
                    
                    npc.velocity.X += (float)npc.direction * 5f;  //  accellerate fwd; can happen midair
                    if ((float)npc.direction * npc.velocity.X > 5)
                    {
                        npc.velocity.X = (float)npc.direction * 5;  //  but cap at top speed
                    }

                    //CHANCE TO JUMP AFTER DASH
                    if (Main.rand.NextBool(8))
                    {
                        npc.TargetClosest(true);

                        npc.spriteDirection = npc.direction;
                        npc.ai[0] = 0f;

                        npc.velocity.Y = -6f;
                    }

                    npc.netUpdate = true;
                }
                if (npc.Distance(Main.player[npc.target].Center) > 80 && Main.rand.NextBool(20))
                {
                    Teleport(npc, 20, false);
                }
            }

        }
        #endregion
    }
}
