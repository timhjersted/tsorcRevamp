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
                        npc.knockBackResist = 0.0f;
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
                        npc.damage = 14;
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
                        npc.damage = 24; // I get the feeling he's going to be pretty damn tough in Expert mode

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
                        npc.damage = 80;
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
                        npc.defense = 18;
                        npc.damage = 35;
                        npc.lifeMax = 1000;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.SkeletronHead): {
                        npc.value = 80000;
                        npc.defense = 12;
                        npc.damage = 55;
                        npc.lifeMax = 5200;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.SkeletronPrime): {
                        npc.value = 250000;
                        npc.defense = 40;
                        npc.damage = 90;
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
                        break;
                    }

                case (NPCID.TheDestroyerBody): {
                        npc.scale = 1.25f;
                        break;
                    }

                case (NPCID.TheDestroyerTail): {
                        npc.scale = 1.25f;
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
                        npc.scale = 1.2f;
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

        public override void AI(NPC npc) {
            if (npc.type == NPCID.BigRainZombie
                || npc.type == NPCID.ZombieRaincoat
                || npc.type == NPCID.SmallRainZombie
                || npc.type == NPCID.Clown
                || npc.type == NPCID.UmbrellaSlime) {

                npc.active = false;
            }
            if ((npc.friendly) && (npc.townNPC == true)) { //town NPCs are immortal (why was i using a hp check?)
                npc.dontTakeDamage = true;
                npc.dontTakeDamageFromHostiles = true;
            }

        }

        public override bool PreAI(NPC npc) {

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

            else return base.PreAI(npc);
        }

        public override bool CheckDead(NPC npc) {
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
            return base.CheckDead(npc);
        }
        public override void NPCLoot(NPC npc) {
            #region Loot Changes

            if (npc.type == NPCID.BigStinger) {
                Item.NewItem(npc.getRect(), mod.ItemType("BloodredMossClump"));
            }

            if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)) && !Main.expertMode) {
                Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"), 10);
                Item.NewItem(npc.getRect(), ItemID.DemoniteOre, 4);
                Item.NewItem(npc.getRect(), ItemID.ShadowScale, 4);
            }

            if ((npc.type >= NPCID.BigHornetStingy && npc.type <= NPCID.LittleHornetFatty) ||
                                (npc.type >= NPCID.GiantMossHornet && npc.type <= NPCID.LittleStinger) ||
                                npc.type == NPCID.Hornet || npc.type == NPCID.ManEater ||
                                npc.type == NPCID.MossHornet ||
                                (npc.type >= NPCID.HornetFatty && npc.type <= NPCID.HornetStingy)) {
                if (Main.rand.NextFloat() >= .33f) { // 66% chance
                    Item.NewItem(npc.getRect(), mod.ItemType("BloodredMossClump"));
                }
            }

            if (npc.type == NPCID.KingSlime) {
                Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"), 500);
                if (!Main.expertMode) {
                    Item.NewItem(npc.getRect(), ItemID.GoldCoin, 10); //obtained from boss bag in Expert mode (see tsorcGlobalItem for boss bag edits)
                }
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

            }
            if (npc.type == NPCID.SkeletronPrime && !Main.expertMode) {
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrestOfSteel>(), 2);
                Item.NewItem(npc.getRect(), ItemID.AngelWings);
                Item.NewItem(npc.getRect(), ItemID.AngelWings);
            }
            if ((npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism) && !Main.expertMode) {
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrestOfSky>(), 2);
                Item.NewItem(npc.getRect(), ModContent.ItemType<DarkSoul>(), 1000);
            }

            if (npc.netID == NPCID.GreenSlime && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"));
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
                Item.NewItem(npc.getRect(), ItemID.HermesBoots);
            }

            if (npc.type == NPCID.PossessedArmor && Main.rand.Next(50) == 0 && !Main.expertMode) {
                Item.NewItem(npc.getRect(), mod.ItemType("WallTome"));
            }

            if ((npc.type == NPCID.PossessedArmor || npc.type == NPCID.Wraith) && Main.rand.Next(25) == 0 && Main.expertMode) {
                Item.NewItem(npc.getRect(), mod.ItemType("WallTome"));
            }

            if (npc.type == NPCID.Shark && Main.rand.Next(20) == 0) {
                Item.NewItem(npc.getRect(), mod.ItemType("CoralSword"));
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
                Item.NewItem(npc.getRect(), ItemID.Heart, 7);
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
                    Item.NewItem(npc.getRect(), mod.ItemType("Pulsar"));
                }

                if (Main.rand.Next(200) == 0) { // 0.5% 
                    Item.NewItem(npc.getRect(), mod.ItemType("ToxicCatalyzer"));
                }
            }
            if (npc.type == NPCID.Golem) {
                Item.NewItem(npc.getRect(), ItemID.Picksaw);
            }
            #endregion
            #region Pillar ModWorld bools
            if (npc.type == NPCID.LunarTowerVortex) tsorcRevampWorld.DownedVortex = true;
            if (npc.type == NPCID.LunarTowerNebula) tsorcRevampWorld.DownedNebula = true;
            if (npc.type == NPCID.LunarTowerVortex) tsorcRevampWorld.DownedStardust = true;
            if (npc.type == NPCID.LunarTowerVortex) tsorcRevampWorld.DownedSolar = true;
            #endregion
        }
    }
}
