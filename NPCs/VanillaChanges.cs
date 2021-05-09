using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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
                        npc.damage = 43;
                        npc.knockBackResist = 0.2f;
                        npc.defense = 36;
                        npc.value = 450;
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
                //Evaluates based on groups of hornets according to https://terraria.fandom.com/wiki/NPC_IDs
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
                        npc.defense = 38;
                        npc.damage = 55;
                        npc.lifeMax = 23000;
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
                        npc.lifeMax = 28000;
                        npc.value = 120000;
                        npc.damage = 85;
                        npc.defense = 35;
                        break;
                    }

                case (NPCID.SpikeBall): {
                        npc.scale = 1.5f;
                        npc.damage = 70;
                        break;
                    }

                case (NPCID.TheDestroyer): {
                        npc.lifeMax = 120000;
                        npc.value = 200000;
                        npc.damage = 180;
                        npc.scale = 1.25f;
                        npc.defense = 50;
                        break;
                    }

                case (NPCID.TheDestroyerBody): {
                        npc.lifeMax = 100000;
                        npc.damage = 55;
                        npc.scale = 1.25f;
                        npc.defense = 55;
                        break;
                    }

                case (NPCID.TheDestroyerTail): {
                        npc.lifeMax = 100000;
                        npc.damage = 75;
                        npc.scale = 1.25f;
                        npc.defense = 35;
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
                        npc.value = 500;
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

                case (NPCID.Zombie): {
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
                || npc.type == NPCID.SmallRainZombie
                || npc.type == NPCID.Clown
                || npc.type == NPCID.UmbrellaSlime) {

                npc.active = false;
            }
            if ((npc.friendly) && (npc.lifeMax == 250)) { //town NPCs are immortal
                npc.dontTakeDamage = true;
                npc.dontTakeDamageFromHostiles = true;
            }
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
    }
}
