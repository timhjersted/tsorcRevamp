using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using TerraUI.Objects;
using tsorcRevamp.Buffs;
using tsorcRevamp.Items;
using tsorcRevamp.Projectiles.Pets;
using tsorcRevamp.UI;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.ModLoader.Config;
using System.Linq;
using Terraria.UI.Gamepad;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Accessories.Ranged;
using tsorcRevamp.Items.Armors.Melee;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Utilities;
using tsorcRevamp.Buffs.Armor;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Buffs.Accessories;
using Terraria.Audio;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Projectiles.Summon;
using tsorcRevamp.Buffs.Weapons.Summon;

namespace tsorcRevamp
{
    //update loops that run every frame
    public partial class tsorcRevampPlayer
    {
        public static float MeleeBonusMultiplier = 0.5f;

        public bool NoDamageSpread = false;

        public Vector2 greatMirrorWarpPoint;
        public int warpWorld;
        public bool warpSet;

        public int townWarpX;
        public int townWarpY;
        public int townWarpWorld;
        public bool townWarpSet;

        public bool SilverSerpentRing = false;
        public bool SoulSerpentRing = false;
        public bool DragonStoneImmunity = false;
        public static bool DragonStonePotency = false;
        public int SoulReaper = 5;
        public bool TornWings = false;
        public bool Crippled = false;

        public bool Celestriad = false;
        public bool UndeadTalisman = false;
        public bool WolfRing = false;
        public bool BarrierRing;

        public bool DragoonBoots = false;
        public bool DragoonBootsEnable = false;
        public bool DragoonHorn = false;

        public bool ConditionOverload = true;

        public int CurseLevel = 1;
        public int PowerfulCurseLevel = 1;
        public int curseDecayTimer = 0;
        public int powerfulCurseDecayTimer = 0;

        public bool BrokenSpirit;

        public int MaxMinionTurretMultiplier;

        public float BotCMeleeBaseAttackSpeedMult = 0.85f;
        public int BotCLethalTempoDuration = 3;
        public float BotCLethalTempoStacks = 0;
        public int BotCLethalTempoMaxStacks = 10;
        public float BotCLethalTempoBonus = 0.03f;

        public float BotCRangedBaseCritMult = 0f;
        public float BotCCurrentAccuracyPercent = 0f;
        public float BotcAccuracyPercentMax = 1f;
        public float BotCAccuracyMaxFlatCrit = 8.5f;
        public float BotCAccuracyMaxCritMult = 1.25f;
        public float BotCAccuracyGain = 0.04f;
        public float BotCAccuracyLoss = 0.1f;
        public float CurrentTotalRangedCritChance;

        public float BotCCeruleanFlaskMaxManaScaling = 12f;
        public float BotCMagicDamageAmp = 15f;
        public float BotCMagicAttackSpeedAmp = 15f;

        public float BotCSummonBaseDamageMult = 0.2f;
        public int BotCConquerorDuration = 4;
        public float BotCConquerorStacks = 0;
        public int BotCConquerorMaxStacks = 10;
        public float BotCConquerorBonus = 0.09f;
        public float BotCWhipRangeMult = 0.75f;

        public bool SteraksGage;
        public bool InfinityEdge;
        public bool LudensTempest;
        public bool Goredrinker;
        public bool GoredrinkerReady = false;
        public bool GoredrinkerSwung = false;
        public int GoredrinkerHits = 1;

        public bool BoneRing;
        public bool CelestialCloak;

        public int SoulVessel = 0;
        public float MaxManaAmplifier;

        public int MagicPlatingStacks = 0;

        public bool ChloranthyRing1 = false;
        public bool ChloranthyRing2 = false;

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
        public bool HasShadowSickle = false;
        public bool MagmaArmor;
        public bool PortlyPlateArmor;

        public bool OldWeapon = false;

        public bool MythrilOrichalcumCritDamage = false;
        public bool Shunpo = false;
        public float ShunpoTimer = 0;
        public Vector2 ShunpoVelocity;
        public bool ChallengersGloveCritDamage = false;
        public float WhipCritHitboxSize = 1f;

        public bool PhoenixSkull = false;

        public int SteelTempestStacks = 0;
        public Vector2 SweepingBladeVelocity;

        public int EssenceThief = 0;
        public int SpiritRushCharges = 3;
        public float SpiritRushTimer = 0f;
        public int SpiritRushSoundStyle = 0;
        public float SpiritRushCooldown = 0f;
        public Vector2 SpiritRushVelocity;

        public float CrystalNunchakuDefenseDamage;

        public float DragoonLashFireBreathTimer = 0f;

        public float SearingLashStacks;
        public float NightsCrackerStacks;
        public float TerraFallStacks;

        public float SummonTagStrength;
        public float SummonTagDuration;

        public bool MythrilBulwark = false;
        public bool IceboundMythrilAegis = false;


        public bool InterstellarBoost = false;
        public int MinimumMinionCircleRadius = 150;
        public int MaximumMinionCircleRadius = 500;
        public float MinionCircleRadius = 150;
        public int CotUStardustCount = 0;

        public bool WaspPower = false;
        public bool DemonPower = false;
        public bool WitchPower = false;

        public bool HollowSoldierAgility = false;
        public bool SmoughShieldSkills = false;
        public bool BurdenOfSmough = false;
        public bool SmoughAttackSpeedReduction = false;

        public bool Miakoda = false;
        public bool RTQ2 = false;

        public bool BoneRevenge = false;
        public bool SoulSiphon = false;
        public float SoulSiphonScaling = 1;
        public int ConsSoulChanceMult;
        public bool SoulSickle = false;

        public bool BurningAura = false;
        public bool BurningStone = false;

        public bool Sharpened = false;
        public bool AmmoBox = false;
        public bool AmmoReservationPotion = false;
        public float AmmoReservationDamageScaling = 1f;

        public bool SOADrain = false;

        public int supersonicLevel = 0;

        public int darkSoulQuantity;

        //An int because it'll probably be necessary to split it into multiple levels
        public int manaShield = 0;

        public int staminaShield = 0;
        public bool DragonCrestShieldEquipped = false;

        //How many more frames the Mana Shield is disabled after using a mana potion
        public int manaShieldCooldown = 0;
        //What frame of the shield's animation it's on
        public int shieldFrame = 0;

        public int staminaShieldFrame = 0;
        //Did they have the shield up last frame?
        public bool shieldUp = false;

        public bool staminaShieldUp = false;

        public bool chestBankOpen;
        public int chestBank = -1;

        public bool chestPiggyOpen;
        public int chestPiggy = -1;

        public int FracturingArmor = 1;
        public bool HasFracturingArmor = false;

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
        public bool StarlightShardRestoration;
        int healingTimer = 0;
        float restorationTimer = 0;

        public Item[] PotionBagItems = new Item[PotionBagUIState.POTION_BAG_SIZE];
        public int potionBagCountdown = 0; //You can't move items around if an item is still 'in use'. This lets us delay opening the bag until that finishes.

        public UIItemSlot SoulSlot;

        public int MaxAcquiredHP; //To prevent purging stones and humanity from raising hp above your max acquired hp from life crystals and life fruit.

        public int PiercingGazeCharge;

        public bool PowerWithin;
        public int StaminaReaper = 0;

        public bool AuraOfIlluminance;

        public List<int> ActivePermanentPotions;

        public Vector2[] oldPos = new Vector2[60];

