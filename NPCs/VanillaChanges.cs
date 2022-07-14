using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Pets;

namespace tsorcRevamp.NPCs
{
    class VanillaChanges : GlobalNPC
    {
        #region SetDefaults

        public override void SetDefaults(NPC npc)
        {




            //Only mess with it if it's one of our bosses
            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp"))
            {
                if (npc.boss)
                {
                    //Bosses are 1.3x weaker in normal mode. This is reverted for expert in ScaleExpertStats
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


            switch (npc.type)
            {

                #region Pre-Hardmode NPCs

                #region Zombies

                case int zombie when (
                (zombie >= NPCID.BigRainZombie && zombie <= NPCID.SmallRainZombie) ||
                (zombie >= NPCID.BigFemaleZombie && zombie <= NPCID.SmallFemaleZombie) ||
                (zombie >= NPCID.BigTwiggyZombie && zombie <= NPCID.SmallZombie) ||
                zombie == NPCID.Zombie ||
                zombie == NPCID.BaldZombie ||
                zombie == NPCID.ZombieEskimo ||
                (zombie >= NPCID.PincushionZombie && zombie <= NPCID.TwiggyZombie) ||
                zombie == NPCID.FemaleZombie ||
                zombie == NPCID.ZombieRaincoat ||
                (zombie >= NPCID.ZombieDoctor && zombie <= NPCID.ZombiePixie) ||
                zombie == NPCID.ZombieXmas ||
                zombie == NPCID.ZombieSweater ||
                zombie == NPCID.TorchZombie ||
                zombie == NPCID.MaggotZombie
                ):
                    {
                        npc.value = 100;
                        break;
                    }

                case int armedzombie when ((armedzombie >= NPCID.ArmedZombie && armedzombie <= NPCID.ArmedZombieCenx) || armedzombie == NPCID.ArmedTorchZombie):
                    {
                        npc.value = 110;
                        break;
                    }

                case (NPCID.BloodZombie):
                    {
                        npc.value = 370;
                        break;
                    }

                #endregion

                #region Skeletons

                case int skeleton when (
                (skeleton >= NPCID.BigPantlessSkeleton && skeleton <= NPCID.SmallSkeleton) ||
                 skeleton == NPCID.Skeleton ||
                (skeleton >= NPCID.HeadacheSkeleton && skeleton <= NPCID.PantlessSkeleton) ||
                (skeleton >= NPCID.SkeletonTopHat && skeleton <= NPCID.SkeletonAlien)
                ):
                    {
                        npc.value = 140;
                        break;
                    }

                case int throwingskeleton when ((throwingskeleton >= NPCID.BoneThrowingSkeleton && throwingskeleton <= NPCID.BoneThrowingSkeleton4)):
                    {
                        npc.value = 210;
                        break;
                    }

                case (NPCID.UndeadViking):
                    {
                        npc.value = 190;
                        break;
                    }

                case (NPCID.SporeSkeleton):
                    {
                        npc.value = 170;
                        break;
                    }

                case (NPCID.UndeadMiner):
                    {
                        npc.value = 280;
                        break;
                    }

                case (NPCID.GreekSkeleton):
                    {
                        npc.value = 630;
                        break;
                    }

                #endregion

                #region Demon Eyes

                case int demoneye when (
                (demoneye >= NPCID.DemonEye2 && demoneye <= NPCID.CataractEye2) || //grouping because DemonEye2 and CataractEye2 IDs are all next to each other
                 demoneye == NPCID.DemonEye ||
                 (demoneye >= NPCID.CataractEye && demoneye <= NPCID.PurpleEye) ||
                 demoneye == NPCID.DemonEyeOwl ||
                 demoneye == NPCID.DemonEyeSpaceship
                 ):
                    {
                        npc.value = 180;
                        break;
                    }

                case (NPCID.Drippler):
                    {
                        npc.value = 320;
                        break;
                    }

                #endregion

                #region Slimes

                case int slime when (
                (slime >= NPCID.YellowSlime && slime <= NPCID.BlackSlime) ||
                slime == NPCID.GreenSlime ||
                slime == NPCID.BlueSlime ||
                slime == NPCID.IceSlime ||
                slime == NPCID.UmbrellaSlime ||
                slime == NPCID.SlimeMasked ||
                (slime >= NPCID.SlimeRibbonWhite && slime <= NPCID.SlimeRibbonRed) ||
                slime == NPCID.SandSlime
                ):
                    {
                        npc.value = 60;
                        break;
                    }

                case (NPCID.GoldenSlime):
                    {
                        npc.value = 1240;
                        break;
                    }

                case (NPCID.MotherSlime):
                    {
                        npc.scale = 1.25f;
                        npc.value = 170;
                        break;
                    }

                case (NPCID.BabySlime):
                    {
                        npc.scale = 0.5f;
                        npc.value = 60;
                        break;
                    }

                case (NPCID.JungleSlime):
                    {
                        npc.value = 250;
                        break;
                    }

                case (NPCID.DungeonSlime):
                    {
                        npc.value = 400;
                        break;
                    }

                case (NPCID.SpikedIceSlime):
                    {
                        npc.value = 300;
                        break;
                    }

                case (NPCID.SpikedJungleSlime):
                    {
                        npc.value = 500;
                        break;
                    }

                case (NPCID.LavaSlime):
                    {
                        npc.knockBackResist = 0.4f;
                        npc.scale = 2.5f;
                        npc.value = 650;
                        break;
                    }

                #endregion

                #region Bats
                case (NPCID.CaveBat):
                    {
                        npc.value = 120;
                        break;
                    }

                case (NPCID.SporeBat):
                    {
                        npc.value = 1450;
                        break;
                    }

                case (NPCID.IceBat):
                    {
                        npc.value = 170;
                        break;
                    }

                case (NPCID.JungleBat):
                    {
                        npc.value = 230;
                        break;
                    }

                case (NPCID.Hellbat):
                    {
                        npc.damage = 46;
                        npc.scale = 1.5f;
                        npc.value = 480;
                        break;
                    }
                #endregion

                #region Worms

                case int giantworm when (
                (giantworm >= NPCID.GiantWormHead && giantworm <= NPCID.GiantWormTail)
                ):
                    {
                        npc.npcSlots = 3;
                        npc.value = 3400;

                        if (npc.type == NPCID.GiantWormHead)
                        {
                            npc.lifeMax = 500;
                            npc.damage = 25;
                            npc.defense = 20;
                        }
                        if (npc.type == NPCID.GiantWormBody)
                        {
                            npc.lifeMax = 500;
                            npc.damage = 10;
                            npc.defense = 8;
                            npc.knockBackResist = 0.1f;
                        }

                        if (npc.type == NPCID.GiantWormTail)
                        {
                            npc.lifeMax = 500;
                            npc.defense = 0;
                            npc.damage = 15;
                            npc.knockBackResist = 0.2f;
                        }
                        break;
                    }

                case int devourer when (
                (devourer >= NPCID.DevourerHead && devourer <= NPCID.DevourerTail)
                ):
                    {
                        npc.lifeMax = 800;
                        npc.npcSlots = 4;
                        npc.value = 5800;

                        if (npc.type == NPCID.DevourerHead)
                        {
                            npc.damage = 35;
                            npc.defense = 4;
                            npc.knockBackResist = 0.05f;
                        }
                        if (npc.type == NPCID.DevourerBody)
                        {
                            npc.damage = 20;
                            npc.defense = 30;
                            npc.knockBackResist = 0.1f;
                        }

                        if (npc.type == NPCID.DevourerTail)
                        {
                            npc.damage = 30;
                            npc.defense = 4;
                            npc.knockBackResist = 0.2f;
                        }
                        break;
                    }

                case int dunesplicer when (
                (dunesplicer >= NPCID.DuneSplicerHead && dunesplicer <= NPCID.DuneSplicerTail)
                ):
                    {
                        npc.lifeMax = 650;
                        npc.npcSlots = 4;
                        npc.value = 6000;

                        if (npc.type == NPCID.DuneSplicerHead)
                        {
                            npc.damage = 40;
                            npc.defense = 4;
                            npc.knockBackResist = 0.05f;
                        }
                        if (npc.type == NPCID.DuneSplicerBody)
                        {
                            npc.damage = 25;
                            npc.defense = 30;
                            npc.knockBackResist = 0.1f;
                        }

                        if (npc.type == NPCID.DuneSplicerTail)
                        {
                            npc.damage = 35;
                            npc.defense = 4;
                            npc.knockBackResist = 0.2f;
                        }
                        break;
                    }

                case int boneserpent when (
                (boneserpent >= NPCID.BoneSerpentHead && boneserpent <= NPCID.BoneSerpentTail)
                ):
                    {
                        npc.lifeMax = 1800;
                        npc.npcSlots = 5;
                        npc.value = 8000;

                        if (npc.type == NPCID.BoneSerpentHead)
                        {
                            npc.damage = 50;
                            npc.defense = 2;
                        }
                        if (npc.type == NPCID.BoneSerpentBody)
                        {
                            npc.damage = 25;
                            npc.defense = 12;
                            npc.knockBackResist = 0.1f;
                        }

                        if (npc.type == NPCID.BoneSerpentTail)
                        {
                            npc.damage = 35;
                            npc.defense = 25;
                            npc.knockBackResist = 0.2f;
                        }
                        break;
                    }

                #endregion

                #region Crimeras, Soul Eaters

                case int crimera when (
                (crimera >= NPCID.BigCrimera && crimera <= NPCID.LittleCrimera) ||
                crimera == NPCID.Crimera
                ):
                    {
                        npc.value = 270;

                        if (npc.type == NPCID.BigCrimera)
                        {
                            npc.scale = 1.5f;
                        }

                        if (npc.type == NPCID.LittleCrimera)
                        {
                            npc.scale = 0.5f;
                        }
                        break;
                    }

                case int souleater when (
                (souleater >= NPCID.BigEater && souleater <= NPCID.LittleEater) ||
                souleater == NPCID.EaterofSouls
                ):
                    {
                        npc.value = 230;

                        if (npc.type == NPCID.BigEater)
                        {
                            npc.scale = 1.5f;
                        }

                        if (npc.type == NPCID.LittleEater)
                        {
                            npc.scale = 0.5f;
                        }
                        break;
                    }

                #endregion

                # region Hornets

                case int hornet when (
                (hornet >= NPCID.BigHornetStingy && hornet <= NPCID.LittleHornetFatty) ||
                (hornet >= NPCID.BigStinger && hornet <= NPCID.LittleStinger) ||
                hornet == NPCID.Hornet ||
                (hornet >= NPCID.HornetFatty && hornet <= NPCID.HornetStingy)
                ):
                    {
                        npc.value = 350;
                        break;
                    }
                #endregion

                # region Angry Bones

                case int angrybones when (
                (angrybones >= NPCID.BigBoned && angrybones <= NPCID.ShortBones) ||
                angrybones == NPCID.AngryBones ||
                (angrybones >= NPCID.AngryBonesBig && angrybones <= NPCID.AngryBonesBigHelmet)
                ):
                    {
                        npc.knockBackResist = 0.5f;
                        npc.value = 420;
                        break;
                    }

                #endregion

                #region Mages


                case (NPCID.DarkCaster):
                    {
                        npc.lifeMax = 100;
                        npc.damage = 0;
                        npc.defense = 5;
                        npc.knockBackResist = 0.8f;
                        npc.value = 450;
                        break;
                    }

                case (NPCID.WaterSphere):
                    {
                        npc.damage = 30;
                        npc.value = 20;
                        break;
                    }


                case (NPCID.FireImp):
                    {
                        npc.lifeMax = 112;
                        npc.damage = 0;
                        npc.defense = 18;
                        npc.knockBackResist = 0.6f;
                        npc.value = 600;
                        break;
                    }

                case (NPCID.BurningSphere):
                    {
                        npc.value = 30;
                        break;
                    }

                #endregion

                # region Beetles, Crawdads and Shellies

                case int beetles when (
                (beetles >= NPCID.CochinealBeetle && beetles <= NPCID.LacBeetle)
                ):
                    {
                        npc.value = 290;
                        break;
                    }

                case int crawdad when ((crawdad == NPCID.Crawdad || crawdad == NPCID.Crawdad2)):
                    {
                        npc.value = 560;
                        break;
                    }

                case int shelly when ((shelly == NPCID.GiantShelly || shelly == NPCID.GiantShelly2)):
                    {
                        npc.value = 530;
                        break;
                    }

                #endregion

                # region Fishes

                case (NPCID.CorruptGoldfish):
                    {
                        npc.value = 320;
                        break;
                    }

                case (NPCID.CrimsonGoldfish):
                    {
                        npc.value = 350;
                        break;
                    }

                case (NPCID.Piranha):
                    {
                        npc.value = 130;
                        break;
                    }

                case (NPCID.Shark):
                    {
                        npc.value = 500;
                        break;
                    }

                #endregion

                # region Others

                case (NPCID.Gnome):
                    {
                        npc.value = 150;
                        break;
                    }

                case (NPCID.FlyingFish):
                    {
                        npc.value = 70;
                        break;
                    }

                case (NPCID.Raven):
                    {
                        npc.value = 160;
                        break;
                    }

                case (NPCID.Ghost):
                    {
                        npc.value = 690;
                        break;
                    }

                case (NPCID.CrimsonPenguin):
                    {
                        npc.value = 410;
                        break;
                    }

                case int salamanders when ((salamanders >= NPCID.Salamander && salamanders <= NPCID.Salamander9)):
                    {
                        npc.value = 590;
                        break;
                    }

                case (NPCID.Antlion):
                    {
                        npc.lifeMax = 50;
                        npc.damage = 46;
                        npc.value = 360;
                        break;
                    }

                case (NPCID.LarvaeAntlion):
                    {
                        npc.value = 210;
                        break;
                    }

                case (NPCID.GiantWalkingAntlion):
                    {
                        npc.lifeMax = 60;
                        npc.defense = 10;
                        npc.value = 430;
                        break;
                    }
                case (NPCID.WalkingAntlion):
                    {
                        npc.lifeMax = 55;
                        npc.defense = 6;
                        npc.value = 380;
                        break;
                    }
                case (NPCID.GiantFlyingAntlion):
                    {
                        npc.defense = 8;
                        npc.lifeMax = 50;
                        npc.value = 420;
                        break;
                    }

                case (NPCID.FlyingAntlion):
                    {
                        npc.defense = 4;
                        npc.lifeMax = 45;
                        npc.value = 370;
                        break;
                    }

                case (NPCID.SnowFlinx):
                    {
                        npc.value = 380;
                        break;
                    }

                case (NPCID.GraniteFlyer):
                    {
                        npc.value = 340;
                        break;
                    }

                case (NPCID.GraniteGolem):
                    {
                        npc.value = 440;
                        break;
                    }

                case (NPCID.CorruptBunny):
                    {
                        npc.damage = 53;
                        npc.value = 310;
                        break;
                    }

                case (NPCID.CrimsonBunny):
                    {
                        npc.value = 340;
                        break;
                    }

                case (NPCID.FaceMonster):
                    {
                        npc.value = 240;
                        break;
                    }

                case int bloodcrawler when (
                bloodcrawler == NPCID.BloodCrawler ||
                bloodcrawler == NPCID.BloodCrawlerWall
                ):
                    {
                        npc.value = 320;
                        break;
                    }

                case (NPCID.Harpy):
                    {
                        npc.value = 500;
                        break;
                    }

                case (NPCID.GoblinScout):
                    {
                        npc.value = 3000;
                        break;
                    }

                case int wallcreeper when (
                (wallcreeper >= NPCID.WallCreeper && wallcreeper <= NPCID.WallCreeperWall)
                ):
                    {
                        npc.value = 400;
                        break;
                    }

                case (NPCID.Snatcher):
                    {
                        npc.damage = 34;
                        npc.value = 290;
                        break;
                    }

                case (NPCID.ManEater):
                    {
                        npc.damage = 45;
                        npc.lifeMax = 130;
                        npc.defense = 14;
                        npc.value = 370;
                        break;
                    }

                case int normalbees when (
                normalbees == NPCID.Bee ||
                normalbees == NPCID.BeeSmall
                ):
                    {
                        npc.value = 40;
                        break;
                    }

                case (NPCID.MeteorHead):
                    {
                        npc.defense = 10;
                        npc.value = 300;
                        break;
                    }


                case (NPCID.CursedSkull):
                    {
                        npc.lifeMax = 53;
                        npc.damage = 51;
                        npc.defense = 8;
                        npc.knockBackResist = 0.1f;
                        npc.value = 520;
                        break;
                    }

                case (NPCID.BlazingWheel):
                    {
                        npc.scale = 1.2f;
                        npc.damage = 53;
                        break;
                    }

                case (NPCID.SpikeBall):
                    {
                        npc.scale = 1.5f;
                        npc.damage = 70;
                        break;
                    }

                case (NPCID.Demon):
                    {
                        npc.lifeMax = 140;
                        npc.defense = 24;
                        npc.knockBackResist = 0.6f;
                        npc.value = 700;
                        break;
                    }

                case (NPCID.VoodooDemon):
                    {
                        npc.defense = 30;
                        npc.damage = 42;
                        npc.lifeMax = 250;
                        npc.knockBackResist = 0.9f;
                        npc.value = 1000;
                        break;
                    }

                case (NPCID.BlueJellyfish):
                    {
                        npc.value = 240;
                        break;
                    }

                case (NPCID.Crab):
                    {
                        npc.value = 140;
                        break;
                    }

                case (NPCID.SeaSnail):
                    {
                        npc.value = 370;
                        break;
                    }

                case (NPCID.Squid):
                    {
                        npc.value = 330;
                        break;
                    }

                case (NPCID.FungiBulb):
                    {
                        npc.value = 330;
                        break;
                    }

                case (NPCID.DungeonGuardian):
                    {
                        npc.knockBackResist = 0.1f;
                        npc.value = 999990;
                        break;
                    }


                #endregion

                #endregion

                #region Hardmode

                #region  Slimes

                case (NPCID.ToxicSludge):
                    {
                        npc.value = 1000;
                        break;
                    }

                case (NPCID.HoppinJack):
                    {
                        npc.value = 1240;
                        break;
                    }

                case (NPCID.Slimer):
                    {
                        npc.defense = 50;
                        npc.damage = 70;
                        npc.scale = 1.5f;
                        npc.value = 200;
                        break;
                    }

                case (NPCID.Slimer2):
                    {
                        npc.defense = 60;
                        npc.damage = 100;
                        npc.scale = 1.5f;
                        npc.value = 1500;
                        break;
                    }

                case (NPCID.CorruptSlime):
                    {
                        npc.scale = 1.1f;
                        npc.value = 1000;
                        break;
                    }

                case (NPCID.Slimeling): //Corrupt Slime baby
                    {
                        npc.value = 600;
                        break;
                    }

                case (NPCID.IlluminantSlime):
                    {
                        npc.scale = 3f;
                        npc.value = 1800;
                        break;
                    }

                case int crimslime when (
                                          (crimslime >= NPCID.BigCrimslime && crimslime <= NPCID.LittleCrimslime) ||
                                          crimslime == NPCID.Crimslime
                                          ):
                    {
                        npc.value = 3000;
                        break;
                    }

                case (NPCID.RainbowSlime):
                    {
                        npc.damage = 200;
                        npc.scale = 2f;
                        npc.knockBackResist = 0.2f;
                        npc.lifeMax = 800;
                        npc.value = 10000;
                        break;
                    }

                #endregion

                # region Moss Hornets

                case int mosshornet when (
                    (mosshornet >= NPCID.GiantMossHornet && mosshornet <= NPCID.TinyMossHornet) ||
                    mosshornet == NPCID.MossHornet
                    ):
                    {
                        npc.value = 4670;
                        break;
                    }

                #endregion

                #region Bats

                case (NPCID.GiantBat):
                    {
                        npc.lifeMax = 105;
                        npc.damage = 49;
                        npc.defense = 20;
                        npc.knockBackResist = 0.2f;
                        npc.value = 1570;
                        break;
                    }

                case (NPCID.IlluminantBat):
                    {
                        npc.defense = 27;
                        npc.knockBackResist = 0.6f;
                        npc.value = 2140;
                        break;
                    }

                case (NPCID.Lavabat):
                    {
                        npc.value = 4440;
                        break;
                    }

                case (NPCID.GiantFlyingFox):
                    {
                        npc.value = 5380;
                        break;
                    }

                #endregion

                #region Worms

                case int digger when (
                                          (digger >= NPCID.DiggerHead && digger <= NPCID.DiggerTail)
                                          ):
                    {
                        {
                            npc.lifeMax = 1900;
                            npc.scale = 0.9f;
                            npc.value = 8560;
                        }
                        if (npc.type == NPCID.DiggerHead)
                        {
                            npc.defense = 15;
                            npc.damage = 60;
                        }
                        if (npc.type == NPCID.DiggerBody)
                        {
                            npc.defense = 40;
                            npc.damage = 35;
                            npc.knockBackResist = 0.1f;
                        }
                        if (npc.type == NPCID.DiggerTail)
                        {
                            npc.defense = 25;
                            npc.damage = 45;
                            npc.knockBackResist = 0.2f;
                        }
                        break;
                    }

                case int tombcrawler when (
                      (tombcrawler >= NPCID.TombCrawlerHead && tombcrawler <= NPCID.TombCrawlerTail)
                      ):
                    {
                            npc.lifeMax = 1600;
                            npc.scale = 1.3f;
                            npc.value = 9040;

                        if (npc.type == NPCID.TombCrawlerHead)
                        {
                            npc.defense = 15;
                            npc.damage = 66;
                        }
                        if (npc.type == NPCID.TombCrawlerBody)
                        {
                            npc.defense = 40;
                            npc.damage = 40;
                            npc.knockBackResist = 0.1f;
                        }
                        if (npc.type == NPCID.TombCrawlerTail)
                        {
                            npc.defense = 25;
                            npc.damage = 50;
                            npc.knockBackResist = 0.2f;
                        }
                            break;
                    }

                case int worldfeeder when (
                      (worldfeeder >= NPCID.SeekerHead && worldfeeder <= NPCID.SeekerTail)
                      ):
                    {
                        {
                            npc.scale = 1.3f;
                            npc.lifeMax = 2500;
                            npc.value = 11110;
                        }
                        if (npc.type == NPCID.SeekerHead)
                        {
                            npc.defense = 50;
                            npc.damage = 80;
                        }
                        if (npc.type == NPCID.SeekerBody)
                        {
                            npc.defense = 35;
                            npc.damage = 55;
                        }
                        if (npc.type == NPCID.SeekerTail)
                        {
                            npc.defense = 10;
                            npc.damage = 30;
                        }
                        break;
                    }

                #endregion

                #region Fishes

                case (NPCID.AnglerFish):
                    {
                        npc.value = 3930;
                        break;
                    }

                case (NPCID.BloodFeeder):
                    {
                        npc.damage = 90;
                        npc.knockBackResist = 0f;
                        npc.value = 4990;
                        break;
                    }

                case (NPCID.Arapaima):
                    {
                        npc.value = 6960;
                        break;
                    }

                case int sandshark when ((sandshark >= NPCID.SandShark && sandshark <= NPCID.SandsharkHallow)):
                    {
                        if (npc.type == NPCID.SandShark)
                        {
                            npc.value = 7450;
                        }
                        npc.value = 8990;
                        break;
                    }

                #endregion

                #region Spiders

                case int blackrecluse when (
                        blackrecluse == NPCID.BlackRecluse ||
                        blackrecluse == NPCID.BlackRecluseWall
                        ):
                    {
                        npc.value = 4990;
                        break;
                    }

                case int junglecreeper when (
                junglecreeper == NPCID.JungleCreeper ||
                junglecreeper == NPCID.JungleCreeperWall
                ):
                    {
                        npc.value = 6430;
                        break;
                    }

                case int desertscorpion when (desertscorpion == NPCID.DesertScorpionWalk || desertscorpion == NPCID.DesertScorpionWall):
                    {
                        npc.value = 5650;
                        break;
                    }

                #endregion

                #region Pigrons
                case int pigron when (
                pigron == NPCID.PigronCorruption ||
                pigron == NPCID.PigronHallow
                ):
                    {
                        npc.value = 4340;
                        break;
                    }
                #endregion

                #region Possessed Weapons
                case int possessedweapon when (
                possessedweapon == NPCID.CursedHammer ||
                possessedweapon == NPCID.EnchantedSword ||
                possessedweapon == NPCID.CrimsonAxe
                  ):
                    {
                        npc.value = 3890;
                        break;
                    }
                #endregion

                #region Armored People and beasts

                case (NPCID.PossessedArmor):
                    {
                        npc.value = 1840;
                        break;
                    }

                case (NPCID.HeavySkeleton): //ArmoredSkeleton
                    {
                        npc.npcSlots = 2;
                        npc.lifeMax = 1000;
                        npc.damage = 43;
                        npc.knockBackResist = 0.35f;
                        npc.defense = 36;
                        npc.value = 8490;
                        break;
                    }

                case (NPCID.ArmoredViking):
                    {
                        npc.npcSlots = 4;
                        npc.lifeMax = 3000;
                        npc.damage = 66;
                        npc.knockBackResist = 0f;
                        npc.defense = 20;
                        npc.value = 12340;
                        break;
                    }

                case (NPCID.SkeletonArcher):
                    {
                        npc.value = 2430;
                        npc.defense = 28;
                        npc.damage = 0;
                        npc.value = 2470;
                        break;
                    }

                case (NPCID.Werewolf):
                    {
                        npc.defense = 40;
                        npc.damage = 85;
                        npc.knockBackResist = 0.1f;
                        npc.value = 3670;
                        break;
                    }


                #endregion

                #region Jellyfish

                case (NPCID.GreenJellyfish):
                    {
                        npc.value = 4250;
                        break;
                    }

                case (NPCID.PinkJellyfish):
                    {
                        npc.value = 10;
                        npc.life = 200;
                        npc.defense = 15;
                        npc.damage = 5640;
                        break;
                    }

                case (NPCID.BloodJelly):
                    {
                        npc.knockBackResist = 0f;
                        npc.value = 8400;
                        break;
                    }

                case (NPCID.FungoFish):
                    {
                        npc.life = 1000;
                        npc.defense = 100;
                        npc.damage = 100;
                        npc.knockBackResist = 0f;
                        npc.value = 14440;
                        break;
                    }

                #endregion

                #region Armored Bones

                case int rustyarmoredbones when (
                (rustyarmoredbones >= NPCID.RustyArmoredBonesAxe && rustyarmoredbones <= NPCID.RustyArmoredBonesSwordNoArmor)
                ):
                    {
                        npc.value = 11580;
                        break;
                    }

                case int bluearmoredbones when (
                (bluearmoredbones >= NPCID.BlueArmoredBones && bluearmoredbones <= NPCID.BlueArmoredBonesSword)
                 ):
                    {
                        npc.value = 12370;
                        break;
                    }

                case int hellarmoredbones when (
                    (hellarmoredbones >= NPCID.HellArmoredBones && hellarmoredbones <= NPCID.HellArmoredBonesSword)
                     ):
                    {
                        npc.value = 13430;
                        break;
                    }

                case (NPCID.BoneLee):
                    {
                        npc.value = 19330;
                        break;
                    }

                #endregion

                #region Mages

                case (NPCID.DesertDjinn):
                    {
                        npc.value = 7590;
                        break;
                    }

                case int raggedcaster when (raggedcaster == NPCID.RaggedCaster || raggedcaster == NPCID.RaggedCasterOpenCoat):
                    {
                        npc.value = 22720;
                        break;
                    }

                case int necromancershadowbeam when (necromancershadowbeam == NPCID.Necromancer || necromancershadowbeam == NPCID.NecromancerArmored):
                    {
                        npc.value = 16140;
                        break;
                    }

                case int diabolist when (diabolist == NPCID.DiabolistRed || diabolist == NPCID.DiabolistWhite):
                    {
                        npc.value = 18980;
                        break;
                    }

                #endregion

                #region Mummies, Ghouls & Lamia

                case int mummy when (
                (mummy >= NPCID.Mummy && mummy <= NPCID.LightMummy)
                 || mummy == NPCID.BloodMummy
                ):
                    {
                        npc.value = 2560;

                        if (npc.type == NPCID.LightMummy)
                        {
                            npc.knockBackResist = 0.6f;
                        }

                        if (npc.type == NPCID.DarkMummy)
                        {
                            npc.knockBackResist = 0.1f;
                            npc.damage = 85;
                        }
                        if (npc.type == NPCID.BloodMummy)
                        {
                            npc.knockBackResist = 0f;
                            npc.damage = 100;
                            npc.defense = 50;
                            npc.lifeMax = 2000;
                            npc.value = 1050;
                        }
                        break;
                    }

                case int ghoul when (
                (ghoul >= NPCID.DesertGhoul && ghoul <= NPCID.DesertGhoulHallow)
                ):
                    {
                        npc.value = 3670;

                        if (npc.type == NPCID.DesertGhoulHallow)
                        {
                            npc.knockBackResist = 0.6f;
                        }

                        if (npc.type == NPCID.DesertGhoulCorruption)
                        {
                            npc.knockBackResist = 0.1f;
                            npc.damage = 85;
                        }
                        if (npc.type == NPCID.DesertGhoulCrimson)
                        {
                            npc.knockBackResist = 0f;
                            npc.damage = 15000;
                            npc.defense = 70;
                            npc.lifeMax = 6000;
                            npc.value = 13510;
                        }
                        break;
                    }

                case int lamia when (lamia == NPCID.DesertLamiaDark || lamia == NPCID.DesertLamiaLight):
                    {
                        npc.value = 4210;
                        break;
                    }

                #endregion

                #region Others

                case (NPCID.DemonTaxCollector):
                    {
                        npc.value = 990;
                        break;
                    }

                case (NPCID.Vulture):
                    {
                        npc.damage = 200;
                        npc.lifeMax = 500;
                        npc.value = 2490;
                        break;
                    }

                case (NPCID.Tumbleweed):
                    {
                        npc.damage = 300;
                        npc.lifeMax = 300;
                        npc.value = 2990;
                        break;
                    }

                case int mushroomzombie when (
                mushroomzombie == NPCID.ZombieMushroom ||
                mushroomzombie == NPCID.ZombieMushroomHat
                ):
                    {
                        npc.value = 6330;
                        break;
                    }

                case int mushroombeetle when (
                mushroombeetle == NPCID.AnomuraFungus ||
                mushroombeetle == NPCID.MushiLadybug
                ):
                    {
                        npc.lifeMax = 1000;
                        npc.defense = 50;
                        npc.damage = 100;
                        npc.value = 8220;
                        break;
                    }

                case (NPCID.GiantFungiBulb):
                    {
                        npc.defense = 80;
                        npc.lifeMax = 1500;
                        npc.damage = 50;
                        npc.value = 10770;
                        break;
                    }

                case (NPCID.FungiSpore):
                    {
                        npc.value = 300;
                        npc.damage = 90;
                        break;
                    }

                case (NPCID.DesertBeast):
                    {
                        npc.value = 3210;
                        break;
                    }

                case (NPCID.WanderingEye):
                    {
                        npc.value = 2580;
                        break;
                    }

                case (NPCID.AngryNimbus):
                    {
                        npc.value = 4210;
                        break;
                    }

                case (NPCID.Wraith):
                    {
                        npc.defense = 18;
                        npc.damage = 75;
                        npc.lifeMax = 500;
                        npc.scale = 1.2f;
                        npc.knockBackResist = 0;
                        npc.value = 5120;
                        break;
                    }


                case (NPCID.Pixie):
                    {
                        npc.value = 2850;
                        break;
                    }

                case (NPCID.Gastropod):
                    {
                        npc.value = 3240;
                        break;
                    }

                case (NPCID.Unicorn):
                    {
                        npc.knockBackResist = 0.2f;
                        npc.damage = 85;
                        npc.value = 4690;
                        break;
                    }

                case (NPCID.ChaosElemental):
                    {
                        npc.lifeMax = 396;
                        npc.damage = 46;
                        npc.defense = 25;
                        npc.knockBackResist = 0.2f;
                        npc.value = 4190;
                        break;
                    }

                case (NPCID.Corruptor):
                    {
                        npc.value = 4130;
                        break;
                    }

                case (NPCID.VileSpit):
                    {
                        npc.damage = 80;
                        npc.value = 100;
                        break;
                    }

                case (NPCID.Clinger):
                    {
                        npc.lifeMax = 650;
                        npc.value = 5430;
                        break;
                    }

                case (NPCID.Herpling):
                    {
                        npc.damage = 80;
                        npc.lifeMax = 400;
                        npc.knockBackResist = 0f;
                        npc.value = 5470;
                        break;
                    }

                case (NPCID.FloatyGross):
                    {
                        npc.lifeMax = 500;
                        npc.knockBackResist = 0f;
                        npc.damage = 60;
                        npc.value = 5990;
                        break;
                    }

                case (NPCID.IchorSticker):
                    {
                        npc.lifeMax = 400;
                        npc.defense = 40;
                        npc.value = 5490;
                        break;
                    }

                case (NPCID.IceTortoise):
                    {
                        npc.value = 5780;
                        break;
                    }

                case (NPCID.Wolf):
                    {
                        npc.value = 2550;
                        break;
                    }

                case (NPCID.IceElemental):
                    {
                        npc.value = 3970;
                        break;
                    }

                case (NPCID.IcyMerman):
                    {
                        npc.value = 4210;
                        break;
                    }

                case (NPCID.RedDevil):
                    {
                        npc.lifeMax = 2000; //this is like 3x their normal value
                        npc.defense = 90;
                        npc.damage = 66; //their projectiles two-shot you though
                        npc.knockBackResist = 0.1f;
                        npc.value = 16660;
                        break;
                    }

                case (NPCID.GiantTortoise):
                    {
                        npc.value = 7760;
                        break;
                    }

                case (NPCID.AngryTrapper):
                    {
                        npc.value = 5390;
                        break;
                    }

                case (NPCID.Derpling):
                    {
                        npc.knockBackResist = 0f;
                        npc.value = 3390;
                        break;
                    }

                case (NPCID.SkeletonSniper):
                    {
                        npc.value = 18680;
                        break;
                    }

                case (NPCID.TacticalSkeleton):
                    {
                        npc.value = 16340;
                        break;
                    }

                case (NPCID.SkeletonCommando):
                    {
                        npc.value = 19490;
                        break;
                    }

                case int lihzard when (
                        lihzard == NPCID.Lihzahrd ||
                        lihzard == NPCID.LihzahrdCrawler
                        ):
                    {
                        npc.value = 7380;
                        break;
                    }

                case (NPCID.FlyingSnake):
                    {
                        npc.value = 8430;
                        break;
                    }

                case (NPCID.MartianProbe): //rip
                    {
                        npc.defense = 100;
                        npc.value = 10000;
                        break;
                    }

                #endregion

                #endregion

                #region Pre-Hardmode Minibosses

                case (NPCID.Pinky):
                    {
                        npc.value = 4390;
                        break;
                    }

                case (NPCID.TheGroom):
                    {
                        npc.damage = 45;
                        npc.lifeMax = 250;
                        npc.value = 6210;
                        break;
                    }

                case (NPCID.TheBride):
                    {
                        npc.damage = 52;
                        npc.lifeMax = 180;
                        npc.value = 6290;
                        break;
                    }

                case (NPCID.ZombieMerman):
                    {
                        npc.value = 9740;
                        break;
                    }

                case (NPCID.EyeballFlyingFish):
                    {
                        npc.value = 8970;
                        break;
                    }

                case (NPCID.Tim):
                    {
                        npc.GivenName = "Tim Hjersted";
                        npc.damage = 100;
                        npc.lifeMax = 2000;
                        npc.defense = 30;
                        npc.scale = 1.5f;
                        npc.knockBackResist = 0f;
                        npc.value = 88880;
                        break;
                    }

                case (NPCID.ChaosBallTim):
                    {
                        npc.damage = 200;
                        npc.value = 120;
                        npc.scale = 1.5f;
                        break;
                    }

                case (NPCID.DoctorBones): //this guy is very very rare
                    {
                        npc.GivenName = "Indiana Jones";
                        npc.damage = 100;
                        npc.lifeMax = 1500;
                        npc.defense = 20;
                        npc.knockBackResist = 0f;
                        npc.scale = 1.2f;
                        npc.value = 65490;
                        break;
                    }

                case int nymph when (
                        nymph == NPCID.LostGirl ||
                        nymph == NPCID.Nymph
                        ):
                    {
                        if (npc.type == NPCID.LostGirl)
                        {
                            npc.lifeMax = 50;
                        }
                        npc.lifeMax = 1000;
                        npc.defense = 10;
                        npc.damage = 66;
                        npc.knockBackResist = 0.05f;
                        npc.scale = 0.7f;
                        npc.value = 44000;
                        break;
                    }

                #endregion

                #region Hardmode Minibosses

                case (NPCID.Clown):
                    {
                        npc.damage = 50;
                        npc.lifeMax = 4000;
                        npc.value = 50910;
                        npc.defense = 35;
                        break;
                    }

                case (NPCID.ChatteringTeethBomb):
                    {
                        npc.value = 1300;
                        break;
                    }

                case int wyvern when (
                (wyvern >= NPCID.WyvernHead && wyvern <= NPCID.WyvernTail)
                ):
                    {
                        npc.defense = 20;
                        npc.lifeMax = 6000;
                        npc.value = 33560;

                        if (npc.type == NPCID.WyvernHead)
                        {
                            npc.damage = 100;
                        }
                        break;
                    }

                case (NPCID.GoblinShark):
                    {
                        npc.lifeMax = 2000;
                        npc.damage = 80;
                        npc.value = 65580;
                        npc.defense = 60;
                        npc.knockBackResist = 0.1f;
                        break;
                    }

                case int bloodeel when ((bloodeel >= NPCID.BloodEelHead && bloodeel <= NPCID.BloodEelTail)):
                    {
                        if (npc.type == NPCID.BloodEelHead)
                        {
                            npc.damage = 120;
                        }
                        npc.value = 46590;
                        break;
                    }

                case (NPCID.RuneWizard):
                    {
                        npc.value = 67530;
                        break;
                    }

                case (NPCID.RockGolem):
                    {
                        npc.value = 8750;
                        break;
                    }

                case (NPCID.Moth):
                    {
                        npc.life = 10000;
                        npc.defense = 100;
                        npc.damage = 300;
                        npc.value = 19450;
                        break;
                    }

                case (NPCID.Medusa):
                    {
                        npc.value = 15410;
                        break;
                    }

                case (NPCID.IceGolem):
                    {
                        npc.lifeMax = 6000;
                        npc.value = 30240;
                        break;
                    }

                case (NPCID.SandElemental):
                    {
                        npc.lifeMax = 7000;
                        npc.value = 37630;
                        break;
                    }

                case (NPCID.Mimic):
                    {
                        npc.value = 9550;
                        break;
                    }

                case (NPCID.IceMimic):
                    {
                        npc.value = 12220;
                        break;
                    }

                case int biomemimic when ((biomemimic >= NPCID.BigMimicCorruption && biomemimic <= NPCID.BigMimicJungle)):
                    {
                        npc.value = 21400;
                        break;
                    }

                case (NPCID.Paladin):
                    {
                        npc.value = 47390;
                        break;
                    }

                #endregion

                #region Bosses

                #region Pre-Hardmode Bosses

                #region King Slime
                case (NPCID.KingSlime):
                    {
                        npc.damage = 50;
                        npc.defense = 15;
                        npc.scale = 1.25f;
                        npc.value = 25450;
                        break;
                    }

                case (NPCID.SlimeSpiked):
                    {
                        npc.value = 230;
                        break;
                    }
                #endregion

                #region Eye of Cthulhu
                case (NPCID.EyeofCthulhu):
                    {
                        //damage changes here are for first phase
                        npc.damage = 27; //legacy: 37
                        npc.value = 33330;
                        if (Main.expertMode)
                        {
                            npc.damage = 21; //revamp expert: 42
                        }
                        if (Main.player[Main.myPlayer].ZoneJungle)
                        {
                            if (Main.expertMode)
                            {
                                npc.lifeMax = 3077; // Which is actually 4k hp in expert mode
                            }
                            npc.scale = 1.1f;
                        }
                        break;
                    }

                case (NPCID.ServantofCthulhu):
                    {
                        npc.damage = 13;
                        npc.value = 30;
                        break;
                    }
                #endregion

                #region Eater of Worlds
                case (NPCID.EaterofWorldsHead):
                    {
                        npc.lifeMax = 180;
                        npc.damage = 30;
                        npc.defense = 22;
                        npc.value = 1;
                        npc.buffImmune[BuffID.Poisoned] = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.Frostburn] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.EaterofWorldsBody):
                    {
                        npc.lifeMax = 180;
                        npc.damage = 18; //legacy: 22
                        npc.defense = 5;
                        npc.value = 1; //ignored
                        npc.buffImmune[BuffID.Poisoned] = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.Frostburn] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.EaterofWorldsTail):
                    {
                        npc.lifeMax = 155;
                        npc.defense = 8;
                        npc.value = 1; //ignored
                        npc.buffImmune[BuffID.Poisoned] = true;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.Frostburn] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        break;
                    }

                case (NPCID.VileSpitEaterOfWorlds):
                    {
                        npc.value = 20;
                        break;
                    }
                #endregion

                #region Brain of Cthulhu
                case (NPCID.BrainofCthulhu):
                    {
                        npc.value = 55550;
                        break;
                    }

                case (NPCID.Creeper):
                    {
                        npc.value = 1110;
                        break;
                    }
                #endregion

                #region Queen Bee
                case (NPCID.QueenBee):
                    {
                        npc.value = 67430;
                        break;
                    }
                #endregion

                #region Skeletron
                case (NPCID.SkeletronHead):
                    {
                        npc.value = 84480;
                        npc.defense = 12;
                        npc.damage = 35; //legacy: 50
                        npc.lifeMax = 4400;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.SkeletronHand):
                    {
                        npc.value = 5780;
                        npc.defense = 14; //legacy: 12
                        npc.damage = 22; //legacy: 40
                        npc.lifeMax = 600;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }
                #endregion

                #region Deerclops
                case (NPCID.Deerclops):
                    {
                        npc.value = 113520;
                        break;
                    }
                #endregion

                #region Wall of Flesh
                case (NPCID.WallofFlesh):
                    {
                        npc.value = 102580;
                        npc.damage = 100;
                        npc.lifeMax = 14000;
                        break;
                    }

                case (NPCID.TheHungry):
                    {
                        npc.value = 880;
                        npc.knockBackResist = 0.3f;
                        break;
                    }

                case (NPCID.TheHungryII):
                    {
                        npc.value = 450;
                        npc.knockBackResist = 0.5f;
                        break;
                    }

                case int leech when (
                      (leech >= NPCID.LeechHead && leech <= NPCID.LeechTail)
                      ):
                    {
                        {
                            npc.lifeMax = 333;
                            npc.value = 1230;
                        }
                        if (npc.type == NPCID.LeechHead)
                        {
                            npc.defense = 25;
                            npc.damage = 30;
                        }
                        if (npc.type == NPCID.LeechBody)
                        {
                            npc.defense = 17;
                        }
                        if (npc.type == NPCID.LeechTail)
                        {
                            npc.defense = -25;
                        }
                        break;
                    }
                #endregion

                #endregion


                #region HardmodeBosses


                #region Queen Slime
                case (NPCID.QueenSlimeBoss):
                    {
                        npc.value = 154780;
                        break;
                    }

                case (NPCID.QueenSlimeMinionBlue):
                    {
                        npc.value = 1230;
                        break;
                    }

                case (NPCID.QueenSlimeMinionPink):
                    {
                        npc.value = 1240;
                        break;
                    }

                case (NPCID.QueenSlimeMinionPurple):
                    {
                        npc.value = 1270;
                        break;
                    }
                #endregion

                #region Dreadnautilus
                case (NPCID.BloodNautilus):
                    {
                        npc.value = 114500;
                        break;
                    }

                case (NPCID.BloodSquid):
                    {
                        npc.value = 1450;
                        break;
                    }
                #endregion

                #region The Twins
                case int thetwins when (thetwins ==NPCID.Retinazer || thetwins == NPCID.Spazmatism):
                    {
                        npc.value = 164540;
                        if(npc.type == NPCID.Retinazer)
                        {
                            npc.defense = 28;
                            npc.damage = 56; //legacy: 80
                            npc.lifeMax = 25000;
                        }
                        if(npc.type == NPCID.Spazmatism)
                        {
                            npc.damage = 80;
                            npc.defense = 36;
                            npc.lifeMax = 25000;
                        }
                        break;
                    }
                #endregion

                #region The Destroyer
                case (NPCID.TheDestroyer):
                    {
                        npc.lifeMax = 40000;
                        npc.value = 203430;
                        npc.scale = 1.25f;
                        npc.damage = Main.expertMode ? 40 /* x4 in expert */: 60; //legacy: 200, vanilla: 70
                        npc.defense = 20; //legacy: 50, vanilla: 0
                        destroyerAttackIndex = 0; //These variables are static and global, since we don't have any way to attach extra data to the destroyers NPC instance itself
                        destroyerChargeTimer = 0;
                        destroyerJustSpawned = true;
                        destroyerReachedHeight = false;
                        break;
                    }

                case (NPCID.TheDestroyerBody):
                    {
                        npc.scale = 1.25f;
                        npc.damage = 60; //legacy: 60, vanilla: 55
                        npc.defense = 55; //legacy: 55, vanilla: 30
                        break;
                    }

                case (NPCID.TheDestroyerTail):
                    {
                        npc.scale = 1.25f;
                        npc.damage = 80; //legacy: 80, vanilla: 40
                        npc.defense = 0;
                        break;
                    }

                case (NPCID.Probe):
                    {
                        npc.defense = 30;
                        npc.damage = 55;
                        npc.value = 6670;
                        break;
                    }
                #endregion

                #region Skeletron Prime
                case (NPCID.SkeletronPrime):
                    {
                        npc.value = 256430;
                        npc.defense = 40;
                        npc.damage = 100;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.buffImmune[BuffID.CursedInferno] = true;
                        PrimeLaserCooldown = 99999999;
                        break;
                    }

                case (NPCID.PrimeCannon):
                    {
                        npc.value = 25190;
                        npc.damage = 45;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.PrimeLaser):
                    {
                        npc.value = 25870;
                        npc.defense = 30;
                        npc.damage = 40;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.PrimeSaw):
                    {
                        npc.value = 25980;
                        npc.defense = 38;
                        npc.damage = 60;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }

                case (NPCID.PrimeVice):
                    {
                        npc.value = 25630;
                        npc.defense = 34;
                        npc.damage = 55;
                        npc.buffImmune[BuffID.OnFire] = true;
                        break;
                    }
                #endregion

                #region Plantera
                case (NPCID.Plantera):
                    {
                        npc.value = 275930;
                        npc.lifeMax = 35000;
                        break;
                    }

                case (NPCID.PlanterasTentacle):
                    {
                        npc.value = 5490;
                        break;
                    }

                case (NPCID.Spore):
                    {
                        npc.value = 130;
                        break;
                    }
                #endregion

                #region Duke Fishron
                case (NPCID.DukeFishron):
                    {
                        npc.value = 306660;
                        break;
                    }

                case (NPCID.DetonatingBubble):
                    {
                        npc.value = 1030;
                        break;
                    }

                case int sharkron when (sharkron == NPCID.Sharkron || sharkron == NPCID.Sharkron2):
                    {
                        npc.value = 9460;
                        break;
                    }
                #endregion

                #region Golem
                case (NPCID.Golem):
                    {
                        npc.value = 233650;
                        break;
                    }

                case (NPCID.GolemHead):
                    {
                        npc.value = 24500;
                        break;
                    }

                case int golemfists when (golemfists == NPCID.GolemFistLeft || golemfists == NPCID.GolemFistRight):
                    {
                        npc.value = 16980;
                        break;
                    }
                #endregion

                #region Betsy

                case (NPCID.DD2Betsy):
                    {
                        npc.value = 307890;
                        break;
                    }

                case (NPCID.DD2WyvernT3):
                    {
                        npc.value = 6380;
                        break;
                    }

                #endregion

                #region Empress of Light
                case (NPCID.HallowBoss):
                    {
                        npc.value = 444440;
                        break;
                    }
                #endregion

                #region Lunatic Cultist

                case (NPCID.CultistBoss):
                    {
                        npc.value = 123450;
                        break;
                    }

                case (NPCID.CultistBossClone):
                    {
                        npc.value = 1; //ignored
                        break;
                    }

                case int phantasmdragon when (phantasmdragon >= NPCID.CultistDragonHead && phantasmdragon <= NPCID.CultistDragonTail):
                    {
                        npc.value = 34560;
                        npc.lifeMax = 5000;
                        if (npc.type == NPCID.CultistDragonHead)
                        {
                            npc.damage = 130;
                            npc.defense = 60;
                        }
                        if (npc.type == NPCID.CultistDragonBody1)
                        {
                            npc.damage = 90;
                            npc.defense = 80;
                            npc.knockBackResist = 0.05f;
                        }
                        if (npc.type == NPCID.CultistDragonTail)
                        {
                            npc.damage = 170;
                            npc.defense = 200;
                        }
                        break;
                    }

                case (NPCID.AncientCultistSquidhead):
                    {
                        npc.value = 67890;
                        break;
                    }

                case (NPCID.AncientLight):
                    {
                        npc.value = 2370;
                        break;
                    }

                case (NPCID.AncientDoom):
                    {
                        npc.value = 3410;
                        break;
                    }

                #endregion

                #region Moon Lord
                case (NPCID.MoonLordCore):
                    {
                        npc.value = 464480;
                        break;
                    }

                case (NPCID.MoonLordHand):
                    {
                        npc.value = 69880;
                        break;
                    }

                case (NPCID.MoonLordHead):
                    {
                        npc.value = 87650;
                        break;
                    }

                case (NPCID.MoonLordFreeEye):
                    {
                        npc.value = 10; //currently unkillable
                        break;
                    }

                case (NPCID.MoonLordLeechBlob):
                    {
                        npc.lifeMax = 400;
                        npc.value = 8790;
                        break;
                    }
                #endregion

                #endregion


                #region Sacrificial Lambs

                case (NPCID.OldMan):
                    {
                        npc.value = 440;
                        break;
                    }

                case (NPCID.Guide):
                    {
                        npc.value = 1110;
                        break;
                    }

                case int cultistarcher when (cultistarcher == NPCID.CultistArcherBlue || cultistarcher == NPCID.CultistArcherWhite):
                    {
                        npc.value = 590;
                        break;
                    }

                case (NPCID.CultistDevote):
                    {
                        npc.value = 510;
                        break;
                    }

                case (NPCID.CultistTablet):
                    {
                        npc.value = 520;
                        break;
                    }

                #endregion


                #endregion

                #region Invasions

                #region Goblin Army
                case int goblinarmy when (
                (goblinarmy >= NPCID.GoblinPeon && goblinarmy <= NPCID.GoblinSorcerer)
                || goblinarmy == NPCID.GoblinArcher
                    ):
                    {
                        npc.value = 420; // x/10 * (80+[y*40]) (x = value, y = player count) for approximate total soul drops per army 

                        if (npc.type == NPCID.GoblinPeon)
                        {
                            npc.damage = 20;
                            npc.defense = 10;
                            npc.scale = 0.5f;
                            npc.lifeMax = 75;
                        }
                        if (npc.type == NPCID.GoblinThief)
                        {
                            npc.damage = 50;
                            npc.defense = 6;
                            npc.scale = 0.75f;
                            npc.lifeMax = 125;
                        }
                        if (npc.type == NPCID.GoblinWarrior)
                        {
                            npc.damage = 40;
                            npc.defense = 30;
                            npc.lifeMax = 300;
                            npc.scale = 1.5f;
                        }
                        if (npc.type == NPCID.GoblinSorcerer)
                        {
                            npc.lifeMax = 100;
                            npc.defense = 0;
                            npc.damage = 0;
                            npc.knockBackResist = 0.1f;
                        }
                        if (npc.type == NPCID.GoblinArcher)
                        {
                            npc.lifeMax = 150;
                            npc.defense = 14;
                            npc.damage = 0;
                            npc.knockBackResist = 0.1f;
                        }
                        break;
                    }

                case (NPCID.ChaosBall):
                    {
                        npc.damage = 30;
                        npc.value = 190;
                        break;
                    }

                case (NPCID.GoblinSummoner):
                    {
                        npc.value = 15690;
                        break;
                    }

                case (NPCID.ShadowFlameApparition):
                    {
                        npc.value = 2560;
                        break;
                    }

                #endregion

                # region Old One's Army

                case (NPCID.DD2GoblinT1):
                    {
                        npc.value = 490;
                        break;
                    }

                case (NPCID.DD2GoblinT2):
                    {
                        npc.value = 3420;
                        break;
                    }

                case (NPCID.DD2GoblinT3):
                    {
                        npc.value = 4510;
                        break;
                    }

                case (NPCID.DD2GoblinBomberT1):
                    {
                        npc.value = 530;
                        break;
                    }

                case (NPCID.DD2GoblinBomberT2):
                    {
                        npc.value = 3730;
                        break;
                    }

                case (NPCID.DD2GoblinBomberT3):
                    {
                        npc.value = 5220;
                        break;
                    }

                case (NPCID.DD2WyvernT1):
                    {
                        npc.value = 690;
                        break;
                    }

                case (NPCID.DD2WyvernT2): //see tier 3 at Betsy's
                    {
                        npc.value = 4490;
                        break;
                    }

                case (NPCID.DD2JavelinstT1):
                    {
                        npc.value = 580;
                        break;
                    }

                case (NPCID.DD2JavelinstT2):
                    {
                        npc.value = 3940;
                        break;
                    }

                case (NPCID.DD2JavelinstT3):
                    {
                        npc.value = 5990;
                        break;
                    }

                case (NPCID.DD2DarkMageT1):
                    {
                        npc.value = 6350;
                        break;
                    }

                case (NPCID.DD2DarkMageT3):
                    {
                        npc.value = 16730;
                        break;
                    }

                case (NPCID.DD2SkeletonT1):
                    {
                        npc.value = 40;
                        break;
                    }

                case (NPCID.DD2SkeletonT3):
                    {
                        npc.value = 130;
                        break;
                    }

                case (NPCID.DD2WitherBeastT2):
                    {
                        npc.value = 9090;
                        break;
                    }

                case (NPCID.DD2WitherBeastT3):
                    {
                        npc.value = 24480;
                        break;
                    }

                case (NPCID.DD2DrakinT2):
                    {
                        npc.value = 8970;
                        break;
                    }

                case (NPCID.DD2DrakinT3):
                    {
                        npc.value = 19010;
                        break;
                    }

                case (NPCID.DD2KoboldWalkerT2):
                    {
                        npc.value = 2790;
                        break;
                    }

                case (NPCID.DD2KoboldWalkerT3):
                    {
                        npc.value = 4980;
                        break;
                    }

                case (NPCID.DD2KoboldFlyerT2):
                    {
                        npc.value = 2610;
                        break;
                    }

                case (NPCID.DD2KoboldFlyerT3):
                    {
                        npc.value = 5920;
                        break;
                    }

                case (NPCID.DD2OgreT2):
                    {
                        npc.value = 27390;
                        break;
                    }

                case (NPCID.DD2OgreT3):
                    {
                        npc.value = 35890;
                        break;
                    }

                case (NPCID.DD2LightningBugT3):
                    {
                        npc.lifeMax = 500;
                        npc.damage = 69;
                        npc.defense = 35;
                        npc.knockBackResist = 0.46f;
                        npc.value = 2500;
                        break;
                    }

                #endregion


                # region Frost Legion

                case int snowmen when ((snowmen >= NPCID.SnowmanGangsta && snowmen <= NPCID.SnowBalla)):
                    {
                        npc.value = 870; // x/10 * (80+[y*40]) (x = value, y = player count) for approximate total soul drops per legion
                        break;
                    }

                #endregion

                # region Pirate Invasion

                case int pirates when ((pirates >= NPCID.PirateDeckhand && pirates <= NPCID.PirateCaptain) || pirates == NPCID.PirateShip || pirates == NPCID.PirateShipCannon || pirates == NPCID.PirateGhost):
                    {
                        npc.value = 890; //wiki doesn't tell me how many pirates come so idk
                        if (npc.type == NPCID.PirateCaptain)
                        {
                            npc.value = 4430;
                        }
                        if (npc.type == NPCID.PirateShipCannon)
                        {
                            npc.value = 5540;
                        }
                        if (npc.type == NPCID.PirateShip)
                        {
                            npc.value = 14490;
                        }
                        break;
                    }

                case (NPCID.Parrot):
                    {
                        npc.value = 770;
                        break;
                    }
                #endregion

                #region Solar Eclipse

                case (NPCID.Butcher):
                    {
                        npc.value = 4410;
                        break;
                    }

                case (NPCID.CreatureFromTheDeep):
                    {
                        npc.value = 3780;
                        break;
                    }

                case (NPCID.Fritz):
                    {
                        npc.value = 3730;
                        break;
                    }

                case (NPCID.Nailhead):
                    {
                        npc.value = 20940;
                        break;
                    }

                case (NPCID.Psycho):
                    {
                        npc.damage = 250;
                        npc.value = 5120;
                        break;
                    }

                case (NPCID.DeadlySphere):
                    {
                        npc.lifeMax = 25;
                        npc.defense = 300;
                        npc.damage = 200;
                        npc.value = 5340;
                        break;
                    }

                case (NPCID.DrManFly):
                    {
                        npc.value = 5120;
                        break;
                    }

                case (NPCID.ThePossessed):
                    {
                        npc.value = 4100;
                        break;
                    }

                case int vampire when (
                                        (vampire >= NPCID.VampireBat && vampire <= NPCID.Vampire)
                                        ):
                    {
                        npc.value = 6840;
                        break;
                    }

                case (NPCID.Frankenstein):
                    {
                        npc.value = 3890;
                        break;
                    }

                case (NPCID.SwampThing):
                    {
                        npc.value = 3400;
                        break;
                    }

                case (NPCID.Reaper):
                    {
                        npc.value = 4210;
                        break;
                    }

                case (NPCID.Eyezor):
                    {
                        npc.value = 10420;
                        break;
                    }

                case int mothron when ((mothron >= NPCID.Mothron && mothron <= NPCID.MothronSpawn)):
                    {
                        npc.value = 3250;
                        if (npc.type == NPCID.Mothron)
                        {
                            npc.value = 48380;
                        }
                        break;
                    }

                #endregion

                #region Pumpkin Moon

                case int scarecrow when ((scarecrow >= NPCID.Scarecrow1 && scarecrow <= NPCID.Scarecrow10)):
                    {
                        npc.value = 12980;
                        break;
                    }

                case (NPCID.HeadlessHorseman):
                    {
                        npc.value = 17930;
                        break;
                    }

                case (NPCID.MourningWood):
                    {
                        npc.value = 23339;
                        break;
                    }

                case (NPCID.Splinterling):
                    {
                        npc.value = 14480;
                        break;
                    }

                case (NPCID.Pumpking):
                    {
                        npc.value = 32090;
                        break;
                    }

                case (NPCID.PumpkingBlade):
                    {
                        break;
                    }

                case (NPCID.Hellhound):
                    {
                        npc.value = 15480;
                        break;
                    }

                case (NPCID.Poltergeist):
                    {
                        npc.value = 15630;
                        break;
                    }
                #endregion

                #region Frost Moon

                case int zombieelfs when ((zombieelfs >= NPCID.ZombieElf && zombieelfs <= NPCID.ZombieElfGirl)):
                    {
                        npc.value = 13490;
                        break;
                    }

                case (NPCID.PresentMimic):
                    {
                        npc.value = 24580;
                        break;
                    }

                case (NPCID.GingerbreadMan):
                    {
                        npc.value = 16730;
                        break;
                    }

                case (NPCID.Yeti):
                    {
                        npc.value = 24530;
                        break;
                    }

                case (NPCID.Everscream):
                    {
                        npc.value = 30490;
                        break;
                    }

                case (NPCID.IceQueen):
                    {
                        npc.value = 49420;
                        break;
                    }

                case (NPCID.SantaNK1):
                    {
                        npc.value = 36730;
                        break;
                    }

                case (NPCID.ElfCopter):
                    {
                        npc.value = 20590;
                        break;
                    }

                case int nutcracker when (nutcracker == NPCID.Nutcracker || nutcracker == NPCID.NutcrackerSpinning):
                    {
                        npc.value = 18780;
                        break;
                    }

                case (NPCID.Krampus):
                    {
                        npc.value = 2448;
                        break;
                    }

                case (NPCID.Flocko):
                    {
                        npc.value = 17830;
                        break;
                    }

                #endregion

                #region Martian Madness

                case (NPCID.BrainScrambler):
                    {
                        npc.value = 12430;
                        break;
                    }

                case (NPCID.RayGunner):
                    {
                        npc.value = 10930;
                        break;
                    }

                case (NPCID.MartianOfficer):
                    {
                        npc.value = 24920;
                        break;
                    }

                case (NPCID.ForceBubble):
                    {
                        npc.value = 100;
                        break;
                    }

                case (NPCID.GrayGrunt):
                    {
                        npc.value = 6380;
                        break;
                    }

                case (NPCID.MartianEngineer):
                    {
                        npc.value = 7480;
                        break;
                    }

                case (NPCID.MartianTurret):
                    {
                        npc.value = 10560;
                        break;
                    }

                case (NPCID.MartianDrone):
                    {
                        npc.value = 9430;
                        break;
                    }

                case (NPCID.GigaZapper):
                    {
                        npc.value = 9930;
                        break;
                    }

                case (NPCID.ScutlixRider):
                    {
                        npc.value = 3380;
                        break;
                    }

                case (NPCID.Scutlix):
                    {
                        npc.value = 9520;
                        break;
                    }

                case (NPCID.MartianWalker):
                    {
                        npc.value = 14450;
                        break;
                    }

                case int martiansaucer when ((martiansaucer >= NPCID.MartianSaucer && martiansaucer <= NPCID.MartianSaucerCore)):
                    {
                        npc.value = 14090;
                        if (npc.type == NPCID.MartianSaucerCore)
                        {
                            npc.value = 54630;
                        }
                        break;
                    }

                #endregion

                #region Lunar Event

                case int pillars when (pillars == NPCID.LunarTowerVortex || pillars == NPCID.LunarTowerStardust || pillars == NPCID.LunarTowerSolar || pillars == NPCID.LunarTowerNebula):
                    {
                        npc.value = 66660;
                        break;
                    }

                case (NPCID.SolarGoop):
                    {
                        npc.value = 190;
                        break;
                    }

                #region Stardust

                case int milkywayweaver when ((milkywayweaver >= NPCID.StardustWormHead && milkywayweaver <= NPCID.StardustWormTail)):
                    {
                        npc.value = 7320;
                        break;
                    }

                case int stardustcell when (stardustcell == NPCID.StardustCellBig || stardustcell == NPCID.StardustCellSmall):
                    {
                        npc.value = 4390;
                        break;
                    }

                case int flowinvader when (flowinvader == NPCID.StardustJellyfishBig || flowinvader == NPCID.StardustJellyfishSmall):
                    {
                        npc.value = 9850;
                        break;
                    }

                case (NPCID.StardustSpiderBig):
                    {
                        npc.value = 5670;
                        break;
                    }

                case (NPCID.StardustSpiderSmall):
                    {
                        npc.value = 3290;
                        break;
                    }

                case (NPCID.StardustSoldier):
                    {
                        npc.value = 8940;
                        break;
                    }

                #endregion

                #region Vortex

                case int vortexsoldier when (vortexsoldier == NPCID.VortexSoldier || vortexsoldier == NPCID.VortexRifleman):
                    {
                        npc.value = 5630;
                        break;
                    }

                case int vortexhornet when ((vortexhornet >= NPCID.VortexHornetQueen && vortexhornet <= NPCID.VortexLarva)):
                    {
                        if (npc.type == NPCID.VortexLarva)
                        {
                            npc.value = 7210;
                        }
                        if (npc.type == NPCID.VortexHornet)
                        {
                            npc.value = 9080;
                        }
                        npc.value = 12310;
                        break;
                    }

                #endregion

                #region Nebula

                case (NPCID.NebulaBrain):
                    {
                        npc.value = 10430;
                        break;
                    }

                case (NPCID.NebulaHeadcrab):
                    {
                        npc.value = 5230;

                        break;
                    }

                case (NPCID.NebulaBeast):
                    {
                        npc.value = 8790;

                        break;
                    }

                case (NPCID.NebulaSoldier):
                    {
                        npc.value = 9540;

                        break;
                    }

                #endregion

                #region Solar

                case (NPCID.SolarSpearman):
                    {
                        npc.value = 9780;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.lavaImmune = true;
                        break;
                    }

                case int crawltipede when (crawltipede >= NPCID.SolarCrawltipedeHead && crawltipede <= NPCID.SolarCrawltipedeTail):
                    {
                        npc.value = 13490;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.lavaImmune = true;
                        break;
                    }

                case int drakomire when (drakomire == NPCID.SolarDrakomire || drakomire == NPCID.SolarDrakomireRider):
                    {
                        npc.value = 4430;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.lavaImmune = true;
                        break;
                    }

                case (NPCID.SolarSroller):
                    {
                        npc.value = 10390;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.lavaImmune = true;
                        break;
                    }

                case (NPCID.SolarCorite):
                    {
                        npc.value = 7450;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.lavaImmune = true;
                        break;
                    }

                case (NPCID.SolarSolenian):
                    {
                        npc.value = 8280;
                        npc.buffImmune[BuffID.OnFire] = true;
                        npc.lavaImmune = true;
                        break;
                    }
                    #endregion

                #endregion


                #endregion
            }
        }





