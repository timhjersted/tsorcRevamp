using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Pets;

namespace tsorcRevamp.NPCs {
    class VanillaChanges : GlobalNPC {
        #region SetDefaults

        public override void SetDefaults(NPC npc) {

            //Only mess with it if it's one of our bosses
            if (npc.modNPC != null && npc.modNPC.mod == ModLoader.GetMod("tsorcRevamp") && npc.boss)
            {
                //Bosses are 1.3x weaker in normal mode. This is reverted for expert in ScaleExpertStats
                //Doing it like this means we can simply set npc.lifeMax to exactly value we want their expert mode health to be, saving us a headache.
                //Rounded, because casting to an int truncates it which causes slight inaccuracies later on
                npc.lifeMax = (int)Math.Round(npc.lifeMax / 1.3f);
            }

            switch (npc.type) {
                case (NPCID.AngryBones): {
                        npc.lifeMax = 145;
                        npc.damage = 33;
                        npc.knockBackResist = 0.2f;
                        break;
                    }

                case (NPCID.Antlion): {
                        npc.lifeMax = 50;
                        npc.damage = 46;
                        break;
                    }

                case (NPCID.ArmoredSkeleton): {
                        npc.npcSlots = 2;
                        npc.lifeMax = 740;
                        npc.damage = 43;
                        npc.knockBackResist = 0.2f;
                        npc.defense = 36;
                        npc.value = 500;
                        break;
                    }

                case (NPCID.BaldZombie): {
                        npc.knockBackResist = 0.8f;
                        break;
                    }

                case (NPCID.BigBoned): {
                        npc.lifeMax = 200;
                        npc.damage = 46;
                        npc.knockBackResist = 0.68f;
                        npc.value = 500;
                        break;
                    }

                case (NPCID.BigEater): {
                        npc.scale = 1.15f;
                        break;
                    }

                case (NPCID.BigStinger): {
                        npc.scale = 1.2f;
                        npc.value = 400;
                        break;
                    }

                case (NPCID.BlazingWheel): {
                        npc.scale = 1.2f;
                        npc.damage = 53;
                        break;
                    }

                case (NPCID.BlueJellyfish): {
                        npc.value = 50;
                        break;
                    }

                case (NPCID.BoneSerpentBody): {
                        npc.lifeMax = 1450;
                        npc.damage = 20;
                        npc.value = 2750;
                        npc.defense = 12;
                        break;
                    }

                case (NPCID.BoneSerpentHead): {
                        npc.lifeMax = 1450;
                        npc.damage = 50;
                        npc.value = 2750;
                        npc.defense = 2;
                        break;
                    }

                case (NPCID.BoneSerpentTail): {
                        npc.lifeMax = 1450;
                        npc.value = 2500;
                        npc.defense = 25;
                        npc.knockBackResist = 0.1f;
                        break;
                    }

                case (NPCID.ChaosBall): {
                        npc.damage = 26;
                        break;
                    }

                case (NPCID.ChaosElemental): {
                        npc.lifeMax = 396;
                        npc.damage = 46;
                        npc.value = 1500;
                        npc.defense = 25;
                        npc.knockBackResist = 0.2f;
                        break;
                    }

                case (NPCID.Clinger): {
                        npc.lifeMax = 410;
                        npc.value = 800;
                        break;
                    }

                case (NPCID.Clown): {
                        npc.damage = 50;
                        npc.lifeMax = 10;
                        npc.value = 1000;
                        npc.defense = 20;
                        break;
                    }

                case (NPCID.CorruptBunny): {
                        npc.damage = 53;
                        npc.value = 80;
                        break;
                    }

                case (NPCID.CorruptGoldfish): {
                        npc.value = 90;
                        break;
                    }

                case (NPCID.CorruptSlime): {
                        npc.scale = 1.1f;
                        break;
                    }

                case (NPCID.CursedSkull): {
                        npc.lifeMax = 53;
                        npc.damage = 51;
                        npc.value = 350;
                        npc.defense = 8;
                        npc.knockBackResist = 0f;
                        break;
                    }

                case (NPCID.DarkCaster): {
                        npc.lifeMax = 100;
                        npc.damage = 46;
                        npc.value = 250;
                        npc.defense = 5;
                        npc.knockBackResist = 0.2f;
                        break;
                    }

                case (NPCID.DD2LightningBugT3):
                    {
                        npc.lifeMax = 500;
                        npc.damage = 69; //was 329 damage at 166?
                        npc.value = 2500;
                        npc.defense = 35;
                        npc.knockBackResist = 0.46f;
                        //int witheringBolts ??
                        break;
                    }
                    
                case (NPCID.Demon): {
                        npc.lifeMax = 140;
                        npc.value = 630;
                        npc.defense = 23;
                        npc.knockBackResist = 0.4f;
                        break;
                    }

                case (NPCID.DevourerBody): {
                        npc.defense = 8;
                        break;
                    }

                case (NPCID.DevourerHead): {
                        npc.defense = 8;
                        break;
                    }

                case (NPCID.DiggerBody): {
                        npc.scale = .9f;
                        break;
                    }

                case (NPCID.DiggerHead): {
                        npc.scale = .9f;
                        break;
                    }

                case (NPCID.DiggerTail): {
                        npc.scale = .9f;
                        break;
                    }

                case (NPCID.DukeFishron):
                    {
                        npc.value = 250000;
                        break;
                    }
                    
                case (NPCID.DungeonSlime): {
                        npc.value = 250;
                        break;
                    }

                case (NPCID.EaterofSouls): {
                        npc.value = 100;
                        break;
                    }

                case (NPCID.EaterofWorldsBody): {
                        npc.lifeMax = 180;
                        npc.damage = 18; //legacy: 22
                        npc.defense = 5;
                        npc.value = 1000;
                        npc.buffImmune[BuffID.Poisoned] = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.Frostburn] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.EaterofWorldsHead): {
                        npc.lifeMax = 180;
                        npc.damage = 30;
                        npc.defense = 22;
                        npc.value = 1000;
                        npc.buffImmune[BuffID.Poisoned] = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.Frostburn] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.EaterofWorldsTail): {
                        npc.lifeMax = 155;
                        npc.defense = 8;
                        npc.value = 1000;
                        npc.buffImmune[BuffID.Poisoned] = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.Frostburn] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.EyeofCthulhu): {
                        //damage changes here are for first phase
                        npc.damage = 27; //legacy: 37
                        if (Main.expertMode) {
                            npc.damage = 21; //revamp expert: 42
                        }
                        if (Main.player[Main.myPlayer].ZoneJungle) {
                            if (Main.expertMode) {
                                npc.lifeMax = 3077; // Which is actually 4k hp in expert mode
                            }
                            npc.scale = 1.1f;
                        }
                        break;
                    }

                case (NPCID.FireImp): {
                        npc.lifeMax = 112;
                        npc.value = 300;
                        npc.defense = 18;
                        break;
                    }
                    
                case (NPCID.GiantBat): {
                        npc.lifeMax = 105;
                        npc.damage = 49;
                        npc.value = 250;
                        npc.defense = 20;
                        npc.knockBackResist = 0.2f;
                        break;
                    }

                case (NPCID.GiantWormHead): {
                        npc.damage = 13;
                        npc.value = 90;
                        break;
                    }
                    
                //case (NPCID.GoblinShark):
                   // {
                        //npc.lifeMax = 100;
                        //npc.damage = 40;
                        //npc.value = 550;
                        //npc.defense = 10;
                        //npc.knockBackResist = 0.1f;
                      //  break;
                   // }
                case (NPCID.GoblinSorcerer): {
                        npc.lifeMax = 100;
                        npc.damage = 40;
                        npc.value = 550;
                        npc.defense = 10;
                        npc.knockBackResist = 0.1f;
                        break;
                    }

                case (NPCID.GoblinWarrior): {
                        npc.damage = 36;
                        npc.value = 350;
                        npc.scale = 1.1f;
                        break;
                    }

                case (NPCID.HeavySkeleton): {
                        npc.value = 600;
                        npc.defense = 41;
                        npc.scale = 1.15f;
                        break;
                    }

                case (NPCID.Hellbat): {
                        npc.damage = 46;
                        npc.scale = 1.1f;
                        break;
                    }

                case (NPCID.Hornet): {
                        npc.lavaImmune = true;
                        npc.value = 260;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.IlluminantBat): {
                        npc.value = 650;
                        npc.defense = 27;
                        npc.knockBackResist = 0.6f;
                        break;
                    }

                case (NPCID.IlluminantSlime): {
                        npc.value = 450;
                        npc.scale = 1.05f;
                        break;
                    }

                case (NPCID.KingSlime): {
                        npc.damage = 33;
                        npc.defense = 15;
                        npc.scale = 1.25f;
                        break;
                    }
                //Evaluates npcd on groups of hornets according to https://terraria.fandom.com/wiki/NPC_IDs
                case int n when ((n >= NPCID.BigHornetStingy && n <= NPCID.LittleHornetFatty) ||
                                (n >= NPCID.GiantMossHornet && n <= NPCID.LittleStinger) ||
                                n == NPCID.Hornet ||
                                n == NPCID.MossHornet ||
                                (n >= NPCID.HornetFatty && n <= NPCID.HornetStingy)): {
                        npc.lavaImmune = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.LavaSlime): {
                        npc.knockBackResist = 0.4f;
                        npc.scale = 1.1f;
                        break;
                    }

                case (NPCID.LeechBody): {
                        npc.defense = 17;
                        break;
                    }

                case (NPCID.LeechHead): {
                        npc.damage = 30;
                        npc.defense = 25;
                        break;
                    }

                case (NPCID.LittleEater): {
                        npc.value = 126;
                        npc.scale = 0.85f;
                        break;
                    }

                case (NPCID.ManEater): {
                        npc.damage = 45;
                        npc.lifeMax = 130;
                        npc.defense = 14;
                        npc.buffImmune[BuffID.Poisoned] = false;
                        break;
                    }

                case (NPCID.Mimic): {
                        npc.value = 2500;
                        break;
                    }

                case (NPCID.MeteorHead): {
                        npc.value = 200;
                        npc.defense = 10;
                        break;
                    }