        public bool CowardsAffliction;
        public bool GravityField;
        public float TextCooldown; //Used for if we want text to display when a Thing happens, but not to spam the player
        public float FieldTimer;

        public bool startedQuest;
        public bool finishedQuest;
        public bool touchedSurface;

        public tsorcAuraState CurrentAuraState;

        public float rotation3d;
        public static Vector2 RealMouseWorld;

        public string DeathText;
        bool setDeathText = false;
        public static List<string> DeathTextList;

        public override void ResetEffects()
        {
            SilverSerpentRing = false;
            SoulSerpentRing = false;
            DragonStoneImmunity = false;
            if (Main.netMode == NetmodeID.Server || Main.netMode == NetmodeID.SinglePlayer)
            {
                DragonStonePotency = false;
            }
            SoulReaper = 5;

            DragoonBoots = false;
            //player.eocDash = 0;
            Player.armorEffectDrawShadowEOCShield = false;
            UndeadTalisman = false;

            HasShadowSickle = false;

            SteraksGage = false;
            InfinityEdge = false;
            LudensTempest = false;
            Goredrinker = false;

            BoneRing = false;

            ChloranthyRing1 = false;
            ChloranthyRing2 = false;
            WolfRing = false;
            BarrierRing = false;

            BrokenSpirit = false;

            MythrilOrichalcumCritDamage = false;
            Shunpo = false;
            ChallengersGloveCritDamage = false;
            WhipCritHitboxSize = 1;

            PhoenixSkull = false;

            SummonTagStrength = 1f;
            SummonTagDuration = 1f;

            MaxMinionTurretMultiplier = 1;

            MythrilBulwark = false; 
            IceboundMythrilAegis = false;

            Celestriad = false;
            MaxManaAmplifier = 0f;
            CelestialCloak = false;

            DragoonBoots = false;
            OldWeapon = false;
            Miakoda = false;
            MagmaArmor = false;
            PortlyPlateArmor = false;

            WaspPower = false;
            DemonPower = false;
            WitchPower = false;

            HollowSoldierAgility = false;
            SmoughShieldSkills = false;
            BurdenOfSmough = false;
            SmoughAttackSpeedReduction = false;

            RTQ2 = false;
            DarkInferno = false;
            BoneRevenge = false;
            SoulSiphon = false;
            SoulSiphonScaling = 1f;

            CrimsonDrain = false;
            Shockwave = false;
            SOADrain = false;
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
            staminaShield = 0;

            ConditionOverload = false;
            supersonicLevel = 0;
            ConsSoulChanceMult = 0;
            SoulSickle = false;
            TornWings = false;

            Crippled = false;
            ShadowWeight = false;
            ReflectionShiftEnabled = false;

            Sharpened = false;
            AmmoBox = false;
            AmmoReservationPotion = false;
            AmmoReservationDamageScaling = 1f;

            PhazonCorruption = false;
            LifegemHealing = false;
            RadiantLifegemHealing = false;
            StarlightShardRestoration = false;

            PowerWithin = false;
            BurningAura = false;
            BurningStone = false;
            StaminaReaper = 0;

            ActivePermanentPotions = new List<int>();
            CowardsAffliction = false;
            DragonCrestShieldEquipped = false;

            if (!HasFracturingArmor)
            {
                FracturingArmor = 1;
            }
            HasFracturingArmor = false;
            GravityField = false;
            DragoonHorn = false;
            if (!Player.channel) rotation3d = 0;
            MeleeArmorVamp10 = false;
            AuraOfIlluminance = false;
            CurrentAuraState = tsorcAuraState.None;
        }
        public override void PreUpdate()
        {
            RealMouseWorld = Main.MouseWorld;
            tsorcRevampPlayerAuraDrawLayers.HandleAura(this);
            //Fixes bug where switching from a better to a worse pair of wings keeps the previous wing time cap
            if (Player.wingTime > Player.wingTimeMax)
            {
                Player.wingTime = Player.wingTimeMax;
            }


            //No more Distorted debuff
            Player.buffImmune[BuffID.VortexDebuff] = true;

            //Hacky but necessary. Mount items seem to bypass the CanUseItem check for some stupid reason
            if (!NPC.downedBoss2)
            {
                Player.ClearBuff(BuffID.SlimeMount);
            }
            if (!NPC.downedMechBoss3)
            {
                Player.ClearBuff(BuffID.QueenSlimeMount);
            }

            if (tsorcRevampWorld.BossAlive)
            {
                Player.AddBuff(ModContent.BuffType<Buffs.BossZenBuff>(), 5);
            }
            if (TextCooldown > 0)
            {
                TextCooldown--;
            }

            Point point = Player.Center.ToTileCoordinates();
            Player.ZoneBeach = Player.ZoneOverworldHeight && (point.X < 1000 || point.X > Main.maxTilesX - 8398); //default 380 and 380

            Player.fullRotationOrigin = new Vector2(11, 22);
            SetDirection(true);

            darkSoulQuantity = Player.CountItem(ModContent.ItemType<DarkSoul>(), 999999);

            //the item in the soul slot will only ever be souls, so we dont need to check type
            if (SoulSlot.Item.stack > 0) { darkSoulQuantity += SoulSlot.Item.stack; }
            
            if (!Player.HasBuff(ModContent.BuffType<Bonfire>()))
            { //this ensures that BonfireUIState is only visible when within Bonfire range
                if (Player.whoAmI == Main.LocalPlayer.whoAmI)
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
                if (Player.whoAmI == Main.myPlayer)
                {
                    if (!PotionBagUIState.Visible)
                    {
                        Player.chest = -1;
                        Main.playerInventory = true;
                        PotionBagUIState.Visible = true;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
                    }
                    else
                    {
                        PotionBagUIState.Visible = false;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuClose);
                    }
                }
            }


            #region Miakoda

            MiakodaEffectsTimer++;

            if (MiakodaFullHeal1)
            { //dust loop on player the instant they get healed
                for (int d = 0; d < 100; d++)
                {
                    int dust = Dust.NewDust(Player.position, Player.width, Player.height, 107, 0f, 0f, 30, default, .75f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 3.5f);
                    Main.dust[dust].noGravity = true;
                }
            }

            if (MiakodaCrescentDust1)
            { //dust loop on player the instant they get imbue
                for (int d = 0; d < 100; d++)
                {
                    int dust = Dust.NewDust(Player.position, Player.width, Player.height, 164, 0f, 0f, 30, default, 1.2f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 5f);
                    Main.dust[dust].noGravity = false;
                }
            }
            if (MiakodaCrescentBoost)
            {
                MiakodaCrescentBoostTimer++;
            }
            if (MiakodaCrescentBoostTimer > Items.Pets.MiakodaCrescent.BoostDuration * 60)
            {
                Player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentBoost = false;
                MiakodaCrescentBoostTimer = 0;
            }

