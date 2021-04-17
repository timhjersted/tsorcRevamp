using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Potions.PermanentPotions;
using tsorcRevamp.Buffs;
using System;

namespace tsorcRevamp {
    public class tsorcRevampPlayer : ModPlayer {

        public int warpX;
        public int warpY;
        public int warpWorld;
        public bool warpSet;

        public int townWarpX;
        public int townWarpY;
        public int townWarpWorld;
        public bool townWarpSet;

        public bool SilverSerpentRing = false;
        public bool DragonStone = false;
        public int SoulReaper = 0;

        public bool DuskCrownRing = false;
        public bool UndeadTalisman = false;

        public bool DragoonBoots = false;
        public bool DragoonBootsEnable = false;

        public bool GemBox = false;
        public bool ConditionOverload = true;

        public int CurseLevel = 1;
        public bool DarkInferno = false;
        public bool CrimsonDrain = false;

        public bool Shockwave = false;
        public bool Falling;
        public int StopFalling;
        public float FallDist;
        public float fallStartY;

        public bool MeleeArmorVamp10 = false;
        public bool NUVamp = false;

        public bool OldWeapon = false;

        public bool Miakoda = false;
        public bool RTQ2 = false;

        public bool BoneRevenge = false;
        public bool SoulSiphon = false;

        public int souldroplooptimer = 0;
        public int souldroptimer = 0;

        public bool[] PermanentBuffToggles = new bool[52]; //todo dont forget to increment this if you add buffs to the dictionary

        public override TagCompound Save() {
            return new TagCompound {
            {"warpX", warpX},
            {"warpY", warpY},
            {"warpWorld", warpWorld},
            {"warpSet", warpSet},
            {"townWarpX", townWarpX},
            {"townWarpY", townWarpY},
            {"townWarpWorld", townWarpWorld},
            {"townWarpSet", townWarpSet},
            };

        }

        public override void Load(TagCompound tag) {
            warpX = tag.GetInt("warpX");
            warpY = tag.GetInt("warpY");
            warpWorld = tag.GetInt("warpWorld");
            warpSet = tag.GetBool("warpSet");
            townWarpX = tag.GetInt("townWarpX");
            townWarpY = tag.GetInt("townWarpY");
            townWarpWorld = tag.GetInt("townWarpWorld");
            townWarpSet = tag.GetBool("townWarpSet");
        }

        public override void ResetEffects() {
            SilverSerpentRing = false;
            DragonStone = false;
            SoulReaper = 0;
            DragoonBoots = false;
            player.eocDash = 0;
            player.armorEffectDrawShadowEOCShield = false;
            UndeadTalisman = false;
            DuskCrownRing = false;
            DragoonBoots = false;
            GemBox = false;
            OldWeapon = false;
            Miakoda = false;
            RTQ2 = false;
            DarkInferno = false;
            BoneRevenge = false;
            SoulSiphon = false;
            CrimsonDrain = false;
            Shockwave = false;
            souldroplooptimer = 0;
            souldroptimer = 0;
        }