                case (NPCID.MotherSlime): {
                        npc.value = 400;
                        npc.scale = 1.25f;
                        break;
                    }

                case (NPCID.Pixie): {
                        npc.buffImmune[BuffID.Poisoned] = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.PrimeCannon): {
                        npc.damage = 45;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.PrimeLaser): {
                        npc.defense = 30;
                        npc.damage = 40;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.PrimeSaw): {
                        npc.defense = 38;
                        npc.damage = 60;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.PrimeVice): {
                        npc.defense = 34;
                        npc.damage = 55;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.Probe): {
                        npc.defense = 30;
                        npc.damage = 55;
                        break;
                    }

                case (NPCID.Retinazer): {
                        npc.defense = 28;
                        npc.damage = 68; //legacy: 80
                        npc.lifeMax = 25000;
                        npc.value = 120000;
                        break;
                    }

                case (NPCID.SeekerBody): {
                        npc.lifeMax = 2000;
                        npc.defense = 40;
                        npc.damage = 75;
                        break;
                    }

                case (NPCID.SeekerHead): {
                        npc.lifeMax = 3000;
                        npc.defense = 40;
                        npc.damage = 100;
                        break;
                    }

                case (NPCID.SeekerTail): {
                        npc.lifeMax = 2000;
                        npc.defense = 40;
                        npc.damage = 55;
                        break;
                    }

                case (NPCID.ServantofCthulhu): {
                        npc.damage = 13;
                        npc.value = 10;
                        break;
                    }

                case (NPCID.SkeletonArcher): {
                        npc.value = 750;
                        npc.defense = 28;
                        npc.damage = 55;
                        break;
                    }

                case (NPCID.SkeletronHand): {
                        npc.value = 5000;
                        npc.defense = 14; //legacy: 12
                        npc.damage = 22; //legacy: 40
                        npc.lifeMax = 600;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.SkeletronHead): {
                        npc.value = 80000;
                        npc.defense = 12;
                        npc.damage = 35; //legacy: 50
                        npc.lifeMax = 4400;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.SkeletronPrime): {
                        npc.value = 250000;
                        npc.defense = 40;
                        npc.damage = 100;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.Slimer): {
                        npc.value = 450;
                        npc.defense = 50;
                        npc.damage = 80;
                        npc.scale = 1.1f;
                        break;
                    }

                case (NPCID.Slimer2): {
                        npc.value = 300;
                        npc.defense = 60;
                        npc.damage = 80;
                        npc.scale = 0.9f;
                        break;
                    }

                case (NPCID.Snatcher): {
                        npc.value = 300;
                        npc.damage = 34;
                        npc.buffImmune[BuffID.Poisoned] = false;
                        break;
                    }

                case (NPCID.SolarSpearman):
                    {
                        npc.value = 3000;
                        //npc.damage = 34;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.lavaImmune = true;
                        break;
                    }

                case (NPCID.SolarCrawltipedeHead):
                    {
                        npc.value = 10000;
                        //npc.damage = 34;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.lavaImmune = true;
                        break;
                    }

                case (NPCID.SolarDrakomire):
                    {
                        npc.value = 3000;
                        //npc.damage = 34;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.lavaImmune = true;
                        break;
                    }

                case (NPCID.SolarSroller):
                    {
                        npc.value = 2000;
                        //npc.damage = 34;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.lavaImmune = true;
                        break;
                    }

                case (NPCID.SolarCorite):
                    {
                        npc.value = 1000;
                        //npc.damage = 34;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.lavaImmune = true;
                        break;
                    }

                case (NPCID.SolarSolenian):
                    {
                        npc.value = 3000;
                        //npc.damage = 34;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.lavaImmune = true;
                        break;
                    }

                case (NPCID.Spazmatism): {
                        npc.lifeMax = 25000;
                        npc.value = 120000;
                        npc.damage = 80;
                        npc.defense = 35;
                        break;
                    }

                case (NPCID.SpikeBall): {
                        npc.scale = 1.5f;
                        npc.damage = 70;
                    
                        break;
                    }

                case (NPCID.TheDestroyer): {
                        npc.value = 200000;
                        npc.scale = 1.25f;
                        npc.damage = Main.expertMode ? 85 /* x4 in expert */: 150; //legacy: 200, vanilla: 70
                        npc.defense = 10; //legacy: 50, vanilla: 0
                        break;
                    }

                case (NPCID.TheDestroyerBody): {
                        npc.scale = 1.25f;
                        npc.damage = 60; //legacy: 60, vanilla: 55
                        npc.defense = 55; //legacy: 55, vanilla: 30
                        break;
                    }

                case (NPCID.TheDestroyerTail): {
                        npc.scale = 1.25f;
                        npc.damage = 80; //legacy: 80, vanilla: 40
                        break;
                    }

                case (NPCID.TheGroom): {
                        npc.value = 1000;
                        npc.damage = 45;
                        npc.lifeMax = 250;
                        break;
                    }

                case (NPCID.TheHungry): {
                        npc.value = 500;
                        npc.knockBackResist = 0.3f;
                        npc.damage = 50;
                        npc.lifeMax = 300;
                        npc.defense = 10;
                        break;
                    }

                case (NPCID.TheHungryII): {
                        npc.value = 300;
                        npc.damage = 50;
                        npc.lifeMax = 150;
                        npc.knockBackResist = 0.5f;
                        break;
                    }

                case (NPCID.Tim): {
                        npc.GivenName = "Tim Hjersted";
                        npc.value = 15000;
                        npc.damage = 100;
                        npc.lifeMax = 500;
                        npc.defense = 18;
                        npc.scale = 1f;
                        npc.knockBackResist = 0.2f;
                        break;
                    }

                case (NPCID.Unicorn): {
                        npc.value = 600;
                        npc.knockBackResist = 0.2f;
                        npc.damage = 85;
                        break;
                    }

                case (NPCID.VileSpit): {
                        npc.damage = 80;
                        break;
                    }

                case (NPCID.VoodooDemon): {
                        npc.defense = 10;
                        npc.damage = 42;
                        npc.lifeMax = 250;
                        break;
                    }

                case (NPCID.Vulture): {
                        npc.damage = 60;
                        npc.lifeMax = 100;
                        npc.value = 350;
                        break;
                    }

                case (NPCID.WanderingEye): {
                        npc.value = 600;
                        break;
                    }

                case (NPCID.WaterSphere): {
                        npc.damage = 30;
                        break;
                    }
                case (NPCID.WallofFlesh):
                    {
                        npc.damage = 100;
                        npc.lifeMax = 15000;
                        break;
                    }

                case (NPCID.Werewolf): {
                        npc.defense = 40;
                        npc.damage = 85;
                        npc.knockBackResist = 0.1f;
                        break;
                    }

                case (NPCID.Wraith): {
                        npc.defense = 18;
                        npc.damage = 75;
                        npc.lifeMax = 500;
                        npc.scale = 1.1f;
                        npc.knockBackResist = 0;
                        npc.value = 2300;
                        break;
                    }

                case (NPCID.WyvernBody): {
                        npc.defense = 20;
                        npc.damage = 42;
                        npc.lifeMax = 4000;
                        npc.value = 2000;
                        npc.buffImmune[BuffID.Poisoned] = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.WyvernBody2): {
                        npc.defense = 20;
                        npc.damage = 42;
                        npc.lifeMax = 4000;
                        npc.value = 2000;
                        npc.buffImmune[BuffID.Poisoned] = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.WyvernBody3): {
                        npc.defense = 20;
                        npc.damage = 42;
                        npc.lifeMax = 4000;
                        npc.value = 2000;
                        npc.buffImmune[BuffID.Poisoned] = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.WyvernHead): {
                        npc.lifeMax = 5200;
                        npc.buffImmune[BuffID.Poisoned] = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.WyvernLegs): {
                        npc.defense = 20;
                        npc.damage = 42;
                        npc.lifeMax = 4000;
                        npc.value = 2000;
                        npc.buffImmune[BuffID.Poisoned] = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.WyvernTail): {
                        npc.defense = 20;
                        npc.damage = 42;
                        npc.lifeMax = 4000;
                        npc.value = 2000;
                        npc.buffImmune[BuffID.Poisoned] = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.DarkMummy): {
                        npc.knockBackResist = 0.2f;
                        break;
                    }

                case (NPCID.LightMummy): {
                        npc.knockBackResist = 0.35f;
                        npc.damage = 85;
                        break;
                    }