                    #endregion

        public override void ScaleExpertStats(NPC npc, int numPlayers, float bossLifeScale)
        {
            //Only mess with it if it's one of our bosses
            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp") && npc.boss)
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
        public override void AI(NPC npc)
        {
            

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {

                //Block vanilla enemies in certain regions/conditions
                if (Main.hardMode)
                {                    
                    if (npc.type == NPCID.AngryBones //All these pre-hardmode dungeon spawns take up a million spawn slots and die in one hit
                        || npc.type == NPCID.DarkCaster 
                        || npc.type == NPCID.AngryBonesBig
                        || npc.type == NPCID.AngryBonesBigHelmet 
                        || npc.type == NPCID.AngryBonesBigMuscle
                        || npc.type == NPCID.BlazingWheel 
                        || npc.type == NPCID.DesertDjinn //Literally make the lava/spike section near the entrance to the Hunter's dungeon in the underground desert impossible
                        )
                    {
                        npc.active = false;
                    }
                }
                
                if (npc.type == NPCID.BigRainZombie
                        || npc.type == NPCID.SmallRainZombie
                        || npc.type == NPCID.ZombieRaincoat
                        || npc.type == NPCID.UmbrellaSlime
                        || npc.type == NPCID.BigHeadacheSkeleton
                        || npc.type == NPCID.SmallHeadacheSkeleton
                        || npc.type == NPCID.BigSlimedZombie
                        || npc.type == NPCID.RedSlime
                        || npc.type == NPCID.BlueSlime
                        || npc.type == NPCID.GreenSlime
                        || npc.type == NPCID.TheGroom
                        || npc.type == NPCID.SantaClaus
                        || npc.type == NPCID.Unicorn
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
                        || npc.type == NPCID.ArmedZombieEskimo
                        || npc.type == NPCID.ArmedZombieSlimed
                        || npc.type == NPCID.BoneThrowingSkeleton2
                        || npc.type == NPCID.BoneThrowingSkeleton3
                        || npc.type == NPCID.Butcher
                        || npc.type == NPCID.TheBride
                        || npc.type == NPCID.MartianProbe
                        || npc.type == NPCID.WindyBalloon
                        || npc.type == NPCID.ToxicSludge
                        || npc.type == NPCID.BloodCrawlerWall
                        || npc.type == NPCID.BoundGoblin
                        || npc.type == NPCID.BoundMechanic
                        || npc.type == NPCID.BoundWizard)
                {
                    npc.active = false;
                }
            }

            if ((npc.friendly) && (npc.townNPC == true))
            { //town NPCs are immortal (why was i using a hp check?)
                npc.dontTakeDamage = true;
                npc.dontTakeDamageFromHostiles = true;
                npc.life = npc.lifeMax;
            }

        }

        public override bool PreAI(NPC npc)
        {
            if (npc.HasBuff(BuffID.Frozen))
            {
                if (!npc.boss)
                {
                    npc.velocity = Vector2.Zero;
                    //Dust.NewDust(npc.position, npc.width, npc.height, DustID.IceGolem, Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1), Scale: 2);
                    Vector2 dustPos = npc.position;
                    dustPos.X += Main.rand.NextFloat(0, npc.width);
                    dustPos.Y += Main.rand.NextFloat(0, npc.height);
                    Vector2 dustVel = Main.rand.NextVector2Square(-0.4f, 0.4f);
                    Dust.NewDustPerfect(dustPos, DustID.MagicMirror, dustVel, Scale: 2);

                    return true;
                }
            }

            //If they have the slowed debuff, cap their velocity
            if (npc.HasBuff(BuffID.Slow))
            {
                if (!npc.boss)
                {
                    //Dust.NewDust(npc.position, npc.width, npc.height, DustID.IceGolem, Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1), Scale: 2);
                    if (Main.rand.NextBool(5))
                    {
                        Vector2 dustPos = npc.position;
                        dustPos.X += Main.rand.NextFloat(0, npc.width);
                        dustPos.Y += Main.rand.NextFloat(0, npc.height);
                        Vector2 dustVel = Main.rand.NextVector2Square(-0.4f, 0.4f);
                        Dust.NewDustPerfect(dustPos, DustID.MagicMirror, dustVel, Scale: 1);
                    }

                    npc.velocity *= 0.92f;
                }
            }
            if (npc.type == NPCID.EyeofCthulhu)
            {
                AI_EoC(npc);
                return false;
            }


            if (npc.type == NPCID.WallofFlesh)
            {
                #region WoF AI


                if (npc.position.X < 160f || npc.position.X > (float)((Main.maxTilesX - 10) * 16))
                {
                    npc.active = false;
                }
                if (npc.localAI[0] == 0f)
                {
                    npc.localAI[0] = 1f;
                    Main.wofDrawAreaBottom = -1;
                    Main.wofDrawAreaTop = -1;
                }
                npc.ai[1] += 1f;
                if (npc.ai[2] == 0f)
                {
                    if ((double)npc.life < (double)npc.lifeMax * 0.5)
                    {
                        npc.ai[1] += 1f;
                    }
                    if ((double)npc.life < (double)npc.lifeMax * 0.2)
                    {
                        npc.ai[1] += 1f;
                    }
                    if (npc.ai[1] > 2700f)
                    {
                        npc.ai[2] = 1f;
                    }
                }
                if (npc.ai[2] > 0f && npc.ai[1] > 60f)
                {
                    int num333 = 3;
                    if ((double)npc.life < (double)npc.lifeMax * 0.3)
                    {
                        num333++;
                    }
                    npc.ai[2] += 1f;
                    npc.ai[1] = 0f;
                    if (npc.ai[2] > (float)num333)
                    {
                        npc.ai[2] = 0f;
                    }
                    if (Main.netMode != 1)
                    {
                        int num334 = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.position.X + (float)(npc.width / 2)), (int)(npc.position.Y + (float)(npc.height / 2) + 20f), 117, 1);
                        Main.npc[num334].velocity.X = npc.direction * 8;
                    }
                }
                npc.localAI[3] += 1f;
                if (npc.localAI[3] >= (float)(600 + Main.rand.Next(1000)))
                {
                    npc.localAI[3] = -Main.rand.Next(200);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                }
                Main.wofNPCIndex = npc.whoAmI;
                int num335 = (int)(npc.position.X / 16f);
                int num336 = (int)((npc.position.X + (float)npc.width) / 16f);
                int num337 = (int)((npc.position.Y + (float)(npc.height / 2)) / 16f);
                int num338 = 0;
                int num339 = num337 + 7;
                while (num338 < 15 && num339 > Main.maxTilesY - 200)
                {
                    num339++;
                    for (int num340 = num335; num340 <= num336; num340++)
                    {
                        try
                        {
                            if (WorldGen.SolidTile(num340, num339) || Main.tile[num340, num339].LiquidAmount > 0)
                            {
                                num338++;
                            }
                        }
                        catch
                        {
                            num338 += 15;
                        }
                    }
                }
                num339 += 4;
                if (Main.wofDrawAreaBottom == -1)
                {
                    Main.wofDrawAreaBottom = num339 * 16;
                }
                else if (Main.wofDrawAreaBottom > num339 * 16)
                {
                    Main.wofDrawAreaBottom--;
                    if (Main.wofDrawAreaBottom < num339 * 16)
                    {
                        Main.wofDrawAreaBottom = num339 * 16;
                    }
                }
                else if (Main.wofDrawAreaBottom < num339 * 16)
                {
                    Main.wofDrawAreaBottom++;
                    if (Main.wofDrawAreaBottom > num339 * 16)
                    {
                        Main.wofDrawAreaBottom = num339 * 16;
                    }
                }
                num338 = 0;
                num339 = num337 - 7;
                while (num338 < 15 && num339 < Main.maxTilesY - 10)
                {
                    num339--;
                    for (int num341 = num335; num341 <= num336; num341++)
                    {
                        try
                        {
                            if (WorldGen.SolidTile(num341, num339) || Main.tile[num341, num339].LiquidAmount > 0)
                            {
                                num338++;
                            }
                        }
                        catch
                        {
                            num338 += 15;
                        }
                    }
                }
                num339 -= 4;
                if (Main.wofDrawAreaTop == -1)
                {
                    Main.wofDrawAreaTop = num339 * 16;
                }
                else if (Main.wofDrawAreaTop > num339 * 16)
                {
                    Main.wofDrawAreaTop--;
                    if (Main.wofDrawAreaTop < num339 * 16)
                    {
                        Main.wofDrawAreaTop = num339 * 16;
                    }
                }
                else if (Main.wofDrawAreaTop < num339 * 16)
                {
                    Main.wofDrawAreaTop++;
                    if (Main.wofDrawAreaTop > num339 * 16)
                    {
                        Main.wofDrawAreaTop = num339 * 16;
                    }
                }
                float num342 = (Main.wofDrawAreaBottom + Main.wofDrawAreaTop) / 2 - npc.height / 2;
                if (npc.position.Y > num342 + 1f)
                {
                    npc.velocity.Y = -1f;
                }
                else if (npc.position.Y < num342 - 1f)
                {
                    npc.velocity.Y = 1f;
                }
                npc.velocity.Y = 0f;
                int num343 = (Main.maxTilesY - 180) * 16;
                if (num342 < (float)num343)
                {
                    num342 = num343;
                }
                npc.position.Y = num342;
                float num344 = 1.5f;
                if ((double)npc.life < (double)npc.lifeMax * 0.75)
                {
                    num344 += 0.25f;
                }
                if ((double)npc.life < (double)npc.lifeMax * 0.5)
                {
                    num344 += 0.4f;
                }
                if ((double)npc.life < (double)npc.lifeMax * 0.25)
                {
                    num344 += 0.5f;
                }
                if ((double)npc.life < (double)npc.lifeMax * 0.1)
                {
                    num344 += 0.6f;
                }
                /*if ((double)npc.life < (double)npc.lifeMax * 0.66 && Main.expertMode)
                {
                    num344 += 0.3f;
                }
                if ((double)npc.life < (double)npc.lifeMax * 0.33 && Main.expertMode)
                {
                    num344 += 0.3f;
                }
                if ((double)npc.life < (double)npc.lifeMax * 0.05 && Main.expertMode)
                {
                    num344 += 0.3f;
                }
                if ((double)npc.life < (double)npc.lifeMax * 0.035 && Main.expertMode)
                {
                    num344 += 0.3f;
                }
                if ((double)npc.life < (double)npc.lifeMax * 0.025 && Main.expertMode)
                {
                    num344 += 0.3f;
                }*/
                if (Main.expertMode)
                {
                    //num344 *= 1.35f;
                    num344 += 0.35f;
                }
                if (npc.velocity.X == 0f)
                {
                    npc.TargetClosest();
                    npc.velocity.X = npc.direction;
                }
                if (npc.velocity.X < 0f)
                {
                    npc.velocity.X = 0f - num344;
                    npc.direction = -1;
                }
                else
                {
                    npc.velocity.X = num344;
                    npc.direction = 1;
                }
                npc.spriteDirection = npc.direction;
                Vector2 vector37 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                float num345 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - vector37.X;
                float num346 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - vector37.Y;
                float num347 = (float)Math.Sqrt(num345 * num345 + num346 * num346);
                float num348 = num347;
                num345 *= num347;
                num346 *= num347;
                if (npc.direction > 0)
                {
                    if (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) > npc.position.X + (float)(npc.width / 2))
                    {
                        npc.rotation = (float)Math.Atan2(0f - num346, 0f - num345) + 3.14f;
                    }
                    else
                    {
                        npc.rotation = 0f;
                    }
                }
                else if (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) < npc.position.X + (float)(npc.width / 2))
                {
                    npc.rotation = (float)Math.Atan2(num346, num345) + 3.14f;
                }
                else
                {
                    npc.rotation = 0f;
                }
                if (Main.expertMode && Main.netMode != 1)
                {
                    int num349 = (int)(1f + (float)npc.life / (float)npc.lifeMax * 10f);
                    num349 *= num349;
                    if (num349 < 400)
                    {
                        num349 = (num349 * 19 + 400) / 20;
                    }
                    if (num349 < 60)
                    {
                        num349 = (num349 * 3 + 60) / 4;
                    }
                    if (num349 < 20)
                    {
                        num349 = (num349 + 20) / 2;
                    }
                    num349 = (int)((double)num349 * 0.7);
                    if (Main.rand.Next(num349) == 0)
                    {
                        int num350 = 0;
                        float[] array = new float[10];
                        for (int num351 = 0; num351 < 200; num351++)
                        {
                            if (num350 < 10 && Main.npc[num351].active && Main.npc[num351].type == 115)
                            {
                                array[num350] = Main.npc[num351].ai[0];
                                num350++;
                            }
                        }
                        int maxValue = 1 + num350 * 2;
                        if (num350 < 10 && Main.rand.Next(maxValue) <= 1)
                        {
                            int num352 = -1;
                            for (int num353 = 0; num353 < 1000; num353++)
                            {
                                int num354 = Main.rand.Next(10);
                                float num355 = (float)num354 * 0.1f - 0.05f;
                                bool flag27 = true;
                                for (int num356 = 0; num356 < num350; num356++)
                                {
                                    if (num355 == array[num356])
                                    {
                                        flag27 = false;
                                        break;
                                    }
                                }
                                if (flag27)
                                {
                                    num352 = num354;
                                    break;
                                }
                            }
                            if (num352 >= 0)
                            {
                                int num357 = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X, (int)num342, 115, npc.whoAmI);
                                Main.npc[num357].ai[0] = (float)num352 * 0.1f - 0.05f;
                            }
                        }
                    }
                }
                if (npc.localAI[0] == 1f && Main.netMode != 1)
                {
                    npc.localAI[0] = 2f;
                    num342 = (Main.wofDrawAreaBottom + Main.wofDrawAreaTop) / 2;
                    num342 = (num342 + (float)Main.wofDrawAreaTop) / 2f;
                    int num358 = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X, (int)num342, 114, npc.whoAmI);
                    Main.npc[num358].ai[0] = 1f;
                    num342 = (Main.wofDrawAreaBottom + Main.wofDrawAreaTop) / 2;
                    num342 = (num342 + (float)Main.wofDrawAreaBottom) / 2f;
                    num358 = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X, (int)num342, 114, npc.whoAmI);
                    Main.npc[num358].ai[0] = -1f;
                    num342 = (Main.wofDrawAreaBottom + Main.wofDrawAreaTop) / 2;
                    num342 = (num342 + (float)Main.wofDrawAreaBottom) / 2f;
                    for (int num359 = 0; num359 < 11; num359++)
                    {
                        num358 = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.position.X, (int)num342, 115, npc.whoAmI);
                        Main.npc[num358].ai[0] = (float)num359 * 0.1f - 0.05f;
                    }
                }


                #endregion
                return false;
            }

            if (npc.type == NPCID.WallofFleshEye)
            {
                #region WoF Eye AI


                if (Main.wofNPCIndex < 0)
                {
                    npc.active = false;
                    return false;
                }
                npc.realLife = Main.wofNPCIndex;
                if (Main.npc[Main.wofNPCIndex].life > 0)
                {
                    npc.life = Main.npc[Main.wofNPCIndex].life;
                }
                npc.TargetClosest();
                npc.position.X = Main.npc[Main.wofNPCIndex].position.X;
                npc.direction = Main.npc[Main.wofNPCIndex].direction;
                npc.spriteDirection = npc.direction;
                float num360 = (Main.wofDrawAreaBottom + Main.wofDrawAreaTop) / 2;
                num360 = ((!(npc.ai[0] > 0f)) ? ((num360 + (float)Main.wofDrawAreaBottom) / 2f) : ((num360 + (float)Main.wofDrawAreaTop) / 2f));
                num360 -= (float)(npc.height / 2);
                if (npc.position.Y > num360 + 1f)
                {
                    npc.velocity.Y = -1f;
                }
                else if (npc.position.Y < num360 - 1f)
                {
                    npc.velocity.Y = 1f;
                }
                else
                {
                    npc.velocity.Y = 0f;
                    npc.position.Y = num360;
                }
                if (npc.velocity.Y > 5f)
                {
                    npc.velocity.Y = 5f;
                }
                if (npc.velocity.Y < -5f)
                {
                    npc.velocity.Y = -5f;
                }
                Vector2 vector38 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                float num361 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - vector38.X;
                float num362 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - vector38.Y;
                float num363 = (float)Math.Sqrt(num361 * num361 + num362 * num362);
                float num364 = num363;
                num361 *= num363;
                num362 *= num363;
                bool flag28 = true;
                if (npc.direction > 0)
                {
                    if (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) > npc.position.X + (float)(npc.width / 2))
                    {
                        npc.rotation = (float)Math.Atan2(0f - num362, 0f - num361) + 3.14f;
                    }
                    else
                    {
                        npc.rotation = 0f;
                        flag28 = false;
                    }
                }
                else if (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) < npc.position.X + (float)(npc.width / 2))
                {
                    npc.rotation = (float)Math.Atan2(num362, num361) + 3.14f;
                }
                else
                {
                    npc.rotation = 0f;
                    flag28 = false;
                }
                if (Main.netMode == 1)
                {
                    return false;
                }
                int num365 = 4;
                npc.localAI[1] += 1f;
                if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.75)
                {
                    npc.localAI[1] += 1f;
                    num365++;
                }
                if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.5)
                {
                    npc.localAI[1] += 1f;
                    num365++;
                }
                if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.25)
                {
                    npc.localAI[1] += 1f;
                    num365 += 2;
                }
                if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.1)
                {
                    npc.localAI[1] += 2f;
                    num365 += 3;
                }
                if (Main.expertMode)
                {
                    npc.localAI[1] += 0.5f;
                    num365++;
                    if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.1)
                    {
                        npc.localAI[1] += 2f;
                        num365 += 3;
                    }
                }
                if (npc.localAI[2] == 0f)
                {
                    if (npc.localAI[1] > 600f)
                    {
                        npc.localAI[2] = 1f;
                        npc.localAI[1] = 0f;
                    }
                }
                else
                {
                    if (!(npc.localAI[1] > 45f) || !Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                    {
                        return false;
                    }
                    npc.localAI[1] = 0f;
                    npc.localAI[2] += 1f;
                    if (npc.localAI[2] >= (float)num365)
                    {
                        npc.localAI[2] = 0f;
                    }
                    if (flag28)
                    {
                        float num366 = 9f;
                        int num367 = 11;
                        int num368 = 83;
                        if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.5)
                        {
                            num367++;
                            num366 += 1f;
                        }
                        if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.25)
                        {
                            num367++;
                            num366 += 1f;
                        }
                        if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.1)
                        {
                            num367 += 2;
                            num366 += 2f;
                        }
                        vector38 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                        num361 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector38.X;
                        num362 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector38.Y;
                        num363 = (float)Math.Sqrt(num361 * num361 + num362 * num362);
                        num363 = num366 / num363;
                        num361 *= num363;
                        num362 *= num363;
                        vector38.X += num361;
                        vector38.Y += num362;
                        int num369 = Projectile.NewProjectile(npc.GetSource_FromThis(), vector38.X, vector38.Y, num361, num362, num368, num367, 0f, Main.myPlayer, 1);
                    }
                }


                npc.ai[3]++;

                if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.75)
                {
                    npc.ai[3] += 0.5f;
                }
                if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.5)
                {
                    npc.ai[3] += 0.5f;
                }
                if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.25)
                {
                    npc.ai[3] += 0.5f;

                }
                if ((double)Main.npc[Main.wofNPCIndex].life < (double)Main.npc[Main.wofNPCIndex].lifeMax * 0.1)
                {
                    npc.ai[3] += 0.5f;
                }

                if (Main.player[npc.target].Distance(npc.Center) < 1200f && npc.ai[3] >= 360 /*&& Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height)*/)
                {
                    float num220 = 0.2f;
                    Vector2 vector28 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                    float num221 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector28.X + (float)Main.rand.Next(-100, 101);
                    float num222 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector28.Y + (float)Main.rand.Next(-100, 101);
                    float num223 = (float)Math.Sqrt(num221 * num221 + num222 * num222);
                    num223 = num220 / num223;
                    num221 *= num223;
                    num222 *= num223;
                    int num224 = 18;
                    int num225 = 44;
                    int num226 = Projectile.NewProjectile(npc.GetSource_FromThis(), vector28.X, vector28.Y, num221, num222, num225, num224, 0f, Main.myPlayer);
                    Main.projectile[num226].timeLeft = 420;
                    npc.ai[3] = 0f;

                }

                if (Main.player[npc.target].Distance(npc.Center) >= 1100f && npc.ai[3] >= 90 /*&& Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height)*/)
                {
                    float num220 = 0.2f;
                    Vector2 vector28 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                    float num221 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector28.X + (float)Main.rand.Next(-100, 101);
                    float num222 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector28.Y + (float)Main.rand.Next(-100, 101);
                    float num223 = (float)Math.Sqrt(num221 * num221 + num222 * num222);
                    num223 = num220 / num223;
                    num221 *= num223;
                    num222 *= num223;
                    int num224 = 21;
                    int num225 = 44;
                    int num226 = Projectile.NewProjectile(npc.GetSource_FromThis(), vector28.X, vector28.Y, num221, num222, num225, num224, 0f, Main.myPlayer);
                    Main.projectile[num226].timeLeft = 420;
                    npc.ai[3] = 0f;

                }
                #endregion
                return false;
            }

            #region Lunar Towers

            if (npc.type == NPCID.LunarTowerNebula || npc.type == NPCID.LunarTowerSolar || npc.type == NPCID.LunarTowerStardust || npc.type == NPCID.LunarTowerVortex)
            {
                if (npc.ai[2] == 1f)
                {
                    npc.velocity = Vector2.UnitY * npc.velocity.Length();
                    if (npc.velocity.Y < 0.25f)
                    {
                        npc.velocity.Y += 0.02f;
                    }
                    if (npc.velocity.Y > 0.25f)
                    {
                        npc.velocity.Y -= 0.02f;
                    }
                    npc.dontTakeDamage = true;
                    npc.ai[1]++;
                    if (npc.ai[1] > 120f)
                    {
                        npc.Opacity = 1f - (npc.ai[1] - 120f) / 60f;
                    }
                    int num474 = 6;
                    switch (npc.type)
                    {
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
                    if (Main.rand.Next(5) == 0 && npc.ai[1] < 120f)
                    {
                        for (int num475 = 0; num475 < 3; num475++)
                        {
                            Dust dust76 = Main.dust[Dust.NewDust(npc.Left, npc.width, npc.height / 2, num474)];
                            dust76.position = npc.Center + Vector2.UnitY.RotatedByRandom(4.1887903213500977) * new Vector2((float)npc.width * 1.5f, (float)npc.height * 1.1f) * 0.8f * (0.8f + Main.rand.NextFloat() * 0.2f);
                            dust76.velocity.X = 0f;
                            dust76.velocity.Y = (0f - Math.Abs(dust76.velocity.Y - (float)num475 + npc.velocity.Y - 4f)) * 3f;
                            dust76.noGravity = true;
                            dust76.fadeIn = 1f;
                            dust76.scale = 1f + Main.rand.NextFloat() + (float)num475 * 0.3f;
                        }
                    }
                    if (npc.ai[1] < 150f)
                    {
                        for (int num476 = 0; num476 < 3; num476++)
                        {
                            if (Main.rand.Next(4) == 0)
                            {
                                Dust dust77 = Main.dust[Dust.NewDust(npc.Top + new Vector2((float)(-npc.width) * (0.33f - 0.11f * (float)num476), -20f), (int)((float)npc.width * (0.66f - 0.22f * (float)num476)), 20, num474)];
                                dust77.velocity.X = 0f;
                                dust77.velocity.Y = (0f - Math.Abs(dust77.velocity.Y - (float)num476 + npc.velocity.Y - 4f)) * (1f + npc.ai[1] / 180f * 0.5f);
                                dust77.noGravity = true;
                                dust77.fadeIn = 1f;
                                dust77.scale = 1f + Main.rand.NextFloat() + (float)num476 * 0.3f;
                            }
                        }
                    }
                    if (Main.rand.Next(5) == 0 && npc.ai[1] < 150f)
                    {
                        for (int num477 = 0; num477 < 3; num477++)
                        {
                            Vector2 position6 = npc.Center + Vector2.UnitY.RotatedByRandom(4.1887903213500977) * new Vector2(npc.width, npc.height) * 0.7f * Main.rand.NextFloat();
                            float num478 = 1f + Main.rand.NextFloat() * 2f + npc.ai[1] / 180f * 4f;
                            for (int num479 = 0; num479 < 6; num479++)
                            {
                                Dust dust78 = Main.dust[Dust.NewDust(position6, 4, 4, num474)];
                                dust78.position = position6;
                                dust78.velocity.X *= num478;
                                dust78.velocity.Y = (0f - Math.Abs(dust78.velocity.Y)) * num478;
                                dust78.noGravity = true;
                                dust78.fadeIn = 1f;
                                dust78.scale = 1.5f + Main.rand.NextFloat() + (float)num479 * 0.13f;
                            }

                            Terraria.Audio.SoundStyle randomStyle = SoundID.NPCHit1;
                            
                            if (Main.rand.NextBool())
                            {
                                randomStyle = SoundID.NPCHit18;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(randomStyle, position6);
                        }
                    }
                    if (Main.rand.Next(3) != 0 && npc.ai[1] < 150f)
                    {
                        Dust dust79 = Main.dust[Dust.NewDust(npc.Left, npc.width, npc.height / 2, 241)];
                        dust79.position = npc.Center + Vector2.UnitY.RotatedByRandom(4.1887903213500977) * new Vector2(npc.width / 2, npc.height / 2) * (0.8f + Main.rand.NextFloat() * 0.2f);
                        dust79.velocity.X = 0f;
                        dust79.velocity.Y = Math.Abs(dust79.velocity.Y) * 0.25f;
                    }
                    if (npc.ai[1] % 60f == 1f)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath22, npc.Center);
                    }
                    if (npc.ai[1] >= 180f)
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 1337.0);
                        npc.checkDead();
                    }
                    return false;
                }
                if (npc.ai[3] > 0f)
                {
                    bool flag98 = npc.dontTakeDamage;
                    switch (npc.type)
                    {
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
                    if (flag98 != npc.dontTakeDamage)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath58, npc.position);
                    }
                    else if (npc.ai[3] == 1f)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath3, npc.position);
                    }
                    npc.ai[3]++;
                    if (npc.ai[3] > 120f)
                    {
                        npc.ai[3] = 0f;
                    }
                }
                switch (npc.type)
                {
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
                if (Main.player[npc.target].Distance(npc.Center) > 2000f)
                {
                    npc.localAI[0]++;
                }
                if (npc.localAI[0] >= 60f && Main.netMode != 1)
                {
                    npc.localAI[0] = 0f;
                    npc.netUpdate = true;
                    npc.life = (int)MathHelper.Clamp(npc.life + 200, 0f, npc.lifeMax);
                }
                else
                {
                    npc.localAI[0] = 0f;
                }
                npc.velocity = new Vector2(0f, (float)Math.Sin((float)Math.PI * 2f * npc.ai[0] / 300f) * 0.5f);
                npc.ai[0]++;
                if (npc.ai[0] >= 300f)
                {
                    npc.ai[0] = 0f;
                    npc.netUpdate = true;
                }
                if (npc.type == 493)
                {
                    if (Main.rand.Next(5) == 0)
                    {
                        Dust dust80 = Main.dust[Dust.NewDust(npc.Left, npc.width, npc.height / 2, 241)];
                        dust80.position = npc.Center + Vector2.UnitY.RotatedByRandom(2.0943951606750488) * new Vector2(npc.width / 2, npc.height / 2) * (0.8f + Main.rand.NextFloat() * 0.2f);
                        dust80.velocity.X = 0f;
                        dust80.velocity.Y = Math.Abs(dust80.velocity.Y) * 0.25f;
                    }
                    for (int num481 = 0; num481 < 3; num481++)
                    {
                        if (Main.rand.Next(5) == 0)
                        {
                            Dust dust58 = Main.dust[Dust.NewDust(npc.Top + new Vector2((float)(-npc.width) * (0.33f - 0.11f * (float)num481), -20f), (int)((float)npc.width * (0.66f - 0.22f * (float)num481)), 20, 135)];
                            dust58.velocity.X = 0f;
                            dust58.velocity.Y = (0f - Math.Abs(dust58.velocity.Y - (float)num481 + npc.velocity.Y - 4f)) * 1f;
                            dust58.noGravity = true;
                            dust58.fadeIn = 1f;
                            dust58.scale = 1f + Main.rand.NextFloat() + (float)num481 * 0.3f;
                        }
                    }
                    if (npc.ai[1] > 0f)
                    {
                        npc.ai[1]--;
                    }
                    if (Main.netMode != 1 && npc.ai[1] <= 0f && Main.player[npc.target].active && !Main.player[npc.target].dead && npc.Distance(Main.player[npc.target].Center) < 1080f && Main.player[npc.target].position.Y - npc.position.Y < 400f)
                    {
                        List<int> list = new List<int>();
                        if (NPC.CountNPCS(405) + NPC.CountNPCS(406) < 2)
                        {
                            list.Add(405);
                        }
                        if (NPC.CountNPCS(402) < 2)
                        {
                            list.Add(402);
                        }
                        if (NPC.CountNPCS(407) < 1)
                        {
                            list.Add(407);
                        }
                        if (list.Count > 0)
                        {
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
                            while (list2.Count > 0)
                            {
                                Vector2 item = list2[0].Item1;
                                int num488 = 1;
                                int num489 = 1;
                                if (num486 > 0 && num484 > 0 && (Main.rand.Next(3) != 0 || num486 == 1))
                                {
                                    num489 = Main.rand.Next(Math.Max(1, list2[0].Item2));
                                    num488++;
                                    num484--;
                                }
                                for (int num490 = 0; num490 < num488; num490++)
                                {
                                    int num492 = list2[0].Item3;
                                    if (num486 == 0)
                                    {
                                        num492 = Utils.SelectRandom<int>(Main.rand, -1, 1);
                                    }
                                    else if (num490 == 1)
                                    {
                                        num492 *= -1;
                                    }
                                    float num493 = ((num486 % 2 == 0) ? 0f : ((float)Math.PI)) + (0.5f - Main.rand.NextFloat()) * ((float)Math.PI / 4f) + (float)num492 * ((float)Math.PI / 4f) * (float)(num486 % 2 == 0).ToDirectionInt();
                                    float scaleFactor9 = 100f + 50f * Main.rand.NextFloat();
                                    int num494 = list2[0].Item2;
                                    if (num490 != 0)
                                    {
                                        num494 = num489;
                                    }
                                    if (num486 == 0)
                                    {
                                        num493 = (0.5f - Main.rand.NextFloat()) * ((float)Math.PI / 4f);
                                        scaleFactor9 = 100f + 100f * Main.rand.NextFloat();
                                    }
                                    Vector2 value52 = (-Vector2.UnitY).RotatedBy(num493) * scaleFactor9;
                                    if (num494 - 1 < 0)
                                    {
                                        value52 = Vector2.Zero;
                                    }
                                    num485 = Projectile.NewProjectile(npc.GetSource_FromThis(), item.X, item.Y, value52.X, value52.Y, 540, 0, 0f, Main.myPlayer, (float)(-num486) * 10f, 0.5f + Main.rand.NextFloat() * 0.5f);
                                    list3.Add(item + value52);
                                    if (num486 < num483 && list2[0].Item2 > 0)
                                    {
                                        list2.Add(Tuple.Create(item + value52, num494 - 1, num492));
                                    }
                                }
                                list2.Remove(list2[0]);
                                int num495 = num487 - 1;
                                num487 = num495;
                                if (num495 == 0)
                                {
                                    num487 = list2.Count;
                                    num486++;
                                }
                            }
                            Main.projectile[num485].localAI[0] = num482;
                        }
                        else
                        {
                            npc.ai[1] = 30f;
                        }
                    }
                }
                if (npc.type == 507)
                {
                    if (Main.rand.Next(5) == 0)
                    {
                        Dust dust59 = Main.dust[Dust.NewDust(npc.Left, npc.width, npc.height / 2, 241)];
                        dust59.position = npc.Center + Vector2.UnitY.RotatedByRandom(2.0943951606750488) * new Vector2(npc.width / 2, npc.height / 2) * (0.8f + Main.rand.NextFloat() * 0.2f);
                        dust59.velocity.X = 0f;
                        dust59.velocity.Y = Math.Abs(dust59.velocity.Y) * 0.25f;
                    }
                    for (int num496 = 0; num496 < 3; num496++)
                    {
                        if (Main.rand.Next(5) == 0)
                        {
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
                if (npc.type == 422)
                {
                    if (Main.rand.Next(5) == 0)
                    {
                        Dust dust61 = Main.dust[Dust.NewDust(npc.Left, npc.width, npc.height / 2, 241)];
                        dust61.position = npc.Center + Vector2.UnitY.RotatedByRandom(2.0943951606750488) * new Vector2(npc.width / 2, npc.height / 2) * (0.8f + Main.rand.NextFloat() * 0.2f);
                        dust61.velocity.X = 0f;
                        dust61.velocity.Y = Math.Abs(dust61.velocity.Y) * 0.25f;
                    }
                    for (int num497 = 0; num497 < 3; num497++)
                    {
                        if (Main.rand.Next(5) == 0)
                        {
                            Dust dust62 = Main.dust[Dust.NewDust(npc.Top + new Vector2((float)(-npc.width) * (0.33f - 0.11f * (float)num497), -20f), (int)((float)npc.width * (0.66f - 0.22f * (float)num497)), 20, 229)];
                            dust62.velocity.X = 0f;
                            dust62.velocity.Y = (0f - Math.Abs(dust62.velocity.Y - (float)num497 + npc.velocity.Y - 4f)) * 1f;
                            dust62.noGravity = true;
                            dust62.fadeIn = 1f;
                            dust62.color = Color.Black;
                            dust62.scale = 1f + Main.rand.NextFloat() + (float)num497 * 0.3f;
                        }
                    }
                    if (npc.ai[1] > 0f)
                    {
                        npc.ai[1]--;
                    }
                    if (Main.netMode != 1 && npc.ai[1] <= 0f && Main.player[npc.target].active && !Main.player[npc.target].dead && npc.Distance(Main.player[npc.target].Center) < 3240f && !Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                    {
                        npc.ai[1] = 60 + Main.rand.Next(120);
                        Point point14 = Main.player[npc.target].Top.ToTileCoordinates();
                        bool flag99 = NPC.CountNPCS(427) + NPC.CountNPCS(426) < 14;
                        for (int num498 = 0; num498 < 10; num498++)
                        {
                            if (WorldGen.SolidTile(point14.X, point14.Y))
                            {
                                break;
                            }
                            if (point14.Y <= 10)
                            {
                                break;
                            }
                            point14.Y--;
                        }
                        if (flag99)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), point14.X * 16 + 8, point14.Y * 16 + 24, 0f, 0f, 579, 0, 0f, Main.myPlayer);
                        }
                        else
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), point14.X * 16 + 8, point14.Y * 16 + 17, 0f, 0f, 578, 0, 1f, Main.myPlayer);
                        }
                    }
                    if (Main.netMode != 1 && npc.ai[1] <= 0f && Main.player[npc.target].active && !Main.player[npc.target].dead && npc.Distance(Main.player[npc.target].Center) < 1080f && Main.player[npc.target].position.Y - npc.position.Y < 400f && NPC.CountNPCS(427) + NPC.CountNPCS(426) * 3 + NPC.CountNPCS(428) < 20)
                    {
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
                        if (vector134.Length() > 2000f)
                        {
                            flag100 = true;
                        }
                        while (!flag100 && num504 < 100)
                        {
                            num504++;
                            int num505 = Main.rand.Next(point3.X - num499, point3.X + num499 + 1);
                            int num506 = Main.rand.Next(point3.Y - num499, point3.Y + num499 + 1);
                            if ((num506 < point3.Y - num501 || num506 > point3.Y + num501 || num505 < point3.X - num501 || num505 > point3.X + num501) && (num506 < point2.Y - num500 || num506 > point2.Y + num500 || num505 < point2.X - num500 || num505 > point2.X + num500) && !Main.tile[num505, num506].HasUnactuatedTile)
                            {
                                bool flag102 = true;
                                if (flag102 && Main.tile[num505, num506].LiquidType == LiquidID.Lava)
                                {
                                    flag102 = false;
                                }
                                if (flag102 && Collision.SolidTiles(num505 - num503, num505 + num503, num506 - num503, num506 + num503))
                                {
                                    flag102 = false;
                                }
                                if (flag102 && !Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                                {
                                    flag102 = false;
                                }
                                if (flag102)
                                {
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), num505 * 16 + 8, num506 * 16 + 8, 0f, 0f, 579, 0, 0f, Main.myPlayer);
                                    flag100 = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (npc.type != 517)
                {
                    return false;
                }
                if (Main.rand.Next(5) == 0)
                {
                    Dust dust63 = Main.dust[Dust.NewDust(npc.Left, npc.width, npc.height / 2, 241)];
                    dust63.position = npc.Center + Vector2.UnitY.RotatedByRandom(2.0943951606750488) * new Vector2(npc.width / 2, npc.height / 2) * (0.8f + Main.rand.NextFloat() * 0.2f);
                    dust63.velocity.X = 0f;
                    dust63.velocity.Y = Math.Abs(dust63.velocity.Y) * 0.25f;
                }
                for (int num507 = 0; num507 < 3; num507++)
                {
                    if (Main.rand.Next(5) == 0)
                    {
                        Dust dust64 = Main.dust[Dust.NewDust(npc.Top + new Vector2((float)(-npc.width) * (0.33f - 0.11f * (float)num507), -20f), (int)((float)npc.width * (0.66f - 0.22f * (float)num507)), 20, 6)];
                        dust64.velocity.X = 0f;
                        dust64.velocity.Y = (0f - Math.Abs(dust64.velocity.Y - (float)num507 + npc.velocity.Y - 4f)) * 1f;
                        dust64.noGravity = true;
                        dust64.fadeIn = 1f;
                        dust64.scale = 1f + Main.rand.NextFloat() + (float)num507 * 0.3f;
                    }
                }
                if (npc.ai[1] > 0f)
                {
                    npc.ai[1]--;
                }
                if (Main.netMode != 1 && npc.ai[1] <= 0f && Main.player[npc.target].active && !Main.player[npc.target].dead && npc.Distance(Main.player[npc.target].Center) < 1080f && Main.player[npc.target].position.Y - npc.position.Y < 700f)
                {
                    Vector2 vector136 = npc.Top + new Vector2((float)(-npc.width) * 0.33f, -20f) + new Vector2((float)npc.width * 0.66f, 20f) * Utils.RandomVector2(Main.rand, 0f, 1f);
                    Vector2 velocity8 = -Vector2.UnitY.RotatedByRandom(0.78539818525314331) * (7f + Main.rand.NextFloat() * 5f);
                    int num508 = NPC.NewNPC(npc.GetSource_FromAI(), (int)vector136.X, (int)vector136.Y, 519, npc.whoAmI);
                    Main.npc[num508].velocity = velocity8;
                    Main.npc[num508].netUpdate = true;
                    npc.ai[1] = 60f;
                }
                return false;
            }

            #endregion

            if (npc.type == NPCID.GoblinTinkerer)
            {
                npc.GivenName = "Elijah";
                return true;
            }
            if (npc.type == NPCID.Mechanic)
            {
                npc.GivenName = "Asha";
                return true;
            }
            if (npc.type == NPCID.Wizard)
            {
                npc.GivenName = "Araz";
                return true;
            }

            else return base.PreAI(npc);
        }

        #region Eye of Cthulhu AI

        private void AI_EoC(NPC npc)
        {
            bool flag24 = false;
            if (Main.expertMode && (double)npc.life < (double)npc.lifeMax * 0.12)
            {
                flag24 = true;
            }
            bool finalDesperation = false;
            if (Main.expertMode && (double)npc.life < (double)npc.lifeMax * 0.04)
            {
                finalDesperation = true;
            }
            float num910 = 20f;
            if (finalDesperation)
            {
                num910 = 10f;
            }
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                npc.TargetClosest();
            }
            bool dead = Main.player[npc.target].dead;
            float num1021 = npc.position.X + (float)(npc.width / 2) - Main.player[npc.target].position.X - (float)(Main.player[npc.target].width / 2);
            float num1132 = npc.position.Y + (float)npc.height - 59f - Main.player[npc.target].position.Y - (float)(Main.player[npc.target].height / 2);
            float num1243 = (float)Math.Atan2(num1132, num1021) + 1.57f;
            if (num1243 < 0f)
            {
                num1243 += 6.283f;
            }
            else if ((double)num1243 > 6.283)
            {
                num1243 -= 6.283f;
            }
            float num1354 = 0f;
            if (npc.ai[0] == 0f && npc.ai[1] == 0f)
            {
                num1354 = 0.02f;
            }
            if (npc.ai[0] == 0f && npc.ai[1] == 2f && npc.ai[2] > 40f)
            {
                num1354 = 0.05f;
            }
            if (npc.ai[0] == 3f && npc.ai[1] == 0f)
            {
                num1354 = 0.05f;
            }
            if (npc.ai[0] == 3f && npc.ai[1] == 2f && npc.ai[2] > 40f)
            {
                num1354 = 0.08f;
            }
            if (npc.ai[0] == 3f && npc.ai[1] == 4f && npc.ai[2] > num910)
            {
                num1354 = 0.15f;
            }
            if (npc.ai[0] == 3f && npc.ai[1] == 5f)
            {
                num1354 = 0.05f;
            }
            if (Main.expertMode)
            {
                num1354 *= 1.5f;
            }
            if (finalDesperation && Main.expertMode)
            {
                num1354 = 0f;
            }
            if (npc.rotation < num1243)
            {
                if ((double)(num1243 - npc.rotation) > 3.1415)
                {
                    npc.rotation -= num1354;
                }
                else
                {
                    npc.rotation += num1354;
                }
            }
            else if (npc.rotation > num1243)
            {
                if ((double)(npc.rotation - num1243) > 3.1415)
                {
                    npc.rotation += num1354;
                }
                else
                {
                    npc.rotation -= num1354;
                }
            }
            if (npc.rotation > num1243 - num1354 && npc.rotation < num1243 + num1354)
            {
                npc.rotation = num1243;
            }
            if (npc.rotation < 0f)
            {
                npc.rotation += 6.283f;
            }
            else if ((double)npc.rotation > 6.283)
            {
                npc.rotation -= 6.283f;
            }
            if (npc.rotation > num1243 - num1354 && npc.rotation < num1243 + num1354)
            {
                npc.rotation = num1243;
            }
            if (Main.rand.Next(5) == 0)
            {
                int num1465 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y + (float)npc.height * 0.25f), npc.width, (int)((float)npc.height * 0.5f), 5, npc.velocity.X, 2f);
                Main.dust[num1465].velocity.X *= 0.5f;
                Main.dust[num1465].velocity.Y *= 0.1f;
            }
            if (Main.dayTime || dead)
            {
                npc.velocity.Y -= 0.04f;
                if (npc.timeLeft > 10)
                {
                    npc.timeLeft = 10;
                }
                return;
            }
            if (npc.ai[0] == 0f)
            {
                if (npc.ai[1] == 0f)
                {
                    float num2 = 5f;
                    float num113 = 0.04f;
                    if (Main.expertMode)
                    {
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
                    if (npc.velocity.X < num224)
                    {
                        npc.velocity.X += num113;
                        if (npc.velocity.X < 0f && num224 > 0f)
                        {
                            npc.velocity.X += num113;
                        }
                    }
                    else if (npc.velocity.X > num224)
                    {
                        npc.velocity.X -= num113;
                        if (npc.velocity.X > 0f && num224 < 0f)
                        {
                            npc.velocity.X -= num113;
                        }
                    }
                    if (npc.velocity.Y < num335)
                    {
                        npc.velocity.Y += num113;
                        if (npc.velocity.Y < 0f && num335 > 0f)
                        {
                            npc.velocity.Y += num113;
                        }
                    }
                    else if (npc.velocity.Y > num335)
                    {
                        npc.velocity.Y -= num113;
                        if (npc.velocity.Y > 0f && num335 < 0f)
                        {
                            npc.velocity.Y -= num113;
                        }
                    }
                    npc.ai[2] += 1f;
                    float num644 = 600f;
                    if (Main.expertMode)
                    {
                        num644 *= 0.35f;
                    }
                    if (npc.ai[2] >= num644)
                    {
                        npc.ai[1] = 1f;
                        npc.ai[2] = 0f;
                        npc.ai[3] = 0f;
                        npc.target = 255;
                        npc.netUpdate = true;
                    }
                    else if ((npc.position.Y + (float)npc.height < Main.player[npc.target].position.Y && num557 < 500f) || (Main.expertMode && num557 < 500f))
                    {
                        if (!Main.player[npc.target].dead)
                        {
                            npc.ai[3] += 1f;
                        }
                        float num655 = 110f;
                        if (Main.expertMode)
                        {
                            num655 *= 0.4f;
                        }
                        if (npc.ai[3] >= num655)
                        {
                            npc.ai[3] = 0f;
                            npc.rotation = num1243;
                            float num666 = 5f;
                            if (Main.expertMode)
                            {
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
                            if (Main.netMode != 1)
                            {
                                int num711 = NPC.NewNPC(npc.GetSource_FromAI(), (int)position.X, (int)position.Y, 5);
                                Main.npc[num711].velocity.X = vector112.X;
                                Main.npc[num711].velocity.Y = vector112.Y;
                                if (Main.netMode == 2 && num711 < 200)
                                {
                                    NetMessage.SendData(23, -1, -1, null, num711);
                                }
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit1, position);
                            for (int m = 0; m < 10; m++)
                            {
                                Dust.NewDust(position, 20, 20, 5, vector112.X * 0.4f, vector112.Y * 0.4f);
                            }
                        }
                    }
                }
                else if (npc.ai[1] == 1f)
                {
                    npc.rotation = num1243;
                    float num722 = 6f;
                    if (Main.expertMode)
                    {
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
                    if (npc.netSpam > 10)
                    {
                        npc.netSpam = 10;
                    }
                }
                else if (npc.ai[1] == 2f)
                {
                    npc.ai[2] += 1f;
                    if (npc.ai[2] >= 40f)
                    {
                        npc.velocity *= 0.98f;
                        if (Main.expertMode)
                        {
                            npc.velocity *= 0.985f;
                        }
                        if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1)
                        {
                            npc.velocity.X = 0f;
                        }
                        if ((double)npc.velocity.Y > -0.1 && (double)npc.velocity.Y < 0.1)
                        {
                            npc.velocity.Y = 0f;
                        }
                    }
                    else
                    {
                        npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - 1.57f;
                    }
                    int num766 = 150;
                    if (Main.expertMode)
                    {
                        num766 = 100;
                    }
                    if (npc.ai[2] >= (float)num766)
                    {
                        npc.ai[3] += 1f;
                        npc.ai[2] = 0f;
                        npc.target = 255;
                        npc.rotation = num1243;
                        if (npc.ai[3] >= 3f)
                        {
                            npc.ai[1] = 0f;
                            npc.ai[3] = 0f;
                        }
                        else
                        {
                            npc.ai[1] = 1f;
                        }
                    }
                }
                float num777 = 0.5f;
                if (Main.expertMode)
                {
                    num777 = 0.65f;
                }
                if ((float)npc.life < (float)npc.lifeMax * num777)
                {
                    npc.ai[0] = 1f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.netUpdate = true;
                    if (npc.netSpam > 10)
                    {
                        npc.netSpam = 10;
                    }
                }
                return;
            }
            if (npc.ai[0] == 1f || npc.ai[0] == 2f)
            {
                if (npc.ai[0] == 1f)
                {
                    npc.ai[2] += 0.005f;
                    if ((double)npc.ai[2] > 0.5)
                    {
                        npc.ai[2] = 0.5f;
                    }
                }
                else
                {
                    npc.ai[2] -= 0.005f;
                    if (npc.ai[2] < 0f)
                    {
                        npc.ai[2] = 0f;
                    }
                }
                npc.rotation += npc.ai[2];
                npc.ai[1] += 1f;
                if (Main.expertMode && npc.ai[1] % 20f == 0f)
                {
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
                    if (Main.netMode != 1)
                    {
                        int num833 = NPC.NewNPC(npc.GetSource_FromAI(), (int)position2.X, (int)position2.Y, 5);
                        Main.npc[num833].velocity.X = vector185.X;
                        Main.npc[num833].velocity.Y = vector185.Y;
                        if (Main.netMode == 2 && num833 < 200)
                        {
                            NetMessage.SendData(23, -1, -1, null, num833);
                        }
                    }
                    for (int n = 0; n < 10; n++)
                    {
                        Dust.NewDust(position2, 20, 20, 5, vector185.X * 0.4f, vector185.Y * 0.4f);
                    }
                }
                if (npc.ai[1] == 100f)
                {
                    npc.ai[0] += 1f;
                    npc.ai[1] = 0f;
                    if (npc.ai[0] == 3f)
                    {
                        npc.ai[2] = 0f;
                    }
                    else
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit1, npc.position);
                        for (int num844 = 0; num844 < 2; num844++)
                        {
                            if (!Main.dedServ)
                            {
                                Gore.NewGore(npc.GetSource_Death(), npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), 8);
                                Gore.NewGore(npc.GetSource_Death(), npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), 7);
                                Gore.NewGore(npc.GetSource_Death(), npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), 6);
                            }
                        }
                        for (int num855 = 0; num855 < 20; num855++)
                        {
                            Dust.NewDust(npc.position, npc.width, npc.height, 5, (float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                    }
                }
                Dust.NewDust(npc.position, npc.width, npc.height, 5, (float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f);
                npc.velocity.X *= 0.98f;
                npc.velocity.Y *= 0.98f;
                if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1)
                {
                    npc.velocity.X = 0f;
                }
                if ((double)npc.velocity.Y > -0.1 && (double)npc.velocity.Y < 0.1)
                {
                    npc.velocity.Y = 0f;
                }
                return;
            }
            npc.defense = 0;
            //npc.damage = 22; //does nothing!

            if (Main.expertMode)
            {
                if (flag24)
                { //third phase (start of faster dashing)
                    npc.damage = (int)(22.5f * 2); //45 (38 - 52)
                    npc.defense = -15;
                }
                if (finalDesperation)
                { //final phase (nonstop dashing)
                    npc.damage = (int)(25f * 2); //50 (43 - 58)
                    npc.defense = -30;
                }
                else
                { //second phase (mouth open)
                    npc.damage = (int)(22.5f * 2); //45 (38 - 52)
                }
            }
            if (npc.ai[1] == 0f && flag24)
            {
                npc.ai[1] = 5f;
            }
            if (npc.ai[1] == 0f)
            {
                float num866 = 6f;
                float num877 = 0.07f;
                Vector2 vector196 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                float num888 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - vector196.X;
                float num899 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - 120f - vector196.Y;
                float num911 = (float)Math.Sqrt(num888 * num888 + num899 * num899);
                if (num911 > 400f && Main.expertMode)
                {
                    num866 += 1f;
                    num877 += 0.05f;
                    if (num911 > 600f)
                    {
                        num866 += 1f;
                        num877 += 0.05f;
                        if (num911 > 800f)
                        {
                            num866 += 1f;
                            num877 += 0.05f;
                        }
                    }
                }
                num911 = num866 / num911;
                num888 *= num911;
                num899 *= num911;
                if (npc.velocity.X < num888)
                {
                    npc.velocity.X += num877;
                    if (npc.velocity.X < 0f && num888 > 0f)
                    {
                        npc.velocity.X += num877;
                    }
                }
                else if (npc.velocity.X > num888)
                {
                    npc.velocity.X -= num877;
                    if (npc.velocity.X > 0f && num888 < 0f)
                    {
                        npc.velocity.X -= num877;
                    }
                }
                if (npc.velocity.Y < num899)
                {
                    npc.velocity.Y += num877;
                    if (npc.velocity.Y < 0f && num899 > 0f)
                    {
                        npc.velocity.Y += num877;
                    }
                }
                else if (npc.velocity.Y > num899)
                {
                    npc.velocity.Y -= num877;
                    if (npc.velocity.Y > 0f && num899 < 0f)
                    {
                        npc.velocity.Y -= num877;
                    }
                }
                npc.ai[2] += 1f;
                if (npc.ai[2] >= 200f)
                {
                    npc.ai[1] = 1f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    if (Main.expertMode && (double)npc.life < (double)npc.lifeMax * 0.35)
                    {
                        npc.ai[1] = 3f;
                    }
                    npc.target = 255;
                    npc.netUpdate = true;
                }
                if (Main.expertMode && finalDesperation)
                {
                    npc.TargetClosest();
                    npc.netUpdate = true;
                    npc.ai[1] = 3f;
                    npc.ai[2] = 0f;
                    npc.ai[3] -= 1000f;
                }
            }
            else if (npc.ai[1] == 1f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.ForceRoar, npc.Center);
                npc.rotation = num1243;
                float num922 = 6.8f;
                if (Main.expertMode && npc.ai[3] == 1f)
                {
                    num922 *= 1.15f;
                }
                if (Main.expertMode && npc.ai[3] == 2f)
                {
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
                if (npc.netSpam > 10)
                {
                    npc.netSpam = 10;
                }
            }
            else if (npc.ai[1] == 2f)
            {
                float num966 = 40f;
                npc.ai[2] += 1f;
                if (Main.expertMode)
                {
                    num966 = 50f;
                }
                if (npc.ai[2] >= num966)
                {
                    npc.velocity *= 0.97f;
                    if (Main.expertMode)
                    {
                        npc.velocity *= 0.98f;
                    }
                    if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1)
                    {
                        npc.velocity.X = 0f;
                    }
                    if ((double)npc.velocity.Y > -0.1 && (double)npc.velocity.Y < 0.1)
                    {
                        npc.velocity.Y = 0f;
                    }
                }
                else
                {
                    npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - 1.57f;
                }
                int num977 = 130;
                if (Main.expertMode)
                {
                    num977 = 90;
                }
                if (npc.ai[2] >= (float)num977)
                {
                    npc.ai[3] += 1f;
                    npc.ai[2] = 0f;
                    npc.target = 255;
                    npc.rotation = num1243;
                    if (npc.ai[3] >= 3f)
                    {
                        npc.ai[1] = 0f;
                        npc.ai[3] = 0f;
                        if (Main.expertMode && Main.netMode != 1 && (double)npc.life < (double)npc.lifeMax * 0.5)
                        {
                            npc.ai[1] = 3f;
                            npc.ai[3] += Main.rand.Next(1, 4);
                        }
                        npc.netUpdate = true;
                        if (npc.netSpam > 10)
                        {
                            npc.netSpam = 10;
                        }
                    }
                    else
                    {
                        npc.ai[1] = 1f;
                    }
                }
            }
            else if (npc.ai[1] == 3f)
            {
                if (npc.ai[3] == 4f && flag24 && npc.Center.Y > Main.player[npc.target].Center.Y)
                {
                    npc.TargetClosest();
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.ai[3] = 0f;
                    npc.netUpdate = true;
                    if (npc.netSpam > 10)
                    {
                        npc.netSpam = 10;
                    }
                }
                else if (Main.netMode != 1)
                {
                    npc.TargetClosest();
                    float num988 = 20f;
                    Vector2 vector218 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                    float num999 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - vector218.X;
                    float num1010 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - vector218.Y;
                    float num1022 = Math.Abs(Main.player[npc.target].velocity.X) + Math.Abs(Main.player[npc.target].velocity.Y) / 4f;
                    num1022 += 10f - num1022;
                    if (num1022 < 5f)
                    {
                        num1022 = 5f;
                    }
                    if (num1022 > 15f)
                    {
                        num1022 = 15f;
                    }
                    if (npc.ai[2] == -1f && !finalDesperation)
                    {
                        num1022 *= 4f;
                        num988 *= 1.3f;
                    }
                    if (finalDesperation)
                    {
                        num1022 *= 2f;
                    }
                    num999 -= Main.player[npc.target].velocity.X * num1022;
                    num1010 -= Main.player[npc.target].velocity.Y * num1022 / 4f;
                    num999 *= 1f + (float)Main.rand.Next(-10, 11) * 0.01f;
                    num1010 *= 1f + (float)Main.rand.Next(-10, 11) * 0.01f;
                    if (finalDesperation)
                    {
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
                    if (finalDesperation)
                    {
                        npc.velocity.X += (float)Main.rand.Next(-50, 51) * 0.1f;
                        npc.velocity.Y += (float)Main.rand.Next(-50, 51) * 0.1f;
                        float num1055 = Math.Abs(npc.velocity.X);
                        float num1066 = Math.Abs(npc.velocity.Y);
                        if (npc.Center.X > Main.player[npc.target].Center.X)
                        {
                            num1066 *= -1f;
                        }
                        if (npc.Center.Y > Main.player[npc.target].Center.Y)
                        {
                            num1055 *= -1f;
                        }
                        npc.velocity.X = num1066 + npc.velocity.X;
                        npc.velocity.Y = num1055 + npc.velocity.Y;
                        npc.velocity.Normalize();
                        npc.velocity *= num988;
                        npc.velocity.X += (float)Main.rand.Next(-20, 21) * 0.1f;
                        npc.velocity.Y += (float)Main.rand.Next(-20, 21) * 0.1f;
                    }
                    else if (num1044 < 100f)
                    {
                        if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                        {
                            float num1077 = Math.Abs(npc.velocity.X);
                            float num1088 = Math.Abs(npc.velocity.Y);
                            if (npc.Center.X > Main.player[npc.target].Center.X)
                            {
                                num1088 *= -1f;
                            }
                            if (npc.Center.Y > Main.player[npc.target].Center.Y)
                            {
                                num1077 *= -1f;
                            }
                            npc.velocity.X = num1088;
                            npc.velocity.Y = num1077;
                        }
                    }
                    else if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                    {
                        float num1099 = (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) / 2f;
                        float num1110 = num1099;
                        if (npc.Center.X > Main.player[npc.target].Center.X)
                        {
                            num1110 *= -1f;
                        }
                        if (npc.Center.Y > Main.player[npc.target].Center.Y)
                        {
                            num1099 *= -1f;
                        }
                        npc.velocity.X = num1110;
                        npc.velocity.Y = num1099;
                    }
                    npc.ai[1] = 4f;
                    npc.netUpdate = true;
                    if (npc.netSpam > 10)
                    {
                        npc.netSpam = 10;
                    }
                }
            }
            else if (npc.ai[1] == 4f)
            {
                if (npc.ai[2] == 0f)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.ForceRoar, npc.Center);
                }
                float num1121 = num910;
                npc.ai[2] += 1f;
                if (npc.ai[2] == num1121 && Vector2.Distance(npc.position, Main.player[npc.target].position) < 200f)
                {
                    npc.ai[2] -= 1f;
                }
                if (npc.ai[2] >= num1121)
                {
                    npc.velocity *= 0.95f;
                    if ((double)npc.velocity.X > -0.1 && (double)npc.velocity.X < 0.1)
                    {
                        npc.velocity.X = 0f;
                    }
                    if ((double)npc.velocity.Y > -0.1 && (double)npc.velocity.Y < 0.1)
                    {
                        npc.velocity.Y = 0f;
                    }
                }
                else
                {
                    npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - 1.57f;
                }
                float num1133 = num1121 + 13f;
                if (npc.ai[2] >= num1133)
                {
                    npc.netUpdate = true;
                    if (npc.netSpam > 10)
                    {
                        npc.netSpam = 10;
                    }
                    npc.ai[3] += 1f;
                    npc.ai[2] = 0f;
                    if (npc.ai[3] >= 5f)
                    {
                        npc.ai[1] = 0f;
                        npc.ai[3] = 0f;
                    }
                    else
                    {
                        npc.ai[1] = 3f;
                    }
                }
            }
            else if (npc.ai[1] == 5f)
            {
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
                if (npc.velocity.X < num1177)
                {
                    npc.velocity.X += num1166;
                    if (npc.velocity.X < 0f && num1177 > 0f)
                    {
                        npc.velocity.X += num1166;
                    }
                }
                else if (npc.velocity.X > num1177)
                {
                    npc.velocity.X -= num1166;
                    if (npc.velocity.X > 0f && num1177 < 0f)
                    {
                        npc.velocity.X -= num1166;
                    }
                }
                if (npc.velocity.Y < num1188)
                {
                    npc.velocity.Y += num1166;
                    if (npc.velocity.Y < 0f && num1188 > 0f)
                    {
                        npc.velocity.Y += num1166;
                    }
                }
                else if (npc.velocity.Y > num1188)
                {
                    npc.velocity.Y -= num1166;
                    if (npc.velocity.Y > 0f && num1188 < 0f)
                    {
                        npc.velocity.Y -= num1166;
                    }
                }
                npc.ai[2] += 1f;
                if (npc.ai[2] >= 70f)
                {
                    npc.TargetClosest();
                    npc.ai[1] = 3f;
                    npc.ai[2] = -1f;
                    npc.ai[3] = Main.rand.Next(-3, 1);
                    npc.netUpdate = true;
                }
            }
            if (finalDesperation && npc.ai[1] == 5f)
            {
                npc.ai[1] = 3f;
            }
        }

        #endregion


        public static int destroyerAttackIndex = 0; //Controls what attack mode the destroyer is in
        public static Vector2 destroyerLaserSafeAngle = new Vector2(1, 1); //Randomly chosen angle along which the destroyer will *never* fire its randomly aimed lasers. Used to ensure players always have a direction they can dodge in.
        public static bool destroyerReachedHeight = false; //When it has entered its spinny phase, this is false until it reaches a point above the player. Used to avoid cheap hits.
        public static int destroyerChargeTimer = 0; //Basic AI timer, controls when the destroyer does almost everything it does.
        public static float destroyerRotation = 0; //Its current angle of rotation. Used during its spinny phase.
        public static bool laserToggle = false; //Used to make only half the lasers fire during a certain phase, but not a *random* half. Instead it toggles on and off with each segment.
        public static float laserRotation = 0f; //What angle should the square array of lasers move along?
        public static float destroyerTargetLerp = 0f; //Determines how fast it should accelerate toward a "safe point" above the player to perform its spiral attack
        public static Vector2 destroyerTargetPosition = Vector2.Zero; //Where it's targeting with certain
        public static bool destroyerJustSpawned = true; //True for the first 10 seconds after it spawns
        public static List<Projectile> horizontalLasers; //These store the locations of the horizontal and vertical sets of lasers during the grid attack
        public static List<Projectile> verticalLasers;
        public static List<Vector2> intersections; //This stores a list of all the intersections between the previous sets of lasers, which is used to determine where to draw the dust telegrpahing when they will activate

        static int PrimeLaserCooldown = 9999999;
        public override void PostAI(NPC npc)
        {
            if (npc.type == NPCID.WallofFlesh || npc.type == NPCID.WallofFleshEye)
            {
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    if (Main.player[0].dead)
                    {
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

            if (npc.type == NPCID.SkeletronPrime)
            {
                PrimeLaserCooldown--;
                if(npc.ai[1] == 1)
                {
                    float partCount = 0;
                    if (NPC.AnyNPCs(NPCID.PrimeLaser))
                    {
                        partCount++;
                    }
                    if (NPC.AnyNPCs(NPCID.PrimeCannon))
                    {
                        partCount++;
                    }
                    if (NPC.AnyNPCs(NPCID.PrimeVice))
                    {
                        partCount++;
                    }
                    if (NPC.AnyNPCs(NPCID.PrimeSaw))
                    {
                        partCount++;
                    }

                    //Bosses speed is cut to 40% of base, but ramps up to almost full speed as its pieces die
                    float speedMultiplier = 0.8f - (0.4f * partCount / 4);
                    npc.velocity *= speedMultiplier;
                }               

                //Laser attack speed ramps up as pieces die
                //TODO: Add orbiting probes that spawn around head when pieces die, which actually shoot the laser. Looks awkward just coming out of its head randomly.
                if (PrimeLaserCooldown == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (!NPC.AnyNPCs(NPCID.PrimeLaser))
                    {
                        PrimeLaserCooldown = 450; //450
                    }
                    if (!NPC.AnyNPCs(NPCID.PrimeCannon))
                    {
                        PrimeLaserCooldown -= 100; //350
                    }
                    if (!NPC.AnyNPCs(NPCID.PrimeVice))
                    {
                        PrimeLaserCooldown -= 100; //250
                    }
                    if (!NPC.AnyNPCs(NPCID.PrimeSaw))
                    {
                        PrimeLaserCooldown -= 100; //150
                    }
                    Vector2 projVel = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 1);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.EnemyRedLaser>(), 20, 0, Main.myPlayer, npc.target, npc.whoAmI);
                }
            }

            if (npc.type == NPCID.TheDestroyerBody && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!destroyerJustSpawned)
                {

                    if ((destroyerAttackIndex == 3 && destroyerReachedHeight))
                    {
                        Vector2 dustPos = Main.npc[(int)npc.ai[3]].velocity;
                        dustPos.Normalize();
                        dustPos *= 48;
                        dustPos = dustPos.RotatedBy(MathHelper.PiOver2);
                        Dust.NewDustPerfect(npc.Center + dustPos, DustID.Torch, Main.npc[(int)npc.ai[3]].velocity.RotatedBy(MathHelper.PiOver2) / 6, Scale: 3).noGravity = true;

                        if (destroyerChargeTimer > -180 && destroyerChargeTimer <= 740 && destroyerChargeTimer % 120 == 0 && laserToggle)
                        {
                            Vector2 projVel = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center + Main.rand.NextVector2CircularEdge(220, 220), 1);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.EnemyLingeringLaser>(), 35, 0, Main.myPlayer, 2000 + npc.target, npc.whoAmI);
                        }
                    }

                    if (destroyerChargeTimer < 60 && destroyerChargeTimer > 0)
                    {
                        Vector2 dustPos = Main.npc[(int)npc.ai[3]].velocity;
                        dustPos.Normalize();
                        dustPos *= 48;
                        dustPos = dustPos.RotatedBy(MathHelper.ToRadians(6 * destroyerChargeTimer));
                        Dust.NewDustPerfect(npc.Center + dustPos, DustID.Torch, Main.npc[(int)npc.ai[3]].velocity.RotatedBy(MathHelper.PiOver2) / 6, Scale: 3).noGravity = true;
                    }

                    //Aims in a ring around the player
                    if (destroyerAttackIndex == 0)
                    {
                        if (destroyerChargeTimer == 0)
                        {
                            Vector2 projVel = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center + (Main.player[npc.target].velocity * 45), 1);

                            int style = -1;

                            //Aims in a ring around the player to constrain their movement.
                            //Impossible for any player other than the one being targeted to dodge, so it doesn't happen in multiplayer.
                            if (destroyerAttackIndex == 0)
                            {
                                if (Main.netMode == NetmodeID.SinglePlayer && Main.player[npc.target].active)
                                {
                                    style = npc.target;
                                }
                                else
                                {
                                    style = -1;
                                }
                            }

                            //Cancel the attack if it's too close to a "safe angle", which ensures the player can always avoid the attack
                            if (UsefulFunctions.CompareAngles(projVel, destroyerLaserSafeAngle) > MathHelper.PiOver4 / 2 && UsefulFunctions.CompareAngles(-projVel, destroyerLaserSafeAngle) > MathHelper.PiOver4 / 2)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.EnemyLingeringLaser>(), 20, 0, Main.myPlayer, style, npc.whoAmI);
                            }
                        }
                    }


                    if (Main.GameUpdateCount % 150 == 0 && (destroyerAttackIndex == 0 || destroyerAttackIndex == 2))
                    {
                        Vector2 projVel = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 1);
                        if (UsefulFunctions.CompareAngles(projVel, destroyerLaserSafeAngle) > MathHelper.Pi / 6f && UsefulFunctions.CompareAngles(-projVel, destroyerLaserSafeAngle) > MathHelper.Pi / 6f)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.EnemyLingeringLaser>(), 20, 0, Main.myPlayer, 1000 + npc.target, npc.whoAmI);
                        }
                    }

                    laserToggle = !laserToggle;
                }                
            }



            if (npc.type == NPCID.TheDestroyer)
            {
                destroyerChargeTimer++;

                if (!destroyerJustSpawned)
                {
                    //Don't let the custom AI override its "run away" code
                    if (!Main.dayTime && !Main.player[npc.target].dead)
                    {
                        if (destroyerAttackIndex == 3)
                        {
                            destroyerGlowPercent += (1f / 180f);
                            if (!destroyerReachedHeight)
                            {
                                Vector2 targetPoint = Main.player[npc.target].Center;
                                targetPoint.Y -= 400;

                                if (Vector2.Distance(npc.Center, targetPoint) < 70)
                                {
                                    destroyerReachedHeight = true;
                                    destroyerRotation = MathHelper.Pi;
                                    destroyerChargeTimer = -300;
                                }
                                else
                                {
                                    if (destroyerTargetLerp < 1)
                                    {
                                        destroyerTargetLerp += 0.001f;
                                    }
                                    npc.velocity = Vector2.Lerp(npc.velocity, UsefulFunctions.GenerateTargetingVector(npc.Center, targetPoint, 15), destroyerTargetLerp);
                                }
                            }
                            //Don't do anything until it's set up
                            else
                            {
                                //Spawn dust
                                Vector2 dustPos = npc.velocity;
                                dustPos.Normalize();
                                dustPos *= 48;
                                dustPos = dustPos.RotatedBy(MathHelper.PiOver2);
                                Dust.NewDustPerfect(npc.Center + dustPos, DustID.Torch, npc.velocity.RotatedBy(MathHelper.PiOver2) / 6, Scale: 3).noGravity = true;

                                //Accelerate over time
                                float factor = (400 + destroyerChargeTimer) / 400f;
                                if (destroyerChargeTimer > 0)
                                {
                                    factor = 1;
                                }

                                //Start slowing down near the end of the attack
                                if (destroyerChargeTimer > 920)
                                {
                                    factor = 0.2f + (0.8f * (980 - destroyerChargeTimer) / 60f);
                                }

                                //Rotate in a circle a decent distance from the player
                                destroyerRotation += 0.09f * factor;
                                npc.velocity = new Vector2(60 * factor, 0).RotatedBy(destroyerRotation);



                                if (destroyerChargeTimer == 980)
                                {
                                    destroyerRotation = MathHelper.Pi;
                                    destroyerAttackIndex = 0;
                                    destroyerChargeTimer = -240;
                                    destroyerReachedHeight = false;
                                    destroyerTargetLerp = 0;

                                    //Clean up
                                    for (int i = 0; i < Main.maxProjectiles; i++)
                                    {
                                        if (Main.projectile[i].type == ModContent.ProjectileType<Projectiles.Enemy.EnemyLingeringLaser>())
                                        {
                                            Main.projectile[i].Kill();
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            destroyerGlowPercent = 0.01f;

                            //Circle of lasers
                            if (destroyerAttackIndex == 2)
                            {
                                if (destroyerChargeTimer % 180 == 90)
                                {
                                    laserRotation = Main.rand.NextVector2Circular(10, 10).ToRotation();
                                    if (Main.netMode != NetmodeID.MultiplayerClient)
                                    {
                                        float subRotation = 0;
                                        for (int i = 0; i < 3; i++)
                                        {
                                            subRotation += 2 * MathHelper.Pi / 3;

                                            Projectile.NewProjectile(npc.GetSource_FromThis(), Main.player[npc.target].Center, new Vector2(0, 1).RotatedBy(laserRotation + subRotation), ModContent.ProjectileType<Projectiles.Enemy.EnemyLingeringLaser>(), 20, 0, Main.myPlayer, -3, npc.whoAmI);
                                        }
                                    }
                                }
                            }

                            //Moving square array                        
                            if (destroyerAttackIndex == 1)
                            {
                                if (destroyerChargeTimer % 290 == 1 && destroyerChargeTimer < 580)
                                {
                                    horizontalLasers = new List<Projectile>();
                                    verticalLasers = new List<Projectile>();
                                    Vector2 randomVector = Main.rand.NextVector2Circular(10, 10);
                                    if (randomVector.Y > 0)
                                    {
                                        //Never let the movement direction of the grid be *down*, makes it very hard to dodge
                                        randomVector.Y *= -1;
                                    }
                                    laserRotation = randomVector.ToRotation();

                                    Vector2 startPos = new Vector2(0, -3200).RotatedBy(laserRotation);
                                    Vector2 step = new Vector2(220, 220).RotatedBy(laserRotation);
                                    Vector2 laserVel = new Vector2(-1, 1).RotatedBy(laserRotation); //Aim down left
                                    if (Main.netMode != NetmodeID.MultiplayerClient)
                                    {
                                        for (int i = 0; i < 15; i++)
                                        {
                                            horizontalLasers.Add(Projectile.NewProjectileDirect(npc.GetSource_FromThis(), Main.player[npc.target].Center + startPos, laserVel, ModContent.ProjectileType<Projectiles.Enemy.EnemyLingeringLaser>(), 20, 0, Main.myPlayer, -1, npc.whoAmI));
                                            startPos += step;
                                        }


                                        startPos = new Vector2(0, -3200).RotatedBy(laserRotation);
                                        step = new Vector2(-220, 220).RotatedBy(laserRotation);
                                        laserVel = new Vector2(1, 1).RotatedBy(laserRotation); //Aim down right
                                        for (int i = 0; i < 15; i++)
                                        {
                                            verticalLasers.Add(Projectile.NewProjectileDirect(npc.GetSource_FromThis(), Main.player[npc.target].Center + startPos, laserVel, ModContent.ProjectileType<Projectiles.Enemy.EnemyLingeringLaser>(), 20, 0, Main.myPlayer, -1, npc.whoAmI));
                                            startPos += step;
                                        }

                                        intersections = new List<Vector2>();
                                        foreach (Projectile hLaser in horizontalLasers)
                                        {
                                            foreach (Projectile vLaser in verticalLasers)
                                            {
                                                Vector2[] collisions = Collision.CheckLinevLine(hLaser.position, hLaser.position + hLaser.velocity * 3000, vLaser.position, vLaser.position + vLaser.velocity * 3000);
                                                if (collisions.Length == 1) //2 lines can only ever intersect once, unless they're parallel and on top of each other. There would be bigger problems if that was the case.
                                                {
                                                    if (!intersections.Contains(collisions[0]))
                                                    {
                                                        intersections.Add(collisions[0]);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (destroyerChargeTimer % 290 < 150 && destroyerChargeTimer < 580)
                                {
                                    Vector2 translationOffset = new Vector2(3, 0).RotatedBy(laserRotation) * (destroyerChargeTimer % 290 - 1);

                                    Rectangle screenRect = new Rectangle((int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100, Main.screenWidth + 100, Main.screenHeight + 100);

                                    if (Main.netMode != NetmodeID.MultiplayerClient)
                                    {
                                        foreach (Vector2 intersection in intersections)
                                        {
                                            if (screenRect.Contains(intersection.ToPoint()) && Vector2.DistanceSquared(intersection + translationOffset, Main.player[npc.target].Center) < 250000)
                                            {
                                                for (int i = 0; i < 5; i++)
                                                {
                                                    Vector2 circularOffset = Main.rand.NextVector2CircularEdge(1, 1);
                                                    circularOffset.Normalize();
                                                    Vector2 velocity = new Vector2(0, 3).RotatedBy(laserRotation);
                                                    circularOffset *= (150 - (destroyerChargeTimer % 290));


                                                    Dust.NewDustPerfect(intersection + circularOffset + translationOffset, DustID.OrangeTorch, velocity, Scale: 1).noGravity = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (destroyerChargeTimer >= 600)
                            {
                                destroyerChargeTimer = 0;
                                destroyerAttackIndex++;
                                if (destroyerAttackIndex == 3)
                                {
                                    UsefulFunctions.BroadcastText("The Destroyer's hull begins glowing fiercely...", Color.OrangeRed);
                                }
                            }
                            if (Main.GameUpdateCount % 60 == 0)
                            {
                                Vector2 sum = Vector2.Zero;
                                float count = 0;
                                for(int i = 0; i < Main.maxNPCs; i++)
                                {
                                    NPC thisNPC = Main.npc[i];
                                    if(thisNPC.type == NPCID.TheDestroyerBody)
                                    {
                                        sum += UsefulFunctions.GenerateTargetingVector(thisNPC.Center, Main.player[npc.target].Center, 5);
                                        count++;
                                    }
                                }

                                sum /= count;
                                destroyerLaserSafeAngle = sum.RotatedBy(MathHelper.PiOver2);
                            }
                        }
                    }
                }
                else
                {
                    if(destroyerChargeTimer > 300)
                    {
                        destroyerChargeTimer = 0;
                        destroyerJustSpawned = false;
                    }
                }
            }
        }

        //Something about the way the game draws the destroyer is fucked up, so instead of re-drawing it myself i'm just fucking with the way the game draws normally draws it
        //This has to be done in a kinda convoluted way. We can't just end then begin the spritebatch every time we draw a piece
        public static bool drawingDestroyer = false;
        public static int lastNPCDrawn = 0;
        public static float destroyerGlowPercent = 0f;
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            //The game draws ever NPC based on their whoAmI, but counting *down*. So if it just counted *up* we know it just started drawing this frame and that we should re-start the spritebatch
            if (npc.whoAmI > lastNPCDrawn)
            {
                drawingDestroyer = false;
            }

            lastNPCDrawn = npc.whoAmI;

            if (npc.type == NPCID.TheDestroyer || npc.type == NPCID.TheDestroyerBody || npc.type == NPCID.TheDestroyerTail)
            {
                if (!drawingDestroyer)
                {
                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);

                    drawingDestroyer = true;


                    if (destroyerReachedHeight || destroyerGlowPercent > 1)
                    {
                        destroyerGlowPercent = 1;
                    }

                    //Wind down as the attack ends
                    if (destroyerChargeTimer > 920)
                    {
                        destroyerGlowPercent = ((destroyerChargeTimer - 920f) / 60f);
                    }

                    data.UseColor(Color.Lerp(Color.Black, Color.OrangeRed, destroyerGlowPercent));
                    data.Apply(null);
                }
                return true;
            }
            else
            {
                if (drawingDestroyer)
                {
                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
                    drawingDestroyer = false;
                }
                return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
            }
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(npc, spriteBatch, screenPos, drawColor);
        }

        public void DrawDestroyerGlow(NPC npc, SpriteBatch spriteBatch, Texture2D texture, Color drawColor)
        {
            SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = sourceRectangle.Size() / 2f;
            
            spriteBatch.Draw((Texture2D)TextureAssets.Dest[1], npc.Center - Main.screenPosition, sourceRectangle, Color.White * 0.45f, npc.rotation, origin, npc.scale, effects, 0f);
            spriteBatch.Draw(texture, npc.Center - Main.screenPosition, sourceRectangle, Color.Orange * 0.2f, npc.rotation, origin, npc.scale, effects, 0f);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, int damage, float knockback, bool crit)
        {
            if (npc.type == NPCID.TheDestroyer || npc.type == NPCID.TheDestroyerBody || npc.type == NPCID.TheDestroyerTail)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type == NPCID.TheDestroyer || Main.npc[i].type == NPCID.TheDestroyerBody || Main.npc[i].type == NPCID.TheDestroyerTail)
                    {
                        Main.npc[i].immune[projectile.owner] = 5;
                    }
                }
            }
            base.OnHitByProjectile(npc, projectile, damage, knockback, crit);
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, int damage, float knockback, bool crit)
        {
            if (npc.type == NPCID.TheDestroyer || npc.type == NPCID.TheDestroyerBody || npc.type == NPCID.TheDestroyerTail)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type == NPCID.TheDestroyer || Main.npc[i].type == NPCID.TheDestroyerBody || Main.npc[i].type == NPCID.TheDestroyerTail)
                    {
                        Main.npc[i].immune[player.whoAmI] = 5;
                    }
                }
            }
            base.OnHitByItem(npc, player, item, damage, knockback, crit);
        }

        public override bool CheckDead(NPC npc)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                if (npc.type == NPCID.LavaSlime)
                {
                    try
                    {
                        int npcTileX = (int)(npc.Center.X / 16f);
                        int npcTileY = (int)(npc.Center.Y / 16f);
                        if (!WorldGen.SolidTile(npcTileX, npcTileY))
                        {
                            Main.tile[npcTileX, npcTileY].LiquidAmount = 0;
                            WorldGen.SquareTileFrame(npcTileX, npcTileY);
                        }
                    }
                    catch
                    { //do nothing
                    }
                }
            }
            return base.CheckDead(npc);
        }

        public override void OnKill(NPC npc)
        {
            #region Loot Changes

            if(npc.type == NPCID.PrimeLaser)
            {
                UsefulFunctions.BroadcastText("WARNING: Laser power redirected......", Color.Red);
                PrimeLaserCooldown = 500;
            }

            if(npc.target > Main.maxPlayers || Main.player[npc.target] == null || Main.player[npc.target].active == false)
            {
                npc.target = 0;
            }

            Player player = Main.player[npc.target];

            if (npc.type == NPCID.BigStinger)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("BloodredMossClump").Type);
            }

            if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)) && !Main.expertMode)
            {

                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.DemoniteOre, 4);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.ShadowScale, 4);
            }

            if ((npc.type >= NPCID.BigHornetStingy && npc.type <= NPCID.LittleHornetFatty) ||
                                (npc.type >= NPCID.GiantMossHornet && npc.type <= NPCID.LittleStinger) ||
                                npc.type == NPCID.Hornet || npc.type == NPCID.ManEater ||
                                npc.type == NPCID.MossHornet ||
                                (npc.type >= NPCID.HornetFatty && npc.type <= NPCID.HornetStingy))
            {
                if (Main.rand.NextFloat() >= .66f)
                { // 33% chance in revamped
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("BloodredMossClump").Type);
                }
            }

            if (npc.type == NPCID.KingSlime)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("DarkSoul").Type, 500);
                if (!Main.expertMode)
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.GoldCoin, 10); //obtained from boss bag in Expert mode (see tsorcGlobalItem for boss bag edits)
                }
            }

            if (npc.type == NPCID.QueenBee)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("DarkSoul").Type, 2000);
            }

            if (npc.type == NPCID.TheDestroyer && !Main.expertMode)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<CrestOfCorruption>(), 2);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<RTQ2>());
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<RTQ2>());


            }

            if (npc.type == NPCID.SkeletronHead && !Main.expertMode)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<MiakodaFull>());
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<MiakodaFull>());
                if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.SkeletronHead) && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<SublimeBoneDust>());

            }
            if (npc.type == NPCID.SkeletronPrime && !Main.expertMode)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<CrestOfSteel>(), 2);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.AngelWings, 1, false, -1);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.AngelWings, 1, false, -1);
                if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.SkeletronPrime) && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<SublimeBoneDust>());
            }
            if ((npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism) && !Main.expertMode)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<CrestOfSky>(), 2);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<DarkSoul>(), 1000);
            }

            if (npc.netID == NPCID.GreenSlime)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("DarkSoul").Type);
            }

            if (npc.netID == NPCID.RedSlime)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("DarkSoul").Type, 2);
            }

            if ((npc.type == NPCID.Mimic || npc.type == NPCID.BigMimicCorruption || npc.type == NPCID.BigMimicCrimson || npc.type == NPCID.BigMimicHallow))
            {
                if (Main.rand.Next(10) == 0)
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("SymbolOfAvarice").Type);
                }
            }

            if (npc.type == NPCID.EyeofCthulhu && !Main.expertMode)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.HerosHat);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.HerosShirt);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.HerosPants);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.HermesBoots, 1, false, -1);
                if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.EyeofCthulhu) && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<SublimeBoneDust>());

            }

            if (npc.type == NPCID.WallofFlesh && !Main.expertMode)
            {
                if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.WallofFlesh) && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<EstusFlaskShard>());
            }

            if (npc.type == NPCID.PossessedArmor && Main.rand.Next(50) == 0 && !Main.expertMode)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("WallTome").Type);
            }

            if ((npc.type == NPCID.PossessedArmor || npc.type == NPCID.Wraith) && Main.rand.Next(25) == 0 && Main.expertMode)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("WallTome").Type);
            }

            if (npc.type == NPCID.Shark && Main.rand.Next(20) == 0)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("CoralSword").Type, 1, false, -1);
            }

            if (Main.rand.Next(25) == 0 && ((npc.type >= NPCID.BigPantlessSkeleton && npc.type <= NPCID.SmallSkeleton) ||
                                (npc.type >= NPCID.HeadacheSkeleton && npc.type <= NPCID.PantlessSkeleton) ||
                                (npc.type >= NPCID.SkeletonTopHat && npc.type <= NPCID.SkeletonAlien) ||
                                (npc.type >= NPCID.BoneThrowingSkeleton && npc.type <= NPCID.BoneThrowingSkeleton4) ||
                                npc.type == NPCID.HeavySkeleton ||
                                npc.type == NPCID.Skeleton ||
                                npc.type == NPCID.ArmoredSkeleton ||
                                npc.type == NPCID.SkeletonArcher))
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("DeadChicken").Type);
            }

            if (npc.type == NPCID.Vulture && Main.rand.Next(10) == 0)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("DeadChicken").Type);
            }

            if (npc.type == NPCID.Wraith)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.Heart);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.Heart);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.Heart);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.Heart);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.Heart);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.Heart);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ItemID.Heart);

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
                                (npc.type >= NPCID.PincushionZombie && npc.type <= NPCID.TwiggyZombie)))
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("DeadChicken").Type);
            }

            if (npc.type == NPCID.GoblinArcher || npc.type == NPCID.GoblinPeon || npc.type == NPCID.GoblinWarrior || npc.type == NPCID.GoblinSorcerer || npc.type == NPCID.GoblinThief)
            {

                if (Main.rand.Next(200) == 0)
                { // 0.5%
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("Pulsar").Type, 1, false, -1);
                }

                else if (Main.rand.Next(200) == 0)
                { // 0.5% 
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), Mod.Find<ModItem>("ToxicCatalyzer").Type, 1, false, -1);
                }
            }
            if (npc.type == NPCID.Plantera && !Main.expertMode)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<CrestOfLife>(), 1);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<CrestOfLife>(), 1);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<SoulOfLife>(), 6);
            }
            if (npc.type == NPCID.Golem && !Main.expertMode)
            {
                if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<BrokenPicksaw>());
                }
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<CrestOfStone>(), 1);
                Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<CrestOfStone>(), 1);
            }
            if (npc.type == NPCID.Snatcher || npc.type == NPCID.ManEater || npc.type == NPCID.AngryTrapper)
            {
                if (Main.rand.Next(3) == 0) Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.Potions.GreenBlossom>());
            }
            if (npc.type == NPCID.HornetLeafy || npc.type == NPCID.BigHornetLeafy || npc.type == NPCID.LittleHornetLeafy)
            {
                if (Main.rand.Next(5) == 0) Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.Potions.GreenBlossom>());
            }
            if (npc.type == NPCID.WallCreeper || npc.type == NPCID.WallCreeperWall || npc.type == NPCID.BlackRecluse || npc.type == NPCID.BlackRecluseWall)
            {
                if (Main.rand.Next(10) == 0) Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.Humanity>());
                if (Main.rand.Next(10) == 0 && Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.Humanity>());
            }


            #endregion
            #region Pillar ModWorld bools
            if (npc.type == NPCID.LunarTowerVortex) tsorcRevampWorld.DownedVortex = true;
            if (npc.type == NPCID.LunarTowerNebula) tsorcRevampWorld.DownedNebula = true;
            if (npc.type == NPCID.LunarTowerStardust) tsorcRevampWorld.DownedStardust = true;
            if (npc.type == NPCID.LunarTowerSolar) tsorcRevampWorld.DownedSolar = true;
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
                if (projectile.aiStyle == 19 && projectile.DamageType == DamageClass.Melee == true)
                {
                    damage *= 2;
                }
            }
        }

        public override bool CheckActive(NPC npc)
        {
            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp") && npc.boss)
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
