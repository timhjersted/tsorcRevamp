using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using TerraUI.Objects;
using tsorcRevamp.Buffs;
using tsorcRevamp.Items;
using tsorcRevamp.Projectiles.Pets;
using tsorcRevamp.UI;

namespace tsorcRevamp
{
    //update loops that run every frame
    public partial class tsorcRevampPlayer
    {

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
        public bool TornWings = false;
        public bool Crippled = false;

        public bool DuskCrownRing = false;
        public bool Celestriad = false;
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
        public bool NUVamp = false;

        public bool OldWeapon = false;

        public bool CritDamage250 = false;

        public int steeltempest = 0;
        public int CritCounter = 0;

        public bool InterstellarBoost = false;

        public bool WaspPower = false;

        public bool HollowSoldierAgility = false;
        public bool HollowSoldierEstusBenefits = false;
        public bool SmoughShieldSkills = false;
        public bool BurdenOfSmough = false;
        public bool SmoughAttackSpeedReduction = false;

        public bool Miakoda = false;
        public bool RTQ2 = false;

        public bool BoneRevenge = false;
        public bool SoulSiphon = false;
        public int ConsSoulChanceMult;
        public bool SoulSickle = false;

        public bool BurningAura = false;
        public bool BurningStone = false;

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
        int healingTimer = 0;

        public Item[] PotionBagItems = new Item[PotionBagUIState.POTION_BAG_SIZE];
        public int potionBagCountdown = 0; //You can't move items around if an item is still 'in use'. This lets us delay opening the bag until that finishes.

        public UIItemSlot SoulSlot;

        public int MaxAcquiredHP; //To prevent purging stones and humanity from raising hp above your max acquired hp from life crystals and life fruit.


        public bool PowerWithin;
        public int StaminaReaper = 0;

        public List<int> ActivePermanentPotions;

        public Vector2[] oldPos = new Vector2[60];

        public bool CowardsAffliction;
        public bool GravityField;
        public float TextCooldown; //Used for if we want text to display when a Thing happens, but not to spam the player
        public float FieldTimer;

        public bool startedQuest;
        public bool finishedQuest;
        public bool touchedSurface;

        public float rotation3d;

        public string DeathText;
        bool setDeathText = false;
        public static List<string> DeathTextList

         = new List<string>() {
             "The dodgeroll makes you immune to attacks. Mastering it is necessary for survival!",
             "Glowing green skulls often hold important secrets...",
             "The dodgeroll keybind can be changed in Controls. Try Left Shift, R or whatever works for you.",
             "You can dodge mid-air to avoid attacks and reverse your momentum instantly...",
             "Crystal shards often hint at secrets, chests or hidden paths nearby...",
             "Using potions during normal combat is often necessary to survive",
             "Losing souls on death can be disabled in the config",
             "Enemies in certain dark areas drop Humanity, which can restore HP lost to curses",
             "Item tooltips often have information vital to your survival...",
             "Check if any of your items can be upgraded with Dark Souls",
             "Progression clues can be found on unique items dropped by bosses",
             "If you feel lost, seek the dark places on your map. There are secrets and paths in almost every direction",
             "Signs offer many important hints",
             "If you go above 100% crit chance, you have a chance to multicrit, allowing you to deal 4 times or even 8 times the damage if you have enough crit chance.",
             "Teal Pressure Pads can only be activated with ranged attacks",
             "Some vanilla recipes have been removed. You must find these items in the world by exploring",
             "Get a bad modifier on a weapon? Talk to Jade, the Emerald Herald. She can remove it with her Blessing",
             //"Grey Stone Gates cannot be broken and must be opened by a lever or switch",
             "You may encounter bosses before you can defeat them. If in doubt, come back when you're stronger",
             "Play with the Recipe Browser mod to easily see what your loot can be transformed into",
             "Welcome to the Dark Souls of Terraria",
             "The Chloranthy Ring massively improves your dodgeroll, allowing superior evasion...",
             "Teal pressure plates must be hit with a ranged weapon to activate",
             "Use the Recipe Browser mod to see what can be crafted with Dark Souls. Anything that says it's a Dark Souls crafting material should be investigated.",
             "Remember to dodgeroll",
             "I am so fucking mad"
            };

        public override void ResetEffects()
        {
            SilverSerpentRing = false;
            DragonStone = false;
            SoulReaper = 5;

            DragoonBoots = false;
            //player.eocDash = 0;
            Player.armorEffectDrawShadowEOCShield = false;
            UndeadTalisman = false;

            DuskCrownRing = false;

            ChloranthyRing1 = false;
            ChloranthyRing2 = false;

            CritDamage250 = false;

            Celestriad = false;

            DragoonBoots = false;
            GemBox = false;
            OldWeapon = false;
            Miakoda = false;

            WaspPower = false;

            HollowSoldierAgility = false;
            HollowSoldierEstusBenefits = false;
            SmoughShieldSkills = false;
            BurdenOfSmough = false;
            SmoughAttackSpeedReduction = false;

            RTQ2 = false;
            DarkInferno = false;
            BoneRevenge = false;
            SoulSiphon = false;

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

            PhazonCorruption = false;
            LifegemHealing = false;
            RadiantLifegemHealing = false;

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
        }
        
