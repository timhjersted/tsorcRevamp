using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Buffs;
using System;
using tsorcRevamp.UI;
using TerraUI.Objects;
using Terraria.Graphics.Effects;
using tsorcRevamp.Projectiles.Pets;

namespace tsorcRevamp {
    //update loops that run every frame
    public partial class tsorcRevampPlayer {

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
        public int SoulReaper = 5;
        public bool Crippled = false;

        public bool DuskCrownRing = false;
        public bool UndeadTalisman = false;

        public bool DragoonBoots = false;
        public bool DragoonBootsEnable = false;
        public bool DragoonHorn = false;

        public bool GemBox = false;
        public bool ConditionOverload = true;

        public int CurseLevel = 1;
        public int PowerfulCurseLevel = 1;
        public int curseDecayTimer = 0;
        public int powerfulCurseDecayTimer = 0;

        public bool DarkInferno = false;
        public bool CrimsonDrain = false;
        public bool PhazonCorruption = false;
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
        public int ConsSoulChanceMult;
        public bool SoulSickle = false;

        public int souldroplooptimer = 0;
        public int souldroptimer = 0;
        public bool SOADrain = false;

        public int supersonicLevel = 0;

        public int darkSoulQuantity;

        //An int because it'll probably be necessary to split it into multiple levels
        public int manaShield = 0;
        //How many more frames the Mana Shield is disabled after using a mana potion
        public int manaShieldCooldown = 0;
        //What frame of the shield's animation it's on
        public int shieldFrame = 0;
        //Did they have the shield up last frame?
        public bool shieldUp = false;

        public bool chestBankOpen;
        public int chestBank = -1;

        public bool chestPiggyOpen;
        public int chestPiggy = -1;

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
        public bool FirstEncounter;
        public bool ReceivedGift;

        public bool[] PermanentBuffToggles;
        public static Dictionary<int, float> DamageDir;

        public bool GiveBossZen;
        public bool BossZenBuff;

        public bool MagicWeapon;
        public bool GreatMagicWeapon;
        public bool CrystalMagicWeapon;
        public bool DarkmoonCloak;

        //increased grab range immediately after killing a boss
        public int bossMagnetTimer;
        public bool bossMagnet;

        public bool ShadowWeight;

        public bool ReflectionShiftEnabled; //Does the player have it equipped?
        public int ReflectionShiftKeypressTime = 0; //If they just pressed an arrow key, this is set to 15. It counts down to 0. If another arrow key is pressed when it is not zero, a dash initiates.
        public Vector2 ReflectionShiftState = Vector2.Zero;
        int[] keyPrimed = new int[4] { 0, 0, 0, 0 }; //Holds the state of each key
        public int FastFallTimer = 0;

        public static readonly int DashDown = 0;
        public static readonly int DashUp = 1;
        public static readonly int DashRight = 2;
        public static readonly int DashLeft = 3;

        public bool BearerOfTheCurse;
        public bool LifegemHealing;
        public bool RadiantLifegemHealing;
        int healingTimer = 0;

        public Item[] PotionBagItems = new Item[PotionBagUIState.POTION_BAG_SIZE];
        public int potionBagCountdown = 0; //You can't move items around if an item is still 'in use'. This lets us delay opening the bag until that finishes.

        public UIItemSlot SoulSlot;

        public int MaxAcquiredHP; //To prevent purging stones and humanity from raising hp above your max acquired hp from life crystals and life fruit.

        public override void ResetEffects() {
            SilverSerpentRing = false;
            DragonStone = false;
            SoulReaper = 5;
            DragoonBoots = false;
            //player.eocDash = 0;
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
            ConditionOverload = false;
            supersonicLevel = 0;
            ConsSoulChanceMult = 0;
            SoulSickle = false;
            Crippled = false;
            ShadowWeight = false;
            ReflectionShiftEnabled = false;
            PhazonCorruption = false;
            LifegemHealing = false;
            RadiantLifegemHealing = false;
        }