            if (MiakodaNewDust1)
            { //dust loop on player the instant they get boost
                for (int d = 0; d < 100; d++)
                {
                    int dust = Dust.NewDust(Player.position, Player.width, Player.height, 57, 0f, 0f, 50, default, 1.2f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(2f, 7.5f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (MiakodaNewBoost)
            {
                MiakodaNewBoostTimer++;
                Player.armorEffectDrawShadow = true;

            }
            if (MiakodaNewBoostTimer > Items.Pets.MiakodaNew.BoostDuration * 60)
            {
                Player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewBoost = false;
                MiakodaNewBoostTimer = 0;
            }

            #endregion

            #region manashield
            if (manaShield > 0)
            {
                shieldFrame++;
                if (shieldFrame > 23)
                {
                    shieldFrame = 0;
                }

                //Disable Mana Regen Potions
                Player.manaRegenBuff = false;
                Player.buffImmune[BuffID.ManaRegeneration] = true;
            }
            #endregion manashield

            #region Abyss Shader
            bool hasCoA = false;

            if (Player.whoAmI == Main.myPlayer)
            {

                //does the player have a covenant of artorias
                for (int i = 3; i < (8 + Player.extraAccessorySlots); i++)
                {
                    if (Player.armor[i].type == ModContent.ItemType<Items.Accessories.Defensive.CovenantOfArtorias>())
                    {
                        hasCoA = true;
                        break;
                    }
                }

                //if they do, and the shader is inactive
                if (hasCoA && !(Filters.Scene["tsorcRevamp:TheAbyss"].Active))
                {
                    Filters.Scene.Activate("tsorcRevamp:TheAbyss");
                }

                //if the abyss shader is active and the player is no longer wearing the CoA                
                if (Filters.Scene["tsorcRevamp:TheAbyss"].Active && !hasCoA)
                {
                    Filters.Scene["tsorcRevamp:TheAbyss"].Deactivate();
                }
            }

            #endregion

            #region Reflection Shift
            if (ReflectionShiftEnabled)
            {

                int dashCooldown = 30;
                if (ReflectionShiftKeypressTime > 0)
                {
                    ReflectionShiftKeypressTime--;
                }
                else
                {
                    //This would have looked so much nicer if controlUp, controlLeft, etc were all in an array like doubleTapCardinalTimer, but...
                    if (Player.controlUp && keyPrimed[DashUp] == 0)
                    {
                        keyPrimed[DashUp] = 1;
                    }
                    if (Player.releaseUp && keyPrimed[DashUp] == 1)
                    {
                        keyPrimed[DashUp] = 2;
                    }
                    if (Player.doubleTapCardinalTimer[DashUp] == 0)
                    {
                        keyPrimed[DashUp] = 0;
                    }
                    if (Player.controlUp && Player.doubleTapCardinalTimer[DashUp] < 15 && keyPrimed[DashUp] == 2)
                    {
                        ReflectionShiftKeypressTime = dashCooldown;
                        ReflectionShiftState.Y = -1;
                    }

                    if (Player.controlLeft && keyPrimed[DashLeft] == 0)
                    {
                        keyPrimed[DashLeft] = 1;
                    }
                    if (Player.releaseLeft && keyPrimed[DashLeft] == 1)
                    {
                        keyPrimed[DashLeft] = 2;
                    }
                    if (Player.doubleTapCardinalTimer[DashLeft] == 0)
                    {
                        keyPrimed[DashLeft] = 0;
                    }
                    if (Player.controlLeft && Player.doubleTapCardinalTimer[DashLeft] < 15 && keyPrimed[DashLeft] == 2)
                    {
                        ReflectionShiftKeypressTime = dashCooldown;
                        ReflectionShiftState.X = -1;
                    }

                    if (Player.controlRight && keyPrimed[DashRight] == 0)
                    {
                        keyPrimed[DashRight] = 1;
                    }
                    if (Player.releaseRight && keyPrimed[DashRight] == 1)
                    {
                        keyPrimed[DashRight] = 2;
                    }
                    if (Player.doubleTapCardinalTimer[DashRight] == 0)
                    {
                        keyPrimed[DashRight] = 0;
                    }
                    if (Player.controlRight && Player.doubleTapCardinalTimer[DashRight] < 15 && keyPrimed[DashRight] == 2)
                    {
                        ReflectionShiftKeypressTime = dashCooldown;
                        ReflectionShiftState.X = 1;
                    }

                    if (Player.controlDown && keyPrimed[DashDown] == 0)
                    {
                        keyPrimed[DashDown] = 1;
                    }
                    if (Player.releaseDown && keyPrimed[DashDown] == 1)
                    {
                        keyPrimed[DashDown] = 2;
                    }
                    if (Player.doubleTapCardinalTimer[DashDown] == 0)
                    {
                        keyPrimed[DashDown] = 0;
                    }
                    if (Player.controlDown && Player.doubleTapCardinalTimer[DashDown] < 15 && keyPrimed[DashDown] == 2)
                    {
                        ReflectionShiftKeypressTime = dashCooldown;
                        ReflectionShiftState.Y = 1;
                    }
                }
            }
            #endregion


            if (Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 10 && (Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Bows.SagittariusBow>() || Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Bows.ArtemisBow>()
                || Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Bows.CernosPrime>() || Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Magic.DivineSpark>() || Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Magic.DivineBoomCannon>()))
            {
                Player.channel = false;
            }

            if (Player.itemAnimation != 0 && (Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Magic.DivineSpark>() || Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Magic.DivineBoomCannon>()))
            {
                Player.statMana -= 1;
                if (Player.statMana < 1) { Player.channel = false; }
                if (Player.statMana < 0) { Player.statMana = 0; }

            }

            if (Framing.GetTileSafely(new Point((int)Player.position.X / 16, (int)Player.position.Y / 16)).WallType == WallID.StarlitHeavenWallpaper)
            {
                Player.AddBuff(BuffID.Darkness, 60);
            }

            if (MaxAcquiredHP < Player.statLifeMax)
            {
                MaxAcquiredHP = Player.statLifeMax;
            }

            if (Player.HasBuff(ModContent.BuffType<NondescriptOwlBuff>()) && Player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.Archer.NondescriptOwlProjectile>()] == 0) {
                Item staff = new();
                staff.SetDefaults(ModContent.ItemType<Items.Weapons.Summon.PeculiarSphere>());
                int damage = staff.damage;
                int p = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Summon.Archer.NondescriptOwlProjectile>(), damage, 0, Player.whoAmI);
                Main.projectile[p].originalDamage = damage;
            }

            if (Player.HasBuff(ModContent.BuffType<SunsetQuasarBuff>()) && Player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.SunsetQuasar.SunsetQuasarMinion>()] == 0) {
                Item staff = new();
                staff.SetDefaults(ModContent.ItemType<Items.Weapons.Summon.SunsetQuasar>());
                int damage = staff.damage;
                int p = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Summon.SunsetQuasar.SunsetQuasarMinion>(), damage, 0, Player.whoAmI);
                Main.projectile[p].originalDamage = damage;
            }
            //--------------------

            //TODO REMOVE WHEN FINALIZED

            //--------------------
            return;




            if (finishedQuest)
                return;
            //{72, 492} is the house on the very left edge, where the shaman elder lives
            //64x32 rectangle to start the quest
            if (Collision.CheckAABBvAABBCollision(Player.position, new Vector2(Player.width, Player.height), new Vector2(72, 492) * 16, new Vector2(64, 32) * 16) && !startedQuest) {
                startedQuest = true;
                Main.NewText(LangUtils.GetTextValue("Quest.Start"));
            }
            if (Player.whoAmI != Main.myPlayer)
                return;
            if (!startedQuest)
                return;

            //stay underground!
            if (!Player.ShoppingZone_BelowSurface) {
                touchedSurface = true;
            }

            //if the player is on the left edge of the world and hasnt gone past the angler's house, they're in the grace zone
            if (Player.position.ToTileCoordinates16().X < 350) {
                touchedSurface = false;
            }

