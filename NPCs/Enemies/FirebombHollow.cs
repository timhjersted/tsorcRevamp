using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class FirebombHollow : ModNPC
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Firebomb Hollow");
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.BoneThrowingSkeleton];
        }

        public override void SetDefaults()
        {
            npc.CloneDefaults(NPCID.BoneThrowingSkeleton);
            animationType = NPCID.BoneThrowingSkeleton;
            npc.aiStyle = -1;
            aiType = -1;
            npc.lifeMax = 45;
            npc.damage = 16;
            npc.scale = 1;
            npc.value = 250;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath2;
            npc.defense = 6;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.FirebombHollowBanner>();
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (tsorcRevampWorld.SuperHardMode) return 0.002f;

            if (Main.expertMode && Main.bloodMoon && spawnInfo.player.ZoneOverworldHeight) return chance = 0.075f;

            if (Main.expertMode && Main.bloodMoon) return chance = 0.035f;

            if (((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) && spawnInfo.player.ZoneOverworldHeight && Main.dayTime) return chance = 0.035f;
            if (((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) && spawnInfo.player.ZoneOverworldHeight && !Main.dayTime) return chance = 0.045f;

            if ((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) return chance = 0.01f;

            return chance;
        }
        public override void NPCLoot()
        {
            Item.NewItem(npc.getRect(), mod.ItemType("Firebomb"), Main.rand.Next(2, 4));
            if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), mod.ItemType("FadingSoul"));
            if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), mod.ItemType("CharcoalPineResin"));
        }

        public override void DrawEffects(ref Color drawColor)
        {
           if (Main.rand.Next(40) == 0)
           {
               int dust = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y + 18), 12, 12, 6, npc.velocity.X * 0f, npc.velocity.Y * 0f, 30, default(Color), 1f);
               Main.dust[dust].noGravity = true;
           }
        }
        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 10; i++)
            {
                int dustType = 5;
                int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (npc.life <= 0)
            {
                for (int i = 0; i < 80; i++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, 54, 2.5f * (float)hitDirection, -1.5f, 70, default(Color), 1f);
                    Dust.NewDust(npc.position, npc.width, npc.height, 5, 1.5f * (float)hitDirection, -2.5f, 50, default(Color), 1f);
                }
            }
        }

        #region AI
        public override void AI()
        {
            bool flag4 = false;
            if (npc.velocity.X == 0f)
            {
                flag4 = true;
            }
            if (npc.justHit)
            {
                flag4 = false;
            }
            if (Main.netMode != 1 && npc.type == 198 && (double)npc.life <= (double)npc.lifeMax * 0.55)
            {
                npc.Transform(199);
            }
            if (Main.netMode != 1 && npc.type == 348 && (double)npc.life <= (double)npc.lifeMax * 0.55)
            {
                npc.Transform(349);
            }
            int num37 = 60;
            bool flag5 = false;
            bool flag6 = true;
            if (npc.type == 343 || npc.type == 47 || npc.type == 67 || npc.type == 109 || npc.type == 110 || npc.type == 111 || npc.type == 120 || npc.type == 163 || npc.type == 164 || npc.type == 239 || npc.type == 168 || npc.type == 199 || npc.type == 206 || npc.type == 214 || npc.type == 215 || npc.type == 216 || npc.type == 217 || npc.type == 218 || npc.type == 219 || npc.type == 220 || npc.type == 226 || npc.type == 243 || npc.type == 251 || npc.type == 257 || npc.type == 258 || npc.type == 290 || npc.type == 291 || npc.type == 292 || npc.type == 293 || npc.type == 305 || npc.type == 306 || npc.type == 307 || npc.type == 308 || npc.type == 309 || npc.type == 348 || npc.type == 349 || npc.type == 350 || npc.type == 351 || npc.type == 379 || (npc.type >= 430 && npc.type <= 436) || (npc.type == 380 || npc.type == 381 || npc.type == 382 || npc.type == 383 || npc.type == 386 || npc.type == 391 || ((npc.type >= 449 && npc.type <= 452) || true)) || (npc.type == 466 || npc.type == 464 || npc.type == 166 || npc.type == 469 || npc.type == 468 || npc.type == 471 || npc.type == 470 || npc.type == 480 || npc.type == 481 || npc.type == 482 || npc.type == 411 || npc.type == 424 || npc.type == 409 || (npc.type >= 494 && npc.type <= 506)) || (npc.type == 425 || npc.type == 427 || npc.type == 426 || npc.type == 428 || npc.type == 508 || npc.type == 415 || npc.type == 419 || npc.type == 520 || (npc.type >= 524 && npc.type <= 527)) || npc.type == 528 || npc.type == 529 || npc.type == 530 || npc.type == 532)
            {
                flag6 = false;
            }
            bool flag7 = false;
            int num43 = npc.type;
            if (num43 == 425 || num43 == 471)
            {
                flag7 = true;
            }
            bool flag8 = true;
            num43 = npc.type;
            if (num43 <= 350)
            {
                if (num43 <= 206)
                {
                    if (num43 - 110 > 1 && num43 != 206)
                    {
                        goto IL_2E53;
                    }
                }
                else if (num43 - 214 > 2 && num43 - 291 > 2 && num43 != 350)
                {
                    goto IL_2E53;
                }
            }
            else if (num43 <= 426)
            {
                if (num43 - 379 > 3)
                {
                    switch (num43)
                    {
                        case 409:
                        case 411:
                            break;
                        case 410:
                            goto IL_2E53;
                        default:
                            switch (num43)
                            {
                                case 424:
                                case 426:
                                    break;
                                case 425:
                                    goto IL_2E53;
                                default:
                                    goto IL_2E53;
                            }
                            break;
                    }
                }
            }
            else if (num43 != 466 && num43 - 498 > 8 && num43 != 520)
            {
                goto IL_2E53;
            }
            if (npc.ai[2] > 0f)
            {
                flag8 = false;
            }
        IL_2E53:
            if (!flag7 & flag8)
            {
                if (npc.velocity.Y == 0f && ((npc.velocity.X > 0f && npc.direction < 0) || (npc.velocity.X < 0f && npc.direction > 0)))
                {
                    flag5 = true;
                }
                if ((npc.position.X == npc.oldPosition.X || npc.ai[3] >= (float)num37) | flag5)
                {
                    npc.ai[3] += 1f;
                }
                else if ((double)Math.Abs(npc.velocity.X) > 0.9 && npc.ai[3] > 0f)
                {
                    npc.ai[3] -= 1f;
                }
                if (npc.ai[3] > (float)(num37 * 10))
                {
                    npc.ai[3] = 0f;
                }
                if (npc.justHit)
                {
                    npc.ai[3] = 0f;
                }
                if (npc.ai[3] == (float)num37)
                {
                    npc.netUpdate = true;
                }
            }

            if (npc.ai[3] < (float)num37 && (Main.eclipse || !Main.dayTime || Main.dayTime || (double)npc.position.Y > Main.worldSurface * 16.0 || (Main.invasionType == 1 && (npc.type == 343 || npc.type == 350)) || (Main.invasionType == 1 && (npc.type == 26 || npc.type == 27 || npc.type == 28 || npc.type == 111 || npc.type == 471)) || (npc.type == 73 || (Main.invasionType == 3 && npc.type >= 212 && npc.type <= 216)) || (Main.invasionType == 4 && (npc.type == 381 || npc.type == 382 || npc.type == 383 || npc.type == 385 || npc.type == 386 || npc.type == 389 || npc.type == 391 || npc.type == 520)) || (npc.type == 31 || npc.type == 294 || npc.type == 295 || npc.type == 296 || npc.type == 47 || npc.type == 67 || npc.type == 77 || npc.type == 78 || npc.type == 79 || npc.type == 80 || npc.type == 110 || npc.type == 120 || npc.type == 168 || npc.type == 181 || npc.type == 185 || npc.type == 198 || npc.type == 199 || npc.type == 206 || npc.type == 217 || npc.type == 218 || npc.type == 219 || npc.type == 220 || npc.type == 239 || npc.type == 243 || npc.type == 254 || npc.type == 255 || npc.type == 257 || npc.type == 258 || npc.type == 291 || npc.type == 292 || npc.type == 293 || npc.type == 379 || npc.type == 380 || npc.type == 464 || npc.type == 470 || npc.type == 424 || (npc.type == 411 && (npc.ai[1] >= 180f || npc.ai[1] < 90f))) || (npc.type == 409 || npc.type == 425 || npc.type == 429 || npc.type == 427 || npc.type == 428 || npc.type == 508 || npc.type == 415 || npc.type == 419 || (npc.type >= 524 && npc.type <= 527)) || npc.type == 528 || npc.type == 529 || npc.type == 530 || npc.type == 532))
            {
                if ((npc.type == 3 || npc.type == 331 || npc.type == 332 || npc.type == 21 || ((npc.type >= 449 && npc.type <= 452) || true) || npc.type == 31 || npc.type == 294 || npc.type == 295 || npc.type == 296 || npc.type == 77 || npc.type == 110 || npc.type == 132 || npc.type == 167 || npc.type == 161 || npc.type == 162 || npc.type == 186 || npc.type == 187 || npc.type == 188 || npc.type == 189 || npc.type == 197 || npc.type == 200 || npc.type == 201 || npc.type == 202 || npc.type == 203 || npc.type == 223 || npc.type == 291 || npc.type == 292 || npc.type == 293 || npc.type == 320 || npc.type == 321 || npc.type == 319 || npc.type == 481) && Main.rand.Next(1000) == 0)
                {
                    Main.PlaySound(14, (int)npc.position.X, (int)npc.position.Y, 1, 1f, 0f);
                }
                if (npc.type == 489 && Main.rand.Next(800) == 0)
                {
                    Main.PlaySound(14, (int)npc.position.X, (int)npc.position.Y, npc.type, 1f, 0f);
                }
                if ((npc.type == 78 || npc.type == 79 || npc.type == 80) && Main.rand.Next(500) == 0)
                {
                    Main.PlaySound(26, (int)npc.position.X, (int)npc.position.Y, 1, 1f, 0f);
                }
                if (npc.type == 159 && Main.rand.Next(500) == 0)
                {
                    Main.PlaySound(29, (int)npc.position.X, (int)npc.position.Y, 7, 1f, 0f);
                }
                if (npc.type == 162 && Main.rand.Next(500) == 0)
                {
                    Main.PlaySound(29, (int)npc.position.X, (int)npc.position.Y, 6, 1f, 0f);
                }
                if (npc.type == 181 && Main.rand.Next(500) == 0)
                {
                    Main.PlaySound(29, (int)npc.position.X, (int)npc.position.Y, 8, 1f, 0f);
                }
                if (npc.type >= 269 && npc.type <= 280 && Main.rand.Next(1000) == 0)
                {
                    Main.PlaySound(14, (int)npc.position.X, (int)npc.position.Y, 1, 1f, 0f);
                }
                npc.TargetClosest(true);
            }
            else if (npc.ai[2] <= 0f || (npc.type != 110 && npc.type != 111 && npc.type != 206 && npc.type != 216 && npc.type != 214 && npc.type != 215 && npc.type != 291 && npc.type != 292 && npc.type != 293 && npc.type != 350 && npc.type != 381 && npc.type != 382 && npc.type != 383 && npc.type != 385 && npc.type != 386 && npc.type != 389 && npc.type != 391 && npc.type != 469 && npc.type != 166 && npc.type != 466 && npc.type != 471 && npc.type != 411 && npc.type != 409 && npc.type != 424 && npc.type != 425 && npc.type != 426 && npc.type != 415 && npc.type != 419 && npc.type != 520))
            {
                /*if (Main.dayTime && (double)(npc.position.Y / 16f) < Main.worldSurface && npc.timeLeft > 10)
                {
                    npc.timeLeft = 10;
                }*/
                if (npc.velocity.X == 0f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.ai[0] += 1f;
                        if (npc.ai[0] >= 2f)
                        {
                            npc.direction *= -1;
                            npc.spriteDirection = npc.direction;
                            npc.ai[0] = 0f;
                        }
                    }
                }
                else
                {
                    npc.ai[0] = 0f;
                }
                if (npc.direction == 0)
                {
                    npc.direction = 1;
                }
            }
            else if (npc.type == 199)
            {
                if (npc.velocity.X < -4f || npc.velocity.X > 4f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < 4f && npc.direction == 1)
                {
                    if (npc.velocity.Y == 0f && npc.velocity.X < 0f)
                    {
                        npc.velocity.X = npc.velocity.X * 0.8f;
                    }
                    npc.velocity.X = npc.velocity.X + 0.1f;
                    if (npc.velocity.X > 4f)
                    {
                        npc.velocity.X = 4f;
                    }
                }
                else if (npc.velocity.X > -4f && npc.direction == -1)
                {
                    if (npc.velocity.Y == 0f && npc.velocity.X > 0f)
                    {
                        npc.velocity.X = npc.velocity.X * 0.8f;
                    }
                    npc.velocity.X = npc.velocity.X - 0.1f;
                    if (npc.velocity.X < -4f)
                    {
                        npc.velocity.X = -4f;
                    }
                }
            }
            else if (npc.type == 120 || npc.type == 166 || npc.type == 213 || npc.type == 258 || npc.type == 528 || npc.type == 529)
            {
                if (npc.velocity.X < -3f || npc.velocity.X > 3f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < 3f && npc.direction == 1)
                {
                    if (npc.velocity.Y == 0f && npc.velocity.X < 0f)
                    {
                        npc.velocity.X = npc.velocity.X * 0.99f;
                    }
                    npc.velocity.X = npc.velocity.X + 0.07f;
                    if (npc.velocity.X > 3f)
                    {
                        npc.velocity.X = 3f;
                    }
                }
                else if (npc.velocity.X > -3f && npc.direction == -1)
                {
                    if (npc.velocity.Y == 0f && npc.velocity.X > 0f)
                    {
                        npc.velocity.X = npc.velocity.X * 0.99f;
                    }
                    npc.velocity.X = npc.velocity.X - 0.07f;
                    if (npc.velocity.X < -3f)
                    {
                        npc.velocity.X = -3f;
                    }
                }
            }
            else if (npc.type == 461 || npc.type == 27 || npc.type == 77 || npc.type == 104 || npc.type == 163 || npc.type == 162 || npc.type == 196 || npc.type == 197 || npc.type == 212 || npc.type == 257 || npc.type == 326 || npc.type == 343 || npc.type == 348 || npc.type == 351 || (npc.type >= 524 && npc.type <= 527) || npc.type == 530)
            {
                if (npc.velocity.X < -2f || npc.velocity.X > 2f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < 2f && npc.direction == 1)
                {
                    npc.velocity.X = npc.velocity.X + 0.07f;
                    if (npc.velocity.X > 2f)
                    {
                        npc.velocity.X = 2f;
                    }
                }
                else if (npc.velocity.X > -2f && npc.direction == -1)
                {
                    npc.velocity.X = npc.velocity.X - 0.07f;
                    if (npc.velocity.X < -2f)
                    {
                        npc.velocity.X = -2f;
                    }
                }
            }
            else if (npc.type == 21 || npc.type == 26 || npc.type == 31 || npc.type == 294 || npc.type == 295 || npc.type == 296 || npc.type == 47 || npc.type == 73 || npc.type == 140 || npc.type == 164 || npc.type == 239 || npc.type == 167 || npc.type == 168 || npc.type == 185 || npc.type == 198 || npc.type == 201 || npc.type == 202 || npc.type == 203 || npc.type == 217 || npc.type == 218 || npc.type == 219 || npc.type == 226 || npc.type == 181 || npc.type == 254 || npc.type == 338 || npc.type == 339 || npc.type == 340 || npc.type == 342 || npc.type == 385 || npc.type == 389 || npc.type == 462 || npc.type == 463 || npc.type == 466 || npc.type == 464 || npc.type == 469 || npc.type == 470 || npc.type == 480 || npc.type == 482 || npc.type == 425 || npc.type == 429)
            {
                float num58 = 1.5f;
                if (npc.type == 294)
                {
                    num58 = 2f;
                }
                else if (npc.type == 295)
                {
                    num58 = 1.75f;
                }
                else if (npc.type == 296)
                {
                    num58 = 1.25f;
                }
                else if (npc.type == 201)
                {
                    num58 = 1.1f;
                }
                else if (npc.type == 202)
                {
                    num58 = 0.9f;
                }
                else if (npc.type == 203)
                {
                    num58 = 1.2f;
                }
                else if (npc.type == 338)
                {
                    num58 = 1.75f;
                }
                else if (npc.type == 339)
                {
                    num58 = 1.25f;
                }
                else if (npc.type == 340)
                {
                    num58 = 2f;
                }
                else if (npc.type == 385)
                {
                    num58 = 1.8f;
                }
                else if (npc.type == 389)
                {
                    num58 = 2.25f;
                }
                else if (npc.type == 462)
                {
                    num58 = 4f;
                }
                else if (npc.type == 463)
                {
                    num58 = 0.75f;
                }
                else if (npc.type == 466)
                {
                    num58 = 3.75f;
                }
                else if (npc.type == 469)
                {
                    num58 = 3.25f;
                }
                else if (npc.type == 480)
                {
                    num58 = 1.5f + (1f - (float)npc.life / (float)npc.lifeMax) * 2f;
                }
                else if (npc.type == 425)
                {
                    num58 = 6f;
                }
                else if (npc.type == 429)
                {
                    num58 = 4f;
                }
                if (npc.type == 21 || npc.type == 201 || npc.type == 202 || npc.type == 203 || npc.type == 342)
                {
                    num58 *= 1f + (1f - npc.scale);
                }
                if (npc.velocity.X < -num58 || npc.velocity.X > num58)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < num58 && npc.direction == 1)
                {
                    if (npc.type == 466 && npc.velocity.X < -2f)
                    {
                        npc.velocity.X = npc.velocity.X * 0.9f;
                    }
                    npc.velocity.X = npc.velocity.X + 0.07f;
                    if (npc.velocity.X > num58)
                    {
                        npc.velocity.X = num58;
                    }
                }
                else if (npc.velocity.X > -num58 && npc.direction == -1)
                {
                    if (npc.type == 466 && npc.velocity.X > 2f)
                    {
                        npc.velocity.X = npc.velocity.X * 0.9f;
                    }
                    npc.velocity.X = npc.velocity.X - 0.07f;
                    if (npc.velocity.X < -num58)
                    {
                        npc.velocity.X = -num58;
                    }
                }
                if (npc.velocity.Y == 0f && npc.type == 462 && ((npc.direction > 0 && npc.velocity.X < 0f) || (npc.direction < 0 && npc.velocity.X > 0f)))
                {
                    npc.velocity.X = npc.velocity.X * 0.9f;
                }
            }
            else if (npc.type == 78 || npc.type == 79 || npc.type == 80)
            {
                float num61 = 1f;
                float num62 = 0.05f;
                if (npc.life < npc.lifeMax / 2)
                {
                    num61 = 2f;
                    num62 = 0.1f;
                }
                if (npc.type == 79)
                {
                    num61 *= 1.5f;
                }
                if (npc.velocity.X < -num61 || npc.velocity.X > num61)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.7f;
                    }
                }
                else if (npc.velocity.X < num61 && npc.direction == 1)
                {
                    npc.velocity.X = npc.velocity.X + num62;
                    if (npc.velocity.X > num61)
                    {
                        npc.velocity.X = num61;
                    }
                }
                else if (npc.velocity.X > -num61 && npc.direction == -1)
                {
                    npc.velocity.X = npc.velocity.X - num62;
                    if (npc.velocity.X < -num61)
                    {
                        npc.velocity.X = -num61;
                    }
                }
            }
            else if (npc.type == 287)
            {
                float num63 = 5f;
                float num64 = 0.2f;
                if (npc.velocity.X < -num63 || npc.velocity.X > num63)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.7f;
                    }
                }
                else if (npc.velocity.X < num63 && npc.direction == 1)
                {
                    npc.velocity.X = npc.velocity.X + num64;
                    if (npc.velocity.X > num63)
                    {
                        npc.velocity.X = num63;
                    }
                }
                else if (npc.velocity.X > -num63 && npc.direction == -1)
                {
                    npc.velocity.X = npc.velocity.X - num64;
                    if (npc.velocity.X < -num63)
                    {
                        npc.velocity.X = -num63;
                    }
                }
            }
            else if (npc.type == 243)
            {
                float num65 = 1f;
                float num66 = 0.07f;
                num65 += (1f - (float)npc.life / (float)npc.lifeMax) * 1.5f;
                num66 += (1f - (float)npc.life / (float)npc.lifeMax) * 0.15f;
                if (npc.velocity.X < -num65 || npc.velocity.X > num65)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.7f;
                    }
                }
                else if (npc.velocity.X < num65 && npc.direction == 1)
                {
                    npc.velocity.X = npc.velocity.X + num66;
                    if (npc.velocity.X > num65)
                    {
                        npc.velocity.X = num65;
                    }
                }
                else if (npc.velocity.X > -num65 && npc.direction == -1)
                {
                    npc.velocity.X = npc.velocity.X - num66;
                    if (npc.velocity.X < -num65)
                    {
                        npc.velocity.X = -num65;
                    }
                }
            }
            else if (npc.type == 251)
            {
                float num67 = 1f;
                float num68 = 0.08f;
                num67 += (1f - (float)npc.life / (float)npc.lifeMax) * 2f;
                num68 += (1f - (float)npc.life / (float)npc.lifeMax) * 0.2f;
                if (npc.velocity.X < -num67 || npc.velocity.X > num67)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.7f;
                    }
                }
                else if (npc.velocity.X < num67 && npc.direction == 1)
                {
                    npc.velocity.X = npc.velocity.X + num68;
                    if (npc.velocity.X > num67)
                    {
                        npc.velocity.X = num67;
                    }
                }
                else if (npc.velocity.X > -num67 && npc.direction == -1)
                {
                    npc.velocity.X = npc.velocity.X - num68;
                    if (npc.velocity.X < -num67)
                    {
                        npc.velocity.X = -num67;
                    }
                }
            }
            else if (npc.type == 386)
            {
                if (npc.ai[2] > 0f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity.X = npc.velocity.X * 0.8f;
                    }
                }
                else
                {
                    float num69 = 0.15f;
                    float num70 = 1.5f;
                    if (npc.velocity.X < -num70 || npc.velocity.X > num70)
                    {
                        if (npc.velocity.Y == 0f)
                        {
                            npc.velocity *= 0.7f;
                        }
                    }
                    else if (npc.velocity.X < num70 && npc.direction == 1)
                    {
                        npc.velocity.X = npc.velocity.X + num69;
                        if (npc.velocity.X > num70)
                        {
                            npc.velocity.X = num70;
                        }
                    }
                    else if (npc.velocity.X > -num70 && npc.direction == -1)
                    {
                        npc.velocity.X = npc.velocity.X - num69;
                        if (npc.velocity.X < -num70)
                        {
                            npc.velocity.X = -num70;
                        }
                    }
                }
            }
            else if (npc.type == 460)
            {
                float num71 = 3f;
                float num72 = 0.1f;
                if (Math.Abs(npc.velocity.X) > 2f)
                {
                    num72 *= 0.8f;
                }
                if ((double)Math.Abs(npc.velocity.X) > 2.5)
                {
                    num72 *= 0.8f;
                }
                if (Math.Abs(npc.velocity.X) > 3f)
                {
                    num72 *= 0.8f;
                }
                if ((double)Math.Abs(npc.velocity.X) > 3.5)
                {
                    num72 *= 0.8f;
                }
                if (Math.Abs(npc.velocity.X) > 4f)
                {
                    num72 *= 0.8f;
                }
                if ((double)Math.Abs(npc.velocity.X) > 4.5)
                {
                    num72 *= 0.8f;
                }
                if (Math.Abs(npc.velocity.X) > 5f)
                {
                    num72 *= 0.8f;
                }
                if ((double)Math.Abs(npc.velocity.X) > 5.5)
                {
                    num72 *= 0.8f;
                }
                num71 += (1f - (float)npc.life / (float)npc.lifeMax) * 3f;
                if (npc.velocity.X < -num71 || npc.velocity.X > num71)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.7f;
                    }
                }
                else if (npc.velocity.X < num71 && npc.direction == 1)
                {
                    if (npc.velocity.X < 0f)
                    {
                        npc.velocity.X = npc.velocity.X * 0.93f;
                    }
                    npc.velocity.X = npc.velocity.X + num72;
                    if (npc.velocity.X > num71)
                    {
                        npc.velocity.X = num71;
                    }
                }
                else if (npc.velocity.X > -num71 && npc.direction == -1)
                {
                    if (npc.velocity.X > 0f)
                    {
                        npc.velocity.X = npc.velocity.X * 0.93f;
                    }
                    npc.velocity.X = npc.velocity.X - num72;
                    if (npc.velocity.X < -num71)
                    {
                        npc.velocity.X = -num71;
                    }
                }
            }
            else if (npc.type == 508)
            {
                float num73 = 2.5f;
                float num74 = 40f;
                float num75 = Math.Abs(npc.velocity.X);
                if (num75 > 2.75f)
                {
                    num73 = 3.5f;
                    num74 += 80f;
                }
                else if ((double)num75 > 2.25)
                {
                    num73 = 3f;
                    num74 += 60f;
                }
                if ((double)Math.Abs(npc.velocity.Y) < 0.5)
                {
                    if (npc.velocity.X > 0f && npc.direction < 0)
                    {
                        npc.velocity *= 0.9f;
                    }
                    if (npc.velocity.X < 0f && npc.direction > 0)
                    {
                        npc.velocity *= 0.9f;
                    }
                }
                if (Math.Abs(npc.velocity.Y) > 0.3f)
                {
                    num74 *= 3f;
                }
                if (npc.velocity.X <= 0f && npc.direction < 0)
                {
                    npc.velocity.X = (npc.velocity.X * num74 - num73) / (num74 + 1f);
                }
                else if (npc.velocity.X >= 0f && npc.direction > 0)
                {
                    npc.velocity.X = (npc.velocity.X * num74 + num73) / (num74 + 1f);
                }
                else if (Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) > 20f && Math.Abs(npc.velocity.Y) <= 0.3f)
                {
                    npc.velocity.X = npc.velocity.X * 0.99f;
                    npc.velocity.X = npc.velocity.X + (float)npc.direction * 0.025f;
                }
            }
            else if (npc.type == 391 || npc.type == 427 || npc.type == 415 || npc.type == 419 || npc.type == 518 || npc.type == 532)
            {
                float num76 = 5f;
                float num77 = 0.25f;
                float scaleFactor5 = 0.7f;
                if (npc.type == 427)
                {
                    num76 = 6f;
                    num77 = 0.2f;
                    scaleFactor5 = 0.8f;
                }
                else if (npc.type == 415)
                {
                    num76 = 4f;
                    num77 = 0.1f;
                    scaleFactor5 = 0.95f;
                }
                else if (npc.type == 419)
                {
                    num76 = 6f;
                    num77 = 0.15f;
                    scaleFactor5 = 0.85f;
                }
                else if (npc.type == 518)
                {
                    num76 = 5f;
                    num77 = 0.1f;
                    scaleFactor5 = 0.95f;
                }
                else if (npc.type == 532)
                {
                    num76 = 5f;
                    num77 = 0.15f;
                    scaleFactor5 = 0.98f;
                }
                if (npc.velocity.X < -num76 || npc.velocity.X > num76)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= scaleFactor5;
                    }
                }
                else if (npc.velocity.X < num76 && npc.direction == 1)
                {
                    npc.velocity.X = npc.velocity.X + num77;
                    if (npc.velocity.X > num76)
                    {
                        npc.velocity.X = num76;
                    }
                }
                else if (npc.velocity.X > -num76 && npc.direction == -1)
                {
                    npc.velocity.X = npc.velocity.X - num77;
                    if (npc.velocity.X < -num76)
                    {
                        npc.velocity.X = -num76;
                    }
                }
            }
            else if ((npc.type >= 430 && npc.type <= 436) || npc.type == 494 || npc.type == 495)
            {
                if (npc.ai[2] == 0f)
                {
                    npc.damage = npc.defDamage;
                    float num78 = 1f;
                    num78 *= 1f + (1f - npc.scale);
                    if (npc.velocity.X < -num78 || npc.velocity.X > num78)
                    {
                        if (npc.velocity.Y == 0f)
                        {
                            npc.velocity *= 0.8f;
                        }
                    }
                    else if (npc.velocity.X < num78 && npc.direction == 1)
                    {
                        npc.velocity.X = npc.velocity.X + 0.07f;
                        if (npc.velocity.X > num78)
                        {
                            npc.velocity.X = num78;
                        }
                    }
                    else if (npc.velocity.X > -num78 && npc.direction == -1)
                    {
                        npc.velocity.X = npc.velocity.X - 0.07f;
                        if (npc.velocity.X < -num78)
                        {
                            npc.velocity.X = -num78;
                        }
                    }
                    if (npc.velocity.Y == 0f && (!Main.dayTime || (double)npc.position.Y > Main.worldSurface * 16.0) && !Main.player[npc.target].dead)
                    {
                        Vector2 vector14 = npc.Center - Main.player[npc.target].Center;
                        int num79 = 50;
                        if (npc.type >= 494 && npc.type <= 495)
                        {
                            num79 = 42;
                        }
                        if (vector14.Length() < (float)num79 && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                        {
                            npc.velocity.X = npc.velocity.X * 0.7f;
                            npc.ai[2] = 1f;
                        }
                    }
                }
                else
                {
                    npc.damage = (int)((double)npc.defDamage * 1.5);
                    npc.ai[3] = 1f;
                    npc.velocity.X = npc.velocity.X * 0.9f;
                    if ((double)Math.Abs(npc.velocity.X) < 0.1)
                    {
                        npc.velocity.X = 0f;
                    }
                    npc.ai[2] += 1f;
                    if (npc.ai[2] >= 20f || npc.velocity.Y != 0f || (Main.dayTime && (double)npc.position.Y < Main.worldSurface * 16.0))
                    {
                        npc.ai[2] = 0f;
                    }
                }
            }
            else if (npc.type != 110 && npc.type != 111 && npc.type != 206 && npc.type != 214 && npc.type != 215 && npc.type != 216 && npc.type != 290 && npc.type != 291 && npc.type != 292 && npc.type != 293 && npc.type != 350 && npc.type != 379 && npc.type != 380 && npc.type != 381 && npc.type != 382 && ((npc.type < 449 || npc.type > 452) || true) && npc.type != 468 && npc.type != 481 && npc.type != 411 && npc.type != 409 && (npc.type < 498 || npc.type > 506) && npc.type != 424 && npc.type != 426 && npc.type != 520)
            {
                float num80 = 1f;
                if (npc.type == 186)
                {
                    num80 = 1.1f;
                }
                if (npc.type == 187)
                {
                    num80 = 0.9f;
                }
                if (npc.type == 188)
                {
                    num80 = 1.2f;
                }
                if (npc.type == 189)
                {
                    num80 = 0.8f;
                }
                if (npc.type == 132)
                {
                    num80 = 0.95f;
                }
                if (npc.type == 200)
                {
                    num80 = 0.87f;
                }
                if (npc.type == 223)
                {
                    num80 = 1.05f;
                }
                if (npc.type == 489)
                {
                    float num81 = (Main.player[npc.target].Center - npc.Center).Length();
                    num81 *= 0.0025f;
                    if ((double)num81 > 1.5)
                    {
                        num81 = 1.5f;
                    }
                    if (Main.expertMode)
                    {
                        num80 = 3f - num81;
                    }
                    else
                    {
                        num80 = 2.5f - num81;
                    }
                    num80 *= 0.8f;
                }
                if (npc.type == 489 || npc.type == 3 || npc.type == 132 || npc.type == 186 || npc.type == 187 || npc.type == 188 || npc.type == 189 || npc.type == 200 || npc.type == 223 || npc.type == 331 || npc.type == 332)
                {
                    num80 *= 1f + (1f - npc.scale);
                }
                if (npc.velocity.X < -num80 || npc.velocity.X > num80)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < num80 && npc.direction == 1)
                {
                    npc.velocity.X = npc.velocity.X + 0.07f;
                    if (npc.velocity.X > num80)
                    {
                        npc.velocity.X = num80;
                    }
                }
                else if (npc.velocity.X > -num80 && npc.direction == -1)
                {
                    npc.velocity.X = npc.velocity.X - 0.07f;
                    if (npc.velocity.X < -num80)
                    {
                        npc.velocity.X = -num80;
                    }
                }
            }
            if (npc.type >= 277 && npc.type <= 280)
            {
                Lighting.AddLight((int)npc.Center.X / 16, (int)npc.Center.Y / 16, 0.2f, 0.1f, 0f);
            }
            else if (npc.type == 520)
            {
                Lighting.AddLight(npc.Top + new Vector2(0f, 20f), 0.3f, 0.3f, 0.7f);
            }
            else if (npc.type == 525)
            {
                Vector3 rgb = new Vector3(0.7f, 1f, 0.2f) * 0.5f;
                Lighting.AddLight(npc.Top + new Vector2(0f, 15f), rgb);
            }
            else if (npc.type == 526)
            {
                Vector3 rgb2 = new Vector3(1f, 1f, 0.5f) * 0.4f;
                Lighting.AddLight(npc.Top + new Vector2(0f, 15f), rgb2);
            }
            else if (npc.type == 527)
            {
                Vector3 rgb3 = new Vector3(0.6f, 0.3f, 1f) * 0.4f;
                Lighting.AddLight(npc.Top + new Vector2(0f, 15f), rgb3);
            }
            else if (npc.type == 415)
            {
                npc.hide = false;
                int num25;
                for (int num82 = 0; num82 < 200; num82 = num25 + 1)
                {
                    if (Main.npc[num82].active && Main.npc[num82].type == 416 && Main.npc[num82].ai[0] == (float)npc.whoAmI)
                    {
                        npc.hide = true;
                        break;
                    }
                    num25 = num82;
                }
            }
            else if (npc.type == 258)
            {
                if (npc.velocity.Y != 0f)
                {
                    npc.TargetClosest(true);
                    npc.spriteDirection = npc.direction;
                    if (Main.player[npc.target].Center.X < npc.position.X && npc.velocity.X > 0f)
                    {
                        npc.velocity.X = npc.velocity.X * 0.95f;
                    }
                    else if (Main.player[npc.target].Center.X > npc.position.X + (float)npc.width && npc.velocity.X < 0f)
                    {
                        npc.velocity.X = npc.velocity.X * 0.95f;
                    }
                    if (Main.player[npc.target].Center.X < npc.position.X && npc.velocity.X > -5f)
                    {
                        npc.velocity.X = npc.velocity.X - 0.1f;
                    }
                    else if (Main.player[npc.target].Center.X > npc.position.X + (float)npc.width && npc.velocity.X < 5f)
                    {
                        npc.velocity.X = npc.velocity.X + 0.1f;
                    }
                }
                else if (Main.player[npc.target].Center.Y + 50f < npc.position.Y && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    npc.velocity.Y = -7f;
                }
            }
            else if (npc.type == 425)
            {
                if (npc.velocity.Y == 0f)
                {
                    npc.ai[2] = 0f;
                }
                if (npc.velocity.Y != 0f && npc.ai[2] == 1f)
                {
                    npc.TargetClosest(true);
                    npc.spriteDirection = -npc.direction;
                    if (Collision.CanHit(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                    {
                        float num83 = Main.player[npc.target].Center.X - (float)(npc.direction * 400) - npc.Center.X;
                        float num84 = Main.player[npc.target].Bottom.Y - npc.Bottom.Y;
                        if (num83 < 0f && npc.velocity.X > 0f)
                        {
                            npc.velocity.X = npc.velocity.X * 0.9f;
                        }
                        else if (num83 > 0f && npc.velocity.X < 0f)
                        {
                            npc.velocity.X = npc.velocity.X * 0.9f;
                        }
                        if (num83 < 0f && npc.velocity.X > -5f)
                        {
                            npc.velocity.X = npc.velocity.X - 0.1f;
                        }
                        else if (num83 > 0f && npc.velocity.X < 5f)
                        {
                            npc.velocity.X = npc.velocity.X + 0.1f;
                        }
                        if (npc.velocity.X > 6f)
                        {
                            npc.velocity.X = 6f;
                        }
                        if (npc.velocity.X < -6f)
                        {
                            npc.velocity.X = -6f;
                        }
                        if (num84 < -20f && npc.velocity.Y > 0f)
                        {
                            npc.velocity.Y = npc.velocity.Y * 0.8f;
                        }
                        else if (num84 > 20f && npc.velocity.Y < 0f)
                        {
                            npc.velocity.Y = npc.velocity.Y * 0.8f;
                        }
                        if (num84 < -20f && npc.velocity.Y > -5f)
                        {
                            npc.velocity.Y = npc.velocity.Y - 0.3f;
                        }
                        else if (num84 > 20f && npc.velocity.Y < 5f)
                        {
                            npc.velocity.Y = npc.velocity.Y + 0.3f;
                        }
                    }
                    if (Main.rand.Next(3) == 0)
                    {
                        Vector2 position = npc.Center + new Vector2((float)(npc.direction * -14), -8f) - Vector2.One * 4f;
                        Vector2 vector15 = new Vector2((float)(npc.direction * -6), 12f) * 0.2f + Utils.RandomVector2(Main.rand, -1f, 1f) * 0.1f;
                        Dust dust11 = Main.dust[Dust.NewDust(position, 8, 8, 229, vector15.X, vector15.Y, 100, Color.Transparent, 1f + Main.rand.NextFloat() * 0.5f)];
                        dust11.noGravity = true;
                        dust11.velocity = vector15;
                        dust11.customData = this;
                    }
                    int num25;
                    for (int num85 = 0; num85 < 200; num85 = num25 + 1)
                    {
                        if (num85 != npc.whoAmI && Main.npc[num85].active && Main.npc[num85].type == npc.type && Math.Abs(npc.position.X - Main.npc[num85].position.X) + Math.Abs(npc.position.Y - Main.npc[num85].position.Y) < (float)npc.width)
                        {
                            if (npc.position.X < Main.npc[num85].position.X)
                            {
                                npc.velocity.X = npc.velocity.X - 0.05f;
                            }
                            else
                            {
                                npc.velocity.X = npc.velocity.X + 0.05f;
                            }
                            if (npc.position.Y < Main.npc[num85].position.Y)
                            {
                                npc.velocity.Y = npc.velocity.Y - 0.05f;
                            }
                            else
                            {
                                npc.velocity.Y = npc.velocity.Y + 0.05f;
                            }
                        }
                        num25 = num85;
                    }
                }
                else if (Main.player[npc.target].Center.Y + 100f < npc.position.Y && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    npc.velocity.Y = -5f;
                    npc.ai[2] = 1f;
                }
                if (Main.netMode != 1)
                {
                    npc.localAI[2] += 1f;
                    if (npc.localAI[2] >= (float)(360 + Main.rand.Next(360)) && npc.Distance(Main.player[npc.target].Center) < 400f && Math.Abs(npc.DirectionTo(Main.player[npc.target].Center).Y) < 0.5f && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                    {
                        npc.localAI[2] = 0f;
                        Vector2 vector16 = npc.Center + new Vector2((float)(npc.direction * 30), 2f);
                        Vector2 vector17 = npc.DirectionTo(Main.player[npc.target].Center) * 7f;
                        if (vector17.HasNaNs())
                        {
                            vector17 = new Vector2((float)(npc.direction * 8), 0f);
                        }
                        int num86 = Main.expertMode ? 50 : 75;
                        int num25;
                        for (int num87 = 0; num87 < 4; num87 = num25 + 1)
                        {
                            Vector2 vector18 = vector17 + Utils.RandomVector2(Main.rand, -0.8f, 0.8f);
                            Projectile.NewProjectile(vector16.X, vector16.Y, vector18.X, vector18.Y, 577, num86, 1f, Main.myPlayer, 0f, 0f);
                            num25 = num87;
                        }
                    }
                }
            }
            else if (npc.type == 427)
            {
                if (npc.velocity.Y == 0f)
                {
                    npc.ai[2] = 0f;
                    npc.rotation = 0f;
                }
                else
                {
                    npc.rotation = npc.velocity.X * 0.1f;
                }
                if (npc.velocity.Y != 0f && npc.ai[2] == 1f)
                {
                    npc.TargetClosest(true);
                    npc.spriteDirection = -npc.direction;
                    if (Collision.CanHit(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                    {
                        float num88 = Main.player[npc.target].Center.X - npc.Center.X;
                        float num89 = Main.player[npc.target].Center.Y - npc.Center.Y;
                        if (num88 < 0f && npc.velocity.X > 0f)
                        {
                            npc.velocity.X = npc.velocity.X * 0.98f;
                        }
                        else if (num88 > 0f && npc.velocity.X < 0f)
                        {
                            npc.velocity.X = npc.velocity.X * 0.98f;
                        }
                        if (num88 < -20f && npc.velocity.X > -6f)
                        {
                            npc.velocity.X = npc.velocity.X - 0.015f;
                        }
                        else if (num88 > 20f && npc.velocity.X < 6f)
                        {
                            npc.velocity.X = npc.velocity.X + 0.015f;
                        }
                        if (npc.velocity.X > 6f)
                        {
                            npc.velocity.X = 6f;
                        }
                        if (npc.velocity.X < -6f)
                        {
                            npc.velocity.X = -6f;
                        }
                        if (num89 < -20f && npc.velocity.Y > 0f)
                        {
                            npc.velocity.Y = npc.velocity.Y * 0.98f;
                        }
                        else if (num89 > 20f && npc.velocity.Y < 0f)
                        {
                            npc.velocity.Y = npc.velocity.Y * 0.98f;
                        }
                        if (num89 < -20f && npc.velocity.Y > -6f)
                        {
                            npc.velocity.Y = npc.velocity.Y - 0.15f;
                        }
                        else if (num89 > 20f && npc.velocity.Y < 6f)
                        {
                            npc.velocity.Y = npc.velocity.Y + 0.15f;
                        }
                    }
                    int num25;
                    for (int num90 = 0; num90 < 200; num90 = num25 + 1)
                    {
                        if (num90 != npc.whoAmI && Main.npc[num90].active && Main.npc[num90].type == npc.type && Math.Abs(npc.position.X - Main.npc[num90].position.X) + Math.Abs(npc.position.Y - Main.npc[num90].position.Y) < (float)npc.width)
                        {
                            if (npc.position.X < Main.npc[num90].position.X)
                            {
                                npc.velocity.X = npc.velocity.X - 0.05f;
                            }
                            else
                            {
                                npc.velocity.X = npc.velocity.X + 0.05f;
                            }
                            if (npc.position.Y < Main.npc[num90].position.Y)
                            {
                                npc.velocity.Y = npc.velocity.Y - 0.05f;
                            }
                            else
                            {
                                npc.velocity.Y = npc.velocity.Y + 0.05f;
                            }
                        }
                        num25 = num90;
                    }
                }
                else if (Main.player[npc.target].Center.Y + 100f < npc.position.Y && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    npc.velocity.Y = -5f;
                    npc.ai[2] = 1f;
                }
            }
            else if (npc.type == 426)
            {
                if (npc.ai[1] > 0f && npc.velocity.Y > 0f)
                {
                    npc.velocity.Y = npc.velocity.Y * 0.85f;
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity.Y = -0.4f;
                    }
                }
                if (npc.velocity.Y != 0f)
                {
                    npc.TargetClosest(true);
                    npc.spriteDirection = npc.direction;
                    if (Collision.CanHit(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                    {
                        float num91 = Main.player[npc.target].Center.X - (float)(npc.direction * 300) - npc.Center.X;
                        if (num91 < 40f && npc.velocity.X > 0f)
                        {
                            npc.velocity.X = npc.velocity.X * 0.98f;
                        }
                        else if (num91 > 40f && npc.velocity.X < 0f)
                        {
                            npc.velocity.X = npc.velocity.X * 0.98f;
                        }
                        if (num91 < 40f && npc.velocity.X > -5f)
                        {
                            npc.velocity.X = npc.velocity.X - 0.2f;
                        }
                        else if (num91 > 40f && npc.velocity.X < 5f)
                        {
                            npc.velocity.X = npc.velocity.X + 0.2f;
                        }
                        if (npc.velocity.X > 6f)
                        {
                            npc.velocity.X = 6f;
                        }
                        if (npc.velocity.X < -6f)
                        {
                            npc.velocity.X = -6f;
                        }
                    }
                }
                else if (Main.player[npc.target].Center.Y + 100f < npc.position.Y && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    npc.velocity.Y = -6f;
                }
                int num25;
                for (int num92 = 0; num92 < 200; num92 = num25 + 1)
                {
                    if (num92 != npc.whoAmI && Main.npc[num92].active && Main.npc[num92].type == npc.type && Math.Abs(npc.position.X - Main.npc[num92].position.X) + Math.Abs(npc.position.Y - Main.npc[num92].position.Y) < (float)npc.width)
                    {
                        if (npc.position.X < Main.npc[num92].position.X)
                        {
                            npc.velocity.X = npc.velocity.X - 0.1f;
                        }
                        else
                        {
                            npc.velocity.X = npc.velocity.X + 0.1f;
                        }
                        if (npc.position.Y < Main.npc[num92].position.Y)
                        {
                            npc.velocity.Y = npc.velocity.Y - 0.1f;
                        }
                        else
                        {
                            npc.velocity.Y = npc.velocity.Y + 0.1f;
                        }
                    }
                    num25 = num92;
                }
                if (Main.rand.Next(6) == 0 && npc.ai[1] <= 20f)
                {
                    Dust dust12 = Main.dust[Dust.NewDust(npc.Center + new Vector2((float)((npc.spriteDirection == 1) ? 8 : -20), -20f), 8, 8, 229, npc.velocity.X, npc.velocity.Y, 100, default(Color), 1f)];
                    dust12.velocity = dust12.velocity / 4f + npc.velocity / 2f;
                    dust12.scale = 0.6f;
                    dust12.noLight = true;
                }
                if (npc.ai[1] >= 57f)
                {
                    int num93 = Utils.SelectRandom<int>(Main.rand, new int[]
                    {
                        161,
                        229
                    });
                    Dust dust13 = Main.dust[Dust.NewDust(npc.Center + new Vector2((float)((npc.spriteDirection == 1) ? 8 : -20), -20f), 8, 8, num93, npc.velocity.X, npc.velocity.Y, 100, default(Color), 1f)];
                    dust13.velocity = dust13.velocity / 4f + npc.DirectionTo(Main.player[npc.target].Top);
                    dust13.scale = 1.2f;
                    dust13.noLight = true;
                }
                if (Main.rand.Next(6) == 0)
                {
                    Dust dust14 = Main.dust[Dust.NewDust(npc.Center, 2, 2, 229, 0f, 0f, 0, default(Color), 1f)];
                    dust14.position = npc.Center + new Vector2((float)((npc.spriteDirection == 1) ? 26 : -26), 24f);
                    dust14.velocity.X = 0f;
                    if (dust14.velocity.Y < 0f)
                    {
                        dust14.velocity.Y = 0f;
                    }
                    dust14.noGravity = true;
                    dust14.scale = 1f;
                    dust14.noLight = true;
                }
            }
            else if (npc.type == 185)
            {
                if (npc.velocity.Y == 0f)
                {
                    npc.rotation = 0f;
                    npc.localAI[0] = 0f;
                }
                else if (npc.localAI[0] == 1f)
                {
                    npc.rotation += npc.velocity.X * 0.05f;
                }
            }
            else if (npc.type == 428)
            {
                if (npc.velocity.Y == 0f)
                {
                    npc.rotation = 0f;
                }
                else
                {
                    npc.rotation += npc.velocity.X * 0.08f;
                }
            }
            if (npc.type == 159 && Main.netMode != 1)
            {
                Vector2 vector19 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                float num94 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector19.X;
                float num95 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector19.Y;
                float num96 = (float)Math.Sqrt((double)(num94 * num94 + num95 * num95));
                if (num96 > 300f)
                {
                    npc.Transform(158);
                }
            }
            if (npc.type == 164 && Main.netMode != 1 && npc.velocity.Y == 0f)
            {
                int num97 = (int)npc.Center.X / 16;
                int num98 = (int)npc.Center.Y / 16;
                bool flag10 = false;
                int num25;
                for (int num99 = num97 - 1; num99 <= num97 + 1; num99 = num25 + 1)
                {
                    for (int num100 = num98 - 1; num100 <= num98 + 1; num100 = num25 + 1)
                    {
                        if (Main.tile[num99, num100].wall > 0)
                        {
                            flag10 = true;
                        }
                        num25 = num100;
                    }
                    num25 = num99;
                }
                if (flag10)
                {
                    npc.Transform(165);
                }
            }
            if (npc.type == 239 && Main.netMode != 1 && npc.velocity.Y == 0f)
            {
                int num101 = (int)npc.Center.X / 16;
                int num102 = (int)npc.Center.Y / 16;
                bool flag11 = false;
                int num25;
                for (int num103 = num101 - 1; num103 <= num101 + 1; num103 = num25 + 1)
                {
                    for (int num104 = num102 - 1; num104 <= num102 + 1; num104 = num25 + 1)
                    {
                        if (Main.tile[num103, num104].wall > 0)
                        {
                            flag11 = true;
                        }
                        num25 = num104;
                    }
                    num25 = num103;
                }
                if (flag11)
                {
                    npc.Transform(240);
                }
            }
            if (npc.type == 530 && Main.netMode != 1 && npc.velocity.Y == 0f)
            {
                int num105 = (int)npc.Center.X / 16;
                int num106 = (int)npc.Center.Y / 16;
                bool flag12 = false;
                int num25;
                for (int num107 = num105 - 1; num107 <= num105 + 1; num107 = num25 + 1)
                {
                    for (int num108 = num106 - 1; num108 <= num106 + 1; num108 = num25 + 1)
                    {
                        if (Main.tile[num107, num108].wall > 0)
                        {
                            flag12 = true;
                        }
                        num25 = num108;
                    }
                    num25 = num107;
                }
                if (flag12)
                {
                    npc.Transform(531);
                }
            }
            if (Main.netMode != 1 && Main.expertMode && npc.target >= 0 && (npc.type == 163 || npc.type == 238) && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
            {
                npc.localAI[0] += 1f;
                if (npc.justHit)
                {
                    npc.localAI[0] -= (float)Main.rand.Next(20, 60);
                    if (npc.localAI[0] < 0f)
                    {
                        npc.localAI[0] = 0f;
                    }
                }
                if (npc.localAI[0] > (float)Main.rand.Next(180, 900))
                {
                    npc.localAI[0] = 0f;
                    Vector2 vector20 = Main.player[npc.target].Center - npc.Center;
                    vector20.Normalize();
                    vector20 *= 8f;
                    Projectile.NewProjectile(npc.Center.X, npc.Center.Y, vector20.X, vector20.Y, 472, 18, 0f, Main.myPlayer, 0f, 0f);
                }
            }
            if (npc.type == 163 && Main.netMode != 1 && npc.velocity.Y == 0f)
            {
                int num109 = (int)npc.Center.X / 16;
                int num110 = (int)npc.Center.Y / 16;
                bool flag13 = false;
                int num25;
                for (int num111 = num109 - 1; num111 <= num109 + 1; num111 = num25 + 1)
                {
                    for (int num112 = num110 - 1; num112 <= num110 + 1; num112 = num25 + 1)
                    {
                        if (Main.tile[num111, num112].wall > 0)
                        {
                            flag13 = true;
                        }
                        num25 = num112;
                    }
                    num25 = num111;
                }
                if (flag13)
                {
                    npc.Transform(238);
                }
            }
            if (npc.type == 236 && Main.netMode != 1 && npc.velocity.Y == 0f)
            {
                int num113 = (int)npc.Center.X / 16;
                int num114 = (int)npc.Center.Y / 16;
                bool flag14 = false;
                int num25;
                for (int num115 = num113 - 1; num115 <= num113 + 1; num115 = num25 + 1)
                {
                    for (int num116 = num114 - 1; num116 <= num114 + 1; num116 = num25 + 1)
                    {
                        if (Main.tile[num115, num116].wall > 0)
                        {
                            flag14 = true;
                        }
                        num25 = num116;
                    }
                    num25 = num115;
                }
                if (flag14)
                {
                    npc.Transform(237);
                }
            }
            if (npc.type == 243)
            {
                if (npc.justHit && Main.rand.Next(3) == 0)
                {
                    npc.ai[2] -= (float)Main.rand.Next(30);
                }
                if (npc.ai[2] < 0f)
                {
                    npc.ai[2] = 0f;
                }
                if (npc.confused)
                {
                    npc.ai[2] = 0f;
                }
                npc.ai[2] += 1f;
                float num117 = (float)Main.rand.Next(30, 900);
                num117 *= (float)npc.life / (float)npc.lifeMax;
                num117 += 30f;
                if (Main.netMode != 1 && npc.ai[2] >= num117 && npc.velocity.Y == 0f && !Main.player[npc.target].dead && !Main.player[npc.target].frozen && ((npc.direction > 0 && npc.Center.X < Main.player[npc.target].Center.X) || (npc.direction < 0 && npc.Center.X > Main.player[npc.target].Center.X)) && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    float num118 = 15f;
                    Vector2 vector21 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + 20f);
                    vector21.X += (float)(10 * npc.direction);
                    float num119 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector21.X;
                    float num120 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector21.Y;
                    num119 += (float)Main.rand.Next(-40, 41);
                    num120 += (float)Main.rand.Next(-40, 41);
                    float num121 = (float)Math.Sqrt((double)(num119 * num119 + num120 * num120));
                    npc.netUpdate = true;
                    num121 = num118 / num121;
                    num119 *= num121;
                    num120 *= num121;
                    int num122 = 32;
                    int num123 = 257;
                    vector21.X += num119 * 3f;
                    vector21.Y += num120 * 3f;
                    Projectile.NewProjectile(vector21.X, vector21.Y, num119, num120, num123, num122, 0f, Main.myPlayer, 0f, 0f);
                    npc.ai[2] = 0f;
                }
            }
            if (npc.type == 251)
            {
                if (npc.justHit)
                {
                    npc.ai[2] -= (float)Main.rand.Next(30);
                }
                if (npc.ai[2] < 0f)
                {
                    npc.ai[2] = 0f;
                }
                if (npc.confused)
                {
                    npc.ai[2] = 0f;
                }
                npc.ai[2] += 1f;
                float num124 = (float)Main.rand.Next(60, 1800);
                num124 *= (float)npc.life / (float)npc.lifeMax;
                num124 += 15f;
                if (Main.netMode != 1 && npc.ai[2] >= num124 && npc.velocity.Y == 0f && !Main.player[npc.target].dead && !Main.player[npc.target].frozen && ((npc.direction > 0 && npc.Center.X < Main.player[npc.target].Center.X) || (npc.direction < 0 && npc.Center.X > Main.player[npc.target].Center.X)) && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                {
                    float num125 = 15f;
                    Vector2 vector22 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + 12f);
                    vector22.X += (float)(6 * npc.direction);
                    float num126 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector22.X;
                    float num127 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector22.Y;
                    num126 += (float)Main.rand.Next(-40, 41);
                    num127 += (float)Main.rand.Next(-30, 0);
                    float num128 = (float)Math.Sqrt((double)(num126 * num126 + num127 * num127));
                    npc.netUpdate = true;
                    num128 = num125 / num128;
                    num126 *= num128;
                    num127 *= num128;
                    int num129 = 30;
                    int num130 = 83;
                    vector22.X += num126 * 3f;
                    vector22.Y += num127 * 3f;
                    Projectile.NewProjectile(vector22.X, vector22.Y, num126, num127, num130, num129, 0f, Main.myPlayer, 0f, 0f);
                    npc.ai[2] = 0f;
                }
            }
            if (npc.type == 386)
            {
                if (npc.confused)
                {
                    npc.ai[2] = -60f;
                }
                else
                {
                    if (npc.ai[2] < 60f)
                    {
                        npc.ai[2] += 1f;
                    }
                    if (npc.ai[2] > 0f && NPC.CountNPCS(387) >= 4 * NPC.CountNPCS(386))
                    {
                        npc.ai[2] = 0f;
                    }
                    if (npc.justHit)
                    {
                        npc.ai[2] = -30f;
                    }
                    if (npc.ai[2] == 30f)
                    {
                        int num131 = (int)npc.position.X / 16;
                        int num132 = (int)npc.position.Y / 16;
                        int num133 = (int)npc.position.X / 16;
                        int num134 = (int)npc.position.Y / 16;
                        int num135 = 5;
                        int num136 = 0;
                        bool flag15 = false;
                        int num137 = 2;
                        int num138 = 0;
                        while (!flag15 && num136 < 100)
                        {
                            int num25 = num136;
                            num136 = num25 + 1;
                            int num139 = Main.rand.Next(num131 - num135, num131 + num135);
                            int num140 = Main.rand.Next(num132 - num135, num132 + num135);
                            for (int num141 = num140; num141 < num132 + num135; num141 = num25 + 1)
                            {
                                if ((num141 < num132 - num137 || num141 > num132 + num137 || num139 < num131 - num137 || num139 > num131 + num137) && (num141 < num134 - num138 || num141 > num134 + num138 || num139 < num133 - num138 || num139 > num133 + num138) && Main.tile[num139, num141].nactive())
                                {
                                    bool flag16 = true;
                                    if (Main.tile[num139, num141 - 1].lava())
                                    {
                                        flag16 = false;
                                    }
                                    if (flag16 && Main.tileSolid[(int)Main.tile[num139, num141].type] && !Collision.SolidTiles(num139 - 1, num139 + 1, num141 - 4, num141 - 1))
                                    {
                                        int num142 = NPC.NewNPC(num139 * 16 - npc.width / 2, num141 * 16, 387, 0, 0f, 0f, 0f, 0f, 255);
                                        Main.npc[num142].position.Y = (float)(num141 * 16 - Main.npc[num142].height);
                                        flag15 = true;
                                        npc.netUpdate = true;
                                        break;
                                    }
                                }
                                num25 = num141;
                            }
                        }
                    }
                    if (npc.ai[2] == 60f)
                    {
                        npc.ai[2] = -120f;
                    }
                }
            }
            if (npc.type == 389)
            {
                if (npc.confused)
                {
                    npc.ai[2] = -60f;
                }
                else
                {
                    if (npc.ai[2] < 20f)
                    {
                        npc.ai[2] += 1f;
                    }
                    if (npc.justHit)
                    {
                        npc.ai[2] = -30f;
                    }
                    if (npc.ai[2] == 20f && Main.netMode != 1)
                    {
                        npc.ai[2] = (float)(-10 + Main.rand.Next(3) * -10);
                        Projectile.NewProjectile(npc.Center.X, npc.Center.Y + 8f, (float)(npc.direction * 6), 0f, 437, 25, 1f, Main.myPlayer, 0f, 0f);
                    }
                }
            }
            if (npc.type == 110 || npc.type == 111 || npc.type == 206 || npc.type == 214 || npc.type == 215 || npc.type == 216 || npc.type == 290 || npc.type == 291 || npc.type == 292 || npc.type == 293 || npc.type == 350 || npc.type == 379 || npc.type == 380 || npc.type == 381 || npc.type == 382 || ((npc.type >= 449 && npc.type <= 452) || true) || (npc.type == 468 || npc.type == 481 || npc.type == 411 || npc.type == 409 || (npc.type >= 498 && npc.type <= 506)) || npc.type == 424 || npc.type == 426 || npc.type == 520)
            {
                bool flag17 = npc.type == 381 || npc.type == 382 || npc.type == 520;
                bool flag18 = npc.type == 426;
                bool flag19 = true;
                int num143 = -1;
                int num144 = -1;
                if (npc.type == 411)
                {
                    flag17 = true;
                    num143 = 90;
                    num144 = 90;
                    if (npc.ai[1] <= 150f)
                    {
                        flag19 = false;
                    }
                }
                if (npc.confused)
                {
                    npc.ai[2] = 0f;
                }
                else
                {
                    if (npc.ai[1] > 0f)
                    {
                        npc.ai[1] -= 1f;
                    }
                    if (npc.justHit)
                    {
                        npc.ai[1] = 30f;
                        npc.ai[2] = 0f;
                    }
                    int num145 = 70;
                    if (npc.type == 379 || npc.type == 380)
                    {
                        num145 = 80;
                    }
                    if (npc.type == 381 || npc.type == 382)
                    {
                        num145 = 80;
                    }
                    if (npc.type == 520)
                    {
                        num145 = 15;
                    }
                    if (npc.type == 350)
                    {
                        num145 = 110;
                    }
                    if (npc.type == 291)
                    {
                        num145 = 200;
                    }
                    if (npc.type == 292)
                    {
                        num145 = 120;
                    }
                    if (npc.type == 293)
                    {
                        num145 = 90;
                    }
                    if (npc.type == 111)
                    {
                        num145 = 180;
                    }
                    if (npc.type == 206)
                    {
                        num145 = 50;
                    }
                    if (npc.type == 481)
                    {
                        num145 = 100;
                    }
                    if (npc.type == 214)
                    {
                        num145 = 40;
                    }
                    if (npc.type == 215)
                    {
                        num145 = 80;
                    }
                    if (npc.type == 290)
                    {
                        num145 = 30;
                    }
                    if (npc.type == 411)
                    {
                        num145 = 300;
                    }
                    if (npc.type == 409)
                    {
                        num145 = 60;
                    }
                    if (npc.type == 424)
                    {
                        num145 = 180;
                    }
                    if (npc.type == 426)
                    {
                        num145 = 60;
                    }
                    bool flag20 = false;
                    if (npc.type == 216)
                    {
                        if (npc.localAI[2] >= 20f)
                        {
                            flag20 = true;
                        }
                        if (flag20)
                        {
                            num145 = 60;
                        }
                        else
                        {
                            num145 = 8;
                        }
                    }
                    int num146 = num145 / 2;
                    if (npc.type == 424)
                    {
                        num146 = num145 - 1;
                    }
                    if (npc.type == 426)
                    {
                        num146 = num145 - 1;
                    }
                    if (npc.ai[2] > 0f)
                    {
                        if (flag19)
                        {
                            npc.TargetClosest(true);
                        }
                        if (npc.ai[1] == (float)num146)
                        {
                            if (npc.type == 216)
                            {
                                npc.localAI[2] += 1f;
                            }
                            float num147 = 11f;
                            if (npc.type == 111)
                            {
                                num147 = 9f;
                            }
                            if (npc.type == 206)
                            {
                                num147 = 7f;
                            }
                            if (npc.type == 290)
                            {
                                num147 = 9f;
                            }
                            if (npc.type == 293)
                            {
                                num147 = 4f;
                            }
                            if (npc.type == 214)
                            {
                                num147 = 14f;
                            }
                            if (npc.type == 215)
                            {
                                num147 = 16f;
                            }
                            if (npc.type == 382)
                            {
                                num147 = 7f;
                            }
                            if (npc.type == 520)
                            {
                                num147 = 8f;
                            }
                            if (npc.type == 409)
                            {
                                num147 = 4f;
                            }
                            if ((npc.type >= 449 && npc.type <= 452) || true)
                            {
                                num147 = 7f;
                            }
                            if (npc.type == 481)
                            {
                                num147 = 8f;
                            }
                            if (npc.type == 468)
                            {
                                num147 = 7.5f;
                            }
                            if (npc.type == 411)
                            {
                                num147 = 1f;
                            }
                            if (npc.type >= 498 && npc.type <= 506)
                            {
                                num147 = 7f;
                            }
                            Vector2 vector23 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                            if (npc.type == 481)
                            {
                                vector23.Y -= 14f;
                            }
                            if (npc.type == 206)
                            {
                                vector23.Y -= 10f;
                            }
                            if (npc.type == 290)
                            {
                                vector23.Y -= 10f;
                            }
                            if (npc.type == 381 || npc.type == 382)
                            {
                                vector23.Y += 6f;
                            }
                            if (npc.type == 520)
                            {
                                vector23.Y = npc.position.Y + 20f;
                            }
                            if (npc.type >= 498 && npc.type <= 506)
                            {
                                vector23.Y -= 8f;
                            }
                            if (npc.type == 426)
                            {
                                vector23 += new Vector2((float)(npc.spriteDirection * 2), -12f);
                            }
                            float num148 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector23.X;
                            float num149 = Math.Abs(num148) * 0.1f;
                            if (npc.type == 291 || npc.type == 292)
                            {
                                num149 = 0f;
                            }
                            if (npc.type == 215)
                            {
                                num149 = Math.Abs(num148) * 0.08f;
                            }
                            if (npc.type == 214 || (npc.type == 216 && !flag20))
                            {
                                num149 = 0f;
                            }
                            if (npc.type == 381 || npc.type == 382 || npc.type == 520)
                            {
                                num149 = 0f;
                            }
                            if ((npc.type >= 449 && npc.type <= 452) || true)
                            {
                                num149 = Math.Abs(num148) * (float)Main.rand.Next(10, 50) * 0.01f;
                            }
                            if (npc.type == 468)
                            {
                                num149 = Math.Abs(num148) * (float)Main.rand.Next(10, 50) * 0.01f;
                            }
                            if (npc.type == 481)
                            {
                                num149 = Math.Abs(num148) * (float)Main.rand.Next(-10, 11) * 0.0035f;
                            }
                            if (npc.type >= 498 && npc.type <= 506)
                            {
                                num149 = Math.Abs(num148) * (float)Main.rand.Next(1, 11) * 0.0025f;
                            }
                            float num150 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector23.Y - num149;
                            if (npc.type == 291)
                            {
                                num148 += (float)Main.rand.Next(-40, 41) * 0.2f;
                                num150 += (float)Main.rand.Next(-40, 41) * 0.2f;
                            }
                            else if (npc.type == 381 || npc.type == 382 || npc.type == 520)
                            {
                                num148 += (float)Main.rand.Next(-100, 101) * 0.4f;
                                num150 += (float)Main.rand.Next(-100, 101) * 0.4f;
                                num148 *= (float)Main.rand.Next(85, 116) * 0.01f;
                                num150 *= (float)Main.rand.Next(85, 116) * 0.01f;
                                if (npc.type == 520)
                                {
                                    num148 += (float)Main.rand.Next(-100, 101) * 0.6f;
                                    num150 += (float)Main.rand.Next(-100, 101) * 0.6f;
                                    num148 *= (float)Main.rand.Next(85, 116) * 0.015f;
                                    num150 *= (float)Main.rand.Next(85, 116) * 0.015f;
                                }
                            }
                            else if (npc.type == 481)
                            {
                                num148 += (float)Main.rand.Next(-40, 41) * 0.4f;
                                num150 += (float)Main.rand.Next(-40, 41) * 0.4f;
                            }
                            else if (npc.type >= 498 && npc.type <= 506)
                            {
                                num148 += (float)Main.rand.Next(-40, 41) * 0.3f;
                                num150 += (float)Main.rand.Next(-40, 41) * 0.3f;
                            }
                            else if (npc.type != 292)
                            {
                                num148 += (float)Main.rand.Next(-40, 41);
                                num150 += (float)Main.rand.Next(-40, 41);
                            }
                            float num151 = (float)Math.Sqrt((double)(num148 * num148 + num150 * num150));
                            npc.netUpdate = true;
                            num151 = num147 / num151;
                            num148 *= num151;
                            num150 *= num151;
                            int num152 = 20;
                            int num153 = 82;
                            if (npc.type == 111)
                            {
                                num152 = 11;
                            }
                            if (npc.type == 206)
                            {
                                num152 = 37;
                            }
                            if (npc.type == 379 || npc.type == 380)
                            {
                                num152 = 40;
                            }
                            if (npc.type == 350)
                            {
                                num152 = 45;
                            }
                            if (npc.type == 468)
                            {
                                num152 = 50;
                            }
                            if (npc.type == 111)
                            {
                                num153 = 81;
                            }
                            if (npc.type == 379 || npc.type == 380)
                            {
                                num153 = 81;
                            }
                            if (npc.type == 381)
                            {
                                num153 = 436;
                                num152 = 24;
                            }
                            if (npc.type == 382)
                            {
                                num153 = 438;
                                num152 = 30;
                            }
                            if (npc.type == 520)
                            {
                                num153 = 592;
                                num152 = 35;
                            }
                            if ((npc.type >= 449 && npc.type <= 452) || true)
                            {
                                num153 = mod.ProjectileType("EnemyFirebomb");
                                num152 = 20;
                            }
                            if (npc.type >= 498 && npc.type <= 506)
                            {
                                num153 = 572;
                                num152 = 14;
                            }
                            if (npc.type == 481)
                            {
                                num153 = 508;
                                num152 = 18;
                            }
                            if (npc.type == 206)
                            {
                                num153 = 177;
                            }
                            if (npc.type == 468)
                            {
                                num153 = 501;
                            }
                            if (npc.type == 411)
                            {
                                num153 = 537;
                                num152 = (Main.expertMode ? 45 : 60);
                            }
                            if (npc.type == 424)
                            {
                                num153 = 573;
                                num152 = (Main.expertMode ? 45 : 60);
                            }
                            if (npc.type == 426)
                            {
                                num153 = 581;
                                num152 = (Main.expertMode ? 45 : 60);
                            }
                            if (npc.type == 291)
                            {
                                num153 = 302;
                                num152 = 100;
                            }
                            if (npc.type == 290)
                            {
                                num153 = 300;
                                num152 = 60;
                            }
                            if (npc.type == 293)
                            {
                                num153 = 303;
                                num152 = 60;
                            }
                            if (npc.type == 214)
                            {
                                num153 = 180;
                                num152 = 25;
                            }
                            if (npc.type == 215)
                            {
                                num153 = 82;
                                num152 = 40;
                            }
                            if (npc.type == 292)
                            {
                                num152 = 50;
                                num153 = 180;
                            }
                            if (npc.type == 216)
                            {
                                num153 = 180;
                                num152 = 30;
                                if (flag20)
                                {
                                    num152 = 100;
                                    num153 = 240;
                                    npc.localAI[2] = 0f;
                                }
                            }
                            vector23.X += num148;
                            vector23.Y += num150;
                            if (Main.expertMode && npc.type == 290)
                            {
                                num152 = (int)((double)num152 * 0.75);
                            }
                            if (Main.expertMode && npc.type >= 381 && npc.type <= 392)
                            {
                                num152 = (int)((double)num152 * 0.8);
                            }
                            if (Main.netMode != 1)
                            {
                                if (npc.type == 292)
                                {
                                    int num25;
                                    for (int num154 = 0; num154 < 4; num154 = num25 + 1)
                                    {
                                        num148 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector23.X;
                                        num150 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector23.Y;
                                        num151 = (float)Math.Sqrt((double)(num148 * num148 + num150 * num150));
                                        num151 = 12f / num151;
                                        num148 += (float)Main.rand.Next(-40, 41);
                                        num150 += (float)Main.rand.Next(-40, 41);
                                        num148 *= num151;
                                        num150 *= num151;
                                        Projectile.NewProjectile(vector23.X, vector23.Y, num148, num150, num153, num152, 0f, Main.myPlayer, 0f, 0f);
                                        num25 = num154;
                                    }
                                }
                                else if (npc.type == 411)
                                {
                                    Projectile.NewProjectile(vector23.X, vector23.Y, num148, num150, num153, num152, 0f, Main.myPlayer, 0f, (float)npc.whoAmI);
                                }
                                else if (npc.type == 424)
                                {
                                    int num25;
                                    for (int num155 = 0; num155 < 4; num155 = num25 + 1)
                                    {
                                        Projectile.NewProjectile(npc.Center.X - (float)(npc.spriteDirection * 4), npc.Center.Y + 6f, (float)(-3 + 2 * num155) * 0.15f, -(float)Main.rand.Next(0, 3) * 0.2f - 0.1f, num153, num152, 0f, Main.myPlayer, 0f, (float)npc.whoAmI);
                                        num25 = num155;
                                    }
                                }
                                else if (npc.type == 409)
                                {
                                    int num156 = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, 410, npc.whoAmI, 0f, 0f, 0f, 0f, 255);
                                    Main.npc[num156].velocity = new Vector2(num148, -6f + num150);
                                }
                                else
                                {
                                    Projectile.NewProjectile(vector23.X, vector23.Y, num148, num150, num153, num152, 0f, Main.myPlayer, 0f, 0f);
                                }
                            }
                            if (Math.Abs(num150) > Math.Abs(num148) * 2f)
                            {
                                if (num150 > 0f)
                                {
                                    npc.ai[2] = 1f;
                                }
                                else
                                {
                                    npc.ai[2] = 5f;
                                }
                            }
                            else if (Math.Abs(num148) > Math.Abs(num150) * 2f)
                            {
                                npc.ai[2] = 3f;
                            }
                            else if (num150 > 0f)
                            {
                                npc.ai[2] = 2f;
                            }
                            else
                            {
                                npc.ai[2] = 4f;
                            }
                        }
                        if ((npc.velocity.Y != 0f && !flag18) || npc.ai[1] <= 0f)
                        {
                            npc.ai[2] = 0f;
                            npc.ai[1] = 0f;
                        }
                        else if (!flag17 || (num143 != -1 && npc.ai[1] >= (float)num143 && npc.ai[1] < (float)(num143 + num144) && (!flag18 || npc.velocity.Y == 0f)))
                        {
                            npc.velocity.X = npc.velocity.X * 0.9f;
                            npc.spriteDirection = npc.direction;
                        }
                    }
                    if (npc.type == 468 && !Main.eclipse)
                    {
                        flag17 = true;
                    }
                    else if ((npc.ai[2] <= 0f | flag17) && (npc.velocity.Y == 0f | flag18) && npc.ai[1] <= 0f && !Main.player[npc.target].dead)
                    {
                        bool flag21 = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);
                        if (npc.type == 520)
                        {
                            flag21 = Collision.CanHitLine(npc.Top + new Vector2(0f, 20f), 0, 0, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);
                        }
                        if (Main.player[npc.target].stealth == 0f && Main.player[npc.target].itemAnimation == 0)
                        {
                            flag21 = false;
                        }
                        if (flag21)
                        {
                            float num157 = 10f;
                            Vector2 vector24 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                            float num158 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector24.X;
                            float num159 = Math.Abs(num158) * 0.1f;
                            float num160 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector24.Y - num159;
                            num158 += (float)Main.rand.Next(-40, 41);
                            num160 += (float)Main.rand.Next(-40, 41);
                            float num161 = (float)Math.Sqrt((double)(num158 * num158 + num160 * num160));
                            float num162 = 700f;
                            if (npc.type == 214)
                            {
                                num162 = 550f;
                            }
                            if (npc.type == 215)
                            {
                                num162 = 800f;
                            }
                            if (npc.type >= 498 && npc.type <= 506)
                            {
                                num162 = 190f;
                            }
                            if ((npc.type >= 449 && npc.type <= 452) || true)
                            {
                                num162 = 200f;
                            }
                            if (npc.type == 481)
                            {
                                num162 = 400f;
                            }
                            if (npc.type == 468)
                            {
                                num162 = 400f;
                            }
                            if (num161 < num162)
                            {
                                npc.netUpdate = true;
                                npc.velocity.X = npc.velocity.X * 0.5f;
                                num161 = num157 / num161;
                                num158 *= num161;
                                num160 *= num161;
                                npc.ai[2] = 3f;
                                npc.ai[1] = (float)num145;
                                if (Math.Abs(num160) > Math.Abs(num158) * 2f)
                                {
                                    if (num160 > 0f)
                                    {
                                        npc.ai[2] = 1f;
                                    }
                                    else
                                    {
                                        npc.ai[2] = 5f;
                                    }
                                }
                                else if (Math.Abs(num158) > Math.Abs(num160) * 2f)
                                {
                                    npc.ai[2] = 3f;
                                }
                                else if (num160 > 0f)
                                {
                                    npc.ai[2] = 2f;
                                }
                                else
                                {
                                    npc.ai[2] = 4f;
                                }
                            }
                        }
                    }
                    if (npc.ai[2] <= 0f || (flag17 && (num143 == -1 || npc.ai[1] < (float)num143 || npc.ai[1] >= (float)(num143 + num144))))
                    {
                        float num163 = 1f;
                        float num164 = 0.07f;
                        float scaleFactor6 = 0.8f;
                        if (npc.type == 214)
                        {
                            num163 = 2f;
                            num164 = 0.09f;
                        }
                        else if (npc.type == 215)
                        {
                            num163 = 1.5f;
                            num164 = 0.08f;
                        }
                        else if (npc.type == 381 || npc.type == 382)
                        {
                            num163 = 2f;
                            num164 = 0.5f;
                        }
                        else if (npc.type == 520)
                        {
                            num163 = 4f;
                            num164 = 1f;
                            scaleFactor6 = 0.7f;
                        }
                        else if (npc.type == 411)
                        {
                            num163 = 2f;
                            num164 = 0.5f;
                        }
                        else if (npc.type == 409)
                        {
                            num163 = 2f;
                            num164 = 0.5f;
                        }
                        bool flag22 = false;
                        if ((npc.type == 381 || npc.type == 382) && Vector2.Distance(npc.Center, Main.player[npc.target].Center) < 300f && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                        {
                            flag22 = true;
                            npc.ai[3] = 0f;
                        }
                        if (npc.type == 520 && Vector2.Distance(npc.Center, Main.player[npc.target].Center) < 400f && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                        {
                            flag22 = true;
                            npc.ai[3] = 0f;
                        }
                        if ((npc.velocity.X < -num163 || npc.velocity.X > num163) | flag22)
                        {
                            if (npc.velocity.Y == 0f)
                            {
                                npc.velocity *= scaleFactor6;
                            }
                        }
                        else if (npc.velocity.X < num163 && npc.direction == 1)
                        {
                            npc.velocity.X = npc.velocity.X + num164;
                            if (npc.velocity.X > num163)
                            {
                                npc.velocity.X = num163;
                            }
                        }
                        else if (npc.velocity.X > -num163 && npc.direction == -1)
                        {
                            npc.velocity.X = npc.velocity.X - num164;
                            if (npc.velocity.X < -num163)
                            {
                                npc.velocity.X = -num163;
                            }
                        }
                    }
                    if (npc.type == 520)
                    {
                        npc.localAI[2] += 1f;
                        if (npc.localAI[2] >= 6f)
                        {
                            npc.localAI[2] = 0f;
                            npc.localAI[3] = Main.player[npc.target].DirectionFrom(npc.Top + new Vector2(0f, 20f)).ToRotation();
                        }
                    }
                }
            }
            if (npc.type == 109 && Main.netMode != 1 && !Main.player[npc.target].dead)
            {
                if (npc.justHit)
                {
                    npc.ai[2] = 0f;
                }
                npc.ai[2] += 1f;
                if (npc.ai[2] > 450f)
                {
                    Vector2 vector25 = new Vector2(npc.position.X + (float)npc.width * 0.5f - (float)(npc.direction * 24), npc.position.Y + 4f);
                    int num165 = 3 * npc.direction;
                    int num166 = -5;
                    int num167 = Projectile.NewProjectile(vector25.X, vector25.Y, (float)num165, (float)num166, 75, 0, 0f, Main.myPlayer, 0f, 0f);
                    Main.projectile[num167].timeLeft = 300;
                    npc.ai[2] = 0f;
                }
            }
            bool flag23 = false;
            if (npc.velocity.Y == 0f)
            {
                int num168 = (int)(npc.position.Y + (float)npc.height + 7f) / 16;
                int num169 = (int)npc.position.X / 16;
                int num170 = (int)(npc.position.X + (float)npc.width) / 16;
                int num25;
                for (int num171 = num169; num171 <= num170; num171 = num25 + 1)
                {
                    if (Main.tile[num171, num168] == null)
                    {
                        return;
                    }
                    if (Main.tile[num171, num168].nactive() && Main.tileSolid[(int)Main.tile[num171, num168].type])
                    {
                        flag23 = true;
                        break;
                    }
                    num25 = num171;
                }
            }
            if (npc.type == 428)
            {
                flag23 = false;
            }
            if (npc.velocity.Y >= 0f)
            {
                int num172 = 0;
                if (npc.velocity.X < 0f)
                {
                    num172 = -1;
                }
                if (npc.velocity.X > 0f)
                {
                    num172 = 1;
                }
                Vector2 position2 = npc.position;
                position2.X += npc.velocity.X;
                int num173 = (int)((position2.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 1) * num172)) / 16f);
                int num174 = (int)((position2.Y + (float)npc.height - 1f) / 16f);
                if (Main.tile[num173, num174] == null)
                {
                    Main.tile[num173, num174] = new Tile();
                }
                if (Main.tile[num173, num174 - 1] == null)
                {
                    Main.tile[num173, num174 - 1] = new Tile();
                }
                if (Main.tile[num173, num174 - 2] == null)
                {
                    Main.tile[num173, num174 - 2] = new Tile();
                }
                if (Main.tile[num173, num174 - 3] == null)
                {
                    Main.tile[num173, num174 - 3] = new Tile();
                }
                if (Main.tile[num173, num174 + 1] == null)
                {
                    Main.tile[num173, num174 + 1] = new Tile();
                }
                if (Main.tile[num173 - num172, num174 - 3] == null)
                {
                    Main.tile[num173 - num172, num174 - 3] = new Tile();
                }
                if ((float)(num173 * 16) < position2.X + (float)npc.width && (float)(num173 * 16 + 16) > position2.X && ((Main.tile[num173, num174].nactive() && !Main.tile[num173, num174].topSlope() && !Main.tile[num173, num174 - 1].topSlope() && Main.tileSolid[(int)Main.tile[num173, num174].type] && !Main.tileSolidTop[(int)Main.tile[num173, num174].type]) || (Main.tile[num173, num174 - 1].halfBrick() && Main.tile[num173, num174 - 1].nactive())) && (!Main.tile[num173, num174 - 1].nactive() || !Main.tileSolid[(int)Main.tile[num173, num174 - 1].type] || Main.tileSolidTop[(int)Main.tile[num173, num174 - 1].type] || (Main.tile[num173, num174 - 1].halfBrick() && (!Main.tile[num173, num174 - 4].nactive() || !Main.tileSolid[(int)Main.tile[num173, num174 - 4].type] || Main.tileSolidTop[(int)Main.tile[num173, num174 - 4].type]))) && (!Main.tile[num173, num174 - 2].nactive() || !Main.tileSolid[(int)Main.tile[num173, num174 - 2].type] || Main.tileSolidTop[(int)Main.tile[num173, num174 - 2].type]) && (!Main.tile[num173, num174 - 3].nactive() || !Main.tileSolid[(int)Main.tile[num173, num174 - 3].type] || Main.tileSolidTop[(int)Main.tile[num173, num174 - 3].type]) && (!Main.tile[num173 - num172, num174 - 3].nactive() || !Main.tileSolid[(int)Main.tile[num173 - num172, num174 - 3].type]))
                {
                    float num175 = (float)(num174 * 16);
                    if (Main.tile[num173, num174].halfBrick())
                    {
                        num175 += 8f;
                    }
                    if (Main.tile[num173, num174 - 1].halfBrick())
                    {
                        num175 -= 8f;
                    }
                    if (num175 < position2.Y + (float)npc.height)
                    {
                        float num176 = position2.Y + (float)npc.height - num175;
                        float num177 = 16.1f;
                        if (npc.type == 163 || npc.type == 164 || npc.type == 236 || npc.type == 239 || npc.type == 530)
                        {
                            num177 += 8f;
                        }
                        if (num176 <= num177)
                        {
                            npc.gfxOffY += npc.position.Y + (float)npc.height - num175;
                            npc.position.Y = num175 - (float)npc.height;
                            if (num176 < 9f)
                            {
                                npc.stepSpeed = 1f;
                            }
                            else
                            {
                                npc.stepSpeed = 2f;
                            }
                        }
                    }
                }
            }
            if (flag23)
            {
                int num178 = (int)((npc.position.X + (float)(npc.width / 2) + (float)(15 * npc.direction)) / 16f);
                int num179 = (int)((npc.position.Y + (float)npc.height - 15f) / 16f);
                if (npc.type == 109 || npc.type == 163 || npc.type == 164 || npc.type == 199 || npc.type == 236 || npc.type == 239 || npc.type == 257 || npc.type == 258 || npc.type == 290 || npc.type == 391 || npc.type == 425 || npc.type == 427 || npc.type == 426 || npc.type == 508 || npc.type == 415 || npc.type == 530 || npc.type == 532)
                {
                    num178 = (int)((npc.position.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 16) * npc.direction)) / 16f);
                }
                if (Main.tile[num178, num179] == null)
                {
                    Main.tile[num178, num179] = new Tile();
                }
                if (Main.tile[num178, num179 - 1] == null)
                {
                    Main.tile[num178, num179 - 1] = new Tile();
                }
                if (Main.tile[num178, num179 - 2] == null)
                {
                    Main.tile[num178, num179 - 2] = new Tile();
                }
                if (Main.tile[num178, num179 - 3] == null)
                {
                    Main.tile[num178, num179 - 3] = new Tile();
                }
                if (Main.tile[num178, num179 + 1] == null)
                {
                    Main.tile[num178, num179 + 1] = new Tile();
                }
                if (Main.tile[num178 + npc.direction, num179 - 1] == null)
                {
                    Main.tile[num178 + npc.direction, num179 - 1] = new Tile();
                }
                if (Main.tile[num178 + npc.direction, num179 + 1] == null)
                {
                    Main.tile[num178 + npc.direction, num179 + 1] = new Tile();
                }
                if (Main.tile[num178 - npc.direction, num179 + 1] == null)
                {
                    Main.tile[num178 - npc.direction, num179 + 1] = new Tile();
                }
                Main.tile[num178, num179 + 1].halfBrick();
                if ((Main.tile[num178, num179 - 1].nactive() && (TileLoader.IsClosedDoor(Main.tile[num178, num179 - 1]) || Main.tile[num178, num179 - 1].type == 388)) & flag6)
                {
                    npc.ai[2] += 1f;
                    npc.ai[3] = 0f;
                    if (npc.ai[2] >= 60f)
                    {
                        if (!Main.bloodMoon && (npc.type == 3 || npc.type == 331 || npc.type == 332 || npc.type == 132 || npc.type == 161 || npc.type == 186 || npc.type == 187 || npc.type == 188 || npc.type == 189 || npc.type == 200 || npc.type == 223 || npc.type == 320 || npc.type == 321 || npc.type == 319))
                        {
                            npc.ai[1] = 0f;
                        }
                        npc.velocity.X = 0.5f * -(float)npc.direction;
                        int num180 = 5;
                        if (Main.tile[num178, num179 - 1].type == 388)
                        {
                            num180 = 2;
                        }
                        npc.ai[1] += (float)num180;
                        if (npc.type == 27)
                        {
                            npc.ai[1] += 1f;
                        }
                        if (npc.type == 31 || npc.type == 294 || npc.type == 295 || npc.type == 296)
                        {
                            npc.ai[1] += 6f;
                        }
                        npc.ai[2] = 0f;
                        bool flag24 = false;
                        if (npc.ai[1] >= 10f)
                        {
                            flag24 = true;
                            npc.ai[1] = 10f;
                        }
                        if (npc.type == 460)
                        {
                            flag24 = true;
                        }
                        WorldGen.KillTile(num178, num179 - 1, true, false, false);
                        if (((Main.netMode != 1 || !flag24) & flag24) && Main.netMode != 1)
                        {
                            if (npc.type == 26)
                            {
                                WorldGen.KillTile(num178, num179 - 1, false, false, false);
                                if (Main.netMode == 2)
                                {
                                    NetMessage.SendData(17, -1, -1, null, 0, (float)num178, (float)(num179 - 1), 0f, 0, 0, 0);
                                }
                            }
                            else
                            {
                                if (TileLoader.OpenDoorID(Main.tile[num178, num179 - 1]) >= 0)
                                {
                                    bool flag25 = WorldGen.OpenDoor(num178, num179 - 1, npc.direction);
                                    if (!flag25)
                                    {
                                        npc.ai[3] = (float)num37;
                                        npc.netUpdate = true;
                                    }
                                    if (Main.netMode == 2 & flag25)
                                    {
                                        NetMessage.SendData(19, -1, -1, null, 0, (float)num178, (float)(num179 - 1), (float)npc.direction, 0, 0, 0);
                                    }
                                }
                                if (Main.tile[num178, num179 - 1].type == 388)
                                {
                                    bool flag26 = WorldGen.ShiftTallGate(num178, num179 - 1, false);
                                    if (!flag26)
                                    {
                                        npc.ai[3] = (float)num37;
                                        npc.netUpdate = true;
                                    }
                                    if (Main.netMode == 2 & flag26)
                                    {
                                        NetMessage.SendData(19, -1, -1, null, 4, (float)num178, (float)(num179 - 1), 0f, 0, 0, 0);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    int num181 = npc.spriteDirection;
                    if (npc.type == 425)
                    {
                        num181 *= -1;
                    }
                    if ((npc.velocity.X < 0f && num181 == -1) || (npc.velocity.X > 0f && num181 == 1))
                    {
                        if (npc.height >= 32 && Main.tile[num178, num179 - 2].nactive() && Main.tileSolid[(int)Main.tile[num178, num179 - 2].type])
                        {
                            if (Main.tile[num178, num179 - 3].nactive() && Main.tileSolid[(int)Main.tile[num178, num179 - 3].type])
                            {
                                npc.velocity.Y = -8f;
                                npc.netUpdate = true;
                            }
                            else
                            {
                                npc.velocity.Y = -7f;
                                npc.netUpdate = true;
                            }
                        }
                        else if (Main.tile[num178, num179 - 1].nactive() && Main.tileSolid[(int)Main.tile[num178, num179 - 1].type])
                        {
                            npc.velocity.Y = -6f;
                            npc.netUpdate = true;
                        }
                        else if (npc.position.Y + (float)npc.height - (float)(num179 * 16) > 20f && Main.tile[num178, num179].nactive() && !Main.tile[num178, num179].topSlope() && Main.tileSolid[(int)Main.tile[num178, num179].type])
                        {
                            npc.velocity.Y = -5f;
                            npc.netUpdate = true;
                        }
                        else if (npc.directionY < 0 && npc.type != 67 && (!Main.tile[num178, num179 + 1].nactive() || !Main.tileSolid[(int)Main.tile[num178, num179 + 1].type]) && (!Main.tile[num178 + npc.direction, num179 + 1].nactive() || !Main.tileSolid[(int)Main.tile[num178 + npc.direction, num179 + 1].type]))
                        {
                            npc.velocity.Y = -8f;
                            npc.velocity.X = npc.velocity.X * 1.5f;
                            npc.netUpdate = true;
                        }
                        else if (flag6)
                        {
                            npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                        }
                        if ((npc.velocity.Y == 0f & flag4) && npc.ai[3] == 1f)
                        {
                            npc.velocity.Y = -5f;
                        }
                    }
                    if ((npc.type == 31 || npc.type == 294 || npc.type == 295 || npc.type == 296 || npc.type == 47 || npc.type == 77 || npc.type == 104 || npc.type == 168 || npc.type == 196 || npc.type == 385 || npc.type == 389 || npc.type == 464 || npc.type == 470 || (npc.type >= 524 && npc.type <= 527)) && npc.velocity.Y == 0f && Math.Abs(npc.position.X + (float)(npc.width / 2) - (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2))) < 100f && Math.Abs(npc.position.Y + (float)(npc.height / 2) - (Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2))) < 50f && ((npc.direction > 0 && npc.velocity.X >= 1f) || (npc.direction < 0 && npc.velocity.X <= -1f)))
                    {
                        npc.velocity.X = npc.velocity.X * 2f;
                        if (npc.velocity.X > 3f)
                        {
                            npc.velocity.X = 3f;
                        }
                        if (npc.velocity.X < -3f)
                        {
                            npc.velocity.X = -3f;
                        }
                        npc.velocity.Y = -4f;
                        npc.netUpdate = true;
                    }
                    if (npc.type == 120 && npc.velocity.Y < 0f)
                    {
                        npc.velocity.Y = npc.velocity.Y * 1.1f;
                    }
                    if (npc.type == 287 && npc.velocity.Y == 0f && Math.Abs(npc.position.X + (float)(npc.width / 2) - (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2))) < 150f && Math.Abs(npc.position.Y + (float)(npc.height / 2) - (Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2))) < 50f && ((npc.direction > 0 && npc.velocity.X >= 1f) || (npc.direction < 0 && npc.velocity.X <= -1f)))
                    {
                        npc.velocity.X = (float)(8 * npc.direction);
                        npc.velocity.Y = -4f;
                        npc.netUpdate = true;
                    }
                    if (npc.type == 287 && npc.velocity.Y < 0f)
                    {
                        npc.velocity.X = npc.velocity.X * 1.2f;
                        npc.velocity.Y = npc.velocity.Y * 1.1f;
                    }
                    if (npc.type == 460 && npc.velocity.Y < 0f)
                    {
                        npc.velocity.X = npc.velocity.X * 1.3f;
                        npc.velocity.Y = npc.velocity.Y * 1.1f;
                    }
                }
            }
            else if (flag6)
            {
                npc.ai[1] = 0f;
                npc.ai[2] = 0f;
            }
            if (Main.netMode != 1 && npc.type == 120 && npc.ai[3] >= (float)num37)
            {
                int num182 = (int)Main.player[npc.target].position.X / 16;
                int num183 = (int)Main.player[npc.target].position.Y / 16;
                int num184 = (int)npc.position.X / 16;
                int num185 = (int)npc.position.Y / 16;
                int num186 = 20;
                int num187 = 0;
                bool flag27 = false;
                if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f)
                {
                    num187 = 100;
                    flag27 = true;
                }
                while (!flag27 && num187 < 100)
                {
                    int num25 = num187;
                    num187 = num25 + 1;
                    int num188 = Main.rand.Next(num182 - num186, num182 + num186);
                    int num189 = Main.rand.Next(num183 - num186, num183 + num186);
                    for (int num190 = num189; num190 < num183 + num186; num190 = num25 + 1)
                    {
                        if ((num190 < num183 - 4 || num190 > num183 + 4 || num188 < num182 - 4 || num188 > num182 + 4) && (num190 < num185 - 1 || num190 > num185 + 1 || num188 < num184 - 1 || num188 > num184 + 1) && Main.tile[num188, num190].nactive())
                        {
                            bool flag28 = true;
                            if (npc.type == 32 && Main.tile[num188, num190 - 1].wall == 0)
                            {
                                flag28 = false;
                            }
                            else if (Main.tile[num188, num190 - 1].lava())
                            {
                                flag28 = false;
                            }
                            if (flag28 && Main.tileSolid[(int)Main.tile[num188, num190].type] && !Collision.SolidTiles(num188 - 1, num188 + 1, num190 - 4, num190 - 1))
                            {
                                npc.position.X = (float)(num188 * 16 - npc.width / 2);
                                npc.position.Y = (float)(num190 * 16 - npc.height);
                                npc.netUpdate = true;
                                npc.ai[3] = -120f;
                            }
                        }
                        num25 = num190;
                    }
                }
            }
        }
#endregion

    }
}