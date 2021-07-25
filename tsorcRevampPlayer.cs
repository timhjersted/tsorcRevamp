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
using tsorcRevamp.UI;
using Microsoft.Xna.Framework.Graphics;

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
        public int PowerfulCurseLevel = 1;
        public bool DarkInferno = false;
        public bool CrimsonDrain = false;
        public int count = 0;

        public bool Shockwave = false;
        public bool Falling;
        public int StopFalling;
        public float FallDist;
        public float fallStartY;
        public int fallStart_old = -1;

        public bool MeleeArmorVamp10 = false;
        public bool NUVamp = false;

        public bool OldWeapon = false;

        public bool Miakoda = false;
        public bool RTQ2 = false;

        public bool BoneRevenge = false;
        public bool SoulSiphon = false;

        public int souldroplooptimer = 0;
        public int souldroptimer = 0;
        public bool SOADrain = false;

        //An int because it'll probably be necessary to split it into multiple levels
        public int manaShield = 0;
        //How many more frames the Mana Shield is disabled after using a mana potion
        public int manaShieldCooldown = 0;
        //What frame of the shield's animation it's on
        public int shieldFrame = 0;
        //Did they have the shield up last frame?
        public bool shieldUp = false;

        public bool chests;
        public int safe = -1;

        public int FracturingArmor = 1;

        public int dragonMorphDamage = 45;

        public int MiakodaEffectsTimer;

        public bool MiakodaFull; //Miakoda - Full Moon Form
        public bool MiakodaFullHeal1;
        public bool MiakodaFullHeal2;

        public bool MiakodaCrescent; //Miakoda - Crescent Moon Form
        public bool MiakodaCrescentBoost;
        public int MiakodaCrescentBoostTimer;
        public bool MiakodaCrescentDust1;
        public bool MiakodaCrescentDust2;

        public bool MiakodaNew; //Miakoda - New Moon Form
        public bool MiakodaNewBoost;
        public int MiakodaNewBoostTimer;
        public bool MiakodaNewDust1;
        public bool MiakodaNewDust2;

        internal bool gotPickaxe;

        public bool[] PermanentBuffToggles;
        public static Dictionary<int, float> DamageDir;

        public bool GiveBossZen;
        public bool BossZenBuff;

        public bool MagicWeapon;
        public bool GreatMagicWeapon;
        public bool CrystalMagicWeapon;
        public bool DarkmoonCloak;

        public override void Initialize() {
            PermanentBuffToggles = new bool[53]; //todo dont forget to increment this if you add buffs to the dictionary
            DamageDir = new Dictionary<int, float> {
                { 48, 4 }, //spike
                { 76, 4 }, //hellstone
                { 232, 4 } //wooden spike, in case tim decides to use them
            };
        }

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
            {"gotPickaxe", gotPickaxe},
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
            gotPickaxe = tag.GetBool("gotPickaxe");
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
            SOADrain = false;
            FracturingArmor = 1;
            MiakodaFull = false;
            MiakodaFullHeal1 = false;
            MiakodaCrescent = false;
            MiakodaCrescentDust1 = false;
            MiakodaNew = false;
            MiakodaNewDust1 = false;
            GiveBossZen = false;
            BossZenBuff = false;
            MagicWeapon = false;
            GreatMagicWeapon = false;
            CrystalMagicWeapon = false;
            DarkmoonCloak = false;
            manaShield = 0;
        }

        public override void DrawEffects(PlayerDrawInfo drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright) {
            if (!Main.gameMenu) {
                if (player.HasBuff(ModContent.BuffType<Chilled>())) {
                    r *= 0.3804f;
                    g *= 0.6902f;
                    b *= 254 / 255; 
                }
                if (Shockwave) {
                    r *= 0.7372f;
                    g *= 0.5176f;
                    b *= 0.3686f;
                }

               
            }
        }

        public override void ModifyDrawLayers(List<PlayerLayer> layers)
        {
            layers.Add(tsorcRevampEffects);
        }

        public static readonly PlayerLayer tsorcRevampEffects = new PlayerLayer("tsorcRevamp", "tsorcRevampEffects", PlayerLayer.MiscEffectsFront, delegate (PlayerDrawInfo drawInfo) {

            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();

            #region Mana Shield Related Effects
            if (modPlayer.manaShield > 0 && !modPlayer.player.dead)
            {
                if (modPlayer.player.statMana > Items.Accessories.ManaShield.manaCost)
                {
                    //If they didn't have enough mana for the shield last frame but do now, play a sound to let them know it's back up
                    if (!modPlayer.shieldUp)
                    {
                        //Soundtype Item SoundStyle 28 is powerful magic cast
                        Main.PlaySound(SoundID.Item, modPlayer.player.position, 28);
                        modPlayer.shieldUp = true;
                    }

                    Lighting.AddLight(modPlayer.player.Center, 0f, 0.2f, 0.3f);

                    int shieldFrameCount = 8;
                    float shieldScale = 2.5f;

                    Texture2D texture = tsorcRevamp.TransparentTextures[3];
                    Player drawPlayer = drawInfo.drawPlayer;
                    int drawX = (int)(drawInfo.position.X + drawPlayer.width / 2f - Main.screenPosition.X);
                    int drawY = (int)(drawInfo.position.Y + drawPlayer.height / 2f - Main.screenPosition.Y);
                    int frameHeight = texture.Height / shieldFrameCount;
                    int startY = frameHeight * (modPlayer.shieldFrame / 3);
                    Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
                    Color newColor = Lighting.GetColor((int)((drawInfo.position.X + drawPlayer.width / 2f) / 16f), (int)((drawInfo.position.Y + drawPlayer.height / 2f) / 16f));
                    Vector2 origin = sourceRectangle.Size() / 2f;

                    DrawData data = new DrawData(texture, new Vector2(drawX, drawY), sourceRectangle, newColor, 0f, origin, shieldScale, SpriteEffects.None, 0);
                    Main.playerDrawData.Add(data);
                }
                else
                {
                    if (modPlayer.shieldUp)
                    {
                        //Soundtype Item SoundStyle 60 is the Terra Beam
                        Main.PlaySound(SoundID.Item, modPlayer.player.position, 60);
                        modPlayer.shieldUp = false;
                    }
                    //If the player doesn't have enough mana to tank a hit, then draw particle effects to indicate their mana is too low for it to function.
                    int dust = Dust.NewDust(modPlayer.player.Center, 1, 1, 221, modPlayer.player.velocity.X + Main.rand.Next(-3, 3), modPlayer.player.velocity.Y + Main.rand.Next(-3, 3), 180, Color.Cyan, 1f);
                    Main.dust[dust].noGravity = true;
                    modPlayer.shieldUp = false;
                }
            }
            else
            {
                modPlayer.shieldUp = false;
            }
            
            #endregion

        });


        public override void PostUpdateEquips() {
            if (Main.mouseItem.type == ModContent.ItemType<DarkSoul>()) {
                player.chest = -1;
            }
            if (manaShield > 0)
            {
                player.manaRegenBuff = false;
            }
            int PTilePosX = (int)player.position.X / 16;
            bool Ocean = (PTilePosX < 750 || PTilePosX > Main.maxTilesX - 750);
            bool underground = (player.position.Y >= (Main.maxTilesY / 2.43309f) * 16); //magic number

            if (((underground && player.ZoneHoly && !Ocean && !player.ZoneDungeon /*&& !player.ZoneOverworldHeight*/) || player.ZoneMeteor) && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {

                player.gravControl = true;
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
                    if (manaShield == 0)
                    {
                        player.manaRegenBuff = true;
                    }
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
                    player.buffImmune[BuffID.Shine] = true;
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
                if (item.type == ModContent.ItemType<PermanentArmorDrug>() && PermanentBuffToggles[45]) {
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
                if (item.type == ModContent.ItemType<PermanentDemonDrug>() && PermanentBuffToggles[49]) {
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
                if (item.type == ModContent.ItemType<PermanentSoulSiphonPotion>() && PermanentBuffToggles[52]) {
                    SoulSiphon = true;
                    player.buffImmune[ModContent.BuffType<SoulSiphon>()] = true;
                }
            }

            #endregion


            if (Shockwave) {
                if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
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
                        FallDist = (int)((player.position.Y - fallStartY) / 16);
                        if (FallDist > 5) {
                            Main.PlaySound(SoundID.Item, (int)player.Center.X, (int)player.Center.Y, 14);
                            for (int i = -9; i < 10; i++) { //19 projectiles
                                Vector2 shotDirection = new Vector2(0f, -16f);
                                int shockwaveShot = Projectile.NewProjectile(player.Center, new Vector2(0f, -7f), ModContent.ProjectileType<Projectiles.Shockwave>(), (int)(FallDist * 2.75f), 12, player.whoAmI);
                                Main.projectile[shockwaveShot].velocity = shotDirection.RotatedBy(MathHelper.ToRadians(0 - (10f * i))); // (180 / (projectilecount - 1))
                            }
                        }


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
                else {
                    var P = player;
                    if (Main.rand.Next(50) == 0) {
                        int D = Dust.NewDust(P.position, P.width, P.height, 9, (P.velocity.X * 0.2f) + (P.direction * 3), P.velocity.Y * 1.2f, 60, new Color(), 1f);
                        Main.dust[D].noGravity = true;
                        Main.dust[D].velocity.X *= 1.2f;
                        Main.dust[D].velocity.X *= 1.2f;
                    }
                    if (Main.rand.Next(50) == 0) {
                        int D2 = Dust.NewDust(P.position, P.width, P.height, 9, (P.velocity.X * 0.2f) + (P.direction * 3), P.velocity.Y * 1.2f, 60, new Color(), 1f);
                        Main.dust[D2].noGravity = true;
                        Main.dust[D2].velocity.X *= -1.2f;
                        Main.dust[D2].velocity.X *= 1.2f;
                    }
                    if (Main.rand.Next(50) == 0) {
                        int D3 = Dust.NewDust(P.position, P.width, P.height, 9, (P.velocity.X * 0.2f) + (P.direction * 3), P.velocity.Y * 1.2f, 60, new Color(), 1f);
                        Main.dust[D3].noGravity = true;
                        Main.dust[D3].velocity.X *= 1.2f;
                        Main.dust[D3].velocity.X *= -1.2f;
                    }
                    if (Main.rand.Next(50) == 0) {
                        int D4 = Dust.NewDust(P.position, P.width, P.height, 9, (P.velocity.X * 0.2f) + (P.direction * 3), P.velocity.Y * 1.2f, 60, new Color(), 1f);
                        Main.dust[D4].noGravity = true;
                        Main.dust[D4].velocity.X *= -1.2f;
                        Main.dust[D4].velocity.X *= -1.2f;
                    }
                    int sw = (int)(Main.screenWidth);
                    int sh = (int)(Main.screenHeight);
                    int sx = (int)(Main.screenPosition.X);
                    int sy = (int)(Main.screenPosition.Y);
                    //bool wings = false;
                    //if (ModPlayer.HasItemInArmor(492) || ModPlayer.HasItemInArmor(493) || ModPlayer.HasItemInExtraSlots(492) || ModPlayer.HasItemInExtraSlots(493))
                    //{
                    //	wings = true;
                    //}
                    if (fallStart_old == -1) fallStart_old = P.fallStart;
                    int fall_dist = 0;
                    if (P.velocity.Y == 0f) // && !wings) // detect landing from a fall
                        fall_dist = (int)((float)((int)(P.position.Y / 16f) - fallStart_old) * P.gravDir);
                    Vector2 p_pos = P.position + new Vector2(P.width, P.height) / 2f;

                    if (fall_dist > 3) // just fell
                    {
                        for (int k = 0; k < Main.npc.Length; k++) { // iterate through NPCs
                            NPC N = Main.npc[k];
                            if (!N.active || N.dontTakeDamage || N.friendly || N.life < 1) continue;
                            Vector2 n_pos = new Vector2(N.position.X + (float)N.width * 0.5f, N.position.Y + (float)N.height * 0.5f); // NPC location
                            int HitDir = -1;
                            if (n_pos.X > p_pos.X) HitDir = 1;
                            if ((N.position.X >= sx) && (N.position.X <= sx + sw) && (N.position.Y >= sy) && (N.position.Y <= sy + sh)) { // on screen
                                N.StrikeNPC(2 * fall_dist, 5f, HitDir);
                                if (Main.netMode != NetmodeID.SinglePlayer) NetMessage.SendData(MessageID.StrikeNPC, -1, -1, null, k, 2 * fall_dist, 10f, HitDir, 0); // for multiplayer support
                                                                                                                                                                      // optionally add debuff here
                            } // END on screen
                        } // END iterate through NPCs
                    } // END just fell
                    fallStart_old = P.fallStart;
                }
            }
            if (!Shockwave) {
                Falling = false;
            }

            if (CrimsonDrain) {
                if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                    for (int l = 0; l < 200; l++) {
                        NPC nPC = Main.npc[l];
                        if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[ModContent.BuffType<CrimsonBurn>()] && Vector2.Distance(player.Center, nPC.Center) <= 200) {
                            nPC.AddBuff(ModContent.BuffType<CrimsonBurn>(), 2);
                        }
                    }

                    Vector2 centerOffset = new Vector2(player.Center.X + 2 - player.width / 2, player.Center.Y + 6 - player.height / 2);
                    for (int j = 1; j < 30; j++) {
                        var x = Dust.NewDust(centerOffset + (Vector2.One * (j % 8 == 0 ? Main.rand.Next(15, 125) : 125)).RotatedByRandom(Math.PI * 4.0), player.width / 2, player.height / 2, 235, player.velocity.X, player.velocity.Y);
                        Main.dust[x].noGravity = true;
                    }
                }
                else { //old crimson pot
                    var P = player;
                    int x = (int)P.position.X;
                    int y = (int)P.position.Y;
                    for (int k = 0; k < Main.npc.Length; k++) {
                        NPC N = Main.npc[k];
                        if (N.townNPC) continue;
                        if (!N.active || N.dontTakeDamage || N.friendly || N.life < 1) continue;
                        if (N.position.X >= x - 320 && N.position.X <= x + 320 && N.position.Y >= y - 320 && N.position.Y <= y + 320) {
                            count++;
                            if (count % 50 == 0) {
                                foreach (NPC N2 in Main.npc) {
                                    if (N2.position.X >= x - 320 && N2.position.X <= x + 320 && N2.position.Y >= y - 320 && N2.position.Y <= y + 320) {
                                        if (!N2.active || N2.dontTakeDamage || N2.townNPC || N2.life < 1 || N2.boss || N2.realLife >= 0) continue;
                                        N2.StrikeNPC(1, 0f, 1);
                                    }
                                }
                                count = 0;
                            }
                        }
                    }
                }
            }


            #region Soul Siphon Dusts


            if (SoulSiphon) {

                if (Main.rand.Next(3) == 0) //outermost "ring"
                {
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
                }

                if (Main.rand.Next(6) == 0) {
                    int x = Dust.NewDust(player.position, player.width, player.height, 89, player.velocity.X, player.velocity.Y, 120, default(Color), 1f);
                    Main.dust[x].noGravity = true;
                    Main.dust[x].velocity *= 0.75f;
                    Main.dust[x].fadeIn = 1.3f;
                    Vector2 vector = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vector.Normalize();
                    vector *= (float)Main.rand.Next(50, 100) * 0.05f; //velocity towards player
                    Main.dust[x].velocity = vector;
                    vector.Normalize();
                    vector *= 200f; //spawn distance from player
                    Main.dust[x].position = player.Center - vector;

                    //Vector2.Normalize(start - end) * someSpeed //start and end are also Vector2 // Aparently another way to make things move toward each other

                }

                if (Main.rand.Next(3) == 0) {
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

                if (Main.rand.Next(2) == 0) {
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

                if (Main.rand.Next(2) == 0) //innermost "ring"
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
            #region consistent hellstone and spike damage
            float REDUCE = CheckReduceDefense(player.position, player.width, player.height, player.fireWalk); // <--- added firewalk parameter
            if (REDUCE != 0) {
                REDUCE = 1f - REDUCE;
                player.statDefense = (int)(player.statDefense * REDUCE);
            }
            #endregion
            #region boss zen
            GiveBossZen = CheckBossZen();
            if (GiveBossZen && ModContent.GetInstance<tsorcRevampConfig>().BossZenConfig) {
                player.AddBuff(ModContent.BuffType<BossZenBuff>(), 2, false);
            }
            #endregion
        }

        public static bool CheckBossZen() {
            for (int i = 0; i < 200; i++) {
                if (Main.npc[i].active && Main.npc[i].boss) {
                    return true;
                }
            }
            return false;
        }

        public static float CheckReduceDefense(Vector2 Position, int Width, int Height, bool fireWalk) {

            int playerTileXLeft = (int)(Position.X / 16f) - 1;
            int playerTileXRight = (int)((Position.X + Width) / 16f) + 2;
            int playerTileYBottom = (int)(Position.Y / 16f) - 1;
            int playerTileYTop = (int)((Position.Y + Height) / 16f) + 2;

            #region sanity
            if (playerTileXLeft < 0) {
                playerTileXLeft = 0;
            }
            if (playerTileXRight > Main.maxTilesX) {
                playerTileXRight = Main.maxTilesX;
            }
            if (playerTileYBottom < 0) {
                playerTileYBottom = 0;
            }
            if (playerTileYTop > Main.maxTilesY) {
                playerTileYTop = Main.maxTilesY;
            }
            #endregion

            for (int i = playerTileXLeft; i < playerTileXRight; i++) {
                for (int j = playerTileYBottom; j < playerTileYTop; j++) {
                    if (Main.tile[i, j] != null && Main.tile[i, j].active()) {
                        Vector2 TilePos;
                        TilePos.X = i * 16;
                        TilePos.Y = j * 16;

                        int type = Main.tile[i, j].type;

                        if (DamageDir.ContainsKey(type) && !(fireWalk && type == 76)) {
                            float a = DamageDir[type];
                            float z = 0.5f;
                            if (Position.X + Width > TilePos.X - z &&
                                Position.X < TilePos.X + 16f + z &&
                                Position.Y + Height > TilePos.Y - z &&
                                Position.Y < TilePos.Y + 16f + z) {
                                return a;
                            }
                        }
                    }
                }
            }
            return 0;
        }

        public static float CheckSoulsMultiplier(Player player) {
            float multiplier = 1f;
            if (player.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing) {
                multiplier += 0.25f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SoulSiphon) {
                multiplier += 0.15f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SOADrain) {
                multiplier += 0.4f;
            }
            return multiplier;
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
                        if (item.type == ModContent.ItemType<DarkSoul>() /*&& Main.netMode != NetmodeID.MultiplayerClient*/) { //could this be dropping double though? Test with Zeo
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
            FracturingArmor = 1;
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
            if (MiakodaFull) { //Miakoda Full Moon
                if (MiakodaEffectsTimer > 720) {
                    if (crit) {

                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal2 = true;

                        int HealAmount = (int)((Math.Floor((double)(player.statLifeMax2 / 100)) * 2) + 2);
                        player.statLife += HealAmount;
                        player.HealEffect(HealAmount, false);
                        if (player.statLife > player.statLifeMax2) {
                            player.statLife = player.statLifeMax2;
                        }

                        Main.PlaySound(SoundID.Item30.WithVolume(.7f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }

            if (MiakodaCrescent) { //Miakoda Crescent Moon
                if (MiakodaEffectsTimer > 720) {
                    if (crit) {
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust2 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentBoost = true;

                        Main.PlaySound(SoundID.Item100.WithVolume(.75f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }

            if (MiakodaNew) { //Miakoda New Moon
                if (MiakodaEffectsTimer > 720) {
                    if (crit) {
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewDust1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewDust2 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewBoost = true;

                        Main.PlaySound(SoundID.Item81.WithVolume(.75f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }
        }
        public override void OnHitNPCWithProj(Projectile proj, NPC target, int damage, float knockback, bool crit)
        {
            if (MiakodaFull) { //Miakoda Full Moon
                if (MiakodaEffectsTimer > 720) {
                    if (crit || (proj.minion && Main.player[proj.owner].HeldItem.summon)) {
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal2 = true;
                        


                        //2 per 100 max hp, plus 2
                        int HealAmount = (int)((Math.Floor((double)(player.statLifeMax2 / 100)) * 2) + 2);
                        player.statLife += HealAmount;
                        player.HealEffect(HealAmount, false);
                        if (player.statLife > player.statLifeMax2) {
                            player.statLife = player.statLifeMax2;
                        }

                        /* do not do this
                        if ((player.statLifeMax2 > 99) && (player.statLifeMax2 <= 199)) { 
                            player.HealEffect(4, false);
                            player.statLife += 4;
                            if (player.statLife > player.statLifeMax2) {
                                player.statLife = player.statLifeMax2;
                            }
                        }

                        if ((player.statLifeMax2 > 199) && (player.statLifeMax2 <= 299)) {
                            player.HealEffect(6, false);
                            player.statLife += 6;
                            if (player.statLife > player.statLifeMax2) {
                                player.statLife = player.statLifeMax2;
                            }
                        }

                        if ((player.statLifeMax2 > 299) && (player.statLifeMax2 <= 399)) {
                            player.HealEffect(8, false);
                            player.statLife += 8;
                            if (player.statLife > player.statLifeMax2) {
                                player.statLife = player.statLifeMax2;
                            }
                        }

                        if ((player.statLifeMax2 > 399) && (player.statLifeMax2 <= 499)) {
                            player.HealEffect(10, false);
                            player.statLife += 10;
                            if (player.statLife > player.statLifeMax2) {
                                player.statLife = player.statLifeMax2;
                            }
                        }

                        if (player.statLifeMax2 > 499) {
                            player.HealEffect(12, false);
                            player.statLife += 12;
                            if (player.statLife > player.statLifeMax2) {
                                player.statLife = player.statLifeMax2;
                            }
                        }
                        */
                        Main.PlaySound(SoundID.Item30.WithVolume(.7f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }

            if (MiakodaCrescent) { //Miakoda Crescent Moon
                if (MiakodaEffectsTimer > 720) {
                    if (crit || (proj.minion && Main.player[proj.owner].HeldItem.summon)) {
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust2 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentBoost = true;

                        Main.PlaySound(SoundID.Item100.WithVolume(.75f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }

            if (MiakodaNew) { //Miakoda New Moon
                if (MiakodaEffectsTimer > 720) {
                    if (crit || (proj.minion && Main.player[proj.owner].HeldItem.summon)) {
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewDust1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewDust2 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewBoost = true;

                        Main.PlaySound(SoundID.Item81.WithVolume(.75f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
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
            if (npc.type == NPCID.SkeletronPrime && Main.rand.Next(2) == 0) {
                player.AddBuff(BuffID.Bleeding, 1800);
                player.AddBuff(BuffID.OnFire, 600);
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
            if (projectile.type == ProjectileID.DeathLaser && Main.rand.Next(2) == 0) {
                player.AddBuff(BuffID.BrokenArmor, 180);
                player.AddBuff(BuffID.OnFire, 180);
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

            if (SOADrain) {
                if (player.lifeRegen > 0) {
                    player.lifeRegen = 0;
                }
                player.lifeRegenTime = 0;
                player.lifeRegen = -15;
                if (Main.rand.Next(3) == 0) {
                    int dust = Dust.NewDust(player.position, player.width, player.height, 235, player.velocity.X, player.velocity.Y, 140, default, 0.8f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].fadeIn = 1f;
                }
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet) {
            if (tsorcRevamp.toggleDragoonBoots.JustPressed) {
                DragoonBootsEnable = !DragoonBootsEnable;
            }
        }

        public override void PreUpdate()
        {

            MiakodaEffectsTimer++;

            if (DragoonBoots && DragoonBootsEnable)
            { //lets do this the smart way
                Player.jumpSpeed += 10f;

            }

            if (!player.HasBuff(ModContent.BuffType<Bonfire>()))
            { //this ensures that BonfireUIState is only visible when within Bonfire range
                BonfireUIState.Visible = false;
            }

            if (MiakodaFullHeal1)
            { //dust loop on player the instant they get healed
                for (int d = 0; d < 100; d++)
                {
                    int dust = Dust.NewDust(player.position, player.width, player.height, 107, 0f, 0f, 30, default(Color), .75f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 3.5f);
                    Main.dust[dust].noGravity = true;
                }
            }

            if (MiakodaCrescentDust1)
            { //dust loop on player the instant they get imbue
                for (int d = 0; d < 100; d++)
                {
                    int dust = Dust.NewDust(player.position, player.width, player.height, 164, 0f, 0f, 30, default(Color), 1.2f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 5f);
                    Main.dust[dust].noGravity = false;
                }
            }
            if (MiakodaCrescentBoost)
            {
                MiakodaCrescentBoostTimer++;
            }
            if (MiakodaCrescentBoostTimer > 150)
            {
                player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentBoost = false;
                MiakodaCrescentBoostTimer = 0;
            }

            if (MiakodaNewDust1)
            { //dust loop on player the instant they get boost
                for (int d = 0; d < 100; d++)
                {
                    int dust = Dust.NewDust(player.position, player.width, player.height, 57, 0f, 0f, 50, default(Color), 1.2f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(2f, 7.5f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (MiakodaNewBoost)
            {
                MiakodaNewBoostTimer++;
                player.armorEffectDrawShadow = true;

            }
            if (MiakodaNewBoostTimer > 150)
            {
                player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewBoost = false;
                MiakodaNewBoostTimer = 0;
            }

            

            #region manashield
            if (manaShield > 0)
            {
                shieldFrame++;
                if (shieldFrame > 23)
                {
                    shieldFrame = 0;
                }

                //Disable Mana Regen Potions
                player.manaRegenBuff = false;
                player.buffImmune[BuffID.ManaRegeneration] = true;
            }
            #endregion manashield
        }

        //On hit, subtract the mana cost and disable natural mana regen for a short period
        //The latter is absolutely necessary, because natural mana regen scales with your base mana
        //Even as melee there are mana boosting accessories you can stack, as well as armor like Dragoon that makes mana regen obscenely powerful.
        //This means you can tank until your mana bar is exhausted, then have to back off for a bit and actually dodge
        public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit)
        {
            base.Hurt(pvp, quiet, damage, hitDirection, crit);
            if (manaShield == 1)
            {
                if (player.statMana >= Items.Accessories.ManaShield.manaCost)
                {
                    player.statMana -= Items.Accessories.ManaShield.manaCost;
                    player.manaRegenDelay = Items.Accessories.ManaShield.regenDelay;
                }
            }
        }

        //Reduces the mana restored from potions and such to zero
        public override void GetHealMana(Item item, bool quickHeal, ref int healValue)
        {
            if (manaShield >= 1)
            {
                healValue = 0;
            }
        }

        public override void PostUpdateBuffs() {
            if (MiakodaCrescentBoost) {
                player.allDamageMult += 0.07f;
            }

            if (MiakodaNewBoost) {
                player.moveSpeed += 0.9f;
                player.endurance = .5f;
                player.noKnockback = true;
            }
        }

        public override void FrameEffects() {
            if (MiakodaNewBoost) {
                player.armorEffectDrawShadow = true;
            }
        }

        public override void OnEnterWorld(Player player) {
            if (Main.worldName.Contains("Red Cloud")) {
                Main.NewText("If you are using the custom map, please enable Adventure Mode in Mod Configuration for the intended experience!", Color.GreenYellow);
            }
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && !gotPickaxe) { //sandbox mode only, and only once
                player.QuickSpawnItem(ModContent.ItemType<DiamondPickaxe>());
                gotPickaxe = true;
            }

        }
    }
}