        public override void PostUpdateEquips() {
            if (Main.mouseItem.type == ModContent.ItemType<DarkSoul>()) {
                player.chest = -1;
            }


            #region Permanent Potions

            foreach (Item item in player.inventory) {
                if (item.type == ModContent.ItemType<PermanentObsidianSkinPotion>() && PermanentBuffToggles[0]) {
                    player.lavaImmune = true;
                    player.fireWalk = true;
                    player.buffImmune[BuffID.OnFire] = true;
                    player.buffImmune[BuffID.ObsidianSkin] = true;
                }
                if (item.type == ModContent.ItemType<PermanentRegenerationPotion>() && PermanentBuffToggles[1]) {
                    player.lifeRegen += 4;
                    player.buffImmune[BuffID.Regeneration] = true;
                }
                if (item.type == ModContent.ItemType<PermanentSwiftnessPotion>() && PermanentBuffToggles[2]) {
                    player.moveSpeed += 0.25f;
                    player.buffImmune[BuffID.Swiftness] = true;
                }
                if (item.type == ModContent.ItemType<PermanentGillsPotion>() && PermanentBuffToggles[3]) {
                    player.gills = true;
                    player.buffImmune[BuffID.Gills] = true;
                }
                if (item.type == ModContent.ItemType<PermanentIronskinPotion>() && PermanentBuffToggles[4]) {
                    player.statDefense += 8;
                    player.buffImmune[BuffID.Ironskin] = true;
                }
                if (item.type == ModContent.ItemType<PermanentManaRegenerationPotion>() && PermanentBuffToggles[5]) {
                    player.manaRegenBuff = true;
                    player.buffImmune[BuffID.ManaRegeneration] = true;
                }
                if (item.type == ModContent.ItemType<PermanentMagicPowerPotion>() && PermanentBuffToggles[6]) {
                    player.magicDamage += 0.2f;
                    player.buffImmune[BuffID.MagicPower] = true;
                }
                if (item.type == ModContent.ItemType<PermanentFeatherfallPotion>() && PermanentBuffToggles[7]) {
                    player.slowFall = true;
                    player.buffImmune[BuffID.Featherfall] = true;
                }
                if (item.type == ModContent.ItemType<PermanentSpelunkerPotion>() && PermanentBuffToggles[8]) {
                    player.findTreasure = true;
                    player.buffImmune[BuffID.Spelunker] = true;
                }
                if (item.type == ModContent.ItemType<PermanentInvisibilityPotion>() && PermanentBuffToggles[9]) {
                    player.invis = true;
                    player.buffImmune[BuffID.Invisibility] = true;
                }
                if (item.type == ModContent.ItemType<PermanentShinePotion>() && PermanentBuffToggles[10]) {
                    Lighting.AddLight((int)(player.Center.X / 16), (int)(player.Center.Y / 16), 0.8f, 0.95f, 1f);
                    player.buffImmune[BuffID.Invisibility] = true;
                }
                if (item.type == ModContent.ItemType<PermanentNightOwlPotion>() && PermanentBuffToggles[11]) {
                    player.nightVision = true;
                    player.buffImmune[BuffID.NightOwl] = true;
                }
                if (item.type == ModContent.ItemType<PermanentBattlePotion>() && PermanentBuffToggles[12]) {
                    player.enemySpawns = true;
                    player.buffImmune[BuffID.Battle] = true;
                }
                if (item.type == ModContent.ItemType<PermanentThornsPotion>() && PermanentBuffToggles[13]) {
                    player.thorns += 1f;
                    player.buffImmune[BuffID.Thorns] = true;
                }
                if (item.type == ModContent.ItemType<PermanentWaterWalkingPotion>() && PermanentBuffToggles[14]) {
                    player.waterWalk = true;
                    player.buffImmune[BuffID.WaterWalking] = true;
                }
                if (item.type == ModContent.ItemType<PermanentArcheryPotion>() && PermanentBuffToggles[15]) {
                    player.archery = true;
                    player.buffImmune[BuffID.Archery] = true;
                }
                if (item.type == ModContent.ItemType<PermanentHunterPotion>() && PermanentBuffToggles[16]) {
                    player.detectCreature = true;
                    player.buffImmune[BuffID.Hunter] = true;
                }
                if (item.type == ModContent.ItemType<PermanentGravitationPotion>() && PermanentBuffToggles[17]) {
                    player.gravControl = true;
                    player.buffImmune[BuffID.Gravitation] = true;
                }
                if (item.type == ModContent.ItemType<PermanentAle>() && PermanentBuffToggles[18]) {
                    player.statDefense -= 4;
                    player.meleeDamage += 0.1f;
                    player.meleeCrit += 2;
                    player.meleeSpeed += 0.1f;
                    player.buffImmune[BuffID.Tipsy] = true;
                }
                if (item.type == ModContent.ItemType<PermanentFlaskOfVenom>() && PermanentBuffToggles[19]) {
                    player.meleeEnchant = 1;
                    player.buffImmune[BuffID.WeaponImbueVenom] = true;
                }
                if (item.type == ModContent.ItemType<PermanentFlaskOfCursedFlames>() && PermanentBuffToggles[20]) {
                    player.meleeEnchant = 2;
                    player.buffImmune[BuffID.WeaponImbueCursedFlames] = true;
                }
                if (item.type == ModContent.ItemType<PermanentFlaskOfFire>() && PermanentBuffToggles[21]) {
                    player.meleeEnchant = 3;
                    player.buffImmune[BuffID.WeaponImbueFire] = true;
                }
                if (item.type == ModContent.ItemType<PermanentFlaskOfGold>() && PermanentBuffToggles[22]) {
                    player.meleeEnchant = 4;
                    player.buffImmune[BuffID.WeaponImbueGold] = true;
                }
                if (item.type == ModContent.ItemType<PermanentFlaskOfIchor>() && PermanentBuffToggles[23]) {
                    player.meleeEnchant = 5;
                    player.buffImmune[BuffID.WeaponImbueIchor] = true;
                }
                if (item.type == ModContent.ItemType<PermanentFlaskOfNanites>() && PermanentBuffToggles[24]) {
                    player.meleeEnchant = 6;
                    player.buffImmune[BuffID.WeaponImbueNanites] = true;
                }
                if (item.type == ModContent.ItemType<PermanentFlaskOfParty>() && PermanentBuffToggles[25]) {
                    player.meleeEnchant = 7;
                    player.buffImmune[BuffID.WeaponImbueConfetti] = true;
                }
                if (item.type == ModContent.ItemType<PermanentFlaskOfPoison>() && PermanentBuffToggles[26]) {
                    player.meleeEnchant = 8;
                    player.buffImmune[BuffID.WeaponImbuePoison] = true;
                }
                if (item.type == ModContent.ItemType<PermanentMiningPotion>() && PermanentBuffToggles[27]) {
                    player.pickSpeed -= 0.25f;
                    player.buffImmune[BuffID.Mining] = true;
                }
                if (item.type == ModContent.ItemType<PermanentHeartreachPotion>() && PermanentBuffToggles[28]) {
                    player.lifeMagnet = true;
                    player.buffImmune[BuffID.Heartreach] = true;
                }
                if (item.type == ModContent.ItemType<PermanentCalmingPotion>() && PermanentBuffToggles[29]) {
                    player.calmed = true;
                    player.buffImmune[BuffID.Calm] = true;
                }
                if (item.type == ModContent.ItemType<PermanentBuilderPotion>() && PermanentBuffToggles[30]) {
                    player.tileSpeed += 0.25f;
                    player.wallSpeed += 0.25f;
                    player.blockRange++;
                    player.buffImmune[BuffID.Builder] = true;
                }
                if (item.type == ModContent.ItemType<PermanentTitanPotion>() && PermanentBuffToggles[31]) {
                    player.kbBuff = true;
                    player.buffImmune[BuffID.Titan] = true;
                }
                if (item.type == ModContent.ItemType<PermanentFlipperPotion>() && PermanentBuffToggles[32]) {
                    player.accFlipper = true;
                    player.ignoreWater = true;
                    player.buffImmune[BuffID.Flipper] = true;
                }
                if (item.type == ModContent.ItemType<PermanentSummoningPotion>() && PermanentBuffToggles[33]) {
                    player.maxMinions++;
                    player.buffImmune[BuffID.Summoning] = true;
                }
                if (item.type == ModContent.ItemType<PermanentDangersensePotion>() && PermanentBuffToggles[34]) {
                    player.dangerSense = true;
                    player.buffImmune[BuffID.Dangersense] = true;
                }
                if (item.type == ModContent.ItemType<PermanentAmmoReservationPotion>() && PermanentBuffToggles[35]) {
                    player.ammoPotion = true;
                    player.buffImmune[BuffID.AmmoReservation] = true;
                }
                if (item.type == ModContent.ItemType<PermanentLifeforcePotion>() && PermanentBuffToggles[36]) {
                    player.lifeForce = true;
                    player.statLifeMax2 += player.statLifeMax / 5 / 20 * 20;
                    player.buffImmune[BuffID.Lifeforce] = true;
                }
                if (item.type == ModContent.ItemType<PermanentEndurancePotion>() && PermanentBuffToggles[37]) {
                    player.endurance += 0.1f;
                    player.buffImmune[BuffID.Endurance] = true;
                }
                if (item.type == ModContent.ItemType<PermanentRagePotion>() && PermanentBuffToggles[38]) {
                    player.magicCrit += 10;
                    player.meleeCrit += 10;
                    player.rangedCrit += 10;
                    player.thrownCrit += 10;
                    player.buffImmune[BuffID.Rage] = true;
                }
                if (item.type == ModContent.ItemType<PermanentInfernoPotion>() && PermanentBuffToggles[39]) {
                    player.buffImmune[BuffID.Inferno] = true;
                    player.inferno = true;
                    Lighting.AddLight((int)(player.Center.X / 16f), (int)(player.Center.Y / 16f), 0.65f, 0.4f, 0.1f);
                    int num = 24;
                    float num12 = 200f;
                    bool flag = player.infernoCounter % 60 == 0;
                    int damage = 10;
                    if (player.whoAmI == Main.myPlayer) {
                        for (int l = 0; l < 200; l++) {
                            NPC nPC = Main.npc[l];
                            if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[num] && Vector2.Distance(player.Center, nPC.Center) <= num12) {
                                if (nPC.FindBuffIndex(num) == -1) {
                                    nPC.AddBuff(num, 120);
                                }
                                if (flag) {
                                    player.ApplyDamageToNPC(nPC, damage, 0f, 0, crit: false);
                                }
                            }
                        }
                        if (Main.netMode != NetmodeID.SinglePlayer && player.hostile) {
                            for (int m = 0; m < 255; m++) {
                                Player player = Main.player[m];
                                if (player != base.player && player.active && !player.dead && player.hostile && !player.buffImmune[24] && (player.team != base.player.team || player.team == 0) && Vector2.DistanceSquared(base.player.Center, player.Center) <= num) {
                                    if (player.FindBuffIndex(num) == -1) {
                                        player.AddBuff(num, 120);
                                    }
                                    if (flag) {
                                        player.Hurt(PlayerDeathReason.LegacyEmpty(), damage, 0, pvp: true);
                                        if (Main.netMode != NetmodeID.SinglePlayer) {
                                            PlayerDeathReason reason = PlayerDeathReason.ByPlayer(player.whoAmI);
                                            NetMessage.SendPlayerHurt(m, reason, damage, 0, critical: false, pvp: true, 0);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (item.type == ModContent.ItemType<PermanentWrathPotion>() && PermanentBuffToggles[40]) {
                    player.allDamage += 0.1f;
                    player.buffImmune[BuffID.Wrath] = true;
                }
                if (item.type == ModContent.ItemType<PermanentFishingPotion>() && PermanentBuffToggles[41]) {
                    player.fishingSkill += 15;
                    player.buffImmune[BuffID.Fishing] = true;
                }
                if (item.type == ModContent.ItemType<PermanentSonarPotion>() && PermanentBuffToggles[42]) {
                    player.sonarPotion = true;
                    player.buffImmune[BuffID.Sonar] = true;
                }
                if (item.type == ModContent.ItemType<PermanentCratePotion>() && PermanentBuffToggles[43]) {
                    player.cratePotion = true;
                    player.buffImmune[BuffID.Crate] = true;
                }
                if (item.type == ModContent.ItemType<PermanentWarmthPotion>() && PermanentBuffToggles[44]) {
                    player.resistCold = true;
                    player.buffImmune[BuffID.Warmth] = true;
                }
                if (item.type == ModContent.ItemType<PermanentArmorDrugPotion>() && PermanentBuffToggles[45]) {
                    player.statDefense += 13;
                    player.buffImmune[ModContent.BuffType<ArmorDrug>()] = true;
                }
                if (item.type == ModContent.ItemType<PermanentBattlefrontPotion>() && PermanentBuffToggles[46]) {
                    player.statDefense += 8;
                    player.allDamage += 0.2f;
                    player.magicCrit += 5;
                    player.meleeCrit += 5;
                    player.rangedCrit += 5;
                    player.meleeSpeed += 0.2f;
                    player.pickSpeed += 0.2f;
                    player.thorns += 1f;
                    player.buffImmune[ModContent.BuffType<Battlefront>()] = true;
                }
                if (item.type == ModContent.ItemType<PermanentBoostPotion>() && PermanentBuffToggles[47]) {
                    player.magicCrit += 5;
                    player.meleeCrit += 5;
                    player.rangedCrit += 5;
                    player.buffImmune[ModContent.BuffType<Boost>()] = true;
                }
                if (item.type == ModContent.ItemType<PermanentCrimsonPotion>() && PermanentBuffToggles[48]) {
                    CrimsonDrain = true;
                    player.buffImmune[ModContent.BuffType<CrimsonDrain>()] = true;
                }
                if (item.type == ModContent.ItemType<PermanentDemonDrugPotion>() && PermanentBuffToggles[49]) {
                    player.allDamage += 0.2f;
                    player.buffImmune[ModContent.BuffType<DemonDrug>()] = true;
                }
                if (item.type == ModContent.ItemType<PermanentShockwavePotion>() && PermanentBuffToggles[50]) {
                    Shockwave = true;
                    player.buffImmune[ModContent.BuffType<Shockwave>()] = true;
                }
                if (item.type == ModContent.ItemType<PermanentStrengthPotion>() && PermanentBuffToggles[51]) {
                    player.statDefense += 15;
                    player.allDamage += 0.15f;
                    player.meleeSpeed += 0.15f;
                    player.pickSpeed += 0.15f;
                    player.magicCrit += 2;
                    player.meleeCrit += 2;
                    player.rangedCrit += 2;
                    player.buffImmune[ModContent.BuffType<Strength>()] = true;
                }
            }

            #endregion


            if (Shockwave) {
                if (player.controlDown && player.velocity.Y != 0f) {
                    player.gravity += 5f;
                    player.maxFallSpeed *= 1.25f;
                    if (!Falling) {
                        fallStartY = player.position.Y;
                    }
                    if (player.velocity.Y > 12f) {
                        Falling = true;
                        StopFalling = 0;
                        player.noKnockback = true;
                    }
                }
                if (player.velocity.Y == 0f && Falling && player.controlDown && !player.wet) {
                    for (int i = 0; i < 30; i++) {
                        int dustIndex2 = Dust.NewDust(new Vector2(player.position.X, player.position.Y), player.width, player.height, 31, 0f, 0f, 100);
                        Main.dust[dustIndex2].scale = 0.1f + Main.rand.Next(5) * 0.1f;
                        Main.dust[dustIndex2].fadeIn = 1.5f + Main.rand.Next(5) * 0.1f;
                        Main.dust[dustIndex2].noGravity = true;
                    }
                    for (int i = -8; i < 9; i++) {
                        Vector2 shotDirection = new Vector2(0f, -16f);
                        FallDist = (int)((player.position.Y - fallStartY) / 16);

                        int shockwaveShot = Projectile.NewProjectile(player.Center, new Vector2(0f, -7f), ModContent.ProjectileType<Projectiles.Shockwave>(), (int)(FallDist * 2.75f), 12, player.whoAmI);
                        Main.projectile[shockwaveShot].velocity = shotDirection.RotatedBy(MathHelper.ToRadians(0 - (11.25f * i))); //lerp wasnt working, so do manual interpolation
                    }

                    Main.PlaySound(SoundID.Item, (int)player.Center.X, (int)player.Center.Y, 14);
                    Falling = false;
                }
                if (player.velocity.Y <= 2f) {
                    StopFalling++;
                }
                else {
                    StopFalling = 0;
                }
                if (StopFalling > 1) {
                    Falling = false;
                }
            }
            if (!Shockwave) {
                Falling = false;
            }

            if (CrimsonDrain) {
                for (int l = 0; l < 200; l++) {
                    NPC nPC = Main.npc[l];
                    if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[ModContent.BuffType<CrimsonBurn>()] && Vector2.Distance(player.Center, nPC.Center) <= 200) {
                        nPC.AddBuff(ModContent.BuffType<CrimsonBurn>(), 2);
                    }
                }

                Vector2 centerOffset = new Vector2(player.Center.X + 2 - player.width / 2, player.Center.Y + 6 - player.height / 2);
                for (int j = 1; j < 75; j++) {
                    var x = Dust.NewDust(centerOffset + (Vector2.One * (j % 6 == 0 ? Main.rand.Next(10, 125) : 125)).RotatedByRandom(Math.PI * 4.0), player.width / 2, player.height / 2, 235, player.velocity.X, player.velocity.Y);
                    Main.dust[x].noGravity = true;
                }
            }


            #region Soul Siphon Dusts


            if (SoulSiphon) {
                Vector2 centerOffset = new Vector2(player.Center.X + 2 - player.width / 2, player.Center.Y + 6 - player.height / 2);
                if (Main.rand.Next(2) == 0)
                {
                    //var x = Dust.NewDust(centerOffset + (Vector2.One * (j % 6 == 0 ? Main.rand.Next(10, 80) : 80)).RotatedByRandom(Math.PI * 4.0), player.width / 2, player.height / 2, 89, player.velocity.X, player.velocity.Y, 120, default, 1f);
                    //Main.dust[x].noGravity = true;

                    int num5 = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[num5].noGravity = true;
                    Main.dust[num5].velocity *= 0.75f;
                    Main.dust[num5].fadeIn = 1.5f;
                    Vector2 vector = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vector.Normalize();
                    vector *= (float)Main.rand.Next(50, 100) * 0.04f;
                    Main.dust[num5].velocity = vector;
                    vector.Normalize();
                    vector *= Main.rand.Next(220, 900);
                    Main.dust[num5].position = player.Center - vector;

                    //Vector2.Normalize(start - end) * someSpeed //start and end are also Vector2 // Aparently another way to make things move toward each other

                }

                if (Main.rand.Next(4) == 0)
                {
                    /*var x = Dust.NewDust(centerOffset + (Vector2.One * (l % 6 == 0 ? Main.rand.Next(10, 80) : 80)).RotatedByRandom(Math.PI * 4.0), player.width / 2, player.height / 2, 89, player.velocity.X, player.velocity.Y, 120, default, 1f);
                    Main.dust[x].noGravity = true;*/

                    int x = Dust.NewDust(player.position, player.width, player.height, 89, player.velocity.X, player.velocity.Y, 120, default(Color), 1f);
                    Main.dust[x].noGravity = true;
                    Main.dust[x].velocity *= 0.75f;
                    Main.dust[x].fadeIn = 1.3f;
                    Vector2 vector = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vector.Normalize();
                    vector *= (float)Main.rand.Next(50, 100) * 0.05f; //velocity towards player
                    Main.dust[x].velocity = vector;
                    vector.Normalize();
                    vector *= 200f; //distance form player
                    Main.dust[x].position = player.Center - vector;

                    //Vector2.Normalize(start - end) * someSpeed //start and end are also Vector2 // Aparently another way to make things move toward each other

                }

                if (Main.rand.Next(2) == 0)
                {
                    int z = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 0.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(50, 100) * 0.052f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 150f;
                    Main.dust[z].position = player.Center - vectorother;
                }

                for (int k = 0; k < 1; k++)
                {
                    int z = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 0.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(50, 100) * 0.055f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 90f;
                    Main.dust[z].position = player.Center - vectorother;
                }

                for (int k = 0; k < 1; k++)
                {
                    int z = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 2.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(50, 100) * 0.055f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 45f;
                    Main.dust[z].position = player.Center - vectorother;
                }
            }

            #endregion


        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource) {
            if (ModContent.GetInstance<tsorcRevampConfig>().DeleteDroppedSoulsOnDeath) {
                for (int i = 0; i < 400; i++) {
                    if (Main.item[i].type == ModContent.ItemType<DarkSoul>()) {
                        Main.item[i].active = false;
                    }
                }
            }
            return true;
        }
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) {
            Projectile.NewProjectile(player.Bottom, new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Bloodsign>(), 0, 0, player.whoAmI);
            Main.PlaySound(SoundID.NPCDeath58.WithVolume(0.8f).WithPitchVariance(.3f), player.position);
        }
        public override void UpdateDead() {
            if (ModContent.GetInstance<tsorcRevampConfig>().SoulsDropOnDeath) {
                souldroptimer++;
                if (souldroptimer == 5 && souldroplooptimer < 13) {
                    foreach (Item item in player.inventory) {
                        if (item.type == ModContent.ItemType<DarkSoul>() && Main.netMode != NetmodeID.MultiplayerClient) {
                            Item.NewItem(player.Center, item.type, item.stack);
                            souldroplooptimer++;
                            souldroptimer = 0;
                            item.stack = 0;
                        }
                    }
                }
            }
            DarkInferno = false;
            Falling = false;
        }

        public override void OnHitNPC(Item item, NPC target, int damage, float knockback, bool crit) {
            if (MeleeArmorVamp10) {
                if (Main.rand.Next(10) == 0) {
                    player.HealEffect(damage / 10);
                    player.statLife += (damage / 10);
                }
            }
            if (NUVamp) {
                if (Main.rand.Next(5) == 0) {
                    player.HealEffect(damage / 4);
                    player.statLife += (damage / 4);
                }
            }

        }
        public override void ModifyHitNPC(Item item, NPC target, ref int damage, ref float knockback, ref bool crit) {
            if (OldWeapon) {
                float damageMult = Main.rand.NextFloat(0.0f, 0.8696f);
                damage = (int)(damage * damageMult);
            }
        }
        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection) {
            if (OldWeapon) {
                float damageMult = Main.rand.NextFloat(0.0f, 0.8696f);
                damage = (int)(damage * damageMult);
            }
        }

        public override void ModifyHitByNPC(NPC npc, ref int damage, ref bool crit) {
            int NT = npc.type;
            if (DragonStone) {
                if (NT == 2 || NT == 6 || NT == 34 || NT == 42 || NT == 48 || NT == 49 || NT == 51 || NT == 60 || NT == 61 || NT == 62 || NT == 66 || NT == 75 || NT == 87 || NT == 88 || NT == 89 || NT == 90 || NT == 91 || NT == 92 || NT == 93 || NT == 94 || NT == 112 || NT == 122 || NT == 133 || NT == 137
                    || NT == NPCID.Probe
                    || NT == NPCID.IceBat
                    || NT == NPCID.Lavabat
                    || NT == NPCID.GiantFlyingFox
                    || NT == NPCID.RedDevil
                    || NT == NPCID.VampireBat
                    || NT == NPCID.IceElemental
                    || NT == NPCID.PigronCorruption
                    || NT == NPCID.PigronHallow
                    || NT == NPCID.PigronCrimson
                    || NT == NPCID.Crimera
                    || NT == NPCID.MossHornet
                    || NT == NPCID.CrimsonAxe
                    || NT == NPCID.FloatyGross
                    || NT == NPCID.Moth
                    || NT == NPCID.Bee
                    || NT == NPCID.FlyingFish
                    || NT == NPCID.FlyingSnake
                    || NT == NPCID.AngryNimbus
                    || NT == NPCID.Parrot
                    || NT == NPCID.Reaper
                    || NT == NPCID.IchorSticker
                    || NT == NPCID.DungeonSpirit
                    || NT == NPCID.Ghost
                    || NT == NPCID.ElfCopter
                    || NT == NPCID.Flocko
                    || NT == NPCID.MartianDrone
                    || NT == NPCID.MartianProbe
                    || NT == NPCID.ShadowFlameApparition
                    || NT == NPCID.MothronSpawn
                    || NT == NPCID.GraniteFlyer
                    || NT == NPCID.FlyingAntlion
                    || NT == NPCID.DesertDjinn
                    || NT == NPCID.SandElemental) {
                    damage = 0;
                }
            }
            if (UndeadTalisman) {
                if (NPCID.Sets.Skeletons.Contains(npc.type)
                    || npc.type == NPCID.Zombie
                    || npc.type == NPCID.Skeleton
                    || npc.type == NPCID.BaldZombie
                    || npc.type == NPCID.AngryBones
                    || npc.type == NPCID.ArmoredViking
                    || npc.type == NPCID.UndeadViking
                    || npc.type == NPCID.DarkCaster
                    || npc.type == NPCID.CursedSkull
                    || npc.type == NPCID.UndeadMiner
                    || npc.type == NPCID.Tim
                    || npc.type == NPCID.DoctorBones
                    || npc.type == NPCID.ArmoredSkeleton
                    || npc.type == NPCID.Mummy
                    || npc.type == NPCID.DarkMummy
                    || npc.type == NPCID.LightMummy
                    || npc.type == NPCID.Wraith
                    || npc.type == NPCID.SkeletonArcher
                    || npc.type == NPCID.PossessedArmor
                    || npc.type == NPCID.TheGroom
                    || npc.type == NPCID.SkeletronHand
                    || npc.type == NPCID.SkeletronHead
                    || npc.type == ModContent.NPCType<NPCs.Bosses.GravelordNito>()
                    /* || NT == mod.NPCType("MagmaSkeleton") || NT == mod.NPCType("Troll") || NT == mod.NPCType("HeavyZombie") || NT == mod.NPCType("IceSkeleton") || NT == mod.NPCType("IrateBones")*/) {
                    damage -= 15;

                    if (damage < 0) damage = 0;
                }
            }

        }

        public override void OnHitByNPC(NPC npc, int damage, bool crit) {
            if (player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge) {
                if (!Main.hardMode) {
                    for (int b = 0; b < 8; b++) {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3f, 3f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 20, 4f, 0, 0, 0);
                    }
                }
                else {
                    for (int b = 0; b < 12; b++) {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 40, 5f, 0, 0, 0);
                    }
                }
            }
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, bool crit) {
            if (player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge) {
                if (!Main.hardMode) {
                    for (int b = 0; b < 8; b++) {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3f, 3f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 20, 4f, 0, 0, 0);
                    }
                }
                else {
                    for (int b = 0; b < 12; b++) {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 40, 5f, 0, 0, 0);
                    }
                }
            }
        }

        public override void UpdateBadLifeRegen() {
            if (DarkInferno) {
                if (player.lifeRegen > 0) {
                    player.lifeRegen = 0;
                }
                player.lifeRegenTime = 0;
                player.lifeRegen = -11;
                for (int j = 0; j < 4; j++) {
                    int dust = Dust.NewDust(player.position, player.width / 2, player.height / 2, 54, (player.velocity.X * 0.2f), player.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(player.position, player.width / 2, player.height / 2, 58, (player.velocity.X * 0.2f), player.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust2].noGravity = true;
                }
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet) {
            if (tsorcRevamp.toggleDragoonBoots.JustPressed) {
                DragoonBootsEnable = !DragoonBootsEnable;
            }
        }

        public override void PreUpdate() {
            if (DragoonBoots && DragoonBootsEnable) { //lets do this the smart way
                Player.jumpSpeed += 10f;

            }
        }
    }
}
