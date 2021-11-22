using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using Terraria.UI;
using System.Collections.Generic;
using System;
using System.Reflection;
using ReLogic.Graphics;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework.Audio;
using ReLogic.Utilities;
using Terraria.Audio;

namespace tsorcRevamp {
    class MethodSwaps {

        internal static void ApplyMethodSwaps() {
            On.Terraria.Player.Spawn += SpawnPatch;

            On.Terraria.WorldGen.UpdateLunarApocalypse += StopMoonLord;

            On.Terraria.Recipe.FindRecipes += SoulSlotRecipesPatch;

            On.Terraria.NPC.TypeToHeadIndex += MapHeadPatch;

            On.Terraria.Player.TileInteractionsCheckLongDistance += SignTextPatch;

            On.Terraria.NPC.SpawnNPC += BossZenPatch;

            On.Terraria.Main.DrawMenu += DownloadMapButton;

            On.Terraria.Main.UpdateAudio += UpdateAudioPatch;
        }        

        
        private static void UpdateAudioPatch(On.Terraria.Main.orig_UpdateAudio orig, Main self)
        {

            //Getting fieldinfos            
            if(tsorcRevamp.AudioLockInfo == null)
            {
                tsorcRevamp.AudioLockInfo = typeof(Main).GetField("_audioLock", BindingFlags.NonPublic | BindingFlags.Static);
            }
            if (tsorcRevamp.ActiveSoundInstancesInfo == null)
            {
                tsorcRevamp.ActiveSoundInstancesInfo = typeof(Main).GetField("ActiveSoundInstances", BindingFlags.NonPublic | BindingFlags.Static);
            }
            if (tsorcRevamp.AreSoundsPausedInfo == null)
            {
                tsorcRevamp.AreSoundsPausedInfo = typeof(Main).GetField("_areSoundsPaused", BindingFlags.NonPublic | BindingFlags.Static);
            }
            if (tsorcRevamp.TrackedSoundsInfo == null)
            {
                tsorcRevamp.TrackedSoundsInfo = typeof(Main).GetField("_trackedSounds", BindingFlags.NonPublic | BindingFlags.Static);
            }

            //Getting objects
            Object AudioLock = tsorcRevamp.AudioLockInfo.GetValue(self);
            List<SoundEffectInstance> ActiveSoundInstances = (List<SoundEffectInstance>)tsorcRevamp.ActiveSoundInstancesInfo.GetValue(self);
            bool _areSoundsPaused = (bool)tsorcRevamp.AreSoundsPausedInfo.GetValue(self);
            SlotVector<ActiveSound> _trackedSounds = (SlotVector<ActiveSound>)tsorcRevamp.TrackedSoundsInfo.GetValue(self);

            //All the rest of this is just a re-implementation of UpdateAudio, but with a snippet added in the middle to change the title music
            ///////////////////////////////////////////////////////////////////////////////////////////////
            #region UpdateAudioReimplementation
            if (Main.waveBank == null) //supress extra exceptions from audio engine failing to load
                return;

#if !WINDOWS
            if (Main.engine != null)
                Main.engine.Update();

            
            lock (AudioLock)
            {
                for (int i = 0; i < ActiveSoundInstances.Count; i++)
                {
                    if (ActiveSoundInstances == null)
                    {
                        ActiveSoundInstances.RemoveAt(i);
                        i--;
                    }
                    else if (ActiveSoundInstances[i].State == SoundState.Stopped)
                    {
                        ActiveSoundInstances[i].Dispose();
                        ActiveSoundInstances.RemoveAt(i);
                        i--;
                    }
                }
            }

#endif
            if (!Main.dedServ)
            {
                bool flag = (!Main.hasFocus || Main.gamePaused) && Main.netMode == 0;
                if (flag)
                {
                    foreach (SlotVector<ActiveSound>.ItemPair item2 in (IEnumerable<SlotVector<ActiveSound>.ItemPair>)_trackedSounds)
                    {
                        item2.Value.Pause();
                    }
                }
                else if (_areSoundsPaused && !flag)
                {
                    foreach (SlotVector<ActiveSound>.ItemPair item3 in (IEnumerable<SlotVector<ActiveSound>.ItemPair>)_trackedSounds)
                    {
                        item3.Value.Resume();
                    }
                }

                _areSoundsPaused = flag;
                if (!_areSoundsPaused)
                {
                    foreach (SlotVector<ActiveSound>.ItemPair item4 in (IEnumerable<SlotVector<ActiveSound>.ItemPair>)_trackedSounds)
                    {
                        item4.Value.Update();
                        if (!item4.Value.IsPlaying)
                            _trackedSounds.Remove(item4.Id);
                    }
                }
            }

            if (Main.musicVolume == 0f)
                Main.curMusic = 0;

            try
            {
                if (Main.dedServ)
                    return;

                if (Main.curMusic > 0)
                {
                    if (!self.IsActive)
                    {
                        for (int i = 0; i < Main.music.Length; i++)
                        {
                            if (Main.music[i] != null && !Main.music[i].IsPaused && Main.music[i].IsPlaying && Main.musicFade[i] > 0f)
                            {
                                try
                                {
                                    Main.music[i].Pause();
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }

                        for (int j = 0; j < Main.soundInstanceLiquid.Length; j++)
                        {
                            Main.soundInstanceLiquid[j].Stop();
                        }
                    }
                    else
                    {
                        for (int k = 0; k < Main.music.Length; k++)
                        {
                            if (Main.music[k] != null && Main.music[k].IsPaused && Main.musicFade[k] > 0f)
                            {
                                try
                                {
                                    Main.music[k].Resume();
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }
                }

                bool flag2 = false;
                bool flag3 = false;
                bool flag4 = false;
                bool flag5 = false;
                bool flag6 = false;
                bool flag7 = false;
                bool flag8 = false;
                bool flag9 = false;
                bool flag10 = false;
                bool flag11 = false;
                bool flag12 = false;
                bool flag13 = false;
                Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);
                int num = 5000;
                int modMusic = -1;
                MusicPriority modPriority = MusicPriority.None;
                for (int l = 0; l < 200; l++)
                {
                    if (!Main.npc[l].active)
                        continue;

                    int num2 = 0;
                    switch (Main.npc[l].type)
                    {
                        case 13:
                        case 14:
                        case 15:
                            num2 = 1;
                            break;
                        case 26:
                        case 27:
                        case 28:
                        case 29:
                        case 111:
                            num2 = 11;
                            break;
                        case 113:
                        case 114:
                        case 125:
                        case 126:
                            num2 = 2;
                            break;
                        case 134:
                        case 143:
                        case 144:
                        case 145:
                        case 266:
                            num2 = 3;
                            break;
                        case 212:
                        case 213:
                        case 214:
                        case 215:
                        case 216:
                        case 491:
                            num2 = 8;
                            break;
                        case 245:
                            num2 = 4;
                            break;
                        case 222:
                            num2 = 5;
                            break;
                        case 262:
                        case 263:
                        case 264:
                            num2 = 6;
                            break;
                        case 381:
                        case 382:
                        case 383:
                        case 385:
                        case 386:
                        case 388:
                        case 389:
                        case 390:
                        case 391:
                        case 395:
                        case 520:
                            num2 = 9;
                            break;
                        case 398:
                            num2 = 7;
                            break;
                        case 422:
                        case 493:
                        case 507:
                        case 517:
                            num2 = 10;
                            break;
                        case 439:
                            num2 = 4;
                            break;
                        case 438:
                            if (Main.npc[l].ai[1] == 1f)
                                num2 = 4;
                            break;
                    }

                    if (Main.npc[l].type < NPCID.Sets.BelongsToInvasionOldOnesArmy.Length && NPCID.Sets.BelongsToInvasionOldOnesArmy[Main.npc[l].type])
                        num2 = 12;

                    if (num2 == 0 && Main.npc[l].boss)
                        num2 = 1;

                    if (num2 == 0 && (Main.npc[l].modNPC == null || Main.npc[l].modNPC.music < 0))
                        continue;

                    Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle((int)(Main.npc[l].position.X + (float)(Main.npc[l].width / 2)) - num, (int)(Main.npc[l].position.Y + (float)(Main.npc[l].height / 2)) - num, num * 2, num * 2);
                    if (rectangle.Intersects(value))
                    {
                        if (Main.npc[l].modNPC != null && Main.npc[l].modNPC.music >= 0 && (modMusic < 0 || Main.npc[l].modNPC.musicPriority > modPriority))
                        {
                            modMusic = Main.npc[l].modNPC.music;
                            modPriority = Main.npc[l].modNPC.musicPriority;
                        }
                        switch (num2)
                        {
                            case 1:
                                flag2 = true;
                                break;
                            case 2:
                                flag3 = true;
                                break;
                            case 3:
                                flag4 = true;
                                break;
                            case 4:
                                flag5 = true;
                                break;
                            case 5:
                                flag6 = true;
                                break;
                            case 6:
                                flag7 = true;
                                break;
                            case 7:
                                flag8 = true;
                                break;
                            case 8:
                                flag9 = true;
                                break;
                            case 9:
                                flag10 = true;
                                break;
                            case 10:
                                flag11 = true;
                                break;
                            case 11:
                                flag12 = true;
                                break;
                            case 12:
                                flag13 = true;
                                break;
                        }

                        break;
                    }
                }

                //Reimplementation of ModHooks.UpdateMusic
                foreach (Mod mod in ModLoader.Mods)
                {
                    int thisModMusic = -1;
                    MusicPriority thisModPriority = MusicPriority.BiomeLow;
                    mod.UpdateMusic(ref thisModMusic, ref thisModPriority);
                    if (thisModMusic >= 0 && thisModPriority >= modPriority)
                    {
                        modMusic = thisModMusic;
                        modPriority = thisModPriority;
                    }
                }


                int num3 = (int)((Main.screenPosition.X + (float)(Main.screenWidth / 2)) / 16f);
                if (Main.musicVolume == 0f)
                {
                    self.newMusic = 0;
                }
                else if (Main.gameMenu)
                {
                    if (Main.netMode != 2)
                    {
                        self.newMusic = 6;

                        //This is where the main change happens!
                        Mod musicMod = ModLoader.GetMod("tsorcMusic");
                        if (musicMod != null)
                        {
                            if (Main.musicVolume != 0f && Main.gameMenu && Main.netMode != 2)
                            {
                                self.newMusic = musicMod.GetSoundSlot((Terraria.ModLoader.SoundType)51, "Sounds/Music/Night");
                                Main.curMusic = musicMod.GetSoundSlot((Terraria.ModLoader.SoundType)51, "Sounds/Music/Night");
                            }
                        }
                    }
                    else
                    {
                        self.newMusic = 0;
                    }
                }
                else
                {
                    float num4 = Main.maxTilesX / 4200;
                    num4 *= num4;
                    float num5 = (float)((double)((Main.screenPosition.Y + (float)(Main.screenHeight / 2)) / 16f - (65f + 10f * num4)) / (Main.worldSurface / 5.0));
                    if (modPriority >= MusicPriority.BossHigh)
                    {
                        self.newMusic = modMusic;
                    }
                    else if (flag8)
                    {
                        self.newMusic = 38;
                    }
                    else if (modPriority >= MusicPriority.BossMedium)
                    {
                        self.newMusic = modMusic;
                    }
                    else if (flag10)
                    {
                        self.newMusic = 37;
                    }
                    else if (flag11)
                    {
                        self.newMusic = 34;
                    }
                    else if (flag7)
                    {
                        self.newMusic = 24;
                    }
                    else if (modPriority >= MusicPriority.BossLow)
                    {
                        self.newMusic = modMusic;
                    }
                    else if (flag3)
                    {
                        self.newMusic = 12;
                    }
                    else if (flag2)
                    {
                        self.newMusic = 5;
                    }
                    else if (flag4)
                    {
                        self.newMusic = 13;
                    }
                    else if (flag5)
                    {
                        self.newMusic = 17;
                    }
                    else if (flag6)
                    {
                        self.newMusic = 25;
                    }
                    else if (modPriority >= MusicPriority.Event)
                    {
                        self.newMusic = modMusic;
                    }
                    else if (flag9)
                    {
                        self.newMusic = 35;
                    }
                    else if (flag12)
                    {
                        self.newMusic = 39;
                    }
                    else if (flag13)
                    {
                        self.newMusic = 41;
                    }
                    else if (modPriority >= MusicPriority.Environment)
                    {
                        self.newMusic = modMusic;
                    }
                    else if (Main.player[Main.myPlayer].ZoneSandstorm)
                    {
                        self.newMusic = 40;
                    }
                    else if (Main.player[Main.myPlayer].position.Y > (float)((Main.maxTilesY - 200) * 16))
                    {
                        self.newMusic = 36;
                    }
                    else if (Main.eclipse && (double)Main.player[Main.myPlayer].position.Y < Main.worldSurface * 16.0 + (double)(Main.screenHeight / 2))
                    {
                        self.newMusic = 27;
                    }
                    else if (num5 < 1f)
                    {
                        self.newMusic = 15;
                    }
                    else if (modPriority >= MusicPriority.BiomeHigh)
                    {
                        self.newMusic = modMusic;
                    }
                    else if (Main.tile[(int)(Main.player[Main.myPlayer].Center.X / 16f), (int)(Main.player[Main.myPlayer].Center.Y / 16f)].wall == 87)
                    {
                        self.newMusic = 26;
                    }
                    else if ((Main.bgStyle == 9 && (double)Main.player[Main.myPlayer].position.Y < Main.worldSurface * 16.0 + (double)(Main.screenHeight / 2)) || Main.ugBack == 2)
                    {
                        self.newMusic = 29;
                    }
                    else if (Main.player[Main.myPlayer].ZoneCorrupt)
                    {
                        if ((double)Main.player[Main.myPlayer].position.Y > Main.worldSurface * 16.0 + (double)(Main.screenHeight / 2))
                            self.newMusic = 10;
                        else
                            self.newMusic = 8;
                    }
                    else if (Main.player[Main.myPlayer].ZoneCrimson)
                    {
                        if ((double)Main.player[Main.myPlayer].position.Y > Main.worldSurface * 16.0 + (double)(Main.screenHeight / 2))
                            self.newMusic = 33;
                        else
                            self.newMusic = 16;
                    }
                    else if (modPriority >= MusicPriority.BiomeMedium)
                    {
                        self.newMusic = modMusic;
                    }
                    else if (Main.player[Main.myPlayer].ZoneDungeon)
                    {
                        self.newMusic = 23;
                    }
                    else if (Main.player[Main.myPlayer].ZoneMeteor)
                    {
                        self.newMusic = 2;
                    }
                    else if (Main.player[Main.myPlayer].ZoneJungle)
                    {
                        self.newMusic = 7;
                    }
                    else if (Main.player[Main.myPlayer].ZoneSnow)
                    {
                        if ((double)Main.player[Main.myPlayer].position.Y > Main.worldSurface * 16.0 + (double)(Main.screenHeight / 2))
                            self.newMusic = 20;
                        else
                            self.newMusic = 14;
                    }
                    else if (modPriority >= MusicPriority.BiomeLow)
                    {
                        self.newMusic = modMusic;
                    }
                    else if ((double)Main.player[Main.myPlayer].position.Y > Main.worldSurface * 16.0 + (double)(Main.screenHeight / 2))
                    {
                        if (Main.player[Main.myPlayer].ZoneHoly)
                        {
                            self.newMusic = 11;
                        }
                        else if (Main.sandTiles > 2200)
                        {
                            self.newMusic = 21;
                        }
                        else
                        {
                            if (Main.ugMusic == 0)
                                Main.ugMusic = 4;

                            if (!Main.music[4].IsPlaying && !Main.music[31].IsPlaying)
                            {
                                if (Main.musicFade[4] == 1f)
                                    Main.musicFade[31] = 1f;

                                if (Main.musicFade[31] == 1f)
                                    Main.musicFade[4] = 1f;

                                switch (Main.rand.Next(2))
                                {
                                    case 0:
                                        Main.ugMusic = 4;
                                        Main.musicFade[31] = 0f;
                                        break;
                                    case 1:
                                        Main.ugMusic = 31;
                                        Main.musicFade[4] = 0f;
                                        break;
                                }
                            }

                            self.newMusic = Main.ugMusic;
                        }
                    }
                    else if (Main.dayTime && Main.player[Main.myPlayer].ZoneHoly)
                    {
                        if (Main.cloudAlpha > 0f && !Main.gameMenu)
                            self.newMusic = 19;
                        else
                            self.newMusic = 9;
                    }
                    else if ((double)(Main.screenPosition.Y / 16f) < Main.worldSurface + 10.0 && (num3 < 380 || num3 > Main.maxTilesX - 380))
                    {
                        self.newMusic = 22;
                    }
                    else if (Main.sandTiles > 1000)
                    {
                        self.newMusic = 21;
                    }
                    else if (Main.dayTime)
                    {
                        if (Main.cloudAlpha > 0f && !Main.gameMenu)
                        {
                            self.newMusic = 19;
                        }
                        else
                        {
                            if (Main.dayMusic == 0)
                                Main.dayMusic = 1;

                            if (!Main.music[1].IsPlaying && !Main.music[18].IsPlaying)
                            {
                                switch (Main.rand.Next(2))
                                {
                                    case 0:
                                        Main.dayMusic = 1;
                                        break;
                                    case 1:
                                        Main.dayMusic = 18;
                                        break;
                                }
                            }

                            self.newMusic = Main.dayMusic;
                        }
                    }
                    else if (!Main.dayTime)
                    {
                        if (Main.bloodMoon)
                            self.newMusic = 2;
                        else if (Main.cloudAlpha > 0f && !Main.gameMenu)
                            self.newMusic = 19;
                        else
                            self.newMusic = 3;
                    }

                    if ((double)(Main.screenPosition.Y / 16f) < Main.worldSurface + 10.0 && Main.pumpkinMoon)
                        self.newMusic = 30;

                    if ((double)(Main.screenPosition.Y / 16f) < Main.worldSurface + 10.0 && Main.snowMoon)
                        self.newMusic = 32;
                }

                if (Main.gameMenu || Main.musicVolume == 0f)
                {
                    Main.musicBox2 = -1;
                    Main.musicBox = -1;
                }

                if (Main.musicBox2 >= 0)
                    Main.musicBox = Main.musicBox2;

                if (Main.musicBox >= 0)
                {
                    if (Main.musicBox == 0)
                        self.newMusic = 1;

                    if (Main.musicBox == 1)
                        self.newMusic = 2;

                    if (Main.musicBox == 2)
                        self.newMusic = 3;

                    if (Main.musicBox == 4)
                        self.newMusic = 4;

                    if (Main.musicBox == 5)
                        self.newMusic = 5;

                    if (Main.musicBox == 3)
                        self.newMusic = 6;

                    if (Main.musicBox == 6)
                        self.newMusic = 7;

                    if (Main.musicBox == 7)
                        self.newMusic = 8;

                    if (Main.musicBox == 9)
                        self.newMusic = 9;

                    if (Main.musicBox == 8)
                        self.newMusic = 10;

                    if (Main.musicBox == 11)
                        self.newMusic = 11;

                    if (Main.musicBox == 10)
                        self.newMusic = 12;

                    if (Main.musicBox == 12)
                        self.newMusic = 13;

                    if (Main.musicBox == 13)
                        self.newMusic = 14;

                    if (Main.musicBox == 14)
                        self.newMusic = 15;

                    if (Main.musicBox == 15)
                        self.newMusic = 16;

                    if (Main.musicBox == 16)
                        self.newMusic = 17;

                    if (Main.musicBox == 17)
                        self.newMusic = 18;

                    if (Main.musicBox == 18)
                        self.newMusic = 19;

                    if (Main.musicBox == 19)
                        self.newMusic = 20;

                    if (Main.musicBox == 20)
                        self.newMusic = 21;

                    if (Main.musicBox == 21)
                        self.newMusic = 22;

                    if (Main.musicBox == 22)
                        self.newMusic = 23;

                    if (Main.musicBox == 23)
                        self.newMusic = 24;

                    if (Main.musicBox == 24)
                        self.newMusic = 25;

                    if (Main.musicBox == 25)
                        self.newMusic = 26;

                    if (Main.musicBox == 26)
                        self.newMusic = 27;

                    if (Main.musicBox == 27)
                        self.newMusic = 29;

                    if (Main.musicBox == 28)
                        self.newMusic = 30;

                    if (Main.musicBox == 29)
                        self.newMusic = 31;

                    if (Main.musicBox == 30)
                        self.newMusic = 32;

                    if (Main.musicBox == 31)
                        self.newMusic = 33;

                    if (Main.musicBox == 32)
                        self.newMusic = 38;

                    if (Main.musicBox == 33)
                        self.newMusic = 37;

                    if (Main.musicBox == 34)
                        self.newMusic = 35;

                    if (Main.musicBox == 35)
                        self.newMusic = 36;

                    if (Main.musicBox == 36)
                        self.newMusic = 34;

                    if (Main.musicBox == 37)
                        self.newMusic = 39;

                    if (Main.musicBox == 38)
                        self.newMusic = 40;

                    if (Main.musicBox == 39)
                        self.newMusic = 41;

                    if (Main.musicBox >= Main.maxMusic)
                        self.newMusic = Main.musicBox;
                }

                Main.curMusic = self.newMusic;
                float num6 = 1f;
                if (NPC.MoonLordCountdown > 0)
                {
                    num6 = (float)NPC.MoonLordCountdown / 3600f;
                    num6 *= num6;
                    if (NPC.MoonLordCountdown > 720)
                    {
                        num6 = MathHelper.Lerp(0f, 1f, num6);
                    }
                    else
                    {
                        num6 = 0f;
                        Main.curMusic = 0;
                    }

                    if (NPC.MoonLordCountdown == 1 && Main.curMusic >= 1 && Main.curMusic < Main.music.Length)
                        Main.musicFade[Main.curMusic] = 0f;
                }

                for (int m = 1; m < Main.music.Length; m++)
                {
                    if (Main.music[m] == null)
                        continue; // Race condition, Music is resized during load.

                    if (m == 28)
                    {
                        if (Main.cloudAlpha > 0f && (double)Main.player[Main.myPlayer].position.Y < Main.worldSurface * 16.0 + (double)(Main.screenHeight / 2) && !Main.player[Main.myPlayer].ZoneSnow)
                        {
                            if (Main.ambientVolume == 0f)
                            {
                                if (Main.music[m].IsPlaying)
                                    Main.music[m].Stop(AudioStopOptions.Immediate);

                                continue;
                            }

                            if (!Main.music[m].IsPlaying)
                            {
                                Main.music[m].Reset();
                                Main.music[m].Play();
                                Main.music[m].SetVariable("Volume", Main.musicFade[m] * Main.ambientVolume);
                                continue;
                            }

                            if (Main.music[m].IsPaused && self.IsActive)
                            {
                                Main.music[m].Resume();
                                continue;
                            }

                            Main.musicFade[m] += 0.005f;
                            if (Main.musicFade[m] > 1f)
                                Main.musicFade[m] = 1f;

                            Main.music[m].SetVariable("Volume", Main.musicFade[m] * Main.ambientVolume);
                        }
                        else if (Main.music[m].IsPlaying)
                        {
                            if (Main.musicFade[Main.curMusic] > 0.25f)
                                Main.musicFade[m] -= 0.005f;
                            else if (Main.curMusic == 0)
                                Main.musicFade[m] = 0f;

                            if (Main.musicFade[m] <= 0f)
                            {
                                Main.musicFade[m] -= 0f;
                                Main.music[m].Stop(AudioStopOptions.Immediate);
                            }
                            else
                            {
                                Main.music[m].SetVariable("Volume", Main.musicFade[m] * Main.ambientVolume);
                            }
                        }
                        else
                        {
                            Main.musicFade[m] = 0f;
                        }
                    }
                    else if (m == Main.curMusic)
                    {
                        if (!Main.music[m].IsPlaying)
                        {
                            Main.music[m].Reset();
                            Main.music[m].Play();
                            Main.music[m].SetVariable("Volume", Main.musicFade[m] * Main.musicVolume * num6);
                            continue;
                        }

                        Main.musicFade[m] += 0.005f;
                        if (Main.musicFade[m] > 1f)
                            Main.musicFade[m] = 1f;

                        Main.music[m].SetVariable("Volume", Main.musicFade[m] * Main.musicVolume * num6);
                        Main.music[m].CheckBuffer();
                    }
                    else if (Main.music[m].IsPlaying)
                    {
                        Main.music[m].CheckBuffer();
                        if (Main.musicFade[Main.curMusic] > 0.25f)
                            Main.musicFade[m] -= 0.005f;
                        else if (Main.curMusic == 0)
                            Main.musicFade[m] = 0f;

                        if (Main.musicFade[m] <= 0f)
                        {
                            Main.musicFade[m] -= 0f;
                            Main.music[m].Stop(AudioStopOptions.Immediate);
                        }
                        else
                        {
                            Main.music[m].SetVariable("Volume", Main.musicFade[m] * Main.musicVolume * num6);
                        }
                    }
                    else
                    {
                        Main.musicFade[m] = 0f;
                    }
                }

                Main.engine.Update();
                if (Main.musicError > 0)
                    Main.musicError--;
            }
            catch
            {
                Main.musicError++;
                if (Main.musicError >= 100)
                {
                    Main.musicError = 0;
                    //Main.musicVolume = 0f;
                }
            }
            #endregion
        }




        //allow spawns to be set outside a valid house (for bonfires)
        internal static void SpawnPatch(On.Terraria.Player.orig_Spawn orig, Player self) {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                Main.InitLifeBytes();
                if (self.whoAmI == Main.myPlayer) {
                    if (Main.mapTime < 5) {
                        Main.mapTime = 5;
                    }
                    Main.quickBG = 10;
                    self.FindSpawn();
                    if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                        if (!Player.CheckSpawn(self.SpawnX, self.SpawnY)) {
                            self.SpawnX = -1;
                            self.SpawnY = -1;
                        } 
                    }
                    Main.maxQ = true;
                }
                if (Main.netMode == NetmodeID.MultiplayerClient && self.whoAmI == Main.myPlayer) {
                    NetMessage.SendData(MessageID.SpawnPlayer, -1, -1, null, Main.myPlayer);
                    Main.gameMenu = false;
                }
                self.headPosition = Vector2.Zero;
                self.bodyPosition = Vector2.Zero;
                self.legPosition = Vector2.Zero;
                self.headRotation = 0f;
                self.bodyRotation = 0f;
                self.legRotation = 0f;
                self.lavaTime = self.lavaMax;
                if (self.statLife <= 0) {
                    int num = self.statLifeMax2 / 2;
                    self.statLife = 100;
                    if (num > self.statLife) {
                        self.statLife = num;
                    }
                    self.breath = self.breathMax;
                    if (self.spawnMax) {
                        self.statLife = self.statLifeMax2;
                        self.statMana = self.statManaMax2;
                    }
                }
                self.immune = true;
                if (self.dead) {
                    PlayerHooks.OnRespawn(self);
                }
                self.dead = false;
                self.immuneTime = 0;
                self.active = true;
                if (self.SpawnX >= 0 && self.SpawnY >= 0) {
                    self.position.X = self.SpawnX * 16 + 8 - self.width / 2;
                    self.position.Y = self.SpawnY * 16 - self.height;
                }
                else {
                    self.position.X = Main.spawnTileX * 16 + 8 - self.width / 2;
                    self.position.Y = Main.spawnTileY * 16 - self.height;
                    for (int i = Main.spawnTileX - 1; i < Main.spawnTileX + 2; i++) {
                        for (int j = Main.spawnTileY - 3; j < Main.spawnTileY; j++) {
                            if (Main.tile[i, j] != null) {
                                if (Main.tileSolid[Main.tile[i, j].type] && !Main.tileSolidTop[Main.tile[i, j].type]) {
                                    WorldGen.KillTile(i, j);
                                }
                                if (Main.tile[i, j].liquid > 0) {
                                    Main.tile[i, j].lava(lava: false);
                                    Main.tile[i, j].liquid = 0;
                                    WorldGen.SquareTileFrame(i, j);
                                }
                            }
                        }
                    }
                }
                self.wet = false;
                self.wetCount = 0;
                self.lavaWet = false;
                self.fallStart = (int)(self.position.Y / 16f);
                self.fallStart2 = self.fallStart;
                self.velocity.X = 0f;
                self.velocity.Y = 0f;
                for (int k = 0; k < 3; k++) {
                    self.UpdateSocialShadow();
                }
                self.oldPosition = self.position + self.BlehOldPositionFixer;
                self.talkNPC = -1;
                if (self.whoAmI == Main.myPlayer) {
                    Main.npcChatCornerItem = 0;
                }
                if (self.pvpDeath) {
                    self.pvpDeath = false;
                    self.immuneTime = 300;
                    self.statLife = self.statLifeMax;
                }
                else {
                    self.immuneTime = 60;
                }
                if (self.whoAmI == Main.myPlayer) {
                    Main.BlackFadeIn = 255;
                    Main.renderNow = true;
                    if (Main.netMode == NetmodeID.MultiplayerClient) {
                        Netplay.newRecent();
                    }
                    Main.screenPosition.X = self.position.X + self.width / 2 - Main.screenWidth / 2;
                    Main.screenPosition.Y = self.position.Y + self.height / 2 - Main.screenHeight / 2;
                }
            }
            else {
                orig(self);
            }
        }

        //stop moon lord from spawning after pillars are killed (adventure mode only)
        internal static void StopMoonLord(On.Terraria.WorldGen.orig_UpdateLunarApocalypse orig) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {

                if (!NPC.LunarApocalypseIsUp) {
                    return;
                }
                //bool flag = false; this bool was used to check for moon lord's core
                bool flag2 = false;
                bool flag3 = false;
                bool flag4 = false;
                bool flag5 = false;
                for (int i = 0; i < 200; i++) {
                    if (Main.npc[i].active) {
                        switch (Main.npc[i].type) {
                            /*
                            case 398:
                                flag = true;
                                break;
                            */
                            case 517:
                                flag2 = true;
                                break;
                            case 422:
                                flag3 = true;
                                break;
                            case 507:
                                flag4 = true;
                                break;
                            case 493:
                                flag5 = true;
                                break;
                        }
                    }
                }
                if (!flag2) {
                    NPC.TowerActiveSolar = false;
                }
                if (!flag3) {
                    NPC.TowerActiveVortex = false;
                }
                if (!flag4) {
                    NPC.TowerActiveNebula = false;
                }
                if (!flag5) {
                    NPC.TowerActiveStardust = false;
                }
                if (!NPC.TowerActiveSolar && !NPC.TowerActiveVortex && !NPC.TowerActiveNebula && !NPC.TowerActiveStardust/* && !flag*/) {
                    //WorldGen.StartImpendingDoom();
                    //recreate the effects of StartImpendingDoom, minus the part about spawning moon lord
                    NPC.LunarApocalypseIsUp = false;
                    if (Main.netMode != NetmodeID.MultiplayerClient) {
                        WorldGen.GetRidOfCultists();
                    }
                }

            }
            else {
                orig();
            }
        }

        //allow souls in the soul slot to be included in calculations for craftable recipes
        internal static void SoulSlotRecipesPatch(On.Terraria.Recipe.orig_FindRecipes orig) {
            int num = Main.availableRecipe[Main.focusRecipe];
            float num2 = Main.availableRecipeY[Main.focusRecipe];
            for (int i = 0; i < Recipe.maxRecipes; i++) {
                Main.availableRecipe[i] = 0;
            }
            Main.numAvailableRecipes = 0;
            if (Main.guideItem.type > 0 && Main.guideItem.stack > 0 && Main.guideItem.Name != "") {
                for (int j = 0; j < Recipe.maxRecipes && Main.recipe[j].createItem.type != 0; j++) {
                    for (int k = 0; k < Recipe.maxRequirements && Main.recipe[j].requiredItem[k].type != 0; k++) {
                        if (Main.guideItem.IsTheSameAs(Main.recipe[j].requiredItem[k]) || Main.recipe[j].useWood(Main.guideItem.type, Main.recipe[j].requiredItem[k].type) || Main.recipe[j].useSand(Main.guideItem.type, Main.recipe[j].requiredItem[k].type) || Main.recipe[j].useIronBar(Main.guideItem.type, Main.recipe[j].requiredItem[k].type) || Main.recipe[j].useFragment(Main.guideItem.type, Main.recipe[j].requiredItem[k].type) || Main.recipe[j].AcceptedByItemGroups(Main.guideItem.type, Main.recipe[j].requiredItem[k].type) || Main.recipe[j].usePressurePlate(Main.guideItem.type, Main.recipe[j].requiredItem[k].type)) {
                            Main.availableRecipe[Main.numAvailableRecipes] = j;
                            Main.numAvailableRecipes++;
                            break;
                        }
                    }
                }
            }
            else {
                Dictionary<int, int> dictionary = new Dictionary<int, int>();
                Item[] array = null;
                Item item = null;
                array = Main.player[Main.myPlayer].inventory;
                for (int l = 0; l < 58; l++) {
                    item = array[l];
                    if (item.stack > 0) {
                        if (dictionary.ContainsKey(item.netID)) {
                            dictionary[item.netID] += item.stack;
                        }
                        else {
                            dictionary[item.netID] = item.stack;
                        }
                    }
                }
                //new
                item = Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().SoulSlot.Item;
                if (item.stack > 0) {
                    if (dictionary.ContainsKey(item.netID)) {
                        dictionary[item.netID] += item.stack;
                    }
                    else {
                        dictionary[item.netID] = item.stack;
                    }
                }
                //end new
                if (Main.player[Main.myPlayer].chest != -1) {
                    if (Main.player[Main.myPlayer].chest > -1) {
                        array = Main.chest[Main.player[Main.myPlayer].chest].item;
                    }
                    else if (Main.player[Main.myPlayer].chest == -2) {
                        array = Main.player[Main.myPlayer].bank.item;
                    }
                    else if (Main.player[Main.myPlayer].chest == -3) {
                        array = Main.player[Main.myPlayer].bank2.item;
                    }
                    else if (Main.player[Main.myPlayer].chest == -4) {
                        array = Main.player[Main.myPlayer].bank3.item;
                    }
                    for (int m = 0; m < 40; m++) {
                        item = array[m];
                        if (item.stack > 0) {
                            if (dictionary.ContainsKey(item.netID)) {
                                dictionary[item.netID] += item.stack;
                            }
                            else {
                                dictionary[item.netID] = item.stack;
                            }
                        }
                    }
                }
                for (int n = 0; n < Recipe.maxRecipes && Main.recipe[n].createItem.type != 0; n++) {
                    bool flag = true;
                    if (flag) {
                        for (int num3 = 0; num3 < Recipe.maxRequirements && Main.recipe[n].requiredTile[num3] != -1; num3++) {
                            if (!Main.player[Main.myPlayer].adjTile[Main.recipe[n].requiredTile[num3]]) {
                                flag = false;
                                break;
                            }
                        }
                    }
                    if (flag) {
                        for (int num4 = 0; num4 < Recipe.maxRequirements; num4++) {
                            item = Main.recipe[n].requiredItem[num4];
                            if (item.type == 0) {
                                break;
                            }
                            int num5 = item.stack;
                            bool flag2 = false;
                            foreach (int key in dictionary.Keys) {
                                if (Main.recipe[n].useWood(key, item.type) || Main.recipe[n].useSand(key, item.type) || Main.recipe[n].useIronBar(key, item.type) || Main.recipe[n].useFragment(key, item.type) || Main.recipe[n].AcceptedByItemGroups(key, item.type) || Main.recipe[n].usePressurePlate(key, item.type)) {
                                    num5 -= dictionary[key];
                                    flag2 = true;
                                }
                            }
                            if (!flag2 && dictionary.ContainsKey(item.netID)) {
                                num5 -= dictionary[item.netID];
                            }
                            if (num5 > 0) {
                                flag = false;
                                break;
                            }
                        }
                    }
                    if (flag) {
                        bool num9 = !Main.recipe[n].needWater || Main.player[Main.myPlayer].adjWater || Main.player[Main.myPlayer].adjTile[172];
                        bool flag3 = !Main.recipe[n].needHoney || Main.recipe[n].needHoney == Main.player[Main.myPlayer].adjHoney;
                        bool flag4 = !Main.recipe[n].needLava || Main.recipe[n].needLava == Main.player[Main.myPlayer].adjLava;
                        bool flag5 = !Main.recipe[n].needSnowBiome || Main.player[Main.myPlayer].ZoneSnow;
                        if (!(num9 && flag3 && flag4 && flag5)) {
                            flag = false;
                        }
                    }
                    if (flag && RecipeHooks.RecipeAvailable(Main.recipe[n])) {
                        Main.availableRecipe[Main.numAvailableRecipes] = n;
                        Main.numAvailableRecipes++;
                    }
                }
            }
            for (int num6 = 0; num6 < Main.numAvailableRecipes; num6++) {
                if (num == Main.availableRecipe[num6]) {
                    Main.focusRecipe = num6;
                    break;
                }
            }
            if (Main.focusRecipe >= Main.numAvailableRecipes) {
                Main.focusRecipe = Main.numAvailableRecipes - 1;
            }
            if (Main.focusRecipe < 0) {
                Main.focusRecipe = 0;
            }
            float num7 = Main.availableRecipeY[Main.focusRecipe] - num2;
            for (int num8 = 0; num8 < Recipe.maxRecipes; num8++) {
                Main.availableRecipeY[num8] -= num7;
            }
        }

        //stop npc heads from displaying on the map
        private static int MapHeadPatch(On.Terraria.NPC.orig_TypeToHeadIndex orig, int type) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && (!(Main.EquipPage == 1) || Main.mapFullscreen)) {
                NPC npc = new NPC();
                npc.SetDefaults(type);
                //Mechanic is hidden until any mech boss is killed
                if (npc.type == NPCID.Mechanic && !NPC.downedMechBossAny)
                {
                    return 0;
                }
                //Goblin is hidden until the Jungle Wyvern is killed
                else if (npc.type == NPCID.GoblinTinkerer && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>()))
                {
                    return 0;
                    
                }
                //Wizard is hidden until The Sorrow is killed
                else if (npc.type == NPCID.Wizard && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheSorrow>()))
                {
                    return 0;
                }
                else {
                    return orig(type);
                }

            }
            else { return orig(type); }
        }

        //stop sign text from drawing when the player is too far away / does not have line of sight to the sign
        internal static void SignTextPatch(On.Terraria.Player.orig_TileInteractionsCheckLongDistance orig, Player self, int myX, int myY) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && Main.tileSign[Main.tile[myX, myY].type]) {
                if (Main.tile[myX, myY] == null) {
                    Main.tile[myX, myY] = new Tile();
                }
                if (!Main.tile[myX, myY].active()) {
                    return;
                }
                if (Main.tile[myX, myY].type == 21) {
                    orig(self, myX, myY);
                }
                if (Main.tile[myX, myY].type == 88) {
                    orig(self, myX, myY);
                }
                if (Main.tileSign[Main.tile[myX, myY].type]) {
                    Vector2 signPos = new Vector2(myX * 16, myY * 16);
                    Vector2 toSign = signPos - self.position;
                    if (Collision.CanHitLine(self.position, 0, 0, signPos, 0, 0) && toSign.Length() < 240) { 
                        self.noThrow = 2;
                        int num3 = Main.tile[myX, myY].frameX / 18;
                        int num4 = Main.tile[myX, myY].frameY / 18;
                        num3 %= 2;
                        int num7 = myX - num3;
                        int num5 = myY - num4;
                        Main.signBubble = true;
                        Main.signX = num7 * 16 + 16;
                        Main.signY = num5 * 16;
                        int num6 = Sign.ReadSign(num7, num5);
                        if (num6 != -1) {
                            Main.signHover = num6;
                            self.showItemIcon = false;
                            self.showItemIcon2 = -1;
                        }
                    }
                }
                TileLoader.MouseOverFar(myX, myY);
            }
            else {
                orig(self, myX, myY);
            }
        }

        //boss zen actually zens
        internal static void BossZenPatch(On.Terraria.NPC.orig_SpawnNPC orig) {
            bool BossZen = false;

            for (int i = 0; i < Main.maxPlayers; i++) {
                if (!Main.player[i].active || Main.player[i].dead) { continue; }
                if (Main.player[i].HasBuff(ModContent.BuffType<Buffs.BossZenBuff>())) {
                    BossZen = true;
                    break;
                }
            }

            if (BossZen) { return; }
            else {
                orig();
            }
        }

        internal static void DownloadMapButton(On.Terraria.Main.orig_DrawMenu orig, Main self, GameTime gameTime) {
            orig(self, gameTime);
            Mod mod = ModContent.GetInstance<tsorcRevamp>();
            tsorcRevamp thisMod = (tsorcRevamp)mod;
            if (Main.mouseLeftRelease)
            {
                thisMod.UICooldown = false;
            }

           
            if (Main.menuMode == 16) {

                string downloadText = "Want the Custom Map? Click here to install!";
                Color downloadTextColor = Main.DiscoColor;
                string dataDir = Main.SavePath + "\\Mod Configs\\tsorcRevampData";

                string baseMapFileName = "\\tsorcBaseMap.wld";
                string userMapFileName = "\\TheStoryofRedCloud.wld";
                string worldsFolder = Main.SavePath + "\\Worlds";

                if (File.Exists(worldsFolder + userMapFileName))
                {
                    downloadText = "Custom map loaded! To play it, hit \"Back\" and select it!\n Or, click here to make another fresh copy of the map";
                    downloadTextColor = Color.AliceBlue;
                }



                Vector2 downloadTextOrigin = Main.fontMouseText.MeasureString(downloadText);
                float textScale = 2;
                Vector2 downloadTextPosition = new Vector2((Main.screenWidth / 2) - (downloadTextOrigin.X * 0.5f * textScale), 120 + (80 * 6));

                
                if (Main.mouseX > downloadTextPosition.X && Main.mouseX < downloadTextPosition.X + (downloadTextOrigin.X * textScale)) {
                    if (Main.mouseY > downloadTextPosition.Y && Main.mouseY < downloadTextPosition.Y + (downloadTextOrigin.Y * textScale)) {

                        downloadTextColor = Color.Yellow;

                        if (Main.mouseLeft && !thisMod.UICooldown) {
                            thisMod.UICooldown = true;
                            if (File.Exists(dataDir + baseMapFileName)) {
                                if (!File.Exists(worldsFolder + userMapFileName)) {

                                    FileInfo fileToCopy = new FileInfo(dataDir + baseMapFileName);
                                    mod.Logger.Info("Attempting to copy world.");
                                    try
                                    {
                                        fileToCopy.CopyTo(worldsFolder + userMapFileName, false);
                                    }
                                    catch (System.Security.SecurityException e) {
                                        mod.Logger.Warn("World copy failed ({0}). Try again with administrator privileges?", e);
                                    }
                                    catch (Exception e) {
                                        mod.Logger.Warn("World copy failed ({0}).", e);
                                    }
                                }
                                else {
                                    mod.Logger.Info("World already exists. Making renamed copy.");
                                    FileInfo fileToCopy = new FileInfo(dataDir + baseMapFileName);
                                    try
                                    {
                                        string newFileName;
                                        bool validName = false;
                                        int worldCount = 1;
                                        do
                                        {
                                            newFileName = "\\TheStoryOfRedCloud_" + worldCount.ToString() + ".wld";
                                            if(File.Exists(worldsFolder + newFileName))
                                            {
                                                worldCount++;
                                                if (worldCount > 255)
                                                {
                                                    mod.Logger.Warn("World copy failed, too many copies.");
                                                }
                                            }
                                            else
                                            {
                                                validName = true;
                                            }
                                        } while (!validName);

                                        fileToCopy.CopyTo(worldsFolder + newFileName, false);
                                    }
                                    catch (System.Security.SecurityException e)
                                    {
                                        mod.Logger.Warn("World copy failed ({0}). Try again with administrator privileges?", e);
                                    }
                                    catch (Exception e)
                                    {
                                        mod.Logger.Warn("World copy failed ({0}).", e);
                                    }
                                }
                            }
                        }
                    }
                }
                Main.spriteBatch.Begin();
                DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, Main.fontMouseText, downloadText, downloadTextPosition, downloadTextColor, 0, Vector2.Zero, textScale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
                Main.spriteBatch.End();


               
            }

            if(Main.menuMode == 0)
            {
                string musicModDir = Main.SavePath + "\\Mods\\tsorcMusic.tmod";

                //Only display this if they have tsorcRevamp enabled and the music mod is not downloaded at all
                if (!File.Exists(musicModDir))
                {
                    String musicText = "Click here to get the Story of Red Cloud music mod!";
                    float musicTextScale = 2;
                    Vector2 musicTextOrigin = Main.fontMouseText.MeasureString(musicText);
                    Vector2 musicTextPosition = new Vector2((Main.screenWidth / 2) - musicTextOrigin.X * 0.5f * musicTextScale, 70 + (80 * 6));
                    Color musicTextColor = Main.DiscoColor;

                    if (Main.mouseX > musicTextPosition.X && Main.mouseX < musicTextPosition.X + (musicTextOrigin.X * musicTextScale))
                    {
                        if (Main.mouseY > musicTextPosition.Y && Main.mouseY < musicTextPosition.Y + (musicTextOrigin.Y * musicTextScale))
                        {

                            musicTextColor = Color.Yellow;

                            if (Main.mouseLeft)
                            {

                                mod.Logger.Info("Attempting to download music mod.");
                                ServicePointManager.Expect100Continue = true;
                                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                                string filePath = Main.SavePath + "\\Mods\\tsorcMusic.tmod";


                                if (!File.Exists(filePath))
                                {
                                    log4net.ILog thisLogger = ModLoader.GetMod("tsorcRevamp").Logger;
                                    thisLogger.Info("Attempting to download music file.");
                                    try
                                    {
                                        using (WebClient client = new WebClient())
                                        {
                                            client.DownloadFileAsync(new Uri(VariousConstants.MUSIC_MOD_URL), filePath);
                                        }
                                    }
                                    catch (WebException e)
                                    {
                                        thisLogger.Warn("Automatic music download failed ({0}). Connection to the internet failed or the file's location has changed.", e);
                                    }

                                    catch (Exception e)
                                    {
                                        thisLogger.Warn("Automatic world download failed ({0}).", e);
                                    }
                                }
                            }
                        }
                    }














                    Main.spriteBatch.Begin();
                    DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, Main.fontMouseText, musicText, musicTextPosition, musicTextColor, 0, Vector2.Zero, musicTextScale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
                    Main.spriteBatch.End();
                }               
            }
           
        }

    }
}