        public override void PreUpdate() {
            player.fullRotationOrigin = new Vector2(11, 22);
            SetDirection(true);
            //Main.NewText(MaxAcquiredHP);

            darkSoulQuantity = player.CountItem(ModContent.ItemType<DarkSoul>(), 999999);

            //the item in the soul slot will only ever be souls, so we dont need to check type
            if (SoulSlot.Item.stack > 0) { darkSoulQuantity += SoulSlot.Item.stack; }

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                tsorcScriptedEvents.PlayerScriptedEventCheck(this.player);
            }

            if (!player.HasBuff(ModContent.BuffType<Bonfire>())) { //this ensures that BonfireUIState is only visible when within Bonfire range
                if (player.whoAmI == Main.LocalPlayer.whoAmI)
                {
                    BonfireUIState.Visible = false;
                }
            }

            if (potionBagCountdown > 0)
            {
                potionBagCountdown--;
            }
            if (potionBagCountdown == 1)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    if (!PotionBagUIState.Visible)
                    {
                        player.chest = -1;
                        Main.playerInventory = true;
                        PotionBagUIState.Visible = true;
                        Main.PlaySound(SoundID.MenuOpen);
                    }
                    else
                    {
                        PotionBagUIState.Visible = false;
                        Main.PlaySound(SoundID.MenuClose);
                    }
                }
            }


            #region Miakoda

            MiakodaEffectsTimer++;

            if (MiakodaFullHeal1) { //dust loop on player the instant they get healed
                for (int d = 0; d < 100; d++) {
                    int dust = Dust.NewDust(player.position, player.width, player.height, 107, 0f, 0f, 30, default, .75f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 3.5f);
                    Main.dust[dust].noGravity = true;
                }
            }

            if (MiakodaCrescentDust1) { //dust loop on player the instant they get imbue
                for (int d = 0; d < 100; d++) {
                    int dust = Dust.NewDust(player.position, player.width, player.height, 164, 0f, 0f, 30, default, 1.2f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 5f);
                    Main.dust[dust].noGravity = false;
                }
            }
            if (MiakodaCrescentBoost) {
                MiakodaCrescentBoostTimer++;
            }
            if (MiakodaCrescentBoostTimer > 150) {
                player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentBoost = false;
                MiakodaCrescentBoostTimer = 0;
            }

            if (MiakodaNewDust1) { //dust loop on player the instant they get boost
                for (int d = 0; d < 100; d++) {
                    int dust = Dust.NewDust(player.position, player.width, player.height, 57, 0f, 0f, 50, default, 1.2f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(2f, 7.5f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (MiakodaNewBoost) {
                MiakodaNewBoostTimer++;
                player.armorEffectDrawShadow = true;

            }
            if (MiakodaNewBoostTimer > 150) {
                player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewBoost = false;
                MiakodaNewBoostTimer = 0;
            }

            #endregion

            #region manashield
            if (manaShield > 0) {
                shieldFrame++;
                if (shieldFrame > 23) {
                    shieldFrame = 0;
                }

                //Disable Mana Regen Potions
                player.manaRegenBuff = false;
                player.buffImmune[BuffID.ManaRegeneration] = true;
            }
            #endregion manashield

            #region Abyss Shader
            bool hasCoA = false;

            if (player.whoAmI == Main.myPlayer) {

                //does the player have a covenant of artorias
                for (int i = 3; i < (8 + player.extraAccessorySlots); i++) {
                    if (player.armor[i].type == ModContent.ItemType<Items.Accessories.CovenantOfArtorias>()) {
                        hasCoA = true;
                        break;
                    }
                }

                //if they do, and the shader is inactive
                if (hasCoA && !(Filters.Scene["tsorcRevamp:TheAbyss"].Active)) {
                    Filters.Scene.Activate("tsorcRevamp:TheAbyss");
                }

                //if the abyss shader is active and the player is no longer wearing the CoA
                if (Filters.Scene["tsorcRevamp:TheAbyss"].Active && !hasCoA) {
                    Filters.Scene["tsorcRevamp:TheAbyss"].Deactivate();
                }
            }

            #endregion

            #region Reflection Shift
            if (ReflectionShiftEnabled) {

                int dashCooldown = 30;
                if (ReflectionShiftKeypressTime > 0) {
                    ReflectionShiftKeypressTime--;
                }
                else {
                    //This would have looked so much nicer if controlUp, controlLeft, etc were all in an array like doubleTapCardinalTimer, but...
                    if (player.controlUp && keyPrimed[DashUp] == 0) {
                        keyPrimed[DashUp] = 1;
                    }
                    if (player.releaseUp && keyPrimed[DashUp] == 1) {
                        keyPrimed[DashUp] = 2;
                    }
                    if (player.doubleTapCardinalTimer[DashUp] == 0) {
                        keyPrimed[DashUp] = 0;
                    }
                    if (player.controlUp && player.doubleTapCardinalTimer[DashUp] < 15 && keyPrimed[DashUp] == 2) {
                        ReflectionShiftKeypressTime = dashCooldown;
                        ReflectionShiftState.Y = -1;
                    }

                    if (player.controlLeft && keyPrimed[DashLeft] == 0) {
                        keyPrimed[DashLeft] = 1;
                    }
                    if (player.releaseLeft && keyPrimed[DashLeft] == 1) {
                        keyPrimed[DashLeft] = 2;
                    }
                    if (player.doubleTapCardinalTimer[DashLeft] == 0) {
                        keyPrimed[DashLeft] = 0;
                    }
                    if (player.controlLeft && player.doubleTapCardinalTimer[DashLeft] < 15 && keyPrimed[DashLeft] == 2) {
                        ReflectionShiftKeypressTime = dashCooldown;
                        ReflectionShiftState.X = -1;
                    }

                    if (player.controlRight && keyPrimed[DashRight] == 0) {
                        keyPrimed[DashRight] = 1;
                    }
                    if (player.releaseRight && keyPrimed[DashRight] == 1) {
                        keyPrimed[DashRight] = 2;
                    }
                    if (player.doubleTapCardinalTimer[DashRight] == 0) {
                        keyPrimed[DashRight] = 0;
                    }
                    if (player.controlRight && player.doubleTapCardinalTimer[DashRight] < 15 && keyPrimed[DashRight] == 2) {
                        ReflectionShiftKeypressTime = dashCooldown;
                        ReflectionShiftState.X = 1;
                    }

                    if (player.controlDown && keyPrimed[DashDown] == 0) {
                        keyPrimed[DashDown] = 1;
                    }
                    if (player.releaseDown && keyPrimed[DashDown] == 1) {
                        keyPrimed[DashDown] = 2;
                    }
                    if (player.doubleTapCardinalTimer[DashDown] == 0) {
                        keyPrimed[DashDown] = 0;
                    }
                    if (player.controlDown && player.doubleTapCardinalTimer[DashDown] < 15 && keyPrimed[DashDown] == 2) {
                        ReflectionShiftKeypressTime = dashCooldown;
                        ReflectionShiftState.Y = 1;
                    }
                }
            }
            #endregion


            if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 10 && (player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.SagittariusBow>() || player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.ArtemisBow>() 
                || player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.CernosPrime>() || player.HeldItem.type == ModContent.ItemType<Items.Weapons.Magic.DivineSpark>() || player.HeldItem.type == ModContent.ItemType<Items.Weapons.Magic.DivineBoomCannon>()))
            {
                player.channel = false;
            }

            if (player.itemAnimation != 0 && (player.HeldItem.type == ModContent.ItemType<Items.Weapons.Magic.DivineSpark>() || player.HeldItem.type == ModContent.ItemType<Items.Weapons.Magic.DivineBoomCannon>()))
            {
                player.statMana -= 1;
                if (player.statMana < 1) { player.channel = false; }
                if (player.statMana < 0) { player.statMana = 0; }

            }

            if (Main.tile[(int)player.position.X / 16, (int)player.position.Y / 16].wall == WallID.StarlitHeavenWallpaper)
            {
                player.AddBuff(BuffID.Darkness, 60);
            }

            if (MaxAcquiredHP < player.statLifeMax)
            {
                MaxAcquiredHP = player.statLifeMax;
            }
        }

        public override void PreUpdateBuffs() {
            if (chestBank >= 0) {
                DoPortableChest<SafeProjectile>(ref chestBank, ref chestBankOpen);
            }
            if (chestPiggy >= 0) {
                DoPortableChest<PiggyBankProjectile>(ref chestPiggy, ref chestPiggyOpen);
            }

            if (!Main.playerInventory) {
                chestPiggy = -1;
                chestPiggyOpen = false;
                chestBank = -1;
                chestBankOpen = false;
            }
        }

        public override void PostUpdateBuffs()
        {
            foreach (Item thisItem in PotionBagItems)
            {
                if (thisItem != null && !thisItem.IsAir)
                {
                    thisItem.modItem?.UpdateInventory(player);
                }
            }

            if (MiakodaCrescentBoost)
            {
                player.allDamageMult += 0.07f;
            }

            if (MiakodaNewBoost)
            {
                player.moveSpeed += 0.9f;
                player.endurance = .5f;
                player.noKnockback = true;
            }

            #region Lifegem Healing


            if (LifegemHealing) // 120 hp over 12 seconds
            {
                healingTimer++;

                if (healingTimer == 6)
                {
                    player.statLife += 1;
                    healingTimer = 0;
                }
            }

            if (RadiantLifegemHealing) // 200 hp over 13.33 seconds
            {
                healingTimer++;

                if (healingTimer == 4)
                {
                    player.statLife += 1;
                    healingTimer = 0;
                }
            }

            if (!RadiantLifegemHealing && !LifegemHealing)
            {
                healingTimer = 0;
            }

            #endregion

            if (player.HasBuff(BuffID.WellFed))
            {
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.1f;
            }

            if (player.HasBuff(BuffID.TheTongue))
            {
                for (int i = 0; i < 9; i++)
                {
                    CombatText.NewText(player.Hitbox, CombatText.DamagedFriendly, 999999999, true);
                }
                player.KillMe(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(player.name + " was consumed by The Wall."), 999999999, 1);
            }

            if (!player.HasBuff(ModContent.BuffType<Buffs.CurseBuildup>()))
            {
                CurseLevel = 1; //Not sure why 1 is the default
            }

            if (!player.HasBuff(ModContent.BuffType<Buffs.PowerfulCurseBuildup>()))
            {
                PowerfulCurseLevel = 1; //Not sure why 1 is the default
            }
        }

        public override void PostUpdateEquips() {
            if (manaShield > 0) {
                player.manaRegenBuff = false;
            }
            int PTilePosX = (int)player.position.X / 16;
            bool Ocean = (PTilePosX < 750 || PTilePosX > Main.maxTilesX - 750);
            bool underground = (player.position.Y >= (Main.maxTilesY / 2.43309f) * 16); //magic number

            if (((underground && player.ZoneHoly && !Ocean && !player.ZoneDungeon /*&& !player.ZoneOverworldHeight*/) || player.ZoneMeteor) && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {

                player.gravControl = true;
            }

            if (player.position.X == player.oldPosition.X)
            {
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 1.5f;
            }

            if (ShadowWeight) {
                player.doubleJumpBlizzard = false;
                player.doubleJumpFart = false;
                player.doubleJumpSail = false;
                player.doubleJumpSandstorm = false;
                player.doubleJumpUnicorn = false;
                player.canRocket = false;
                player.rocketTime = 0;
                player.jumpBoost = false;
                player.wingTime = 0;
                float speedCap = 12;
                if (player.velocity.X > speedCap) {
                    player.velocity.X = speedCap;
                }
                if (player.velocity.X < -speedCap) {
                    player.velocity.X = -speedCap;
                }
            }

            if (Crippled) {
                player.doubleJumpBlizzard = false;
                player.doubleJumpCloud = false;
                player.doubleJumpFart = false;
                player.doubleJumpSail = false;
                player.doubleJumpSandstorm = false;
                player.doubleJumpUnicorn = false;
                player.canRocket = false;
                player.rocketTime = 0;
                player.jumpBoost = false;
                player.jumpSpeedBoost = 0f;
                player.wingTime = 0;
                player.moveSpeed *= 0.8f;
            }

            for (int i = 0; i < 50; i++) {
                //block souls from going in normal inventory slots
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                if (player.inventory[i].type == ModContent.ItemType<DarkSoul>()) {
                    //if the player's soul slot is empty
                    if (modPlayer.SoulSlot.Item.type != ModContent.ItemType<DarkSoul>()) {
                        modPlayer.SoulSlot.Item = player.inventory[i].Clone();
                    }
                    else {
                        modPlayer.SoulSlot.Item.stack += player.inventory[i].stack;
                    }
                    //dont send the souls to the normal inventory
                    player.inventory[i].TurnToAir();
                }

            }


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
                                int shockwaveShot = Projectile.NewProjectile(player.Center, new Vector2(0f, -7f), ModContent.ProjectileType<Projectiles.Shockwave>(), (int)(FallDist * (Main.hardMode ? 2.6f : 2.4)), 12, player.whoAmI);
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
                        if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[ModContent.BuffType<CrimsonBurn>()] && Vector2.Distance(player.Center, nPC.Center) <= 240) {
                            nPC.AddBuff(ModContent.BuffType<CrimsonBurn>(), 2);
                        }
                    }

                    Vector2 centerOffset = new Vector2(player.Center.X + 2 - player.width / 2, player.Center.Y + 6 - player.height / 2);
                    for (int j = 1; j < 30; j++) {
                        var x = Dust.NewDust(centerOffset + (Vector2.One * (j % 8 == 0 ? Main.rand.Next(15, 150) : 150)).RotatedByRandom(Math.PI * 4.0), player.width / 2, player.height / 2, 235, player.velocity.X, player.velocity.Y);
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
                    int num5 = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default, 1f);
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
                    int x = Dust.NewDust(player.position, player.width, player.height, 89, player.velocity.X, player.velocity.Y, 120, default, 1f);
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
                    int z = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default, 1f);
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
                    int z = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default, 1f);
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
                    int z = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default, 1f);
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
            #region boss magnet
            //actual item grab range is in GlobalItem::GrabRange
            if (bossMagnet) {
                bossMagnetTimer--;
            }
            if (bossMagnetTimer == 0) {
                bossMagnet = false;
            }
            #endregion


            float shiftDistance = 7;
            #region Reflection Shift

            if (ReflectionShiftKeypressTime > 20) {
                player.immune = true;
            }

            if (ReflectionShiftState != Microsoft.Xna.Framework.Vector2.Zero) {
                //Initiate Dash
                for (int i = 0; i < 30; i++) {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(64, 64);
                    Vector2 velocity = new Vector2(-2, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                    Dust.NewDustPerfect(player.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
                }
                if (Collision.CanHit(player.Center, 1, 1, player.Center + ReflectionShiftState * shiftDistance * 16, 1, 1) || Collision.CanHitLine(player.Center, 1, 1, player.Center + ReflectionShiftState * shiftDistance * 16, 1, 1)) {
                    player.Center += ReflectionShiftState * shiftDistance * 16; //Teleport distance
                }
                FastFallTimer = 30;
                player.velocity = ReflectionShiftState * 20; //Dash speed
                ReflectionShiftState = Vector2.Zero;

                for (int i = 0; i < 30; i++) {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(64, 64);
                    Vector2 velocity = new Vector2(5, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                    Dust.NewDustPerfect(player.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
                }
            }
            #endregion

            if (DragoonBoots && DragoonBootsEnable) {
                //Player.jumpSpeed += 10f; why
                player.jumpSpeedBoost += 10f;
            }
            if (DragoonHorn && (((player.gravDir == 1f) && (player.velocity.Y > 0)) || ((player.gravDir == -1f) && (player.velocity.Y < 0)))) {
                player.meleeDamage *= 2;
            }
        }

        public override void PostUpdateRunSpeeds() {
            if (supersonicLevel == 0) {
                return;
            }
            else {
                float moveSpeedPercentBoost = 1;
                float baseSpeed = 1;

                //SupersonicBoots
                if (supersonicLevel == 1) {
                    //moveSpeedPercentBoost is what percent of a player's moveSpeed bonus should be applied to their max running speed
                    //For vanilla hermes boots and their upgrades, this is 0
                    moveSpeedPercentBoost = 0.35f;
                    //6f is hermes boots speed.
                    baseSpeed = 6f;
                    player.moveSpeed += 0.2f;
                }
                //SupersonicWings
                if (supersonicLevel == 2) {
                    moveSpeedPercentBoost = 0.5f;
                    baseSpeed = 6.8f;
                    player.moveSpeed += 0.3f;
                }
                //SupersonicWings2
                if (supersonicLevel == 3) {

                    moveSpeedPercentBoost = 1f;
                    baseSpeed = 7.5f;
                    player.moveSpeed += 0.6f;
                }


                //((player.moveSpeed * 0.5f) + 0.5) means 50% of the player's moveSpeed bonus will be applied
                //The general form is ((player.moveSpeed * %theyshouldget) + (1 - %theyshouldget))
                player.accRunSpeed = baseSpeed * ((player.moveSpeed * moveSpeedPercentBoost) + (1 - moveSpeedPercentBoost));
                player.maxRunSpeed = baseSpeed * ((player.moveSpeed * moveSpeedPercentBoost) + (1 - moveSpeedPercentBoost));

                if(FastFallTimer > 0)
                {
                    player.maxFallSpeed = 50;
                    FastFallTimer--;
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

            if (PhazonCorruption)
            {
                if (player.lifeRegen > 0)
                {
                    player.lifeRegen = 0;
                }
                player.lifeRegenTime = 0;
                player.lifeRegen = -7;
                for (int j = 0; j < 4; j++)
                {
                    int dust = Dust.NewDust(player.position, player.width / 2, player.height / 2, 29, (player.velocity.X * 0.2f), player.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(player.position, player.width / 2, player.height / 2, DustID.FireworkFountain_Blue, (player.velocity.X * 0.2f), player.velocity.Y * 0.2f, 100, default, 1f);
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

        public override void UpdateDead()
        {
            if (player.whoAmI == Main.myPlayer) {
                if (ModContent.GetInstance<tsorcRevampConfig>().SoulsDropOnDeath && Main.netMode == NetmodeID.SinglePlayer)
                {
                    souldroptimer++;
                    if (souldroptimer == 5 && souldroplooptimer < 13)
                    {
                        foreach (Item item in player.inventory)
                        { 
                            //leaving this in case someone decides to move souls to their normal inventory to stop them from being dropped on death :)
                            if (item.type == ModContent.ItemType<DarkSoul>())
                            {
                                if (Main.netMode == NetmodeID.SinglePlayer)
                                {
                                    Item.NewItem(player.Center, item.type, item.stack);
                                    item.stack = 0;
                                }
                                else
                                {
                                    ModPacket soulPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                                    soulPacket.Write(tsorcPacketID.DropSouls);
                                    soulPacket.WriteVector2(player.Center);
                                    soulPacket.Write(item.stack);
                                    soulPacket.Send();
                                    item.stack = 0;
                                }
                                souldroplooptimer++;
                                souldroptimer = 0;
                            }
                        }

                        if (SoulSlot.Item.stack > 0)
                        {
                            if (souldroplooptimer == 12)
                            {
                                if (Main.netMode == NetmodeID.SinglePlayer)
                                {
                                    Item.NewItem(player.Center, SoulSlot.Item.type, SoulSlot.Item.stack);
                                    SoulSlot.Item.TurnToAir();
                                }
                                else
                                {
                                    ModPacket soulPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                                    soulPacket.Write(tsorcPacketID.DropSouls);
                                    soulPacket.WriteVector2(player.Center);
                                    soulPacket.Write(SoulSlot.Item.stack);
                                    soulPacket.Send();
                                    SoulSlot.Item.TurnToAir();
                                }
                            }
                            else if (Main.netMode == NetmodeID.SinglePlayer)
                            {
                                Item.NewItem(player.Center, SoulSlot.Item.type, 0);
                            }
                            
                            souldroplooptimer++;
                            souldroptimer = 0;
                        }
                    }
                }
                

                DarkInferno = false;
                PhazonCorruption = false;
                Falling = false;
                FracturingArmor = 1;
            }
        }

        public override void PostUpdate() {
            if ((player.HasBuff(ModContent.BuffType<MagicWeapon>()) || player.HasBuff(ModContent.BuffType<GreatMagicWeapon>()) || player.HasBuff(ModContent.BuffType<CrystalMagicWeapon>())) && player.meleeEnchant > 0) {
                int buffIndex = 0;

                foreach (int buffType in player.buffType) {

                    if ((buffType == ModContent.BuffType<MagicWeapon>()) || (buffType == ModContent.BuffType<GreatMagicWeapon>()) || (buffType == ModContent.BuffType<CrystalMagicWeapon>())) {
                        player.buffTime[buffIndex] = 0;
                    }
                    buffIndex++;
                }
            }
            SetDirection();

            if (!player.mount.Active) {
                player.fullRotation = rotation * player.gravDir;
            }

            rotation = 0f;
            if (forcedItemRotation.HasValue) {
                player.itemRotation = forcedItemRotation.Value;

                forcedItemRotation = null;
            }
            

            TryForceFrame(ref player.headFrame, ref forcedHeadFrame);
            TryForceFrame(ref player.bodyFrame, ref forcedBodyFrame);
            TryForceFrame(ref player.legFrame, ref forcedLegFrame);


            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                player.allDamageMult *= 1.2f;

                if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2)
                {
                    player.lifeRegen /= 2;
                }
            }

            if (CurseLevel > 0)
            {
                curseDecayTimer++;

                if (curseDecayTimer >= 60)
                {
                    CurseLevel--;
                    curseDecayTimer = 0;
                }
            }

            if (PowerfulCurseLevel > 0)
            {
                powerfulCurseDecayTimer++;

                if (powerfulCurseDecayTimer >= 30)
                {
                    PowerfulCurseLevel--;
                    powerfulCurseDecayTimer = 0;
                }
            }


        }

        void TryForceFrame(ref Rectangle frame, ref PlayerFrames? newFrame) {
            if (newFrame.HasValue) {
                frame = ToRectangle(newFrame.Value);

                newFrame = null;
            }
        }
        public static Rectangle ToRectangle(PlayerFrames frame) {
            return new Rectangle(0, (int)frame * 56, 40, 56);
        }
        public override void UpdateBiomes() {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                if (Main.dungeonTiles >= 200 && player.Center.Y > Main.worldSurface * 16.0 * 1.5f) {
                    int playerTileX = (int)player.Center.X / 16;
                    int playerTileY = (int)player.Center.Y / 16;
                    for (int i = -10; i < 11; i++) {
                        for (int j = 0; j < 2; j++) {
                            int cross = (2 * j) - 1;
                            //check in an x shape instead of checking the entire region, since checking 100 tiles every frame is a little silly
                            if (Main.wallDungeon[Main.tile[playerTileX + i, playerTileY + (i * cross)].wall] || tsorcRevamp.CustomDungeonWalls[Main.tile[playerTileX + i, playerTileY + (i * cross)].wall]) {
                                player.ZoneDungeon = true;

                            }
                        }
                    }
                } 
            }
        }
    }
}