                case int n when ((n >= NPCID.BigFemaleZombie && n <= NPCID.SmallFemaleZombie) ||
                                (n >= NPCID.BigTwiggyZombie && n <= NPCID.SmallZombie) ||
                                (n >= NPCID.ZombieDoctor && n <= NPCID.ZombiePixie) ||
                                (n >= NPCID.ZombieXmas && n <= NPCID.ZombieSweater) ||
                                (n >= NPCID.ArmedZombie && n <= NPCID.ArmedZombieCenx) ||
                                n == NPCID.Zombie ||
                                n == NPCID.BaldZombie ||
                                n == NPCID.ZombieEskimo ||
                                n == NPCID.FemaleZombie ||
                                (n >= NPCID.PincushionZombie && n <= NPCID.TwiggyZombie)): {
                        npc.value = 80;
                        break;
                    }
            }
        }

        #endregion

        public override void ScaleExpertStats(NPC npc, int numPlayers, float bossLifeScale)
        {
            //Only mess with it if it's one of our bosses
            if (npc.modNPC != null && npc.modNPC.mod == ModLoader.GetMod("tsorcRevamp") && npc.boss)
            {
                //These could've been simplified to one formula, but i'm leaving them like this so it's obvious what they're doing

                //Revert normalmode boss health nerf
                npc.lifeMax = (int)Math.Round(npc.lifeMax * 1.3f);

                //Counter expert mode automatic scaling
                npc.lifeMax = (int)Math.Round(npc.lifeMax / 2f);

                //Add 70% to the boss's health per extra player
                npc.lifeMax = (int)Math.Round(npc.lifeMax * (1f + (0.7f * ((float)bossLifeScale - 1f))));
            }

            //Let npc's do the rest of their normal scaling, including things like projectile damage or defense
            base.ScaleExpertStats(npc, numPlayers, bossLifeScale);
        }


        //BLOCKED NPCS
        public override void AI(NPC npc) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                if (npc.type == NPCID.BigRainZombie
                        || npc.type == NPCID.SmallRainZombie
                        || npc.type == NPCID.ZombieRaincoat
                        || npc.type == NPCID.Clown
                        || npc.type == NPCID.UmbrellaSlime
                        //NEW ADDITIONS
                        || npc.type == NPCID.CursedSkull
                        || npc.type == NPCID.BigHeadacheSkeleton
                        || npc.type == NPCID.SmallHeadacheSkeleton
                        || npc.type == NPCID.BigSlimedZombie
                        || npc.type == NPCID.RedSlime
                        || npc.type == NPCID.GreenSlime
                        || npc.type == NPCID.TheGroom
                        || npc.type == NPCID.Unicorn
                        || npc.type == NPCID.SantaClaus
                        || npc.type == NPCID.SnowmanGangsta
                        || npc.type == NPCID.MisterStabby
                        || npc.type == NPCID.SnowBalla
                        || npc.type == NPCID.ZombieEskimo
                        || npc.type == NPCID.PigronCorruption
                        || npc.type == NPCID.PigronHallow
                        || npc.type == NPCID.PigronCrimson
                        || npc.type == NPCID.FaceMonster
                        || npc.type == NPCID.SlimedZombie
                        || npc.type == NPCID.HeadacheSkeleton
                        || npc.type == NPCID.AngryNimbus
                        || npc.type == NPCID.FloatyGross
                        || npc.type == NPCID.SkeletonSniper
                        || npc.type == NPCID.TacticalSkeleton
                        || npc.type == NPCID.HoppinJack
                        || npc.type == NPCID.ZombieDoctor
                        || npc.type == NPCID.SkeletonTopHat
                        || npc.type == NPCID.SkeletonAstonaut
                        || npc.type == NPCID.ZombieSuperman
                        || npc.type == NPCID.ZombieXmas
                        || npc.type == NPCID.ZombieSweater
                        || npc.type == NPCID.SlimeRibbonWhite
                        || npc.type == NPCID.SlimeRibbonYellow
                        || npc.type == NPCID.SlimeRibbonGreen
                        || npc.type == NPCID.SlimeRibbonRed
                        || npc.type == NPCID.BunnyXmas
                        || npc.type == NPCID.ZombieElf
                        || npc.type == NPCID.ZombieElfBeard
                        || npc.type == NPCID.ZombieElfGirl
                        || npc.type == NPCID.ArmedZombieEskimo
                        || npc.type == NPCID.ArmedZombieSlimed
                        || npc.type == NPCID.BoneThrowingSkeleton2
                        || npc.type == NPCID.BoneThrowingSkeleton3
                        || npc.type == NPCID.Butcher
                        || npc.type == NPCID.BloodZombie
                        || npc.type == NPCID.TheBride
                        || npc.type == NPCID.MartianProbe
                        //|| npc.type == NPCID.WindyBalloon
                        || npc.type == NPCID.UmbrellaSlime
                        || npc.type == NPCID.BoundGoblin
                        || npc.type == NPCID.BoundMechanic
                        || npc.type == NPCID.BoundWizard) {
                    npc.active = false;
                } 
            }

            if ((npc.friendly) && (npc.townNPC == true)) { //town NPCs are immortal (why was i using a hp check?)
                npc.dontTakeDamage = true;
                npc.dontTakeDamageFromHostiles = true;
            }

        }

        public override bool PreAI(NPC npc) {

            if (npc.type == NPCID.EyeofCthulhu) {
                AI_EoC(npc);
                return false;
            }

            #region Lunar Towers

            if (npc.type == NPCID.LunarTowerNebula || npc.type == NPCID.LunarTowerSolar || npc.type == NPCID.LunarTowerStardust || npc.type == NPCID.LunarTowerVortex) {
                if (npc.ai[2] == 1f) {
                    npc.velocity = Vector2.UnitY * npc.velocity.Length();
                    if (npc.velocity.Y < 0.25f) {
                        npc.velocity.Y += 0.02f;
                    }
                    if (npc.velocity.Y > 0.25f) {
                        npc.velocity.Y -= 0.02f;
                    }
                    npc.dontTakeDamage = true;
                    npc.ai[1]++;
                    if (npc.ai[1] > 120f) {
                        npc.Opacity = 1f - (npc.ai[1] - 120f) / 60f;
                    }
                    int num474 = 6;
                    switch (npc.type) {
                        case 517:
                            num474 = 127;
                            break;
                        case 422:
                            num474 = 229;
                            break;
                        case 507:
                            num474 = 242;
                            break;
                        case 493:
                            num474 = 135;
                            break;
                    }
                    if (Main.rand.Next(5) == 0 && npc.ai[1] < 120f) {
                        for (int num475 = 0; num475 < 3; num475++) {
                            Dust dust76 = Main.dust[Dust.NewDust(npc.Left, npc.width, npc.height / 2, num474)];
                            dust76.position = npc.Center + Vector2.UnitY.RotatedByRandom(4.1887903213500977) * new Vector2((float)npc.width * 1.5f, (float)npc.height * 1.1f) * 0.8f * (0.8f + Main.rand.NextFloat() * 0.2f);
                            dust76.velocity.X = 0f;
                            dust76.velocity.Y = (0f - Math.Abs(dust76.velocity.Y - (float)num475 + npc.velocity.Y - 4f)) * 3f;
                            dust76.noGravity = true;
                            dust76.fadeIn = 1f;
                            dust76.scale = 1f + Main.rand.NextFloat() + (float)num475 * 0.3f;
                        }
                    }
                    if (npc.ai[1] < 150f) {
                        for (int num476 = 0; num476 < 3; num476++) {
                            if (Main.rand.Next(4) == 0) {
                                Dust dust77 = Main.dust[Dust.NewDust(npc.Top + new Vector2((float)(-npc.width) * (0.33f - 0.11f * (float)num476), -20f), (int)((float)npc.width * (0.66f - 0.22f * (float)num476)), 20, num474)];
                                dust77.velocity.X = 0f;
                                dust77.velocity.Y = (0f - Math.Abs(dust77.velocity.Y - (float)num476 + npc.velocity.Y - 4f)) * (1f + npc.ai[1] / 180f * 0.5f);
                                dust77.noGravity = true;
                                dust77.fadeIn = 1f;
                                dust77.scale = 1f + Main.rand.NextFloat() + (float)num476 * 0.3f;
                            }
                        }
                    }
                    if (Main.rand.Next(5) == 0 && npc.ai[1] < 150f) {
                        for (int num477 = 0; num477 < 3; num477++) {
                            Vector2 position6 = npc.Center + Vector2.UnitY.RotatedByRandom(4.1887903213500977) * new Vector2(npc.width, npc.height) * 0.7f * Main.rand.NextFloat();
                            float num478 = 1f + Main.rand.NextFloat() * 2f + npc.ai[1] / 180f * 4f;
                            for (int num479 = 0; num479 < 6; num479++) {
                                Dust dust78 = Main.dust[Dust.NewDust(position6, 4, 4, num474)];
                                dust78.position = position6;
                                dust78.velocity.X *= num478;
                                dust78.velocity.Y = (0f - Math.Abs(dust78.velocity.Y)) * num478;
                                dust78.noGravity = true;
                                dust78.fadeIn = 1f;
                                dust78.scale = 1.5f + Main.rand.NextFloat() + (float)num479 * 0.13f;
                            }
                            Main.PlaySound(3, position6, Utils.SelectRandom<int>(Main.rand, 1, 18));
                        }
                    }
                    if (Main.rand.Next(3) != 0 && npc.ai[1] < 150f) {
                        Dust dust79 = Main.dust[Dust.NewDust(npc.Left, npc.width, npc.height / 2, 241)];
                        dust79.position = npc.Center + Vector2.UnitY.RotatedByRandom(4.1887903213500977) * new Vector2(npc.width / 2, npc.height / 2) * (0.8f + Main.rand.NextFloat() * 0.2f);
                        dust79.velocity.X = 0f;
                        dust79.velocity.Y = Math.Abs(dust79.velocity.Y) * 0.25f;
                    }
                    if (npc.ai[1] % 60f == 1f) {
                        Main.PlaySound(4, npc.Center, 22);
                    }
                    if (npc.ai[1] >= 180f) {
                        npc.life = 0;
                        npc.HitEffect(0, 1337.0);
                        npc.checkDead();
                    }
                    return false;
                }
                if (npc.ai[3] > 0f) {
                    bool flag98 = npc.dontTakeDamage;
                    switch (npc.type) {
                        case 517:
                            flag98 = NPC.ShieldStrengthTowerSolar != 0;
                            break;
                        case 422:
                            flag98 = NPC.ShieldStrengthTowerVortex != 0;
                            break;
                        case 507:
                            flag98 = NPC.ShieldStrengthTowerNebula != 0;
                            break;
                        case 493:
                            flag98 = NPC.ShieldStrengthTowerStardust != 0;
                            break;
                    }
                    if (flag98 != npc.dontTakeDamage) {
                        Main.PlaySound(SoundID.NPCDeath58, npc.position);
                    }
                    else if (npc.ai[3] == 1f) {
                        Main.PlaySound(SoundID.NPCDeath3, npc.position);
                    }
                    npc.ai[3]++;
                    if (npc.ai[3] > 120f) {
                        npc.ai[3] = 0f;
                    }
                }
                switch (npc.type) {
                    case 517:
                        npc.dontTakeDamage = NPC.ShieldStrengthTowerSolar != 0;
                        break;
                    case 422:
                        npc.dontTakeDamage = NPC.ShieldStrengthTowerVortex != 0;
                        break;
                    case 507:
                        npc.dontTakeDamage = NPC.ShieldStrengthTowerNebula != 0;
                        break;
                    case 493:
                        npc.dontTakeDamage = NPC.ShieldStrengthTowerStardust != 0;
                        break;
                }
                npc.TargetClosest(faceTarget: false);
                if (Main.player[npc.target].Distance(npc.Center) > 2000f) {
                    npc.localAI[0]++;
                }
                if (npc.localAI[0] >= 60f && Main.netMode != 1) {
                    npc.localAI[0] = 0f;
                    npc.netUpdate = true;
                    npc.life = (int)MathHelper.Clamp(npc.life + 200, 0f, npc.lifeMax);
                }
                else {
                    npc.localAI[0] = 0f;
                }
                npc.velocity = new Vector2(0f, (float)Math.Sin((float)Math.PI * 2f * npc.ai[0] / 300f) * 0.5f);
                npc.ai[0]++;
                if (npc.ai[0] >= 300f) {
                    npc.ai[0] = 0f;
                    npc.netUpdate = true;
                }
                if (npc.type == 493) {
                    if (Main.rand.Next(5) == 0) {
                        Dust dust80 = Main.dust[Dust.NewDust(npc.Left, npc.width, npc.height / 2, 241)];
                        dust80.position = npc.Center + Vector2.UnitY.RotatedByRandom(2.0943951606750488) * new Vector2(npc.width / 2, npc.height / 2) * (0.8f + Main.rand.NextFloat() * 0.2f);
                        dust80.velocity.X = 0f;
                        dust80.velocity.Y = Math.Abs(dust80.velocity.Y) * 0.25f;
                    }
                    for (int num481 = 0; num481 < 3; num481++) {
                        if (Main.rand.Next(5) == 0) {
                            Dust dust58 = Main.dust[Dust.NewDust(npc.Top + new Vector2((float)(-npc.width) * (0.33f - 0.11f * (float)num481), -20f), (int)((float)npc.width * (0.66f - 0.22f * (float)num481)), 20, 135)];
                            dust58.velocity.X = 0f;
                            dust58.velocity.Y = (0f - Math.Abs(dust58.velocity.Y - (float)num481 + npc.velocity.Y - 4f)) * 1f;
                            dust58.noGravity = true;
                            dust58.fadeIn = 1f;
                            dust58.scale = 1f + Main.rand.NextFloat() + (float)num481 * 0.3f;
                        }
                    }
                    if (npc.ai[1] > 0f) {
                        npc.ai[1]--;
                    }
                    if (Main.netMode != 1 && npc.ai[1] <= 0f && Main.player[npc.target].active && !Main.player[npc.target].dead && npc.Distance(Main.player[npc.target].Center) < 1080f && Main.player[npc.target].position.Y - npc.position.Y < 400f) {
                        List<int> list = new List<int>();
                        if (NPC.CountNPCS(405) + NPC.CountNPCS(406) < 2) {
                            list.Add(405);
                        }
                        if (NPC.CountNPCS(402) < 2) {
                            list.Add(402);
                        }
                        if (NPC.CountNPCS(407) < 1) {
                            list.Add(407);
                        }
                        if (list.Count > 0) {
                            int num482 = Utils.SelectRandom(Main.rand, list.ToArray());
                            npc.ai[1] = 30 * Main.rand.Next(5, 16);
                            int num483 = Main.rand.Next(3, 6);
                            int num484 = Main.rand.Next(0, 4);
                            int num485 = 0;
                            List<Tuple<Vector2, int, int>> list2 = new List<Tuple<Vector2, int, int>>();
                            List<Vector2> list3 = new List<Vector2>();
                            list2.Add(Tuple.Create(npc.Top - Vector2.UnitY * 120f, num483, 0));
                            int num486 = 0;
                            int num487 = list2.Count;
                            while (list2.Count > 0) {
                                Vector2 item = list2[0].Item1;
                                int num488 = 1;
                                int num489 = 1;
                                if (num486 > 0 && num484 > 0 && (Main.rand.Next(3) != 0 || num486 == 1)) {
                                    num489 = Main.rand.Next(Math.Max(1, list2[0].Item2));
                                    num488++;
                                    num484--;
                                }
                                for (int num490 = 0; num490 < num488; num490++) {
                                    int num492 = list2[0].Item3;
                                    if (num486 == 0) {
                                        num492 = Utils.SelectRandom<int>(Main.rand, -1, 1);
                                    }
                                    else if (num490 == 1) {
                                        num492 *= -1;
                                    }
                                    float num493 = ((num486 % 2 == 0) ? 0f : ((float)Math.PI)) + (0.5f - Main.rand.NextFloat()) * ((float)Math.PI / 4f) + (float)num492 * ((float)Math.PI / 4f) * (float)(num486 % 2 == 0).ToDirectionInt();
                                    float scaleFactor9 = 100f + 50f * Main.rand.NextFloat();
                                    int num494 = list2[0].Item2;
                                    if (num490 != 0) {
                                        num494 = num489;
                                    }
                                    if (num486 == 0) {
                                        num493 = (0.5f - Main.rand.NextFloat()) * ((float)Math.PI / 4f);
                                        scaleFactor9 = 100f + 100f * Main.rand.NextFloat();
                                    }
                                    Vector2 value52 = (-Vector2.UnitY).RotatedBy(num493) * scaleFactor9;
                                    if (num494 - 1 < 0) {
                                        value52 = Vector2.Zero;
                                    }
                                    num485 = Projectile.NewProjectile(item.X, item.Y, value52.X, value52.Y, 540, 0, 0f, Main.myPlayer, (float)(-num486) * 10f, 0.5f + Main.rand.NextFloat() * 0.5f);
                                    list3.Add(item + value52);
                                    if (num486 < num483 && list2[0].Item2 > 0) {
                                        list2.Add(Tuple.Create(item + value52, num494 - 1, num492));
                                    }
                                }
                                list2.Remove(list2[0]);
                                int num495 = num487 - 1;
                                num487 = num495;
                                if (num495 == 0) {
                                    num487 = list2.Count;
                                    num486++;
                                }
                            }
                            Main.projectile[num485].localAI[0] = num482;
                        }
                        else {
                            npc.ai[1] = 30f;
                        }
                    }
                }
                if (npc.type == 507) {
                    if (Main.rand.Next(5) == 0) {
                        Dust dust59 = Main.dust[Dust.NewDust(npc.Left, npc.width, npc.height / 2, 241)];
                        dust59.position = npc.Center + Vector2.UnitY.RotatedByRandom(2.0943951606750488) * new Vector2(npc.width / 2, npc.height / 2) * (0.8f + Main.rand.NextFloat() * 0.2f);
                        dust59.velocity.X = 0f;
                        dust59.velocity.Y = Math.Abs(dust59.velocity.Y) * 0.25f;
                    }
                    for (int num496 = 0; num496 < 3; num496++) {
                        if (Main.rand.Next(5) == 0) {
                            Dust dust60 = Main.dust[Dust.NewDust(npc.Top + new Vector2((float)(-npc.width) * (0.33f - 0.11f * (float)num496), -20f), (int)((float)npc.width * (0.66f - 0.22f * (float)num496)), 20, 242)];
                            dust60.velocity.X = 0f;
                            dust60.velocity.Y = (0f - Math.Abs(dust60.velocity.Y - (float)num496 + npc.velocity.Y - 4f)) * 1f;
                            dust60.noGravity = true;
                            dust60.fadeIn = 1f;
                            dust60.color = Color.Black;
                            dust60.scale = 1f + Main.rand.NextFloat() + (float)num496 * 0.3f;
                        }
                    }
                }
                if (npc.type == 422) {
                    if (Main.rand.Next(5) == 0) {
                        Dust dust61 = Main.dust[Dust.NewDust(npc.Left, npc.width, npc.height / 2, 241)];
                        dust61.position = npc.Center + Vector2.UnitY.RotatedByRandom(2.0943951606750488) * new Vector2(npc.width / 2, npc.height / 2) * (0.8f + Main.rand.NextFloat() * 0.2f);
                        dust61.velocity.X = 0f;
                        dust61.velocity.Y = Math.Abs(dust61.velocity.Y) * 0.25f;
                    }
                    for (int num497 = 0; num497 < 3; num497++) {
                        if (Main.rand.Next(5) == 0) {
                            Dust dust62 = Main.dust[Dust.NewDust(npc.Top + new Vector2((float)(-npc.width) * (0.33f - 0.11f * (float)num497), -20f), (int)((float)npc.width * (0.66f - 0.22f * (float)num497)), 20, 229)];
                            dust62.velocity.X = 0f;
                            dust62.velocity.Y = (0f - Math.Abs(dust62.velocity.Y - (float)num497 + npc.velocity.Y - 4f)) * 1f;
                            dust62.noGravity = true;
                            dust62.fadeIn = 1f;
                            dust62.color = Color.Black;
                            dust62.scale = 1f + Main.rand.NextFloat() + (float)num497 * 0.3f;
                        }
                    }
                    if (npc.ai[1] > 0f) {
                        npc.ai[1]--;
                    }
                    if (Main.netMode != 1 && npc.ai[1] <= 0f && Main.player[npc.target].active && !Main.player[npc.target].dead && npc.Distance(Main.player[npc.target].Center) < 3240f && !Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0)) {
                        npc.ai[1] = 60 + Main.rand.Next(120);
                        Point point14 = Main.player[npc.target].Top.ToTileCoordinates();
                        bool flag99 = NPC.CountNPCS(427) + NPC.CountNPCS(426) < 14;
                        for (int num498 = 0; num498 < 10; num498++) {
                            if (WorldGen.SolidTile(point14.X, point14.Y)) {
                                break;
                            }
                            if (point14.Y <= 10) {
                                break;
                            }
                            point14.Y--;
                        }
                        if (flag99) {
                            Projectile.NewProjectile(point14.X * 16 + 8, point14.Y * 16 + 24, 0f, 0f, 579, 0, 0f, Main.myPlayer);
                        }
                        else {
                            Projectile.NewProjectile(point14.X * 16 + 8, point14.Y * 16 + 17, 0f, 0f, 578, 0, 1f, Main.myPlayer);
                        }
                    }
                    if (Main.netMode != 1 && npc.ai[1] <= 0f && Main.player[npc.target].active && !Main.player[npc.target].dead && npc.Distance(Main.player[npc.target].Center) < 1080f && Main.player[npc.target].position.Y - npc.position.Y < 400f && NPC.CountNPCS(427) + NPC.CountNPCS(426) * 3 + NPC.CountNPCS(428) < 20) {
                        npc.ai[1] = 420 + Main.rand.Next(360);
                        Point point2 = npc.Center.ToTileCoordinates();
                        Point point3 = Main.player[npc.target].Center.ToTileCoordinates();
                        Vector2 vector134 = Main.player[npc.target].Center - npc.Center;
                        int num499 = 20;
                        int num500 = 3;
                        int num501 = 8;
                        int num503 = 2;
                        int num504 = 0;
                        bool flag100 = false;
                        if (vector134.Length() > 2000f) {
                            flag100 = true;
                        }
                        while (!flag100 && num504 < 100) {
                            num504++;
                            int num505 = Main.rand.Next(point3.X - num499, point3.X + num499 + 1);
                            int num506 = Main.rand.Next(point3.Y - num499, point3.Y + num499 + 1);
                            if ((num506 < point3.Y - num501 || num506 > point3.Y + num501 || num505 < point3.X - num501 || num505 > point3.X + num501) && (num506 < point2.Y - num500 || num506 > point2.Y + num500 || num505 < point2.X - num500 || num505 > point2.X + num500) && !Main.tile[num505, num506].nactive()) {
                                bool flag102 = true;
                                if (flag102 && Main.tile[num505, num506].lava()) {
                                    flag102 = false;
                                }
                                if (flag102 && Collision.SolidTiles(num505 - num503, num505 + num503, num506 - num503, num506 + num503)) {
                                    flag102 = false;
                                }
                                if (flag102 && !Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0)) {
                                    flag102 = false;
                                }
                                if (flag102) {
                                    Projectile.NewProjectile(num505 * 16 + 8, num506 * 16 + 8, 0f, 0f, 579, 0, 0f, Main.myPlayer);
                                    flag100 = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (npc.type != 517) {
                    return false;
                }
                if (Main.rand.Next(5) == 0) {
                    Dust dust63 = Main.dust[Dust.NewDust(npc.Left, npc.width, npc.height / 2, 241)];
                    dust63.position = npc.Center + Vector2.UnitY.RotatedByRandom(2.0943951606750488) * new Vector2(npc.width / 2, npc.height / 2) * (0.8f + Main.rand.NextFloat() * 0.2f);
                    dust63.velocity.X = 0f;
                    dust63.velocity.Y = Math.Abs(dust63.velocity.Y) * 0.25f;
                }
                for (int num507 = 0; num507 < 3; num507++) {
                    if (Main.rand.Next(5) == 0) {
                        Dust dust64 = Main.dust[Dust.NewDust(npc.Top + new Vector2((float)(-npc.width) * (0.33f - 0.11f * (float)num507), -20f), (int)((float)npc.width * (0.66f - 0.22f * (float)num507)), 20, 6)];
                        dust64.velocity.X = 0f;
                        dust64.velocity.Y = (0f - Math.Abs(dust64.velocity.Y - (float)num507 + npc.velocity.Y - 4f)) * 1f;
                        dust64.noGravity = true;
                        dust64.fadeIn = 1f;
                        dust64.scale = 1f + Main.rand.NextFloat() + (float)num507 * 0.3f;
                    }
                }
                if (npc.ai[1] > 0f) {
                    npc.ai[1]--;
                }
                if (Main.netMode != 1 && npc.ai[1] <= 0f && Main.player[npc.target].active && !Main.player[npc.target].dead && npc.Distance(Main.player[npc.target].Center) < 1080f && Main.player[npc.target].position.Y - npc.position.Y < 700f) {
                    Vector2 vector136 = npc.Top + new Vector2((float)(-npc.width) * 0.33f, -20f) + new Vector2((float)npc.width * 0.66f, 20f) * Utils.RandomVector2(Main.rand, 0f, 1f);
                    Vector2 velocity8 = -Vector2.UnitY.RotatedByRandom(0.78539818525314331) * (7f + Main.rand.NextFloat() * 5f);
                    int num508 = NPC.NewNPC((int)vector136.X, (int)vector136.Y, 519, npc.whoAmI);
                    Main.npc[num508].velocity = velocity8;
                    Main.npc[num508].netUpdate = true;
                    npc.ai[1] = 60f;
                }
                return false;
            }

            #endregion

            if (npc.type == NPCID.GoblinTinkerer) {
                npc.GivenName = "Elijah";
                return true;
            }
            if (npc.type == NPCID.Mechanic) {
                npc.GivenName = "Asha";
                return true;
            }
            if (npc.type == NPCID.Wizard) {
                npc.GivenName = "Araz";
                return true;
            }

            else return base.PreAI(npc);
        }

        #region Eye of Cthulhu AI

        private void AI_EoC(NPC npc) {
            bool flag24 = false;
            if (Main.expertMode && (double)npc.life < (double)npc.lifeMax * 0.12) {
                flag24 = true;
            }
            bool finalDesperation = false;
            if (Main.expertMode && (double)npc.life < (double)npc.lifeMax * 0.04) {
                finalDesperation = true;
            }
            float num910 = 20f;
            if (finalDesperation) {
                num910 = 10f;
            }
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active) {
                npc.TargetClosest();
            }
            bool dead = Main.player[npc.target].dead;
            float num1021 = npc.position.X + (float)(npc.width / 2) - Main.player[npc.target].position.X - (float)(Main.player[npc.target].width / 2);
            float num1132 = npc.position.Y + (float)npc.height - 59f - Main.player[npc.target].position.Y - (float)(Main.player[npc.target].height / 2);
            float num1243 = (float)Math.Atan2(num1132, num1021) + 1.57f;
            if (num1243 < 0f) {
                num1243 += 6.283f;
            }
            else if ((double)num1243 > 6.283) {
                num1243 -= 6.283f;
            }
            float num1354 = 0f;
            if (npc.ai[0] == 0f && npc.ai[1] == 0f) {
                num1354 = 0.02f;
            }
            if (npc.ai[0] == 0f && npc.ai[1] == 2f && npc.ai[2] > 40f) {
                num1354 = 0.05f;
            }
            if (npc.ai[0] == 3f && npc.ai[1] == 0f) {
                num1354 = 0.05f;
            }
            if (npc.ai[0] == 3f && npc.ai[1] == 2f && npc.ai[2] > 40f) {
                num1354 = 0.08f;
            }
            if (npc.ai[0] == 3f && npc.ai[1] == 4f && npc.ai[2] > num910) {
                num1354 = 0.15f;
            }
            if (npc.ai[0] == 3f && npc.ai[1] == 5f) {
                num1354 = 0.05f;
            }
            if (Main.expertMode) {
                num1354 *= 1.5f;
            }
            if (finalDesperation && Main.expertMode) {
                num1354 = 0f;
            }
            if (npc.rotation < num1243) {
                if ((double)(num1243 - npc.rotation) > 3.1415) {
                    npc.rotation -= num1354;
                }
                else {
                    npc.rotation += num1354;
                }
            }
            else if (npc.rotation > num1243) {
                if ((double)(npc.rotation - num1243) > 3.1415) {
                    npc.rotation += num1354;
                }
                else {
                    npc.rotation -= num1354;
                }
            }
            if (npc.rotation > num1243 - num1354 && npc.rotation < num1243 + num1354) {
                npc.rotation = num1243;
            }
            if (npc.rotation < 0f) {
                npc.rotation += 6.283f;
            }
            else if ((double)npc.rotation > 6.283) {
                npc.rotation -= 6.283f;
            }
            if (npc.rotation > num1243 - num1354 && npc.rotation < num1243 + num1354) {
                npc.rotation = num1243;
            }
            if (Main.rand.Next(5) == 0) {
                int num1465 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y + (float)npc.height * 0.25f), npc.width, (int)((float)npc.height * 0.5f), 5, npc.velocity.X, 2f);
                Main.dust[num1465].velocity.X *= 0.5f;
                Main.dust[num1465].velocity.Y *= 0.1f;
            }
            if (Main.dayTime || dead) {
                npc.velocity.Y -= 0.04f;
                if (npc.timeLeft > 10) {
                    npc.timeLeft = 10;
                }
                return;
            }
            if (npc.ai[0] == 0f) {
                if (npc.ai[1] == 0f) {
                    float num2 = 5f;
                    float num113 = 0.04f;
                    if (Main.expertMode) {
                        num113 = 0.15f;
                        num2 = 7f;
                    }
                    Vector2 vector = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                    float num224 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - vector.X;
                    float num335 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - 200f - vector.Y;
                    float num446 = (float)Math.Sqrt(num224 * num224 + num335 * num335);
                    float num557 = num446;
                    num446 = num2 / num446;
                    num224 *= num446;
                    num335 *= num446;
                    if (npc.velocity.X < num224) {
                        npc.velocity.X += num113;
                        if (npc.velocity.X < 0f && num224 > 0f) {
                            npc.velocity.X += num113;
                        }
                    }
                    else if (npc.velocity.X > num224) {
                        npc.velocity.X -= num113;
                        if (npc.velocity.X > 0f && num224 < 0f) {
                            npc.velocity.X -= num113;
                        }
                    }
                    if (npc.velocity.Y < num335) {
                        npc.velocity.Y += num113;
                        if (npc.velocity.Y < 0f && num335 > 0f) {
                            npc.velocity.Y += num113;
                        }
                    }
                    else if (npc.velocity.Y > num335) {
                        npc.velocity.Y -= num113;
                        if (npc.velocity.Y > 0f && num335 < 0f) {
                            npc.velocity.Y -= num113;
                        }
                    }
                    npc.ai[2] += 1f;
                    float num644 = 600f;
                    if (Main.expertMode) {
                        num644 *= 0.35f;
                    }
                    if (npc.ai[2] >= num644) {
                        npc.ai[1] = 1f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                        npc.target = 255;
                        npc.netUpdate = true;
                    }
                    else if ((npc.position.Y + (float)npc.height < Main.player[npc.target].position.Y && num557 < 500f) || (Main.expertMode && num557 < 500f)) {
                        if (!Main.player[npc.target].dead) {
                            npc.ai[3] += 1f;
                        }
                        float num655 = 110f;
                        if (Main.expertMode) {
                            num655 *= 0.4f;
                        }
                        if (npc.ai[3] >= num655) {
                            npc.ai[3] = 0f;
                            npc.rotation = num1243;
                            float num666 = 5f;
                            if (Main.expertMode) {
                                num666 = 6f;
                            }
                            float num677 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - vector.X;
                            float num689 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - vector.Y;
                            float num700 = (float)Math.Sqrt(num677 * num677 + num689 * num689);
                            num700 = num666 / num700;
                            Vector2 position = vector;
                            Vector2 vector112 = default(Vector2);
                            vector112.X = num677 * num700;
                            vector112.Y = num689 * num700;
                            position.X += vector112.X * 10f;
                            position.Y += vector112.Y * 10f;
                            if (Main.netMode != 1) {
                                int num711 = NPC.NewNPC((int)position.X, (int)position.Y, 5);
                                Main.npc[num711].velocity.X = vector112.X;
                                Main.npc[num711].velocity.Y = vector112.Y;
                                if (Main.netMode == 2 && num711 < 200) {
                                    NetMessage.SendData(23, -1, -1, null, num711);
                                }
                            }
                            Main.PlaySound(3, (int)position.X, (int)position.Y);
                            for (int m = 0; m < 10; m++) {
                                Dust.NewDust(position, 20, 20, 5, vector112.X * 0.4f, vector112.Y * 0.4f);
                            }
                        }
                    }
                }
                else if (npc.ai[1] == 1f) {
                    npc.rotation = num1243;
                    float num722 = 6f;
                    if (Main.expertMode) {
                        num722 = 7f;
                    }
                    Vector2 vector163 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                    float num733 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - vector163.X;
                    float num744 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - vector163.Y;
                    float num755 = (float)Math.Sqrt(num733 * num733 + num744 * num744);
                    num755 = num722 / num755;
                    npc.velocity.X = num733 * num755;
                    npc.velocity.Y = num744 * num755;
                    npc.ai[1] = 2f;
                    npc.netUpdate = true;
                    if (npc.netSpam > 10) {
                        npc.netSpam = 10;
                    }
                }
                else if (npc.ai[1] == 2f) {
                    npc.ai[2] += 1f;
                    if (npc.ai[2] >= 40f) {
                        npc.velocity *= 0.98f;
                        if (Main.expertMode) {
                            npc.velocity *= 0.985f;
                        }
                        if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1) {
                            npc.velocity.X = 0f;
                        }
                        if ((double)npc.velocity.Y > -0.1 && (double)npc.velocity.Y < 0.1) {
                            npc.velocity.Y = 0f;
                        }
                    }
                    else {
                        npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - 1.57f;
                    }
                    int num766 = 150;
                    if (Main.expertMode) {
                        num766 = 100;
                    }
                    if (npc.ai[2] >= (float)num766) {
                        npc.ai[3] += 1f;
                        npc.ai[2] = 0f;
                        npc.target = 255;
                        npc.rotation = num1243;
                        if (npc.ai[3] >= 3f) {
                            npc.ai[1] = 0f;
                            npc.ai[3] = 0f;
                        }
                        else {
                            npc.ai[1] = 1f;
                        }
                    }
                }
                float num777 = 0.5f;
                if (Main.expertMode) {
                    num777 = 0.65f;
                }
                if ((float)npc.life < (float)npc.lifeMax * num777) {
                    npc.ai[0] = 1f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.netUpdate = true;
                    if (npc.netSpam > 10) {
                        npc.netSpam = 10;
                    }
                }
                return;
            }
            if (npc.ai[0] == 1f || npc.ai[0] == 2f) {
                if (npc.ai[0] == 1f) {
                    npc.ai[2] += 0.005f;
                    if ((double)npc.ai[2] > 0.5) {
                        npc.ai[2] = 0.5f;
                    }
                }
                else {
                    npc.ai[2] -= 0.005f;
                    if (npc.ai[2] < 0f) {
                        npc.ai[2] = 0f;
                    }
                }
                npc.rotation += npc.ai[2];
                npc.ai[1] += 1f;
                if (Main.expertMode && npc.ai[1] % 20f == 0f) {
                    float num788 = 5f;
                    Vector2 vector174 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                    float num800 = Main.rand.Next(-200, 200);
                    float num811 = Main.rand.Next(-200, 200);
                    float num822 = (float)Math.Sqrt(num800 * num800 + num811 * num811);
                    num822 = num788 / num822;
                    Vector2 position2 = vector174;
                    Vector2 vector185 = default(Vector2);
                    vector185.X = num800 * num822;
                    vector185.Y = num811 * num822;
                    position2.X += vector185.X * 10f;
                    position2.Y += vector185.Y * 10f;
                    if (Main.netMode != 1) {
                        int num833 = NPC.NewNPC((int)position2.X, (int)position2.Y, 5);
                        Main.npc[num833].velocity.X = vector185.X;
                        Main.npc[num833].velocity.Y = vector185.Y;
                        if (Main.netMode == 2 && num833 < 200) {
                            NetMessage.SendData(23, -1, -1, null, num833);
                        }
                    }
                    for (int n = 0; n < 10; n++) {
                        Dust.NewDust(position2, 20, 20, 5, vector185.X * 0.4f, vector185.Y * 0.4f);
                    }
                }
                if (npc.ai[1] == 100f) {
                    npc.ai[0] += 1f;
                    npc.ai[1] = 0f;
                    if (npc.ai[0] == 3f) {
                        npc.ai[2] = 0f;
                    }
                    else {
                        Main.PlaySound(3, (int)npc.position.X, (int)npc.position.Y);
                        for (int num844 = 0; num844 < 2; num844++) {
                            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), 8);
                            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), 7);
                            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), 6);
                        }
                        for (int num855 = 0; num855 < 20; num855++) {
                            Dust.NewDust(npc.position, npc.width, npc.height, 5, (float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f);
                        }
                        Main.PlaySound(15, (int)npc.position.X, (int)npc.position.Y, 0);
                    }
                }
                Dust.NewDust(npc.position, npc.width, npc.height, 5, (float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f);
                npc.velocity.X *= 0.98f;
                npc.velocity.Y *= 0.98f;
                if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1) {
                    npc.velocity.X = 0f;
                }
                if ((double)npc.velocity.Y > -0.1 && (double)npc.velocity.Y < 0.1) {
                    npc.velocity.Y = 0f;
                }
                return;
            }
            npc.defense = 0;
            //npc.damage = 22; //does nothing!

            if (Main.expertMode) {
                if (flag24) { //third phase (start of faster dashing)
                    npc.damage = (int)(22.5f * Main.expertDamage); //45 (38 - 52)
                    npc.defense = -15;
                }
                if (finalDesperation) { //final phase (nonstop dashing)
                    npc.damage = (int)(25f * Main.expertDamage); //50 (43 - 58)
                    npc.defense = -30;
                }
                else { //second phase (mouth open)
                    npc.damage = (int)(22.5f * Main.expertDamage); //45 (38 - 52)
                }
            }
            if (npc.ai[1] == 0f && flag24) {
                npc.ai[1] = 5f;
            }
            if (npc.ai[1] == 0f) {
                float num866 = 6f;
                float num877 = 0.07f;
                Vector2 vector196 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                float num888 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - vector196.X;
                float num899 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - 120f - vector196.Y;
                float num911 = (float)Math.Sqrt(num888 * num888 + num899 * num899);
                if (num911 > 400f && Main.expertMode) {
                    num866 += 1f;
                    num877 += 0.05f;
                    if (num911 > 600f) {
                        num866 += 1f;
                        num877 += 0.05f;
                        if (num911 > 800f) {
                            num866 += 1f;
                            num877 += 0.05f;
                        }
                    }
                }
                num911 = num866 / num911;
                num888 *= num911;
                num899 *= num911;
                if (npc.velocity.X < num888) {
                    npc.velocity.X += num877;
                    if (npc.velocity.X < 0f && num888 > 0f) {
                        npc.velocity.X += num877;
                    }
                }
                else if (npc.velocity.X > num888) {
                    npc.velocity.X -= num877;
                    if (npc.velocity.X > 0f && num888 < 0f) {
                        npc.velocity.X -= num877;
                    }
                }
                if (npc.velocity.Y < num899) {
                    npc.velocity.Y += num877;
                    if (npc.velocity.Y < 0f && num899 > 0f) {
                        npc.velocity.Y += num877;
                    }
                }
                else if (npc.velocity.Y > num899) {
                    npc.velocity.Y -= num877;
                    if (npc.velocity.Y > 0f && num899 < 0f) {
                        npc.velocity.Y -= num877;
                    }
                }
                npc.ai[2] += 1f;
                if (npc.ai[2] >= 200f) {
                    npc.ai[1] = 1f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    if (Main.expertMode && (double)npc.life < (double)npc.lifeMax * 0.35) {
                        npc.ai[1] = 3f;
                    }
                    npc.target = 255;
                    npc.netUpdate = true;
                }
                if (Main.expertMode && finalDesperation) {
                    npc.TargetClosest();
                    npc.netUpdate = true;
                    npc.ai[1] = 3f;
                    npc.ai[2] = 0f;
                    npc.ai[3] -= 1000f;
                }
            }
            else if (npc.ai[1] == 1f) {
                Main.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, 0);
                npc.rotation = num1243;
                float num922 = 6.8f;
                if (Main.expertMode && npc.ai[3] == 1f) {
                    num922 *= 1.15f;
                }
                if (Main.expertMode && npc.ai[3] == 2f) {
                    num922 *= 1.3f;
                }
                Vector2 vector207 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                float num933 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - vector207.X;
                float num944 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - vector207.Y;
                float num955 = (float)Math.Sqrt(num933 * num933 + num944 * num944);
                num955 = num922 / num955;
                npc.velocity.X = num933 * num955;
                npc.velocity.Y = num944 * num955;
                npc.ai[1] = 2f;
                npc.netUpdate = true;
                if (npc.netSpam > 10) {
                    npc.netSpam = 10;
                }
            }
            else if (npc.ai[1] == 2f) {
                float num966 = 40f;
                npc.ai[2] += 1f;
                if (Main.expertMode) {
                    num966 = 50f;
                }
                if (npc.ai[2] >= num966) {
                    npc.velocity *= 0.97f;
                    if (Main.expertMode) {
                        npc.velocity *= 0.98f;
                    }
                    if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1) {
                        npc.velocity.X = 0f;
                    }
                    if ((double)npc.velocity.Y > -0.1 && (double)npc.velocity.Y < 0.1) {
                        npc.velocity.Y = 0f;
                    }
                }
                else {
                    npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - 1.57f;
                }
                int num977 = 130;
                if (Main.expertMode) {
                    num977 = 90;
                }
                if (npc.ai[2] >= (float)num977) {
                    npc.ai[3] += 1f;
                    npc.ai[2] = 0f;
                    npc.target = 255;
                    npc.rotation = num1243;
                    if (npc.ai[3] >= 3f) {
                        npc.ai[1] = 0f;
                        npc.ai[3] = 0f;
                        if (Main.expertMode && Main.netMode != 1 && (double)npc.life < (double)npc.lifeMax * 0.5) {
                            npc.ai[1] = 3f;
                            npc.ai[3] += Main.rand.Next(1, 4);
                        }
                        npc.netUpdate = true;
                        if (npc.netSpam > 10) {
                            npc.netSpam = 10;
                        }
                    }
                    else {
                        npc.ai[1] = 1f;
                    }
                }
            }
            else if (npc.ai[1] == 3f) {
                if (npc.ai[3] == 4f && flag24 && npc.Center.Y > Main.player[npc.target].Center.Y) {
                    npc.TargetClosest();
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.netUpdate = true;
                    if (npc.netSpam > 10) {
                        npc.netSpam = 10;
                    }
                }
                else if (Main.netMode != 1) {
                    npc.TargetClosest();
                    float num988 = 20f;
                    Vector2 vector218 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                    float num999 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - vector218.X;
                    float num1010 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - vector218.Y;
                    float num1022 = Math.Abs(Main.player[npc.target].velocity.X) + Math.Abs(Main.player[npc.target].velocity.Y) / 4f;
                    num1022 += 10f - num1022;
                    if (num1022 < 5f) {
                        num1022 = 5f;
                    }
                    if (num1022 > 15f) {
                        num1022 = 15f;
                    }
                    if (npc.ai[2] == -1f && !finalDesperation) {
                        num1022 *= 4f;
                        num988 *= 1.3f;
                    }
                    if (finalDesperation) {
                        num1022 *= 2f;
                    }
                    num999 -= Main.player[npc.target].velocity.X * num1022;
                    num1010 -= Main.player[npc.target].velocity.Y * num1022 / 4f;
                    num999 *= 1f + (float)Main.rand.Next(-10, 11) * 0.01f;
                    num1010 *= 1f + (float)Main.rand.Next(-10, 11) * 0.01f;
                    if (finalDesperation) {
                        num999 *= 1f + (float)Main.rand.Next(-10, 11) * 0.01f;
                        num1010 *= 1f + (float)Main.rand.Next(-10, 11) * 0.01f;
                    }
                    float num1033 = (float)Math.Sqrt(num999 * num999 + num1010 * num1010);
                    float num1044 = num1033;
                    num1033 = num988 / num1033;
                    npc.velocity.X = num999 * num1033;
                    npc.velocity.Y = num1010 * num1033;
                    npc.velocity.X += (float)Main.rand.Next(-20, 21) * 0.1f;
                    npc.velocity.Y += (float)Main.rand.Next(-20, 21) * 0.1f;
                    if (finalDesperation) {
                        npc.velocity.X += (float)Main.rand.Next(-50, 51) * 0.1f;
                        npc.velocity.Y += (float)Main.rand.Next(-50, 51) * 0.1f;
                        float num1055 = Math.Abs(npc.velocity.X);
                        float num1066 = Math.Abs(npc.velocity.Y);
                        if (npc.Center.X > Main.player[npc.target].Center.X) {
                            num1066 *= -1f;
                        }
                        if (npc.Center.Y > Main.player[npc.target].Center.Y) {
                            num1055 *= -1f;
                        }
                        npc.velocity.X = num1066 + npc.velocity.X;
                        npc.velocity.Y = num1055 + npc.velocity.Y;
                        npc.velocity.Normalize();
                        npc.velocity *= num988;
                        npc.velocity.X += (float)Main.rand.Next(-20, 21) * 0.1f;
                        npc.velocity.Y += (float)Main.rand.Next(-20, 21) * 0.1f;
                    }
                    else if (num1044 < 100f) {
                        if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y)) {
                            float num1077 = Math.Abs(npc.velocity.X);
                            float num1088 = Math.Abs(npc.velocity.Y);
                            if (npc.Center.X > Main.player[npc.target].Center.X) {
                                num1088 *= -1f;
                            }
                            if (npc.Center.Y > Main.player[npc.target].Center.Y) {
                                num1077 *= -1f;
                            }
                            npc.velocity.X = num1088;
                            npc.velocity.Y = num1077;
                        }
                    }
                    else if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y)) {
                        float num1099 = (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) / 2f;
                        float num1110 = num1099;
                        if (npc.Center.X > Main.player[npc.target].Center.X) {
                            num1110 *= -1f;
                        }
                        if (npc.Center.Y > Main.player[npc.target].Center.Y) {
                            num1099 *= -1f;
                        }
                        npc.velocity.X = num1110;
                        npc.velocity.Y = num1099;
                    }
                    npc.ai[1] = 4f;
                    npc.netUpdate = true;
                    if (npc.netSpam > 10) {
                        npc.netSpam = 10;
                    }
                }
            }
            else if (npc.ai[1] == 4f) {
                if (npc.ai[2] == 0f) {
                    Main.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                }
                float num1121 = num910;
                npc.ai[2] += 1f;
                if (npc.ai[2] == num1121 && Vector2.Distance(npc.position, Main.player[npc.target].position) < 200f) {
                    npc.ai[2] -= 1f;
                }
                if (npc.ai[2] >= num1121) {
                    npc.velocity *= 0.95f;
                    if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1) {
                        npc.velocity.X = 0f;
                    }
                    if ((double)npc.velocity.Y > -0.1 && (double)npc.velocity.Y < 0.1) {
                        npc.velocity.Y = 0f;
                    }
                }
                else {
                    npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - 1.57f;
                }
                float num1133 = num1121 + 13f;
                if (npc.ai[2] >= num1133) {
                    npc.netUpdate = true;
                    if (npc.netSpam > 10) {
                        npc.netSpam = 10;
                    }
                    npc.ai[3] += 1f;
                    npc.ai[2] = 0f;
                    if (npc.ai[3] >= 5f) {
                        npc.ai[1] = 0f;
                        npc.ai[3] = 0f;
                    }
                    else {
                        npc.ai[1] = 3f;
                    }
                }
            }
            else if (npc.ai[1] == 5f) {
                float num1144 = 600f;
                float num1155 = 9f;
                float num1166 = 0.3f;
                Vector2 vector229 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                float num1177 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - vector229.X;
                float num1188 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) + num1144 - vector229.Y;
                float num1199 = (float)Math.Sqrt(num1177 * num1177 + num1188 * num1188);
                num1199 = num1155 / num1199;
                num1177 *= num1199;
                num1188 *= num1199;
                if (npc.velocity.X < num1177) {
                    npc.velocity.X += num1166;
                    if (npc.velocity.X < 0f && num1177 > 0f) {
                        npc.velocity.X += num1166;
                    }
                }
                else if (npc.velocity.X > num1177) {
                    npc.velocity.X -= num1166;
                    if (npc.velocity.X > 0f && num1177 < 0f) {
                        npc.velocity.X -= num1166;
                    }
                }
                if (npc.velocity.Y < num1188) {
                    npc.velocity.Y += num1166;
                    if (npc.velocity.Y < 0f && num1188 > 0f) {
                        npc.velocity.Y += num1166;
                    }
                }
                else if (npc.velocity.Y > num1188) {
                    npc.velocity.Y -= num1166;
                    if (npc.velocity.Y > 0f && num1188 < 0f) {
                        npc.velocity.Y -= num1166;
                    }
                }
                npc.ai[2] += 1f;
                if (npc.ai[2] >= 70f) {
                    npc.TargetClosest();
                    npc.ai[1] = 3f;
                    npc.ai[2] = -1f;
                    npc.ai[3] = Main.rand.Next(-3, 1);
                    npc.netUpdate = true;
                }
            }
            if (finalDesperation && npc.ai[1] == 5f) {
                npc.ai[1] = 3f;
            }
        }

        #endregion

        public override void PostAI(NPC npc) {
            if (npc.type == NPCID.WallofFlesh || npc.type == NPCID.WallofFleshEye) {
                if (Main.netMode == NetmodeID.SinglePlayer) {
                    if (Main.player[0].dead) {
                        if (npc.type == NPCID.WallofFlesh)
                        {
                            Main.NewText("The Wall's rage is satisfied...", Color.OrangeRed);
                        }
                        npc.life = 0;
                        npc.HitEffect();
                        npc.active = false; 
                    }
                }
            }
        }

        public override bool CheckDead(NPC npc) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                if (npc.type == NPCID.LavaSlime) {
                    try {
                        int npcTileX = (int)(npc.Center.X / 16f);
                        int npcTileY = (int)(npc.Center.Y / 16f);
                        if (!WorldGen.SolidTile(npcTileX, npcTileY)) {
                            Main.tile[npcTileX, npcTileY].liquid = 0;
                            Main.tile[npcTileX, npcTileY].lava(lava: false);
                            Main.tile[npcTileX, npcTileY].honey(honey: false);
                            WorldGen.SquareTileFrame(npcTileX, npcTileY);
                        }
                    }
                    catch { //do nothing
                    }
                } 
            }
            return base.CheckDead(npc);
        }

        public override void NPCLoot(NPC npc) {
            #region Loot Changes

            Player player = Main.player[npc.target];

            if (npc.type == NPCID.BigStinger) {
                Item.NewItem(npc.getRect(), mod.ItemType("BloodredMossClump"));
            }

            if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)) && !Main.expertMode) {
                 
                Item.NewItem(npc.getRect(), ItemID.DemoniteOre, 4);
                Item.NewItem(npc.getRect(), ItemID.ShadowScale, 4);
            }

            if ((npc.type >= NPCID.BigHornetStingy && npc.type <= NPCID.LittleHornetFatty) ||
                                (npc.type >= NPCID.GiantMossHornet && npc.type <= NPCID.LittleStinger) ||
                                npc.type == NPCID.Hornet || npc.type == NPCID.ManEater ||
                                npc.type == NPCID.MossHornet ||
                                (npc.type >= NPCID.HornetFatty && npc.type <= NPCID.HornetStingy)) {
                if (Main.rand.NextFloat() >= .33f && ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) { // 66% chance in legacy
                    Item.NewItem(npc.getRect(), mod.ItemType("BloodredMossClump"));
                }
                if (Main.rand.NextFloat() >= .66f && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) { // 33% chance in revamped
                    Item.NewItem(npc.getRect(), mod.ItemType("BloodredMossClump"));
                }
            }
            
            if (npc.type == NPCID.KingSlime) {
                Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"), 500);
                if (!Main.expertMode) {
                    Item.NewItem(npc.getRect(), ItemID.GoldCoin, 10); //obtained from boss bag in Expert mode (see tsorcGlobalItem for boss bag edits)
                }
            }

            if (npc.type == NPCID.QueenBee) {
                Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"), 1000);
            }

            if (npc.type == NPCID.TheDestroyer && !Main.expertMode) {
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrestOfCorruption>(), 2);
                Item.NewItem(npc.getRect(), ModContent.ItemType<RTQ2>());
                Item.NewItem(npc.getRect(), ModContent.ItemType<RTQ2>());
            }
            if (npc.type == NPCID.SkeletronHead && !Main.expertMode) {
                if (ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) Item.NewItem(npc.getRect(), ModContent.ItemType<Miakoda>()); //dropping 2 together causes them to be difficult to separate
                if (ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) Item.NewItem(npc.getRect(), ModContent.ItemType<Miakoda>());
                if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) Item.NewItem(npc.getRect(), ModContent.ItemType<MiakodaFull>());
                if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) Item.NewItem(npc.getRect(), ModContent.ItemType<MiakodaFull>());
                if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.SkeletronHead) && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) Item.NewItem(npc.getRect(), ModContent.ItemType<SublimeBoneDust>());

            }
            if (npc.type == NPCID.SkeletronPrime && !Main.expertMode) {
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrestOfSteel>(), 2);
                Item.NewItem(npc.getRect(), ItemID.AngelWings, 1, false, -1);
                Item.NewItem(npc.getRect(), ItemID.AngelWings, 1, false, -1);
                if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.SkeletronPrime) && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) Item.NewItem(npc.getRect(), ModContent.ItemType<SublimeBoneDust>());
            }
            if ((npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism) && !Main.expertMode) {
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrestOfSky>(), 2);
                Item.NewItem(npc.getRect(), ModContent.ItemType<DarkSoul>(), 1000);
            }

            if (npc.netID == NPCID.GreenSlime && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"));
            }

            if (npc.netID == NPCID.RedSlime && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
            {
                Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"), 2);
            }

            if ((npc.type == NPCID.Mimic || npc.type == NPCID.BigMimicCorruption || npc.type == NPCID.BigMimicCrimson || npc.type == NPCID.BigMimicHallow) && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                if (Main.rand.Next(10) == 0) {
                    Item.NewItem(npc.getRect(), mod.ItemType("SymbolOfAvarice"));
                }
            }

            if (npc.type == NPCID.EyeofCthulhu && !Main.expertMode) {
                Item.NewItem(npc.getRect(), ItemID.HerosHat);
                Item.NewItem(npc.getRect(), ItemID.HerosShirt);
                Item.NewItem(npc.getRect(), ItemID.HerosPants);
                Item.NewItem(npc.getRect(), ItemID.HermesBoots, 1, false, -1);
                if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.EyeofCthulhu) && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) Item.NewItem(npc.getRect(), ModContent.ItemType<SublimeBoneDust>());

            }

            if (npc.type == NPCID.WallofFlesh && !Main.expertMode)
            {
                if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.WallofFlesh) && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) Item.NewItem(npc.getRect(), ModContent.ItemType<EstusFlaskShard>());
            }

            if (npc.type == NPCID.PossessedArmor && Main.rand.Next(50) == 0 && !Main.expertMode) {
                Item.NewItem(npc.getRect(), mod.ItemType("WallTome"));
            }

            if ((npc.type == NPCID.PossessedArmor || npc.type == NPCID.Wraith) && Main.rand.Next(25) == 0 && Main.expertMode) {
                Item.NewItem(npc.getRect(), mod.ItemType("WallTome"));
            }

            if (npc.type == NPCID.Shark && Main.rand.Next(20) == 0) {
                Item.NewItem(npc.getRect(), mod.ItemType("CoralSword"), 1, false, -1);
            }

            if (Main.rand.Next(25) == 0 && ((npc.type >= NPCID.BigPantlessSkeleton && npc.type <= NPCID.SmallSkeleton) ||
                                (npc.type >= NPCID.HeadacheSkeleton && npc.type <= NPCID.PantlessSkeleton) ||
                                (npc.type >= NPCID.SkeletonTopHat && npc.type <= NPCID.SkeletonAlien) ||
                                (npc.type >= NPCID.BoneThrowingSkeleton && npc.type <= NPCID.BoneThrowingSkeleton4) ||
                                npc.type == NPCID.HeavySkeleton ||
                                npc.type == NPCID.Skeleton ||
                                npc.type == NPCID.ArmoredSkeleton ||
                                npc.type == NPCID.SkeletonArcher)) {
                Item.NewItem(npc.getRect(), mod.ItemType("DeadChicken"));
            }

            if (npc.type == NPCID.Vulture && Main.rand.Next(10) == 0) {
                Item.NewItem(npc.getRect(), mod.ItemType("DeadChicken"));
            }

            if (npc.type == NPCID.Wraith) {
                Item.NewItem(npc.getRect(), ItemID.Heart);
                Item.NewItem(npc.getRect(), ItemID.Heart);
                Item.NewItem(npc.getRect(), ItemID.Heart);
                Item.NewItem(npc.getRect(), ItemID.Heart);
                Item.NewItem(npc.getRect(), ItemID.Heart);
                Item.NewItem(npc.getRect(), ItemID.Heart);
                Item.NewItem(npc.getRect(), ItemID.Heart);

            }
            

            if (Main.rand.Next(25) == 0 && ((npc.type >= NPCID.BigFemaleZombie && npc.type <= NPCID.SmallFemaleZombie) ||
                                (npc.type >= NPCID.BigTwiggyZombie && npc.type <= NPCID.SmallZombie) ||
                                (npc.type >= NPCID.ZombieDoctor && npc.type <= NPCID.ZombiePixie) ||
                                (npc.type >= NPCID.ZombieXmas && npc.type <= NPCID.ZombieSweater) ||
                                (npc.type >= NPCID.ArmedZombie && npc.type <= NPCID.ArmedZombieCenx) ||
                                npc.type == NPCID.Zombie ||
                                npc.type == NPCID.BaldZombie ||
                                npc.type == NPCID.ZombieEskimo ||
                                npc.type == NPCID.FemaleZombie ||
                                (npc.type >= NPCID.PincushionZombie && npc.type <= NPCID.TwiggyZombie))) {
                Item.NewItem(npc.getRect(), mod.ItemType("DeadChicken"));
            }

            if (npc.type == NPCID.GoblinArcher || npc.type == NPCID.GoblinPeon || npc.type == NPCID.GoblinWarrior || npc.type == NPCID.GoblinSorcerer || npc.type == NPCID.GoblinThief) {

                if (Main.rand.Next(200) == 0) { // 0.5%
                    Item.NewItem(npc.getRect(), mod.ItemType("Pulsar"), 1, false, ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems ? PrefixID.Damaged : -1);
                }

                else if (Main.rand.Next(200) == 0) { // 0.5% 
                    Item.NewItem(npc.getRect(), mod.ItemType("ToxicCatalyzer"), 1, false, ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems ? PrefixID.Damaged : -1);
                }
            }
            if (npc.type == NPCID.Plantera && !Main.expertMode)
            {
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrestOfLife>(), 1);
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrestOfLife>(), 1);
                Item.NewItem(npc.getRect(), ModContent.ItemType<SoulOfLife>(), 6);                
            }
            if (npc.type == NPCID.Golem && !Main.expertMode)
            {
                if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<BrokenPicksaw>());
                }
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrestOfStone>(), 1);
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrestOfStone>(), 1);
            }
            if (npc.type == NPCID.Snatcher || npc.type == NPCID.ManEater || npc.type == NPCID.AngryTrapper)
            {
                if (Main.rand.Next(3) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.GreenBlossom>());
            }
            if (npc.type == NPCID.HornetLeafy || npc.type == NPCID.BigHornetLeafy || npc.type == NPCID.LittleHornetLeafy)
            {
                if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.GreenBlossom>());
            }


            #endregion
            #region Pillar ModWorld bools
            if (npc.type == NPCID.LunarTowerVortex) tsorcRevampWorld.DownedVortex = true;
            if (npc.type == NPCID.LunarTowerNebula) tsorcRevampWorld.DownedNebula = true;
            if (npc.type == NPCID.LunarTowerVortex) tsorcRevampWorld.DownedStardust = true;
            if (npc.type == NPCID.LunarTowerVortex) tsorcRevampWorld.DownedSolar = true;
            #endregion
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            if (npc.type == NPCID.WallofFlesh || npc.type == NPCID.WallofFleshEye || npc.type == NPCID.TheHungry || npc.type == NPCID.TheHungryII)
            {
                damage *= 2;
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (npc.type == NPCID.WallofFlesh || npc.type == NPCID.WallofFleshEye || npc.type == NPCID.TheHungry || npc.type == NPCID.TheHungryII)
            {
                //Spears
                if (projectile.aiStyle == 19 && projectile.melee == true)
                {
                    damage *= 2;
                }
            }            
        }

        public override bool CheckActive(NPC npc)
        {
            if (npc.modNPC != null && npc.modNPC.mod == ModLoader.GetMod("tsorcRevamp") && npc.boss)
            {
                return false;
            }
            else
            {
                return base.CheckActive(npc);
            }
        }
    }
}