        public override void PreUpdate()
        {
            //Fixes bug where switching from a better to a worse pair of wings keeps the previous wing time cap
            if(Player.wingTime > Player.wingTimeMax)
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
            if (MiakodaCrescentBoostTimer > 150)
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
            if (MiakodaNewBoostTimer > 150)
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

            if (Main.tile[(int)Player.position.X / 16, (int)Player.position.Y / 16] != null && Main.tile[(int)Player.position.X / 16, (int)Player.position.Y / 16].WallType == WallID.StarlitHeavenWallpaper)
            {
                Player.AddBuff(BuffID.Darkness, 60);
            }

            if (MaxAcquiredHP < Player.statLifeMax)
            {
                MaxAcquiredHP = Player.statLifeMax;
            }

            if (Player.HasBuff(ModContent.BuffType<Buffs.Summon.NondescriptOwlBuff>()) && Player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.Archer.NondescriptOwlProjectile>()] == 0) {
                Item staff = new();
                staff.SetDefaults(ModContent.ItemType<Items.Weapons.Summon.PeculiarSphere>());
                int damage = staff.damage;
                int p = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Summon.Archer.NondescriptOwlProjectile>(), damage, 0, Player.whoAmI);
                Main.projectile[p].originalDamage = damage;
            }

            if (Player.HasBuff(ModContent.BuffType<Buffs.Summon.SunsetQuasarBuff>()) && Player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.SunsetQuasar.SunsetQuasarMinion>()] == 0) {
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
                Main.NewText("Quest started!");
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
                Main.NewText("Disqualified: You touched the surface!");
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
                Main.NewText("Disqualified: Teleporting is not allowed!");
            }

            //{7909, 1081} is the underwater observatory's top left corner, and {320, 119} is its rectangular size
            if (Collision.CheckAABBvAABBCollision(Player.position, new Vector2(Player.width, Player.height), new Vector2(7909, 1081) * 16, new Vector2(320, 119) * 16)) {
                Main.NewText("You finished the quest!");
                Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ItemID.RodofDiscord);
                finishedQuest = true;
            }
        }

        public override void PreUpdateBuffs()
        {
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
                                        UsefulFunctions.BroadcastText("Your grapple strains against the force, but...!", Color.Orange);
                                        TextCooldown = 350;
                                    }
                                    if (FieldTimer >= 340)
                                    {
                                        Player.velocity += new Vector2(0, -15);
                                        Player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 30);
                                        UsefulFunctions.BroadcastText("Your grapple suddenly snaps!!", Color.Red);
                                    }
                                }
                            }

                            Player.velocity += UsefulFunctions.GenerateTargetingVector(new Vector2(4484, 355) * 16, Player.Center, 20);
                            if (TextCooldown <= 0)
                            {
                                UsefulFunctions.BroadcastText("A strong forcefield expels you from the ruins!", Color.Purple);
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
                Player.moveSpeed += 0.9f;
                Player.endurance = .5f;
                Player.noKnockback = true;
            }

            #region Lifegem Healing


            if (LifegemHealing) // 120 hp over 12 seconds
            {
                healingTimer++;

                if (healingTimer == 6)
                {
                    Player.statLife += 1;
                    healingTimer = 0;
                }
            }

            if (RadiantLifegemHealing) // 200 hp over 13.33 seconds
            {
                healingTimer++;

                if (healingTimer == 4)
                {
                    Player.statLife += 1;
                    healingTimer = 0;
                }
            }

            if (!RadiantLifegemHealing && !LifegemHealing)
            {
                healingTimer = 0;
            }

            #endregion

            if (Player.HasBuff(BuffID.WellFed))
            {
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.1f;
            }
            if (Player.HasBuff(BuffID.WellFed2))
            {
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.2f;
            }
            if (Player.HasBuff(BuffID.WellFed3))
            {
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.3f;
            }


            if (Player.HasBuff(BuffID.TheTongue))
            {
                for (int i = 0; i < 9; i++)
                {
                    CombatText.NewText(Player.Hitbox, CombatText.DamagedFriendly, 999999999, true);
                }
                Player.KillMe(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(Player.name + " was consumed by The Wall."), 999999999, 1);
            }

            if (!Player.HasBuff(ModContent.BuffType<Buffs.CurseBuildup>()))
            {
                CurseLevel = 1; //Not sure why 1 is the default
            }

            if (!Player.HasBuff(ModContent.BuffType<Buffs.PowerfulCurseBuildup>()))
            {
                PowerfulCurseLevel = 1; //Not sure why 1 is the default
            }
        }

        public override void PostUpdateEquips()
        {
            Player.GetDamage(DamageClass.Summon) -= 0.25f;

            if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                Player.GetDamage(DamageClass.Melee) *= 1.2f;
                Player.GetDamage(DamageClass.Ranged) *= 1.2f;
                Player.GetDamage(DamageClass.Magic) *= 1.2f;
                Player.GetDamage(DamageClass.SummonMeleeSpeed) *= 1.2f;

                if (Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < Player.GetModPlayer<tsorcRevampStaminaPlayer>().minionStaminaCap)
                {
                    Player.lifeRegen /= 2;
                }
            }
            if (manaShield > 0)
            {
                Player.manaRegenBuff = false;
            }
            int PTilePosX = (int)Player.position.X / 16;
            bool Ocean = (PTilePosX < 750 || PTilePosX > Main.maxTilesX - 750);
            bool underground = (Player.position.Y >= (Main.maxTilesY / 2.43309f) * 16); //magic number

            if (((underground && Player.ZoneHallow && !Ocean && !Player.ZoneDungeon /*&& !player.ZoneOverworldHeight*/) || Player.ZoneMeteor) && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                Player.AddBuff(ModContent.BuffType<Buffs.GravityAlignment>(), 5);
                Player.gravControl = true;
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
                Player.statDefense = (int)(Player.statDefense * REDUCE);
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

            if (DragoonBoots && DragoonBootsEnable)
            {
                //Player.jumpSpeed += 10f; why
                Player.jumpSpeedBoost += 10f;
            }
            if (DragoonHorn && (((Player.gravDir == 1f) && (Player.velocity.Y > 0)) || ((Player.gravDir == -1f) && (Player.velocity.Y < 0))))
            {
                Player.GetDamage(DamageClass.Melee) *= 1.5f;
            }
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
                int? marilithIndex = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.Fiends.FireFiendMarilith>());
                if (marilithIndex != null)
                {
                    NPC marilith = Main.npc[marilithIndex.Value];

                    Player.velocity -= UsefulFunctions.GenerateTargetingVector(Player.Center, marilith.Center, 0.25f);

                    if (Player.HasBuff<MarilithHold>())
                    {
                        Player.velocity = -UsefulFunctions.GenerateTargetingVector(Player.Center, marilith.Center, 4f);
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

            int option = Main.rand.Next(DeathTextList.Count);
            text = DeathTextList[option];
            if (BearerOfTheCurse && Main.rand.NextBool(5))
            {
                if (Main.rand.NextBool())
                {
                    text = "Bearer of the Curse mode can be disabled at any time...";
                }
                else
                {
                    text = "Bearer of the Curse mode is intended for experienced players";
                }
            }

            if (Player.statLifeMax >= 200)
            {
                if (Main.rand.NextBool(20))
                {
                    text = "Avoid getting hit by enemy attacks";
                }
                if (Main.rand.NextBool(100))
                {
                    text = "Get Good :)";
                }
            }

            if (Player.GetModPlayer<tsorcRevampPlayer>().BurdenOfSmough)
            {
                if (Main.rand.NextBool(10))
                {
                    text = "Heavy roll you got there!";
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
                    text = "Don't give up! Some bosses might take several tries to defeat";
                }
                else if (Main.rand.NextBool() && (Main.npc[currentBoss].type == NPCID.Retinazer || Main.npc[currentBoss].type == NPCID.Spazmatism
                    || Main.npc[currentBoss].type == NPCID.Plantera || Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.Death>()
                    || Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>()))
                {
                    text = "Certain bosses can be fought earlier than necessary. If you're struggling, try waiting until you're more powerful.";
                }

                if (Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>())
                {
                    text = "You can't outrun your shadow";
                }

                //If you want to add custom text for other bosses, stick it here using the line above as a template
            }

            if (projectile != null && projectile.type == ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnifeSmall>() && projectile.damage > 999)
            {
                text = "(O_O;)";
            }

            if (projectile != null && projectile.type == ModContent.ProjectileType<Projectiles.Enemy.Marilith.CataclysmicFirestorm>())
            {                
                if (Main.rand.NextBool())
                {
                    text = "Some powerful attacks can only be avoided by dodgerolling!";
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
                        text = "The Chloranthy Ring massively improves your dodgeroll, allowing even more evasion...";
                    }
                }
            }

            return "Tip: " + text;
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
            }

            if(Player.respawnTimer > 600 && !tsorcRevampWorld.BossAlive)
            {
                Player.respawnTimer = 600;
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
    }
}