            if (touchedSurface) {
                startedQuest = false;
                Main.NewText(LangUtils.GetTextValue("Quest.Surface"));
            }

            //teleporting through a one block thick wall horizontally with a
            //rod of discord gives just over 36.1f distance if done perfectly.
            //with max movespeed gear the most i could get was around 27f falling
            //flying can get you as high as 38ish, but that's only possible outdoors
            //because there are no ceilings, and at that point youre already disqualified.
            //its possible to get over 36f by flying up the side of the pyramid though...
            //just dont do that, i guess? its kinda hard anyway
            if (Vector2.Distance(Player.OldPos(1), Player.position) > 36f) {
                startedQuest = false;
                Main.NewText(LangUtils.GetTextValue("Quest.Teleport"));
            }

            //{7909, 1081} is the underwater observatory's top left corner, and {320, 119} is its rectangular size
            if (Collision.CheckAABBvAABBCollision(Player.position, new Vector2(Player.width, Player.height), new Vector2(7909, 1081) * 16, new Vector2(320, 119) * 16)) {
                Main.NewText(LangUtils.GetTextValue("Quest.Finish"));
                Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ItemID.RodofDiscord);
                finishedQuest = true;
            }
        }

        public override void PreUpdateBuffs()
        {
            //Gives the DPS meter effect for free
            //Do not ask why it is called "accDreamCatcher"
            //This has to go here for it to work. Don't ask me why either. I value my sanity too much to go check.
            Player.accDreamCatcher = true;

            if (chestBank >= 0)
            {
                DoPortableChest<SafeProjectile>(ref chestBank, ref chestBankOpen);
            }
            if (chestPiggy >= 0)
            {
                DoPortableChest<PiggyBankProjectile>(ref chestPiggy, ref chestPiggyOpen);
            }

            if (!Main.playerInventory)
            {
                chestPiggy = -1;
                chestPiggyOpen = false;
                chestBank = -1;
                chestBankOpen = false;
            }

            if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (Player.GetModPlayer<tsorcRevampPlayer>().SoulVessel > 0)
                {
                    Player.statManaMax2 += Player.GetModPlayer<tsorcRevampPlayer>().SoulVessel * Items.SoulVessel.MaxManaIncrease;
                }
            }


            
            if (!NPC.downedGolemBoss && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && !NPC.downedEmpressOfLight)
            {
                Vector2 arena = new Vector2(4468, 365);
                float distance = Vector2.DistanceSquared(Player.Center / 16, arena);

                //If EoL isn't alive, do the big forcefield
                if (!NPC.AnyNPCs(NPCID.HallowBoss))
                {
                    if (distance < 42500)
                    {
                        float proximity = distance - 22500;
                        proximity /= 20000f;
                        proximity = 1 - proximity;
                        for (int i = 0; i < 10f * proximity * proximity; i++)
                        {
                            Vector2 diff = Player.Center - arena * 16;
                            diff.Normalize();
                            diff *= 2400;

                            diff = diff.RotatedBy(Main.rand.NextFloat(-MathHelper.Pi / 30, MathHelper.Pi / 30));

                            Vector2 vel = diff;
                            vel.Normalize();
                            vel = vel.RotatedBy(Main.rand.NextBool() ? MathHelper.PiOver2 : -MathHelper.PiOver2);
                            Dust.NewDustPerfect(arena * 16 + diff, DustID.ShadowbeamStaff, vel, default, default, 1.5f * proximity).noGravity = true;
                        }

                        if (distance < 22500)
                        {
                            for (int p = 0; p < 1000; p++)
                            {
                                if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && Main.projectile[p].aiStyle == 7)
                                {
                                    FieldTimer++;
                                    if (FieldTimer == 1)
                                    {
                                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("EoLForcefield.Grapple"), Color.Orange);
                                        TextCooldown = 350;
                                    }
                                    if (FieldTimer >= 340)
                                    {
                                        Player.velocity += new Vector2(0, -15);
                                        Player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 30);
                                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("EoLForcefield.Snap"), Color.Red);
                                    }
                                }
                            }

                            Player.velocity += UsefulFunctions.Aim(new Vector2(4484, 355) * 16, Player.Center, 20);
                            if (TextCooldown <= 0)
                            {
                                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("EoLForcefield.Expelled"), Color.Purple);
                                TextCooldown = 240;
                            }
                        }
                        else
                        {
                            FieldTimer = 0;
                        }
                    }
                }
            }
        }

        public override void PostUpdateBuffs()
        {
            foreach (Item thisItem in PotionBagItems)
            {
                if (thisItem != null && !thisItem.IsAir)
                {
                    thisItem.ModItem?.UpdateInventory(Player);
                }
            }

            if (MiakodaCrescentBoost)
            {
                Player.GetDamage(DamageClass.Generic) *= 1.07f;
            }

            if (MiakodaNewBoost)
            {
                Player.moveSpeed += Items.Pets.MiakodaNew.MoveSpeed2 / 100f;
                Player.endurance *= 0.5f;
                Player.endurance += Items.Pets.MiakodaNew.DamageReduction / 100f;
                Player.noKnockback = true;
            }

            if (Player.HasBuff(BuffID.WellFed))
            {
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += MinorEdits.BotCWellFedStaminaRegen / 100f;
            }
            if (Player.HasBuff(BuffID.WellFed2))
            {
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += MinorEdits.BotCPlentySatisfiedStaminaRegen / 100f;
            }
            if (Player.HasBuff(BuffID.WellFed3))
            {
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += MinorEdits.BotCExquisitelyStuffedStaminaRegen / 100f;
            }


            if (Player.HasBuff(BuffID.TheTongue))
            {
                for (int i = 0; i < 9; i++)
                {
                    CombatText.NewText(Player.Hitbox, CombatText.DamagedFriendly, 999999999, true);
                }
                Player.KillMe(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(Player.name + LangUtils.GetTextValue("DeathText.WallDeathReason")), 999999999, 1);
            }

            if (!Player.HasBuff(ModContent.BuffType<CurseBuildup>()))
            {
                CurseLevel = 1; //Not sure why 1 is the default
            }

            if (!Player.HasBuff(ModContent.BuffType<PowerfulCurseBuildup>()))
            {
                PowerfulCurseLevel = 1; //Not sure why 1 is the default
            }
        }
        public override void PostUpdateEquips()
        {
            if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                Player.GetAttackSpeed(DamageClass.Melee) *= BotCMeleeBaseAttackSpeedMult + (BotCLethalTempoStacks * BotCLethalTempoBonus);

                CurrentTotalRangedCritChance = Player.GetTotalCritChance(DamageClass.Ranged); //catch total ranged crit chance
                Player.GetCritChance(DamageClass.Ranged) -= Player.GetTotalCritChance(DamageClass.Ranged); //subtract total ranged crit chance from ranged crit chance because you can't alter total ranged crit chance
                Player.GetCritChance(DamageClass.Ranged) += (CurrentTotalRangedCritChance + (BotCCurrentAccuracyPercent * BotCAccuracyMaxFlatCrit)) * (BotCRangedBaseCritMult + (BotCCurrentAccuracyPercent * BotCAccuracyMaxCritMult)); //return total ranged crit chance in a way that lets you multiply it with accuracy

                Player.GetDamage(DamageClass.Magic) *= 1f + (BotCMagicDamageAmp / 100f);
                Player.GetAttackSpeed(DamageClass.Magic) *= 1f + (BotCMagicAttackSpeedAmp / 100f);
                if (Main.npc.Any(n => n?.active == true && n.boss && n != Main.npc[200]) || !Player.HasBuff(ModContent.BuffType<Bonfire>()))
                {
                    Player.manaRegenDelay = 100;
                }

                Player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) *= 1.177f; //this is to neutralize the base melee attack speed nerf: 0.85 * 1.177 = 1.00045
                Player.GetDamage(DamageClass.Summon) *= BotCSummonBaseDamageMult + (BotCConquerorStacks * BotCConquerorBonus);
                Player.whipRangeMultiplier *= BotCWhipRangeMult;

                if (Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < Player.GetModPlayer<tsorcRevampStaminaPlayer>().minionStaminaCap)
                {
                    Player.lifeRegen /= 2;
                }


                #region Lifegem Healing and Starlight Shard Restoration


                if (LifegemHealing)
                {
                    healingTimer++;

                    if (healingTimer == Lifegem.HealingDivisor)
                    {
                        Player.statLife += 1;
                        healingTimer = 0;
                    }
                }

                if (RadiantLifegemHealing)
                {
                    healingTimer++;

                    if (healingTimer == RadiantLifegem.HealingDivisor)
                    {
                        Player.statLife += 1;
                        healingTimer = 0;
                    }
                }

                if (!RadiantLifegemHealing && !LifegemHealing)
                {
                    healingTimer = 0;
                }

                if (StarlightShardRestoration) //Restores 1% of maximum mana over 12 seconds by default
                {
                    restorationTimer += (float)Player.statManaMax2 / (100f * 60f) * (1f + ((float)Player.manaRegenBonus / 10f)); //1% of maximum mana per second, since there are 60 ticks per second, manaregenbonuses are usually in the double digits so this is insane scaling

                    if (restorationTimer >= 10f)
                    {
                        Player.statMana += 10;
                        restorationTimer -= 10f;
                    }
                    if (restorationTimer >= 1f)
                    {
                        Player.statMana += 1;
                        restorationTimer -= 1f;
                    }
                }

                if (!StarlightShardRestoration)
                {
                    restorationTimer = 0;
                }

                #endregion
            }
            if (ModContent.GetInstance<tsorcRevampConfig>().DisableGloveAutoswing)
            {
                Player.autoReuseGlove = false;
            }
            if (ModContent.GetInstance<tsorcRevampConfig>().DisableAutomaticQuickMana)
            {
                Player.manaFlower = false;
            }
            if (ModContent.GetInstance<tsorcRevampConfig>().DisableRifleScopeZoom)
            {
                Player.scope = false;
            }
            if (manaShield > 0)
            {
                Player.manaRegenBuff = false;
            }

            if (Player.position.X == Player.oldPosition.X)
            {
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 1.5f;
            }

            if (ShadowWeight)
            {
                Player.hasJumpOption_Blizzard = false;
                Player.hasJumpOption_Fart = false;
                Player.hasJumpOption_Sail = false;
                Player.hasJumpOption_Sandstorm = false;
                Player.hasJumpOption_Unicorn = false;
                Player.canRocket = false;
                Player.rocketTime = 0;
                Player.jumpBoost = false;
                Player.wingTime = 0;
                float speedCap = 12;
                if (Player.velocity.X > speedCap)
                {
                    Player.velocity.X = speedCap;
                }
                if (Player.velocity.X < -speedCap)
                {
                    Player.velocity.X = -speedCap;
                }
            }

            if (TornWings)
            {
                Player.wingTime = 0;
                Player.moveSpeed *= 0.8f;
            }

            if (Crippled)
            {
                Player.hasJumpOption_Blizzard = false;
                Player.hasJumpOption_Cloud = false;
                Player.hasJumpOption_Fart = false;
                Player.hasJumpOption_Sail = false;
                Player.hasJumpOption_Sandstorm = false;
                Player.hasJumpOption_Unicorn = false;
                Player.canRocket = false;
                Player.rocketTime = 0;
                Player.jumpBoost = false;
                Player.jumpSpeedBoost = 0f;
                Player.wingTime = 0;
                Player.moveSpeed *= 0.9f;
            }

            for (int i = 0; i < 50; i++)
            {
                //block souls from going in normal inventory slots
                tsorcRevampPlayer modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
                if (Player.inventory[i].type == ModContent.ItemType<DarkSoul>())
                {
                    //if the player's soul slot is empty
                    if (modPlayer.SoulSlot.Item.type != ModContent.ItemType<DarkSoul>())
                    {
                        modPlayer.SoulSlot.Item = Player.inventory[i].Clone();
                    }
                    else
                    {
                        modPlayer.SoulSlot.Item.stack += Player.inventory[i].stack;
                    }
                    //dont send the souls to the normal inventory
                    Player.inventory[i].TurnToAir();
                }

            }


            if (Shockwave)
            {

                if (Player.controlDown && Player.velocity.Y != 0f)
                {
                    Player.gravity += 5f;
                    Player.maxFallSpeed *= 1.25f;
                    if (!Falling)
                    {
                        fallStartY = Player.position.Y;
                    }
                    if (Player.velocity.Y > 12f)
                    {
                        Falling = true;
                        StopFalling = 0;
                        Player.noKnockback = true;
                    }
                }
                if (Player.velocity.Y == 0f && Falling && Player.controlDown && !Player.wet)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        int dustIndex2 = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y), Player.width, Player.height, 31, 0f, 0f, 100);
                        Main.dust[dustIndex2].scale = 0.1f + Main.rand.Next(5) * 0.1f;
                        Main.dust[dustIndex2].fadeIn = 1.5f + Main.rand.Next(5) * 0.1f;
                        Main.dust[dustIndex2].noGravity = true;
                    }
                    FallDist = (int)((Player.position.Y - fallStartY) / 16);
                    if (FallDist > 5)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Player.Center);
                        for (int i = -9; i < 10; i++)
                        { //19 projectiles
                            Vector2 shotDirection = new Vector2(0f, -16f);
                            int shockwaveShot = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, new Vector2(0f, -7f), ModContent.ProjectileType<Projectiles.Shockwave>(), (int)(FallDist * (Main.hardMode ? 2.6f : 2.4)), 12, Player.whoAmI);
                            Main.projectile[shockwaveShot].velocity = shotDirection.RotatedBy(MathHelper.ToRadians(0 - (10f * i))); // (180 / (projectilecount - 1))
                        }
                    }


                    Falling = false;
                }
                if (Player.velocity.Y <= 2f)
                {
                    StopFalling++;
                }
                else
                {
                    StopFalling = 0;
                }
                if (StopFalling > 1)
                {
                    Falling = false;
                }

            }
            if (!Shockwave)
            {
                Falling = false;
            }

            if (CrimsonDrain)
            {

                for (int l = 0; l < 200; l++)
                {
                    NPC nPC = Main.npc[l];
                    if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[ModContent.BuffType<CrimsonBurn>()] && Vector2.Distance(Player.Center, nPC.Center) <= 240)
                    {
                        nPC.AddBuff(ModContent.BuffType<CrimsonBurn>(), 2);
                    }
                }

                Vector2 centerOffset = new Vector2(Player.Center.X + 2 - Player.width / 2, Player.Center.Y + 6 - Player.height / 2);
                for (int j = 1; j < 30; j++)
                {
                    var x = Dust.NewDust(centerOffset + (Vector2.One * (j % 8 == 0 ? Main.rand.Next(15, 150) : 150)).RotatedByRandom(Math.PI * 4.0), Player.width / 2, Player.height / 2, 235, Player.velocity.X, Player.velocity.Y);
                    Main.dust[x].noGravity = true;
                }

            }

            #region Soul Siphon Dusts


            if (SoulSiphon)
            {

                if (Main.rand.NextBool(3)) //outermost "ring"
                {
                    int num5 = Dust.NewDust(Player.position, Player.width, Player.height, 89, 0f, 0f, 120, default, 1f);
                    Main.dust[num5].noGravity = true;
                    Main.dust[num5].velocity *= 0.75f;
                    Main.dust[num5].fadeIn = 1.5f;
                    Vector2 vector = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vector.Normalize();
                    vector *= (float)Main.rand.Next(50, 100) * 0.04f;
                    Main.dust[num5].velocity = vector;
                    vector.Normalize();
                    vector *= Main.rand.Next(220, 900);
                    Main.dust[num5].position = Player.Center - vector;
                }

                if (Main.rand.NextBool(6))
                {
                    int x = Dust.NewDust(Player.position, Player.width, Player.height, 89, Player.velocity.X, Player.velocity.Y, 120, default, 1f);
                    Main.dust[x].noGravity = true;
                    Main.dust[x].velocity *= 0.75f;
                    Main.dust[x].fadeIn = 1.3f;
                    Vector2 vector = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vector.Normalize();
                    vector *= (float)Main.rand.Next(50, 100) * 0.05f; //velocity towards player
                    Main.dust[x].velocity = vector;
                    vector.Normalize();
                    vector *= 200f; //spawn distance from player
                    Main.dust[x].position = Player.Center - vector;

                    //Vector2.Normalize(start - end) * someSpeed //start and end are also Vector2 // Aparently another way to make things move toward each other

                }

                if (Main.rand.NextBool(3))
                {
                    int z = Dust.NewDust(Player.position, Player.width, Player.height, 89, 0f, 0f, 120, default, 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 0.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(50, 100) * 0.052f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 150f;
                    Main.dust[z].position = Player.Center - vectorother;
                }

                if (Main.rand.NextBool(2))
                {
                    int z = Dust.NewDust(Player.position, Player.width, Player.height, 89, 0f, 0f, 120, default, 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 0.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(50, 100) * 0.055f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 90f;
                    Main.dust[z].position = Player.Center - vectorother;
                }

                if (Main.rand.NextBool(2)) //innermost "ring"
                {
                    int z = Dust.NewDust(Player.position, Player.width, Player.height, 89, 0f, 0f, 120, default, 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 2.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(50, 100) * 0.055f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 45f;
                    Main.dust[z].position = Player.Center - vectorother;
                }
            }

            #endregion
            #region consistent hellstone and spike damage
            float REDUCE = CheckReduceDefense(Player.position, Player.width, Player.height, Player.fireWalk); // <--- added firewalk parameter
            if (REDUCE != 0)
            {
                REDUCE = 1f - REDUCE;
                Player.statDefense *= 0;
                Player.endurance = 0;
            }
            #endregion
            #region boss zen
            GiveBossZen = CheckBossZen();
            if (GiveBossZen && ModContent.GetInstance<tsorcRevampConfig>().BossZenConfig)
            {
                Player.AddBuff(ModContent.BuffType<BossZenBuff>(), 2, false);
                if (Player.position.Y < 3200)
                {
                    Player.AddBuff(ModContent.BuffType<GravityField>(), 2, false);
                }
            }
            #endregion
            #region boss magnet
            //actual item grab range is in GlobalItem::GrabRange
            if (bossMagnet)
            {
                bossMagnetTimer--;
            }
            if (bossMagnetTimer == 0)
            {
                bossMagnet = false;
            }
            #endregion

            float shiftDistance = 7;
            #region Reflection Shift

            if (ReflectionShiftKeypressTime > 20)
            {
                Player.immune = true;
            }

            if (ReflectionShiftState != Microsoft.Xna.Framework.Vector2.Zero)
            {
                Player.timeSinceLastDashStarted = 0;
                //Initiate Dash
                for (int i = 0; i < 30; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(64, 64);
                    Vector2 velocity = new Vector2(-2, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                    Dust.NewDustPerfect(Player.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
                }
                if (Collision.CanHit(Player.Center, 1, 1, Player.Center + ReflectionShiftState * shiftDistance * 16, 1, 1) || Collision.CanHitLine(Player.Center, 1, 1, Player.Center + ReflectionShiftState * shiftDistance * 16, 1, 1))
                {
                    Player.Center += ReflectionShiftState * shiftDistance * 16; //Teleport distance
                }
                FastFallTimer = 30;
                Player.velocity = ReflectionShiftState * 20; //Dash speed
                ReflectionShiftState = Vector2.Zero;

                for (int i = 0; i < 30; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(64, 64);
                    Vector2 velocity = new Vector2(5, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                    Dust.NewDustPerfect(Player.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
                }
            }
            #endregion
            if (PhoenixSkull && Player.HasBuff(ModContent.BuffType<PhoenixRebirthCooldown>()))
            {
                Player.buffTime[Player.FindBuffIndex(ModContent.BuffType<PhoenixRebirthCooldown>())]--;
            }
            if (SteraksGage && Player.statLife < (Player.statLifeMax2 * Items.Accessories.Melee.SteraksGage.LifeThreshold / 100f) && !Player.HasBuff(ModContent.BuffType<SteraksGageCooldown>()))
            {
                Player.statLife += Items.Accessories.Melee.SteraksGage.ShieldHeal;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/SteraksGageShield") with { Volume = 1f }, Player.Center);
                Player.AddBuff(ModContent.BuffType<SteraksGageCooldown>(), Items.Accessories.Melee.SteraksGage.Cooldown * 60);
            }
            if (DragoonBoots && DragoonBootsEnable)
            {
                //Player.jumpSpeed += 10f; why
                Player.jumpSpeedBoost += 10f;
            }
            if (DragoonHorn && (((Player.gravDir == 1f) && (Player.velocity.Y > 0)) || ((Player.gravDir == -1f) && (Player.velocity.Y < 0))))
            {
                Player.GetDamage(DamageClass.Melee) *= 1f + Items.Accessories.Expert.DragoonHorn.MeleeDmgMult / 100f;
            }
            if (BrokenSpirit)
            {
                Player.noKnockback = false;
            }
            if (MaxManaAmplifier > 0f)
            {
                Player.statManaMax2 = (int)(Player.statManaMax2 * (1f + MaxManaAmplifier / 100f));
            }
            if (CelestialCloak)
            {
                Player.thorns += 0.1f + (Player.statManaMax2 / 50f);
            }
            Player.maxMinions *= MaxMinionTurretMultiplier;
            Player.maxTurrets *= MaxMinionTurretMultiplier;
        }

        public override void PostUpdateRunSpeeds()
        {
            if (supersonicLevel != 0)
            {                
                float moveSpeedPercentBoost = 1;
                float baseSpeed = 1;

                //SupersonicBoots
                if (supersonicLevel == 1)
                {
                    //moveSpeedPercentBoost is what percent of a player's moveSpeed bonus should be applied to their max running speed
                    //For vanilla hermes boots and their upgrades, this is 0
                    moveSpeedPercentBoost = 0.35f;
                    //6f is hermes boots speed.
                    baseSpeed = 6f;
                    Player.moveSpeed += 0.2f;
                }
                //SupersonicWings
                if (supersonicLevel == 2)
                {
                    moveSpeedPercentBoost = 0.5f;
                    baseSpeed = 6.8f;
                    Player.moveSpeed += 0.3f;
                }
                //SupersonicWings2
                if (supersonicLevel == 3)
                {

                    moveSpeedPercentBoost = 1f;
                    baseSpeed = 7.5f;
                    Player.moveSpeed += 0.6f;
                }


                //((player.moveSpeed * 0.5f) + 0.5) means 50% of the player's moveSpeed bonus will be applied
                //The general form is ((player.moveSpeed * %theyshouldget) + (1 - %theyshouldget))
                Player.accRunSpeed = baseSpeed * ((Player.moveSpeed * moveSpeedPercentBoost) + (1 - moveSpeedPercentBoost));
                Player.maxRunSpeed = baseSpeed * ((Player.moveSpeed * moveSpeedPercentBoost) + (1 - moveSpeedPercentBoost));

                if (FastFallTimer > 0)
                {
                    Player.maxFallSpeed = 50;
                    FastFallTimer--;
                }
            }

            if (Player.HasBuff<MarilithHold>() || Player.HasBuff<MarilithWind>())
            {
                int? marilithIndex = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.FireFiendMarilith>());
                if (marilithIndex != null)
                {
                    NPC marilith = Main.npc[marilithIndex.Value];

                    Player.velocity -= UsefulFunctions.Aim(Player.Center, marilith.Center, 0.25f);

                    if (Player.HasBuff<MarilithHold>())
                    {
                        Player.velocity = -UsefulFunctions.Aim(Player.Center, marilith.Center, 4f);
                    }

                    /*
                    double angleDiff = UsefulFunctions.CompareAngles(Player.velocity, vectorDiff);
                    if (angleDiff > MathHelper.Pi / 2)
                    {
                    }*/
                }
            }
        }

        public override void UpdateBadLifeRegen()
        {
            if (DarkInferno)
            {
                if (Player.lifeRegen > 0)
                {
                    Player.lifeRegen = 0;
                }
                Player.lifeRegenTime = 0;
                Player.lifeRegen = -11;
                for (int j = 0; j < 4; j++)
                {
                    int dust = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 54, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 54, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 100, default, 1f); //54 was 58
                    Main.dust[dust2].noGravity = true;
                }

                if (Main.rand.NextBool(3))
                {
                    // Render fire particles [every frame]
                    int particle = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 54, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 160, default(Color), 3f);
                    Main.dust[particle].noGravity = true;
                    Main.dust[particle].velocity *= 1.4f;
                    int lol = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 58, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 160, default(Color), 3f);
                    Main.dust[lol].noGravity = true;
                    Main.dust[lol].velocity *= 1.4f;
                }

                // Render smoke particles [every other frame]
                if (Main.GameUpdateCount % 2 == 0)
                {
                    int particle2 = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 58, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 100, default, 1f + (float)Main.rand.Next(2));
                    Main.dust[particle2].noGravity = true;
                    Main.dust[particle2].noLight = true;
                    Main.dust[particle2].fadeIn = 3f;
                }
            }

            if (PhazonCorruption)
            {
                if (Player.lifeRegen > 0)
                {
                    Player.lifeRegen = 0;
                }
                Player.lifeRegenTime = 0;
                Player.lifeRegen = -7;
                for (int j = 0; j < 4; j++)
                {
                    int dust = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 29, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, DustID.FireworkFountain_Blue, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust2].noGravity = true;
                }
            }

            if (SOADrain || PowerWithin)
            {
                if (Player.lifeRegen > 0)
                {
                    Player.lifeRegen = 0;
                }
                Player.lifeRegenTime = 0;
                Player.lifeRegen = -15;
                if (Main.rand.NextBool(3))
                {
                    int dust = Dust.NewDust(Player.position, Player.width, Player.height, 235, Player.velocity.X, Player.velocity.Y, 140, default, 0.8f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (CowardsAffliction)
            {
                if (Player.lifeRegen > 0)
                {
                    Player.lifeRegen = 0;
                }
                Player.lifeRegenTime = 0;
                Player.lifeRegen -= 240;
            }

        }


        public string PickDeathText(Projectile projectile = null)
        {
            string text;
            DeathTextList = new List<string>() {
             LangUtils.GetTextValue("DeathText.01"),
             LangUtils.GetTextValue("DeathText.02"),
             LangUtils.GetTextValue("DeathText.03"),
             LangUtils.GetTextValue("DeathText.04"),
             LangUtils.GetTextValue("DeathText.05"),
             LangUtils.GetTextValue("DeathText.06"),
             LangUtils.GetTextValue("DeathText.07"),
             LangUtils.GetTextValue("DeathText.08"),
             LangUtils.GetTextValue("DeathText.09"),
             LangUtils.GetTextValue("DeathText.10"),
             LangUtils.GetTextValue("DeathText.11"),
             LangUtils.GetTextValue("DeathText.12"),
             LangUtils.GetTextValue("DeathText.13"),
             LangUtils.GetTextValue("DeathText.14"),
             LangUtils.GetTextValue("DeathText.15"),
             LangUtils.GetTextValue("DeathText.16"),
             LangUtils.GetTextValue("DeathText.17"),
             LangUtils.GetTextValue("DeathText.18"),
             LangUtils.GetTextValue("DeathText.19"),
             LangUtils.GetTextValue("DeathText.20"),
             LangUtils.GetTextValue("DeathText.21"),
             LangUtils.GetTextValue("DeathText.22"),
             LangUtils.GetTextValue("DeathText.23"),
             LangUtils.GetTextValue("DeathText.24"),
             LangUtils.GetTextValue("DeathText.25"),
             LangUtils.GetTextValue("DeathText.26"),
             LangUtils.GetTextValue("DeathText.27")
            };

            int option = Main.rand.Next(DeathTextList.Count);
            text = DeathTextList[option];
            if (BearerOfTheCurse && Main.rand.NextBool(5))
            {
                if (Main.rand.NextBool(2))
                {
                    text = LangUtils.GetTextValue("DeathText.BotC1");
                }
                else
                {
                    text = LangUtils.GetTextValue("DeathText.BotC2");
                }
            }

            if (Player.statLifeMax >= 200)
            {
                if (Main.rand.NextBool(20))
                {
                    text = LangUtils.GetTextValue("DeathText.IWantToRemoveThisCaptainObviousShit");
                }
                if (Main.rand.NextBool(100))
                {
                    text = LangUtils.GetTextValue("DeathText.TotallyNotMeantToBeToxic");
                }
            }

            if (Player.GetModPlayer<tsorcRevampPlayer>().BurdenOfSmough)
            {
                if (Main.rand.NextBool(10))
                {
                    text = LangUtils.GetTextValue("DeathText.HeavyRoll");
                }
            }

            int currentBoss = -1;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].boss)
                {
                    currentBoss = i;
                    break;
                }
            }

            if (currentBoss >= 0)
            {
                if (Main.rand.NextBool())
                {
                    text = LangUtils.GetTextValue("DeathText.BossDeath1");
                }
                else if (Main.rand.NextBool() && (Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.RetinazerV2>() || 
                    Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.SpazmatismV2>() || Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.Cataluminance>()
                    || Main.npc[currentBoss].type == NPCID.Plantera || Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.Death>()
                    || Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>()
                    || Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>()))
                {
                    text = LangUtils.GetTextValue("DeathText.BossDeath2");
                }

                if (Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>())
                {
                    text = LangUtils.GetTextValue("DeathText.DarkCloud");
                }

                //If you want to add custom text for other bosses, stick it here using the line above as a template
            }

            if (projectile != null && projectile.type == ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnifeSmall>() && projectile.damage > 999)
            {
                text = LangUtils.GetTextValue("DeathText.Tonberry");
            }

            if (projectile != null && projectile.type == ModContent.ProjectileType<Projectiles.Enemy.Marilith.CataclysmicFirestorm>())
            {                
                if (Main.rand.NextBool())
                {
                    text = LangUtils.GetTextValue("DeathText.Marilith");
                }
                else
                {
                    bool hasRing = false;
                    for (int j = 3; j < 8 + Player.GetAmountOfExtraAccessorySlotsToShow(); j++)
                    {
                        if (Player.armor[j].type == ModContent.ItemType<Items.Accessories.Expert.ChloranthyRing>() || Player.armor[j].type == ModContent.ItemType<Items.Accessories.Expert.ChloranthyRing2>())
                        {
                            hasRing = true;
                            break;
                        }
                    }
                    if (!hasRing)
                    {
                        text = LangUtils.GetTextValue("DeathText.ChloranthyRingTip");
                    }
                }
            }

            return LangUtils.GetTextValue("DeathText.Tip") + text;
        }
        public override void PreUpdateMovement()
        {
            if (ShunpoTimer == 3)
            {
                Player.velocity = Vector2.Zero;
            }
            if (ShunpoTimer == 2)
            {
                Player.velocity = Player.GetModPlayer<tsorcRevampPlayer>().ShunpoVelocity;
            }
            if (ShunpoTimer == 1)
            {
                Player.velocity = Vector2.Zero;
                Player.RefreshMovementAbilities();
            }
        }
        public override void UpdateDead()
        {
            if (Player.whoAmI == Main.myPlayer)
            {
                if (DeathText == null)
                {
                    DeathText = PickDeathText();
                    
                }
                if (ModContent.GetInstance<tsorcRevampConfig>().SoulsDropOnDeath)
                {
                    if (Main.mouseItem.type == ModContent.ItemType<DarkSoul>() && Main.mouseItem.stack > 0) {
                        SoulSlot.Item.stack += Main.mouseItem.stack;
                        Player.inventory[58].TurnToAir();
                        Main.mouseItem.TurnToAir();
                    }
                    int soulCount  = 0;
                    foreach (Item item in Player.inventory)
                    {
                        //leaving this in case someone decides to move souls to their normal inventory to stop them from being dropped on death :)
                        if (item.type == ModContent.ItemType<DarkSoul>())
                        {
                            soulCount += item.stack;
                            item.stack = 0;
                        }
                    }

                    if (SoulSlot.Item.stack > 0)
                    {
                        soulCount += SoulSlot.Item.stack;
                        SoulSlot.Item.TurnToAir();
                    }
                    
                    if(soulCount > 0)
                    {
                        Projectile.NewProjectileDirect(Player.GetSource_Death(), Player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.SoulDrop>(), 0, 0, Player.whoAmI, soulCount, Player.whoAmI);
                    }                    
                }


                DarkInferno = false;
                PhazonCorruption = false;
                Falling = false;
                FracturingArmor = 1;
                tsorcRevampEstusPlayer estusPlayer = Player.GetModPlayer<tsorcRevampEstusPlayer>();
                estusPlayer.isDrinking = false;
                estusPlayer.estusDrinkTimer = 0;
                tsorcRevampCeruleanPlayer ceruleanPlayer = Player.GetModPlayer<tsorcRevampCeruleanPlayer>();
                ceruleanPlayer.isDrinking = false;
                ceruleanPlayer.ceruleanDrinkTimer = 0;
            }

            if(Player.respawnTimer > 240 && !tsorcRevampWorld.BossAlive)
            {  
                Player.respawnTimer = 240;
            }
        }

        public override void PostUpdateMiscEffects() 
        {
            if (GravityField) {
                if (InSpace(Player)) {
                    Player.gravity = Player.defaultGravity;
                    if (Player.wet) {
                        if (Player.honeyWet) {
                            Player.gravity = 0.1f;
                        }
                        else if (Player.merman) {
                            Player.gravity = 0.3f;
                        }
                        else {
                            Player.gravity = 0.2f;
                        }
                    }
                } 
            }
        }

        public override void PostUpdate()
        {

            if ((Player.HasBuff(ModContent.BuffType<MagicWeapon>()) || Player.HasBuff(ModContent.BuffType<GreatMagicWeapon>()) || Player.HasBuff(ModContent.BuffType<CrystalMagicWeapon>())) && Player.meleeEnchant > 0)
            {
                int buffIndex = 0;

                foreach (int buffType in Player.buffType)
                {

                    if ((buffType == ModContent.BuffType<MagicWeapon>()) || (buffType == ModContent.BuffType<GreatMagicWeapon>()) || (buffType == ModContent.BuffType<CrystalMagicWeapon>()))
                    {
                        Player.buffTime[buffIndex] = 0;
                    }
                    buffIndex++;
                }
            }
            SetDirection();

            if (!Player.mount.Active)
            {
                Player.fullRotation = rotation * Player.gravDir;
            }

            rotation = 0f;
            if (forcedItemRotation.HasValue)
            {
                Player.itemRotation = forcedItemRotation.Value;

                forcedItemRotation = null;
            }


            TryForceFrame(ref Player.headFrame, ref forcedHeadFrame);
            TryForceFrame(ref Player.bodyFrame, ref forcedBodyFrame);
            TryForceFrame(ref Player.legFrame, ref forcedLegFrame);

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
            //shift everything in the array forward one slot, starting from the end
            for (int i = oldPos.Length - 1; i > 0; i--)
            {
                oldPos[i] = oldPos[i - 1];
            }
            //except the first slot
            oldPos[0] = Player.position;
            //Main.NewText("" + Player.lifeRegen);
        }

        void TryForceFrame(ref Rectangle frame, ref PlayerFrames? newFrame)
        {
            if (newFrame.HasValue)
            {
                frame = ToRectangle(newFrame.Value);

                newFrame = null;
            }
        }
        public static Rectangle ToRectangle(PlayerFrames frame)
        {
            return new Rectangle(0, (int)frame * 56, 40, 56);
        }

        //taken straight from player.update, dont ask why it does what it does because i have NO idea
        public static bool InSpace(Player player) {
            float x = (float)Main.maxTilesX / 4200f;
            x *= x;
            return (float)((double)(player.position.Y / 16f - (60f + 10f * x)) / (Main.worldSurface / 6.0)) < 1f;
        }

        public bool OverrideCamera;
        public Vector2 newScreenPosition;
        public float progress;
        public override void ModifyScreenPosition()
        {
            if (OverrideCamera)
            {
                Main.screenPosition = newScreenPosition = Vector2.Lerp(Main.screenPosition, newScreenPosition - Main.ScreenSize.ToVector2() / 2f, progress);
                OverrideCamera = false;
                progress = 0;
            }
        }
    }
}
