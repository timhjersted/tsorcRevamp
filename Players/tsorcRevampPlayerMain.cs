using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Terraria.WorldBuilding;
using TerraUI.Objects;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Accessories;
using tsorcRevamp.Buffs.Armor;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Buffs.Runeterra.Ranged;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Accessories.Summon;
using tsorcRevamp.Items.Ammo;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Armors.Magic;
using tsorcRevamp.Items.Armors.Melee;
using tsorcRevamp.Items.Armors.Ranged;
using tsorcRevamp.Items.Armors.Summon;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Tools;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Weapons.Magic;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.Items.Weapons.Melee.Axes;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;
using tsorcRevamp.Items.Weapons.Ranged;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.Projectiles.Magic.Runeterra.LudensTempest;
using tsorcRevamp.Projectiles.Melee.Runeterra;
using tsorcRevamp.Projectiles.Pets;
using tsorcRevamp.Projectiles.Ranged;
using tsorcRevamp.UI;
using tsorcRevamp.Utilities;
using static Humanizer.In;

namespace tsorcRevamp
{
    public partial class tsorcRevampPlayer : ModPlayer
    {
        public static readonly int PermanentBuffCount = 59;
        public static List<int> startingItemsList;
        public StartingClass startingClass = StartingClass.None;
        private const int StartingClassStatsVersion = 9;
        public bool appliedStartingClassStats = false;
        public int appliedStartingClassStatsVersion = 0;

        // Total permanent HP / mana granted by SoulsMode Life and Mana Crystals.
        //
        // These have to exist because vanilla never saves statLifeMax/statManaMax. Player.Serialize writes
        // `100 + ConsumedLifeCrystals * 20 + ConsumedLifeFruit * 5` (and `20 + ConsumedManaCrystals * 20`),
        // and Deserialize re-derives the counters back out of that single number. The consumed-crystal
        // counts are therefore the only life/mana state that survives a save — and they cap at 15 and 9.
        //
        // That makes a reduced per-crystal gain unrepresentable in vanilla's own bookkeeping: "half a
        // crystal" has no encoding, and pushing the counters past 15/9 drives the saved number over 400/200,
        // where Deserialize re-reads the excess as Life Fruit (life) or clamps it away entirely (mana).
        // So we keep our own authoritative totals here, hold the vanilla counters inside their legal range
        // purely as a persistence vehicle, and reconcile the two in ModifyMaxStats.
        // soulsLife/ManaGranted is the SoulsMode valuation (banked at the reduced, party-scaled rate on the
        // frame each crystal was eaten). lifeCrystalsEaten / manaCrystalsEaten is the raw count, which Classic
        // re-values at vanilla's flat +20 — see EffectiveLifeGrant. Both are needed: vanilla's own counter can't
        // stand in for the count because it saturates at 15/9.
        public int soulsLifeGranted = 0;
        public int soulsManaGranted = 0;
        public int lifeCrystalsEaten = 0;
        public int manaCrystalsEaten = 0;

        // Every class converges on the same ceiling; the starting class only changes how many crystals it
        // takes to get there (Melee 28 Life Crystals, Summoner 30, Ranged 31, Magic 32 at +10 each solo).
        // Life Fruit still takes everyone from 400 to 500 afterwards, exactly as in vanilla.
        public const int SoulsModeMaxLife = 400;
        public const int SoulsModeMaxMana = 200;
        public const int SoulsModeManaCrystalGain = 10;

        public bool normansRingAmmoSave = false;
        public List<int> bagsOpened;
        public static int LastHit = 1;
        public static int ShunpoCooldownPerHit = -40;
        public static bool SameHit = false;
        public static bool DiffHit = false;
        public Dictionary<int, int> consumedPotions;
        public Dictionary<Vector2, int> soulDeathLocations = new Dictionary<Vector2, int>();
        public int LastAttackedNPCIndex;
        public int DwarvenContractsGiven = 0;
        private int lastHealthBand = 1;
        private bool guaranteedHurtSoundForBand = false;
        // Enforces a 2 second (120 tick) gap between custom player hurt voice sounds.
        private uint lastHurtSoundTick = 0;

        public override void Initialize()
        {
            PermanentBuffToggles = new bool[PermanentBuffCount]; //todo dont forget to increment this if you add buffs to the dictionary
            DamageDir = new Dictionary<int, float> {
                { 48, 4 }, //spike
                { 76, 4 }, //hellstone
                { 232, 4 } //wooden spike, in case tim decides to use them
            };

            SoulSlot = new UIItemSlot(Vector2.Zero, 52, ItemSlot.Context.InventoryItem, LangUtils.GetTextValue("UI.DarkSouls"), null, SoulSlotCondition, DrawSoulSlotBackground, null, null, false, true);
            SoulSlot.BackOpacity = 0.8f;
            // Dark Souls can't be pulled out of the slot into the inventory / piggy bank — they auto-deposit on
            // pickup and are spent at Demon Altars.
            SoulSlot.DisallowManualRemoval = true;
            SoulSlot.Item = new Item();
            SoulSlot.Item.SetDefaults(0, true);

            RightClickSlot = new UIItemSlot(Vector2.Zero, 52, ItemSlot.Context.InventoryItem, LangUtils.GetTextValue("UI.SecondSlotHover"), null, RightClickSlotCondition, DrawRightClickSlotBackground, null, null, false, true);
            RightClickSlot.BackOpacity = 0.8f;
            RightClickSlot.HitboxRightAndBottomPadding = 24;
            RightClickSlot.AppendHoverTextToItemName = true;
            RightClickSlot.Item = new Item();
            RightClickSlot.Item.SetDefaults(0, true);

            StorageOpenerSlot = new UI.StorageOpenerSlot(52);

            chestBankOpen = false;
            chestBank = -1;

            chestPiggyOpen = false;
            chestPiggy = -1;

            LastAttackedNPCIndex = -1;

            bagsOpened = new List<int>();
        }

        public override void CopyClientState(ModPlayer clientClone)/* tModPorter Suggestion: Replace Item.Clone usages with Item.CopyNetStateTo */
        {
            tsorcRevampPlayer clone = clientClone as tsorcRevampPlayer;
            if (clone == null) { return; }

            SoulSlot.Item.CopyNetStateTo(clone.SoulSlot.Item);
            RightClickSlot.Item.CopyNetStateTo(clone.RightClickSlot.Item);
        }
        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            tsorcRevampPlayer oldClone = clientPlayer as tsorcRevampPlayer;
            if (oldClone == null) { return; }

            if (oldClone.SoulSlot.Item.IsNotSameTypePrefixAndStack(SoulSlot.Item))
            {
                SendSingleItemPacket(tsorcPacketID.SyncSoulSlot, SoulSlot.Item, -1, Player.whoAmI);
            }
            if (oldClone.RightClickSlot.Item.IsNotSameTypePrefixAndStack(RightClickSlot.Item))
            {
                SendSingleItemPacket(tsorcPacketID.SyncRightClickSlot, RightClickSlot.Item, -1, Player.whoAmI);
            }
        }
        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {

            //Sync soul slot
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)tsorcPacketID.SyncSoulSlot);
            packet.Write((byte)Player.whoAmI);
            ItemIO.Send(SoulSlot.Item, packet);
            packet.Send(toWho, fromWho);

            //Sync Right-Click slot
            ModPacket rightClickPacket = Mod.GetPacket();
            rightClickPacket.Write((byte)tsorcPacketID.SyncRightClickSlot);
            rightClickPacket.Write((byte)Player.whoAmI);
            ItemIO.Send(RightClickSlot.Item, rightClickPacket);
            rightClickPacket.Send(toWho, fromWho);

            /*
            ModPacket packet2 = Mod.GetPacket();
            packet2.Write((byte)tsorcPacketID.SyncCurse);
            packet2.Write((byte)Player.whoAmI);
            packet2.Write(cursePoints);
            packet2.Send(toWho, fromWho);*/


            /**
            //For synced random. Called when a new player connects.
            //The server (and only the server) generates a new random seed and sends it to all clients.
            //Could probably get away with not re-seeding the generator every time, instead just syncing the tally and using it to bring new clients up to date. 
            if (Main.netMode == NetmodeID.Server)
            {
                UsefulFunctions.GenerateRandomSeed();
            }
            **/
        }

        public override void SaveData(TagCompound tag)
        {
            // Save storage FIRST so an exception anywhere else in this method can never skip it (the whole method
            // is the ModPlayer's save, and a throw partway would otherwise lose everything below the throw point).
            SaveStorage(tag);

            tag.Add("greatMirrorWarp", greatMirrorWarpPoint);
            tag.Add("warpWorld", warpWorld);
            tag.Add("warpSet", warpSet);
            tag.Add("townWarpX", townWarpX);
            tag.Add("townWarpY", townWarpY);
            tag.Add("townWarpWorld", townWarpWorld);
            tag.Add("townWarpSet", townWarpSet);
            tag.Add("gotDarksign", gotDarksign);
            tag.Add("FirstEncounter", FirstEncounter);
            tag.Add("ReceivedGift", ReceivedGift);
            tag.Add("ReceivedHuntingTome", ReceivedHuntingTome);
            tag.Add("heraldChatState", heraldChatState);
            tag.Add("BonfiresCrafted", BonfiresCrafted);
            tag.Add("BearerOfTheCurse", BearerOfTheCurse);
            tag.Add("Unkindled", Unkindled);
            tag.Add("soulSlot", ItemIO.Save(SoulSlot.Item));
            tag.Add("rightClickSlot", ItemIO.Save(RightClickSlot.Item));
            tag.Add("Curse", CurseActive);
            tag.Add("CurseMaxLifeMult", CurseMaxLifeMultiplier);
            tag.Add("CurseLifeRegen", CurseLifeRegenerationBonus);
            tag.Add("CurseDefense", CurseDefenseBonus);
            tag.Add("CurseResist", CurseResistanceBonus);
            tag.Add("CurseDmg", CurseDamageBonus);
            tag.Add("CurseAtkSpd", CurseAttackSpeedBonus);
            tag.Add("CurseMoveSpd", CurseMovementSpeedBonus);
            tag.Add("powerfulCurse", powerfulCurseActive);
            tag.Add("powerfulCurseMaxLifeMult", powerfulCurseMaxLifeMultiplier);
            tag.Add("powerfulCurseLifeRegen", powerfulCurseLifeRegenerationBonus);
            tag.Add("powerfulCurseDefense", powerfulCurseDefenseBonus);
            tag.Add("powerfulCurseResist", powerfulCurseResistanceBonus);
            tag.Add("powerfulCurseDmg", powerfulCurseDamageBonus);
            tag.Add("powerfulCurseAtkSpd", powerfulCurseAttackSpeedBonus);
            tag.Add("powerfulCurseMoveSpd", powerfulCurseMovementSpeedBonus);
            tag.Add("SoulVessel", SoulVessel);
            tag.Add("DeathTextIndex", currentDeathTextIndex);
            tag.Add("StartingClass", (int)startingClass);
            tag.Add("AppliedStartingClassStats", appliedStartingClassStats);
            tag.Add("AppliedStartingClassStatsVersion", appliedStartingClassStatsVersion);
            tag.Add("SoulsLifeGranted", soulsLifeGranted);
            tag.Add("SoulsManaGranted", soulsManaGranted);
            tag.Add("LifeCrystalsEaten", lifeCrystalsEaten);
            tag.Add("ManaCrystalsEaten", manaCrystalsEaten);
            //tag.Add("SoulLocation", );

            if (bagsOpened == null)
            {
                bagsOpened = new List<int>();
            }
            tag.Add("bagType", bagsOpened);

            List<Item> PotionBagList = new List<Item>();
            if (PotionBagItems == null)
            {
                PotionBagItems = new Item[PotionBagUIState.POTION_BAG_SIZE];
            }

            for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
            {
                if (PotionBagItems[i] == null)
                {
                    PotionBagItems[i] = new Item();
                    PotionBagItems[i].SetDefaults(0);
                }
            }

            foreach (Item thisItem in PotionBagItems)
            {
                PotionBagList.Add(thisItem);
            }

            tag.Add("PotionBag", PotionBagList);

            List<bool> permaBuffs = PermanentBuffToggles.ToList();
            tag.Add("PermanentBuffToggles", permaBuffs);
            tag.Add("finishedQuest", finishedQuest);

            consumedPotions ??= new Dictionary<int, int>();

            List<BuffDefinition> buffDefinitions = new List<BuffDefinition>();
            foreach (int i in consumedPotions.Keys)
            {
                if (i != 0)
                {
                    buffDefinitions.Add(new BuffDefinition(i));
                }
            }

            tag.Add("consumedPotionsBuffTypes", buffDefinitions);
            tag.Add("consumedPotionsValues", consumedPotions.Values.ToList());
        }

        public override void LoadData(TagCompound tag)
        {
            // Load storage FIRST so an exception anywhere else in this method can't leave storage unloaded.
            // (A throw later in LoadData — e.g. a duplicate-key Dictionary.Add in the consumedPotions migration —
            // is caught per-ModPlayer by tModLoader, which would otherwise silently skip storage entirely.)
            LoadStorage(tag);

            int warpX = tag.GetInt("warpX");
            int warpY = tag.GetInt("warpY");
            greatMirrorWarpPoint = tag.Get<Vector2>("greatMirrorWarp");
            if (greatMirrorWarpPoint == Vector2.Zero)
            {
                //This migrates old saves to the new system.
                greatMirrorWarpPoint.X = warpX;
                greatMirrorWarpPoint.Y = warpY;
            }
            warpWorld = tag.GetInt("warpWorld");
            warpSet = tag.GetBool("warpSet");
            townWarpX = tag.GetInt("townWarpX");
            townWarpY = tag.GetInt("townWarpY");
            townWarpWorld = tag.GetInt("townWarpWorld");
            townWarpSet = tag.GetBool("townWarpSet");
            gotDarksign = tag.GetBool("gotDarksign");
            FirstEncounter = tag.GetBool("FirstEncounter");
            ReceivedGift = tag.GetBool("ReceivedGift");
            ReceivedHuntingTome = tag.GetBool("ReceivedHuntingTome");
            heraldChatState = tag.GetInt("heraldChatState");
            BonfiresCrafted = tag.GetInt("BonfiresCrafted");
            startingClass = (StartingClass)tag.GetInt("StartingClass");
            appliedStartingClassStats = tag.GetBool("AppliedStartingClassStats");
            appliedStartingClassStatsVersion = tag.ContainsKey("AppliedStartingClassStatsVersion") ? tag.GetInt("AppliedStartingClassStatsVersion") : (appliedStartingClassStats ? 1 : 0);
            BearerOfTheCurse = tag.GetBool("BearerOfTheCurse");
            if (tag.ContainsKey("Unkindled"))
            {
                Unkindled = tag.GetBool("Unkindled");
            }
            else if (!BearerOfTheCurse)
            {
                // Migration: an existing pre-Unkindled save that wasn't BotC was Classic.
                // Promote them to Unkindled so they pick up the new base Souls mechanics.
                Unkindled = true;
            }
            // Must run after startingClass (the grant caps depend on it) and after Unkindled/BearerOfTheCurse
            // (SoulsMode depends on them). Vanilla's own life/mana block is deserialized before any ModPlayer
            // LoadData, so Player.ConsumedLifeCrystals/ConsumedManaCrystals are already populated here.
            if (tag.ContainsKey("SoulsLifeGranted"))
            {
                soulsLifeGranted = tag.GetInt("SoulsLifeGranted");
                soulsManaGranted = tag.GetInt("SoulsManaGranted");
            }
            else if (SoulsMode)
            {
                // Migration off the old crystal nerf, which let the vanilla counters run past 15/9 and so had
                // its saved life/mana mangled on every reload — the life overflow came back as phantom Life
                // Fruit, and every mana crystal past the 9th was clamped away. Nothing recorded how many
                // crystals were actually eaten, so reconstruct from the surviving counter at the intended
                // +10 each. Any Life Fruit on such a character is almost certainly the phantom kind (the old
                // code raised Life Fruit's unlock threshold to 30 crystals, which was unreachable in practice),
                // so scrub it. Approximate by design: a badly ratcheted character is better off re-rolled.
                soulsLifeGranted = Math.Min(SoulsLifeGrantCap, Player.ConsumedLifeCrystals * 10);
                soulsManaGranted = Math.Min(SoulsManaGrantCap, Player.ConsumedManaCrystals * SoulsModeManaCrystalGain);
                Player.ConsumedLifeFruit = 0;
            }

            // Crystal counts were added after the granted totals, so characters saved by the previous build have
            // the totals but no counts. Recover the count from the total at the solo rate — the only rate a
            // single-player character can have banked. A Classic character has neither, and gets its counts
            // repaired from the vanilla counters by ReconcileCrystalState on the first frame.
            lifeCrystalsEaten = tag.ContainsKey("LifeCrystalsEaten")
                ? tag.GetInt("LifeCrystalsEaten")
                : soulsLifeGranted / 10;
            manaCrystalsEaten = tag.ContainsKey("ManaCrystalsEaten")
                ? tag.GetInt("ManaCrystalsEaten")
                : soulsManaGranted / SoulsModeManaCrystalGain;

            NormalizeCrystalCounters();

            Item soulSlotSouls = ItemIO.Load(tag.GetCompound("soulSlot"));
            SoulSlot.Item = soulSlotSouls.Clone();
            if (tag.ContainsKey("rightClickSlot"))
            {
                RightClickSlot.Item = ItemIO.Load(tag.GetCompound("rightClickSlot")).Clone();
            }
            CurseActive = tag.GetBool("Curse");
            CurseMaxLifeMultiplier = tag.GetFloat("CurseMaxLifeMult");
            CurseLifeRegenerationBonus = tag.GetFloat("CurseLifeRegen");
            CurseDefenseBonus = tag.GetFloat("CurseDefense");
            CurseResistanceBonus = tag.GetFloat("CurseResist");
            CurseDamageBonus = tag.GetFloat("CurseDmg");
            CurseAttackSpeedBonus = tag.GetFloat("CurseAtkSpd");
            CurseMovementSpeedBonus = tag.GetFloat("CurseMoveSpd");
            powerfulCurseActive = tag.GetBool("powerfulCurse");
            powerfulCurseMaxLifeMultiplier = tag.GetFloat("powerfulCurseMaxLifeMult");
            powerfulCurseLifeRegenerationBonus = tag.GetFloat("powerfulCurseLifeRegen");
            powerfulCurseDefenseBonus = tag.GetFloat("powerfulCurseDefense");
            powerfulCurseResistanceBonus = tag.GetFloat("powerfulCurseResist");
            powerfulCurseDamageBonus = tag.GetFloat("powerfulCurseDmg");
            powerfulCurseAttackSpeedBonus = tag.GetFloat("powerfulCurseAtkSpd");
            powerfulCurseMovementSpeedBonus = tag.GetFloat("powerfulCurseMoveSpd");
            SoulVessel = tag.GetInt("SoulVessel");

            if(tag.ContainsKey("DeathTextIndex"))
            {
                currentDeathTextIndex = tag.GetInt("DeathTextIndex");
            }

            if (bagsOpened == null)
            {
                bagsOpened = new List<int>();
            }

            if (tag.ContainsKey("bagType"))
            {
                bagsOpened = tag.Get<List<int>>("bagType");
            }

            PotionBagItems = ((List<Item>)tag.GetList<Item>("PotionBag")).ToArray();
            if (PotionBagItems.Length < PotionBagUIState.POTION_BAG_SIZE)
            {
                Item[] TempArray = new Item[PotionBagUIState.POTION_BAG_SIZE];
                for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
                {
                    if (i < PotionBagItems.Length)
                    {
                        TempArray[i] = PotionBagItems[i];
                    }
                    if (TempArray[i] == null)
                    {
                        TempArray[i] = new Item();
                        TempArray[i].SetDefaults(0);
                    }
                }

                PotionBagItems = TempArray;
            }

            List<bool> permaBuffs = (List<bool>)tag.GetList<bool>("PermanentBuffToggles");

            //characters created before this was added would otherwise crash from OOB
            if (permaBuffs.Count == 0)
            {
                for (int i = 0; i < PermanentBuffCount; i++)
                {
                    permaBuffs.Add(false);
                }
            }
            PermanentBuffToggles = permaBuffs.ToArray<bool>();
            if (PermanentBuffToggles.Length < PermanentBuffCount)
            {
                bool[] tempToggles = new bool[PermanentBuffCount];
                for (int i = 0; i < PermanentBuffToggles.Length; i++)
                {
                    tempToggles[i] = PermanentBuffToggles[i];
                }
                PermanentBuffToggles = tempToggles;
            }

            bool? quest = tag.GetBool("finishedQuest");
            finishedQuest = quest ?? false;

            consumedPotions ??= new Dictionary<int, int>();

            //Convert old potion count saving system to the new one
            if (tag.ContainsKey("consumedPotionsKeys"))
            {
                List<ItemDefinition> potKey = tag.GetList<ItemDefinition>("consumedPotionsKeys") as List<ItemDefinition>;
                List<int> potValue = tag.GetList<int>("consumedPotionsValues") as List<int>;
                for (int i = 0; i < potKey.Count; i++)
                {
                    Item potion = new();
                    potion.SetDefaults(potKey[i].Type);
                    if(potion.buffType == 0) //Mana, healing, recall, etc potions got read into this for some reason
                    {
                        continue;
                    }
                    if (consumedPotions.ContainsKey(potion.buffType))
                    {
                        consumedPotions[potion.buffType] += potValue[i];
                    }
                    else
                    {
                        consumedPotions.Add(potion.buffType, potValue[i]);
                    }
                }
            }

            if (tag.ContainsKey("consumedPotionsBuffTypes"))
            {
                List<BuffDefinition> potKey = tag.GetList<BuffDefinition>("consumedPotionsBuffTypes") as List<BuffDefinition>;
                List<int> potValue = tag.GetList<int>("consumedPotionsValues") as List<int>;
                for (int i = 0; i < potKey.Count; i++)
                {
                    // Indexer (not Add) so a duplicate buff type — e.g. a save that carries both the old
                    // "consumedPotionsKeys" and new "consumedPotionsBuffTypes" — can't throw and abort LoadData.
                    consumedPotions[potKey[i].Type] = potValue[i];
                }
            }
        }

        public void SetDirection() => SetDirection(false);

        private void SetDirection(bool resetForcedDirection)
        {
            if (!Main.dedServ && Main.gameMenu)
            {
                Player.direction = 1;

                return;
            }

            if (!Player.pulley && (!Player.mount.Active || Player.mount.AllowDirectionChange) && (Player.itemAnimation <= 1))
            {
                if (forcedDirection != 0)
                {
                    Player.direction = forcedDirection;

                    if (resetForcedDirection)
                    {
                        forcedDirection = 0;
                    }
                }
            }
        }

        public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
        {
            // Seath's wings deliberately use Down to fast-fall. Holding it is always an intentional
            // attempt to control the descent, so it fully negates ordinary fall damage.
            if (damageSource.SourceOtherIndex == 0 && IsSeathWingFallImmune(Player))
            {
                return true;
            }

            if (Player == Main.LocalPlayer)
            {
                if (Player.HasBuff(ModContent.BuffType<Invincible>()))
                {
                    return true;
                }
                if (Player.GetModPlayer<tsorcRevampPlayer>().BarrierRing && !Player.HasBuff(ModContent.BuffType<BarrierCooldown>()))
                {
                    Player.AddBuff(ModContent.BuffType<BarrierCooldown>(), Items.Accessories.Defensive.Rings.BarrierRing.Cooldown * 60);
                    Player.SetImmuneTimeForAllTypes((int)(Items.Accessories.Defensive.Rings.BarrierRing.ImmuneTimeAfterHit * 60f));
                    return true;
                }
                if (DragonStoneImmunity && damageSource.SourcePlayerIndex > -1)
                {
                    int NT = Main.npc[damageSource.SourceNPCIndex].type;
                    if (NT == NPCID.DemonEye
                        || NT == NPCID.DemonEye2
                        || NT == NPCID.EaterofSouls
                        || NT == NPCID.CursedSkull
                        || NT == NPCID.Hornet
                        || NT == NPCID.Harpy
                        || NT == NPCID.CaveBat
                        || NT == NPCID.JungleBat
                        || NT == NPCID.Hellbat
                        || NT == NPCID.Vulture
                        || NT == NPCID.Demon
                        || NT == NPCID.VoodooDemon
                        || NT == NPCID.Pixie
                        || NT == NPCID.WyvernHead || NT == NPCID.WyvernLegs || NT == NPCID.WyvernBody || NT == NPCID.WyvernBody2 || NT == NPCID.WyvernBody3 || NT == NPCID.WyvernTail
                        || NT == NPCID.GiantBat
                        || NT == NPCID.Corruptor || NT == NPCID.VileSpit
                        || NT == NPCID.Gastropod
                        || NT == NPCID.WanderingEye
                        || NT == NPCID.IlluminantBat
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
                        || NT == NPCID.WyvernHead
                        || NT == NPCID.Harpy
                        || NT == NPCID.CultistDragonHead
                        || NT == NPCID.SandElemental
                        || NT == NPCID.SporeBat
                        || NT == ModContent.NPCType<CloudBat>())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            // The custom wing fall-damage rule should be less punishing than vanilla fall damage.
            // SourceOtherIndex 0 is Terraria's ordinary fall-damage source.
            if (modifiers.DamageSource != null && modifiers.DamageSource.SourceOtherIndex == 0 && Player.equippedWings != null)
            {
                modifiers.FinalDamage *= 0.5f;
            }

            if (!ModContent.GetInstance<tsorcRevampConfig>().UseOriginalPlayerHurtSounds)
            {
                modifiers.DisableSound();
            }

            float REDUCE = CheckReduceDefense(Player.position, Player.width, Player.height, Player.fireWalk);
            if (REDUCE != 0)
            {
            }
            modifiers.FinalDamage.ApplyTo(modifiers.SourceDamage.Base);
            if (Player.HasBuff(ModContent.BuffType<Rejuvenation>()))
            {
                Player.ClearBuff(ModContent.BuffType<Rejuvenation>());
                Player.AddBuff(ModContent.BuffType<RejuvenationCooldown>(), 40 * 60);
            }
            if (Player.HeldItem.type == ModContent.ItemType<ToxicShot>() | Player.HeldItem.type == ModContent.ItemType<AlienGun>() && !Main.player[Main.myPlayer].HasBuff(ModContent.BuffType<ScoutsBoost2>()))
            {
                Player.AddBuff(ModContent.BuffType<ScoutsBoostCooldown>(), ToxicShot.ScoutsBoostOnHitCooldown * 60);
            }
            if (Player.HeldItem.type == ModContent.ItemType<OmegaSquadRifle>() && !Main.player[Main.myPlayer].HasBuff(ModContent.BuffType<ScoutsBoost2Omega>()))
            {
                Player.AddBuff(ModContent.BuffType<ScoutsBoostCooldownOmega>(), ToxicShot.ScoutsBoostOnHitCooldown * 60);
            }
        }

        public override void PostHurt(Player.HurtInfo info)
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().UseOriginalPlayerHurtSounds && Player.whoAmI == Main.myPlayer && info.Damage > 0)
            {
                float damagePct = Player.statLifeMax2 > 0 ? (float)info.Damage / Player.statLifeMax2 : 0f;
                int damageVoiceIndex = 1;
                if (damagePct <= 0.30f) damageVoiceIndex = 1;
                else if (damagePct <= 0.40f) damageVoiceIndex = 2;
                else if (damagePct <= 0.50f) damageVoiceIndex = 3;
                else if (damagePct <= 0.60f) damageVoiceIndex = 4;
                else damageVoiceIndex = 5;

                float healthPct = Player.statLifeMax2 > 0 ? (float)Player.statLife / Player.statLifeMax2 : 1f;
                int currentHealthBand = 1;
                if (healthPct < 0.30f) currentHealthBand = 5;
                else if (healthPct < 0.45f) currentHealthBand = 4;
                else if (healthPct < 0.60f) currentHealthBand = 3;
                else if (healthPct <= 0.75f) currentHealthBand = 2;
                else currentHealthBand = 1;

                if (currentHealthBand != lastHealthBand)
                {
                    guaranteedHurtSoundForBand = true;
                    lastHealthBand = currentHealthBand;
                }

                int voiceIndex = damageVoiceIndex;

                // If the hit itself dealt low damage, calculate the voice variant based on health bands
                if (damageVoiceIndex == 1)
                {
                    if (guaranteedHurtSoundForBand && currentHealthBand > 1)
                    {
                        voiceIndex = currentHealthBand;
                        guaranteedHurtSoundForBand = false;
                    }
                    else if (currentHealthBand > 1)
                    {
                        // 20% chance to play the low-health variant, otherwise play mild hurt-1
                        if (Main.rand.NextFloat() < 0.20f)
                        {
                            voiceIndex = currentHealthBand;
                        }
                        else
                        {
                            voiceIndex = 1;
                        }
                    }
                    else
                    {
                        voiceIndex = 1;
                    }
                }

                // Only play a hurt voice line if at least 2 seconds (120 ticks) have passed since the last one.
                if (Main.GameUpdateCount - lastHurtSoundTick >= 120)
                {
                    float pitchOffset = Main.rand.Next(-1, 2) * 0.08f;

                    if (Player.Male)
                    {
                        SoundEngine.PlaySound(new SoundStyle($"tsorcRevamp/Sounds/DarkSouls/Voices/Male/m-hurt-{voiceIndex}") with { Volume = 0.45f, Pitch = pitchOffset });
                    }
                    else
                    {
                        SoundEngine.PlaySound(new SoundStyle($"tsorcRevamp/Sounds/DarkSouls/Voices/Female/f-hurt-{voiceIndex}") with { Volume = 0.45f, Pitch = pitchOffset });
                    }

                    lastHurtSoundTick = Main.GameUpdateCount;
                }
            }

            // Player Hurt Visuals: trigger the red vignette flash, scaled by the fraction of max HP lost.
            if (Player.whoAmI == Main.myPlayer && info.Damage > 0 && ModContent.GetInstance<tsorcRevampVisualConfig>().PlayerHurtVisuals)
            {
                float dmgFrac = Player.statLifeMax2 > 0 ? (float)info.Damage / Player.statLifeMax2 : 0f;
                float flash = MathHelper.Clamp(0.25f + dmgFrac * 1.4f, 0f, 1f);
                if (flash > hurtVignetteFlash)
                {
                    hurtVignetteFlash = flash;
                }
            }

            if (info.Damage > 1)
            {
                Player.AddBuff(ModContent.BuffType<InCombat>(), 600); //10s
            }

            // Convenant of Everlasting Love
            if (HasLoveRing && loveHealCooldown <= 0 && info.Damage > 0)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player ally = Main.player[i];

                    if (!ally.active || ally.dead)
                        continue;

                    if (!(Player.team == 0 || ally.team == Player.team))
                        continue;

                    if (ally.whoAmI == Player.whoAmI)
                        continue; 

                    if (Vector2.Distance(Player.Center, ally.Center) < 1000f)
                    {
                        ally.statLife += LoveRing.HealAmount;
                        if (ally.statLife > ally.statLifeMax2)
                            ally.statLife = ally.statLifeMax2;

                        ally.HealEffect(LoveRing.HealAmount);

                        for (int b = 0; b < ally.buffType.Length; b++)
                        {
                            int buff = ally.buffType[b];

                            if (buff <= 0)
                                continue;

                            if (!Main.debuff[buff])
                                continue;

                            if (buff == BuffID.PotionSickness || buff == BuffID.ManaSickness) // ofc ingoring these
                                continue;

                            ally.buffTime[b] -= 120; // 2 seconds
                            if (ally.buffTime[b] < 0)
                                ally.buffTime[b] = 0;
                        }

                        for (int h = 0; h < 45; h++)
                        {
                            float t = MathHelper.TwoPi * (h / 40f);

                            float x = 16 * (float)Math.Pow(Math.Sin(t), 3);
                            float y = 13 * (float)Math.Cos(t)
                                    - 5 * (float)Math.Cos(2 * t)
                                    - 2 * (float)Math.Cos(3 * t)
                                    - (float)Math.Cos(4 * t);

                            Vector2 offset = new Vector2(x, -y) * 1.8f;

                            int dust = Dust.NewDust(
                                ally.Center + offset,
                                0, 0,
                                58,
                                0f, 0f,
                                150,
                                default,
                                1.5f
                            );

                            Main.dust[dust].noGravity = true;
                            Main.dust[dust].velocity *= 0.1f;
                        }
                    }
                }

                // Cooldown Love Ring
                loveHealCooldown = LoveRing.Cooldown;
            }
        }

        public override void OnHitAnything(float x, float y, Entity victim)
        {
            if (Shunpo && Player.titaniumStormCooldown >= 0)
            {
                int TitaniumShardBaseDmg = 50; //50 is the base dmg of vanilla Titanium Shards
                int TitaniumShardScaledBonusDmg = (int)Player.GetDamage(DamageClass.Generic).ApplyTo(TitaniumShardBaseDmg);
                Player.titaniumStormCooldown = 10;
                Player.AddBuff(BuffID.TitaniumStorm, 10 * 60);
                if (Player.ownedProjectileCounts[ProjectileID.TitaniumStormShard] < 15)
                {
                    Player.ownedProjectileCounts[ProjectileID.TitaniumStormShard]++;
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        Projectile.NewProjectile(Player.GetSource_OnHit(victim), Player.Center, Vector2.Zero, ProjectileID.TitaniumStormShard, TitaniumShardBaseDmg + TitaniumShardScaledBonusDmg, 15f, Player.whoAmI);
                    }
                }
                else
                {
                    UsefulFunctions.AddPlayerBuffDuration(Player, ModContent.BuffType<ShunpoBlinkCooldown>(), ShunpoCooldownPerHit);
                }
            }
            if (CelestialCloak)
            {
                if (Main.rand.NextBool(25))
                {
                    Vector2 starvector1 = new Vector2(-40, -200) + victim.Center;
                    Vector2 starvector2 = new Vector2(40, -200) + victim.Center;
                    Vector2 starvector3 = new Vector2(0, -200) + victim.Center;
                    Vector2 starmove1 = new Vector2(+4, 20);
                    Vector2 starmove2 = new Vector2(-4, 20);
                    Vector2 starmove3 = new Vector2(0, 20);
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), starvector1, starmove1, ProjectileID.ManaCloakStar, Player.statManaMax2 / 5, 2f, Main.myPlayer);
                        Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), starvector2, starmove2, ProjectileID.ManaCloakStar, Player.statManaMax2 / 5, 2f, Main.myPlayer);
                        Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), starvector3, starmove3, ProjectileID.ManaCloakStar, Player.statManaMax2 / 5, 2f, Main.myPlayer);
                    }
                }
            }
            if (Main.rand.NextBool(9) & MagicPlatingStacks <= 22 & Player.HasBuff(ModContent.BuffType<MagicPlating>()))
            {
                MagicPlatingStacks += 7;
            }
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            // Shared humanoid melee uses an invisible projectile for collision and shield traits, but the helper's
            // internal name should never appear in death text. Preserve projectile attribution through the hit,
            // then swap only the final death reason to the NPC that authored it.
            int sourceProjectileIndex = damageSource.SourceProjectileLocalIndex;
            if (sourceProjectileIndex >= 0 && sourceProjectileIndex < Main.maxProjectiles)
            {
                Projectile sourceProjectile = Main.projectile[sourceProjectileIndex];
                if (sourceProjectile.active
                    && sourceProjectile.type == ModContent.ProjectileType<Projectiles.Enemy.Weapons.HumanoidMeleeHitbox>()
                    && sourceProjectile.GetGlobalProjectile<Projectiles.tsorcGlobalProjectile>().TryGetSourceNPC(out NPC sourceNPC))
                {
                    damageSource = PlayerDeathReason.ByNPC(sourceNPC.whoAmI);
                }
            }

            if (Player.whoAmI == Main.myPlayer)
            {                
                DeathText = PickDeathText();

                tsorcRevampPlayer modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();

                modPlayer.HadBuffAmmoBox = Player.HasBuff(BuffID.AmmoBox);
                modPlayer.HadBuffBewitched = Player.HasBuff(BuffID.Bewitched);
                modPlayer.HadBuffSharpened = Player.HasBuff(BuffID.Sharpened);
                modPlayer.HadBuffStrategist = Player.HasBuff(BuffID.WarTable);
                modPlayer.HadBuffClairvoyance = Player.HasBuff(BuffID.Clairvoyance);
            }
            if (PhoenixSkull && !Player.HasBuff(ModContent.BuffType<PhoenixRebirthCooldown>()))
            {
                Dust dust1 = Main.dust[Dust.NewDust(Player.BottomLeft, Player.width, Player.height - 40, 6, 0f, -5f, 100, default, 1.8f)];
                dust1.velocity.Y = Main.rand.NextFloat(-5, -2.5f);
                dust1.velocity.X = Main.rand.NextFloat(-1, 1);
                Dust dust2 = Main.dust[Dust.NewDust(Player.BottomLeft, Player.width, Player.height - 40, 6, 0f, -5f, 50, default, 1.2f)];
                dust2.velocity.Y = Main.rand.NextFloat(-5, -2.5f);
                dust2.velocity.X = Main.rand.NextFloat(-1, 1);
                if (Main.myPlayer == Player.whoAmI)
                {
                    Projectile.NewProjectile(Player.GetSource_None(), Player.Top, Player.velocity, ProjectileID.DD2ExplosiveTrapT2Explosion, 250, 15, Player.whoAmI);
                }
                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = 4f });

                for (int d = 0; d < 90; d++) // Upwards
                {
                    Dust dust = Main.dust[Dust.NewDust(Player.BottomLeft, Player.width, Player.height - 40, 6, 0f, -5f, 30, default, Main.rand.NextFloat(1, 1.8f))]; // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                    dust.velocity.Y = Main.rand.NextFloat(-5, -0f);
                    dust.velocity.X = Main.rand.NextFloat(-1.5f, 1.5f);
                }

                for (int d = 0; d < 30; d++) // Left
                {
                    Dust dust = Main.dust[Dust.NewDust(Player.BottomLeft, Player.width, Player.height - 55, 6, -6f, -4f, 30, default, Main.rand.NextFloat(1, 1.8f))]; // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                    dust.velocity.Y = Main.rand.NextFloat(-4, -0f);
                    dust.velocity.X = Main.rand.NextFloat(-5, -1.5f);
                }

                for (int d = 0; d < 30; d++) // Right
                {
                    Dust dust = Main.dust[Dust.NewDust(Player.BottomLeft, Player.width, Player.height - 55, 6, 6f, -4f, 30, default, Main.rand.NextFloat(1, 1.8f))]; // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                    dust.velocity.Y = Main.rand.NextFloat(-4, -0f);
                    dust.velocity.X = Main.rand.NextFloat(5, 1.5f);
                }
                Player.statLife = (int)(Player.statLifeMax2 * Items.Accessories.Defensive.PhoenixSkull.HealthPercent / 100f);
                Player.AddBuff(ModContent.BuffType<PhoenixRebirthCooldown>(), Items.Accessories.Defensive.PhoenixSkull.Cooldown * 60);
                Player.AddBuff(ModContent.BuffType<PhoenixRebirthBuff>(), Items.Accessories.Defensive.PhoenixSkull.Duration * 60);
                Player.SetImmuneTimeForAllTypes(1 * 60 + 30);
                return false;
            }
            if (ModContent.GetInstance<tsorcRevampConfig>().DeleteDroppedSoulsOnDeath && Main.netMode == NetmodeID.SinglePlayer)
            {
                for (int i = 0; i < 400; i++)
                {
                    if (Main.item[i].type == ModContent.ItemType<DarkSoul>())
                    {
                        Main.item[i].active = false;
                    }
                }
            }
            return true;
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            if (Main.myPlayer == Player.whoAmI)
            {
                Projectile.NewProjectile(Player.GetSource_Misc("Bloodsign"), Player.Bottom, new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Bloodsign>(), 0, 0, Player.whoAmI);
            }
            //Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath58.WithVolume(0.8f).WithPitchVariance(.3f), player.position);

            //you died sound
            Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/you-died") with { Volume = 0.4f });


            bool onePlayerAlive = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    onePlayerAlive = true;
                }
            }

            SteelTempestStacks = 0;

            if (!onePlayerAlive)
            {
                if (NPC.AnyNPCs(NPCID.LunarTowerSolar))
                {
                    NPC.ShieldStrengthTowerSolar = NPC.ShieldStrengthTowerMax;
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.SolarPillar"), Color.OrangeRed);
                }
                if (NPC.AnyNPCs(NPCID.LunarTowerStardust))
                {
                    NPC.ShieldStrengthTowerStardust = NPC.ShieldStrengthTowerMax;
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.StardustPillar"), Color.Cyan);
                }
                if (NPC.AnyNPCs(NPCID.LunarTowerVortex))
                {
                    NPC.ShieldStrengthTowerVortex = NPC.ShieldStrengthTowerMax;
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.VortexPillar"), Color.Teal);
                }
                if (NPC.AnyNPCs(NPCID.LunarTowerNebula))
                {
                    NPC.ShieldStrengthTowerNebula = NPC.ShieldStrengthTowerMax;
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.NebulaPillar"), Color.Pink);
                }
            }
        }

        public override bool ShiftClickSlot(Item[] inventory, int context, int slot)
        {
            if (Player.HasItem(ModContent.ItemType<PotionBag>()) && (context == ItemSlot.Context.ChestItem || context == ItemSlot.Context.BankItem || context == ItemSlot.Context.InventoryItem))
            {
                if (PotionBagUIState.IsValidPotion(inventory[slot]))
                {
                    //Mostly just lazy copying of OnPickup code, but it works
                    int? emptySlot = null;
                    Item item = inventory[slot];
                    bool inPotionBag = false; //Is the item being clicked in the potion bag? Hard to tell, because the bag is treated like a normal inventory slot. We have to check manually.
                    for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
                    {
                        if (item == PotionBagItems[i])
                        {
                            inPotionBag = true;
                        }
                    }

                    //If moving from other inventories to the bag
                    if (!inPotionBag)
                    {
                        for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
                        {
                            if (PotionBagItems[i].type == 0 && emptySlot == null)
                            {
                                emptySlot = i;
                            }
                            if (PotionBagItems[i].type == item.type && (PotionBagItems[i].stack + item.stack) <= PotionBagItems[i].maxStack)
                            {
                                PotionBagItems[i].stack += item.stack;
                                item.TurnToAir();
                                if (Main.netMode == 1 && Player.chest >= -1 && context == ItemSlot.Context.ChestItem)
                                {
                                    NetMessage.SendData(32, -1, -1, null, Player.chest, slot);
                                }
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8);
                                return true;
                            }
                        }

                        //If it got here, that means there's no existing stacks with room
                        //So go through it again, finding the first empty slot instead
                        if (emptySlot != null)
                        {
                            PotionBagItems[emptySlot.Value] = item.Clone();
                            item.TurnToAir();
                            if (Main.netMode == 1 && Player.chest >= -1 && context == ItemSlot.Context.ChestItem)
                            {
                                NetMessage.SendData(32, -1, -1, null, Player.chest, slot);
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8);
                            return true;
                        }
                    }

                    //Copying from the bag to inventory
                    else
                    {
                        for (int i = 0; i < 50; i++)
                        {
                            if (Player.inventory[i].type == 0 && emptySlot == null)
                            {
                                emptySlot = i;
                            }
                            if (Player.inventory[i].type == item.type && (Player.inventory[i].stack + item.stack) <= Player.inventory[i].maxStack)
                            {
                                Player.inventory[i].stack += item.stack;
                                item.TurnToAir();
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8);
                                return true;
                            }
                        }

                        if (emptySlot != null)
                        {
                            Player.inventory[emptySlot.Value] = item.Clone();
                            item.TurnToAir();
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8);
                            return true;
                        }
                    }


                }
            }

            // Dark Souls Storage: while the Storage pop-up is open, shift-clicking an inventory item sends it
            // to storage. Scoped to "storage open" so it never hijacks the normal shift-click-into-a-chest path.
            // Must also check `inventory == Player.inventory`: the Storage grid's own slots reuse this exact
            // context via ItemSlot.Handle's single-item overload (which hands back ItemSlot.singleSlotArray, a
            // shared scratch array, not the real inventory) — without this guard, shift-clicking an item that's
            // already IN storage would re-deposit it into itself and corrupt the stack.
            if (UI.StorageUIState.Visible
                && (context == ItemSlot.Context.InventoryItem)
                && inventory == Player.inventory
                && IsStorageDepositable(inventory[slot]))
            {
                if (DepositToStorage(inventory[slot]))
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                    return true;
                }
            }

            return false;
        }

        private static Item CreateStartingItem(int itemType, int stack = 1, int prefix = 0)
        {
            Item item = new Item();
            item.SetDefaults(itemType);
            item.stack = stack;
            if (prefix != 0)
            {
                item.prefix = prefix;
            }
            return item;
        }

        private static void AddClassStartingItems(List<Item> startingItems, StartingClass startingClass)
        {
            switch (startingClass)
            {
                case StartingClass.Ranged:
                    startingItems.Add(CreateStartingItem(ModContent.ItemType<Items.Weapons.Ranged.Crossbows.Crossbow>(), prefix: PrefixID.Awful));
                    startingItems.Add(CreateStartingItem(ModContent.ItemType<Bolt>(), 50));
                    startingItems.Add(CreateStartingItem(ItemID.IronShortsword, prefix: PrefixID.Dull));
                    break;
                case StartingClass.Magic:
                    startingItems.Add(CreateStartingItem(ModContent.ItemType<ApprenticesWand>(), prefix: PrefixID.Ignorant));
                    startingItems.Add(CreateStartingItem(ItemID.BorealWoodSword, prefix: PrefixID.Dull));
                    break;
                case StartingClass.Summoner:
                    startingItems.Add(CreateStartingItem(ModContent.ItemType<RustedChain>(), prefix: PrefixID.Terrible));
                    startingItems.Add(CreateStartingItem(ItemID.BabyBirdStaff, prefix: PrefixID.Terrible));
                    break;
                case StartingClass.Deprived:
                    // No bonus weapon added here - Deprived's "weapon" is the vanilla Copper Shortsword, which
                    // OnEnterWorld leaves in place instead of stripping it like it does for every other class.
                    break;
                case StartingClass.Melee:
                default:
                    startingItems.Add(CreateStartingItem(ModContent.ItemType<ForgottenRuneAxe>(), prefix: PrefixID.Dull));
                    startingItems.Add(CreateStartingItem(ItemID.WoodenBoomerang, prefix: PrefixID.Dull));
                    break;
            }
        }

        /// <summary>Item types of the bonus weapon(s) a class starts with, for display purposes (e.g. the
        /// starting-class selection tooltip). Kept in sync by hand with <see cref="AddClassStartingItems"/> —
        /// prefixes/stack sizes don't matter here, only which items to name.</summary>
        internal static int[] GetClassStartingWeaponTypes(StartingClass startingClass)
        {
            return startingClass switch
            {
                StartingClass.Ranged => new[] { ModContent.ItemType<Items.Weapons.Ranged.Crossbows.Crossbow>(), ItemID.IronShortsword },
                StartingClass.Magic => new[] { ModContent.ItemType<ApprenticesWand>(), ItemID.BorealWoodSword },
                StartingClass.Summoner => new[] { ModContent.ItemType<RustedChain>(), ItemID.BabyBirdStaff },
                StartingClass.Deprived => new[] { (int)ItemID.CopperShortsword },
                StartingClass.Melee or _ => new[] { ModContent.ItemType<ForgottenRuneAxe>(), ItemID.WoodenBoomerang },
            };
        }
        public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
        {
            List<Item> startingItems = new List<Item>();
            Item item = new Item();
            item.SetDefaults(ModContent.ItemType<Darksign>());
            startingItems.Add(item);

            Item PotionBagItem = new Item();
            PotionBagItem.SetDefaults(ModContent.ItemType<PotionBag>());
            startingItems.Add(PotionBagItem);

            Item MastersScroll = new Item();
            MastersScroll.SetDefaults(ModContent.ItemType<MastersScroll>());
            startingItems.Add(MastersScroll);

            // Reference book for the mod's non-vanilla systems. Right-click pages through it.
            Item GameManual = new Item();
            GameManual.SetDefaults(ModContent.ItemType<Items.Lore.GameManual>());
            startingItems.Add(GameManual);

            if (!mediumCoreDeath)
            {
                startingItems.Add(CreateStartingItem(ModContent.ItemType<RecommendedControls>()));
            }

            startingItems.Add(CreateStartingItem(ModContent.ItemType<NormansRing>()));

            if (startingClass == StartingClass.None)
            {
                startingClass = tsorcRevamp.PendingStartingClass == StartingClass.None ? StartingClass.Melee : tsorcRevamp.PendingStartingClass;
            }
            AddClassStartingItems(startingItems, startingClass);
            if (ModLoader.TryGetMod("MagicStorage", out Mod MagicStorage))
            {
                Item StorageHeart = new();
                MagicStorage.TryFind("StorageHeart", out ModItem heart);
                StorageHeart.SetDefaults(heart.Type);
                startingItems.Add(StorageHeart);

                Item CraftingAccess = new();
                MagicStorage.TryFind("CraftingAccess", out ModItem ca);
                CraftingAccess.SetDefaults(ca.Type);
                startingItems.Add(CraftingAccess);

                Item StorageUnit = new();
                MagicStorage.TryFind("StorageUnit", out ModItem unit);
                StorageUnit.SetDefaults(unit.Type);
                StorageUnit.stack = 16;
                startingItems.Add(StorageUnit);

                Item EnvironmentAccess = new();
                MagicStorage.TryFind("EnvironmentAccess", out ModItem ea);
                EnvironmentAccess.SetDefaults(ea.Type);
                startingItems.Add(EnvironmentAccess);

            }

            return startingItems;
        }
        private const int ArtoriasAbysswalkerPoiseDuration = 3 * 60;
        private const int ArtoriasAbysswalkerPoiseCooldown = 5 * 60;
        private const int ArtoriasAbysswalkerMeleeCounter = 1;
        private const int ArtoriasAbysswalkerMagicCounter = 2;

        public void TryGrantArtoriasAbysswalkerPoise()
        {
            if (!ArtoriasAbysswalker || !isDodging || Player.HasBuff(ModContent.BuffType<ArtoriasAbysswalkerPoise>()) || Player.HasBuff(ModContent.BuffType<ArtoriasAbysswalkerPoiseCooldown>()))
            {
                return;
            }

            bool dodgedThreat = false;
            Rectangle paddedHitbox = Player.Hitbox;
            paddedHitbox.Inflate(8, 8);

            for (int i = 0; i < Main.npc.Length && !dodgedThreat; i++)
            {
                NPC npc = Main.npc[i];
                dodgedThreat = npc.active && !npc.friendly && npc.damage > 0 && !npc.dontTakeDamage && paddedHitbox.Intersects(npc.Hitbox);
            }

            for (int i = 0; i < Main.projectile.Length && !dodgedThreat; i++)
            {
                Projectile projectile = Main.projectile[i];
                dodgedThreat = projectile.active && projectile.hostile && projectile.damage > 0 && paddedHitbox.Intersects(projectile.Hitbox);
            }

            if (!dodgedThreat)
            {
                return;
            }

            Player.AddBuff(ModContent.BuffType<ArtoriasAbysswalkerPoise>(), ArtoriasAbysswalkerPoiseDuration);
            Player.AddBuff(ModContent.BuffType<ArtoriasAbysswalkerPoiseCooldown>(), ArtoriasAbysswalkerPoiseCooldown);
            SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f, Pitch = -0.35f }, Player.Center);

            for (int i = 0; i < 24; i++)
            {
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(18f, 26f), DustID.ShadowbeamStaff, Main.rand.NextVector2Circular(2.5f, 2.5f), 110, Color.MediumPurple, Main.rand.NextFloat(0.9f, 1.6f));
                dust.noGravity = true;
            }
        }

        private bool CanSpendArtoriasAbysswalkerPoise(DamageClass damageType)
        {
            return ArtoriasAbysswalker && Player.HasBuff(ModContent.BuffType<ArtoriasAbysswalkerPoise>()) && (damageType == DamageClass.Melee || damageType == DamageClass.MeleeNoSpeed || damageType == DamageClass.Magic);
        }

        private void SpendArtoriasAbysswalkerPoise(NPC target, NPC.HitInfo hit)
        {
            if (ArtoriasAbysswalkerCounterType == 0 || !Player.HasBuff(ModContent.BuffType<ArtoriasAbysswalkerPoise>()))
            {
                return;
            }

            Player.ClearBuff(ModContent.BuffType<ArtoriasAbysswalkerPoise>());
            Player.noKnockback = true;
            Player.immune = true;
            Player.SetImmuneTimeForAllTypes(18);

            tsorcRevampStaminaPlayer staminaPlayer = Player.GetModPlayer<tsorcRevampStaminaPlayer>();
            float staminaRestored = staminaPlayer.staminaResourceMax2 * ArtoriasOfTheAbyssHelm.PoiseStaminaRestore / 100f;
            staminaPlayer.staminaResourceCurrent = MathHelper.Clamp(staminaPlayer.staminaResourceCurrent + staminaRestored, 0, staminaPlayer.staminaResourceMax2);

            if (Main.myPlayer == Player.whoAmI)
            {
                if (ArtoriasAbysswalkerCounterType == ArtoriasAbysswalkerMeleeCounter)
                {
                    Vector2 velocity = Player.DirectionTo(target.Center);
                    if (velocity == Vector2.Zero)
                    {
                        velocity = new Vector2(Player.direction, 0);
                    }
                    velocity.Normalize();
                    Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center - velocity * 24f, velocity * 14f, ModContent.ProjectileType<Projectiles.Melee.ArtoriasAbyssSlash>(), (int)Player.GetTotalDamage(DamageClass.Melee).ApplyTo(hit.SourceDamage * 0.55f), 4f, Player.whoAmI, velocity.ToRotation());
                }
                else if (ArtoriasAbysswalkerCounterType == ArtoriasAbysswalkerMagicCounter)
                {
                    Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Magic.ArtoriasAbyssShockwave>(), (int)Player.GetTotalDamage(DamageClass.Magic).ApplyTo(hit.SourceDamage * 0.45f), 5f, Player.whoAmI);
                    Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, new Vector2(-9f, 0f), ModContent.ProjectileType<Projectiles.Melee.ArtoriasAbyssSlash>(), (int)Player.GetTotalDamage(DamageClass.Magic).ApplyTo(hit.SourceDamage * 0.25f), 3f, Player.whoAmI, MathHelper.Pi, 1f);
                    Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, new Vector2(9f, 0f), ModContent.ProjectileType<Projectiles.Melee.ArtoriasAbyssSlash>(), (int)Player.GetTotalDamage(DamageClass.Magic).ApplyTo(hit.SourceDamage * 0.25f), 3f, Player.whoAmI, 0f, 1f);
                }
            }

            ArtoriasAbysswalkerCounterType = 0;
            SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.55f, Pitch = -0.25f }, target.Center);
        }
        public static float AmmoReservationRangedCritDamage = 10f;
        public static float TitanMeleeSize = 15f;
        public static float SharpenedMeleeArmorPen = 50f;
        public static float MythrilOcrichalcumCritDmg = 25f;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (modifiers.DamageType != DamageClass.Default)
            {
                modifiers.HideCombatText(); //CustomCombatText displays a customizable combat text instead
            }
            modifiers.CritDamage.Flat += JaggedFlatCritDmgBonus;
            if (SmoughAttackSpeedReduction)
            {
                modifiers.SetCrit();
                modifiers.CritDamage -= SmoughArmor.BadCritDmg / 100f;
            }
            if (CanUseItemsWhileDodging && !ArtoriasAbysswalker && isDodging && (modifiers.DamageType == DamageClass.Melee || modifiers.DamageType == DamageClass.MeleeNoSpeed))
            {
                modifiers.FinalDamage += ArtoriasArmor.DmgMultWhileRolling;
            }
            if (CanSpendArtoriasAbysswalkerPoise(modifiers.DamageType))
            {
                modifiers.FinalDamage += ArtoriasOfTheAbyssHelm.PoiseDamage / 100f;
                ArtoriasAbysswalkerCounterType = modifiers.DamageType == DamageClass.Magic ? ArtoriasAbysswalkerMagicCounter : ArtoriasAbysswalkerMeleeCounter;
            }
            if (Player.GetModPlayer<tsorcRevampPlayer>().NoDamageSpread)
            {
                modifiers.DamageVariationScale *= 0;
            }
            if (Player.GetModPlayer<tsorcRevampPlayer>().Sharpened)
            {
                modifiers.ScalingArmorPenetration += SharpenedMeleeArmorPen / 100f;
            }
            if (Player.GetModPlayer<tsorcRevampPlayer>().AmmoReservationPotion)
            {
                modifiers.CritDamage += Player.GetModPlayer<tsorcRevampPlayer>().AmmoReservationDamageScaling * AmmoReservationRangedCritDamage / 100f;
            }
            if (OldWeapon)
            {
                float damageMult = Main.rand.NextFloat(0.0f, 0.8696f);
                modifiers.TargetDamageMultiplier *= damageMult;
            }
            if (Player.GetModPlayer<tsorcRevampPlayer>().MythrilOrichalcumCritDamage)
            {
                modifiers.CritDamage += MythrilOcrichalcumCritDmg / 100f;
            }
            if (DragonSoulEffect)
            {
                target.AddBuff(BuffID.Daybreak, 180);
            }
            if (MidasGreedEffect)
            {
                target.AddBuff(BuffID.Midas, 300);
            }
            if (MaskOfTheFather)
            {
                modifiers.CritDamage += Items.Armors.MaskOfTheFather.CritDmgIncrease / 100f;
            }
        }
        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)/* tModPorter If you don't need the Item, consider using ModifyHitNPC instead */
        {
            LastAttackedNPCIndex = target.whoAmI;
            timeSinceLastAttacked = 0;

            if (SmoughAttackSpeedReduction)
            {
                if (modifiers.DamageType == DamageClass.SummonMeleeSpeed)
                {
                    modifiers.SetCrit();
                }
            }
            if ((BurningAura || BurningStone) && target.onFire == true)
            {
                modifiers.TargetDamageMultiplier *= 1.05f;
            }
            OverCrit(Player.GetWeaponCrit(Player.HeldItem), item.DamageType, ref modifiers, out CritColorTier);

            if (target.whoAmI == tsorcRevampPlayer.LastHit)
            {
                tsorcRevampPlayer.SameHit = true;
            }
            else
            {
                tsorcRevampPlayer.DiffHit = true;
            }
            tsorcRevampPlayer.LastHit = target.whoAmI;
        }
        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)/* tModPorter If you don't need the Projectile, consider using ModifyHitNPC instead */
        {
            // Last attacked NPC is used for summon targeting, it would be pointless if the target updated with summon attacks
            if (!Main.projPet[proj.type])
            { 
                LastAttackedNPCIndex = target.whoAmI;
                timeSinceLastAttacked = 0;
            }

            if (SmoughAttackSpeedReduction && modifiers.DamageType == DamageClass.SummonMeleeSpeed)
            {
                if (!ProjectileID.Sets.IsAWhip[proj.type])
                {
                    modifiers.SetCrit();
                }
            }
            if (CanUseItemsWhileDodging && !ArtoriasAbysswalker && isDodging && (proj.type == ProjectileID.NebulaBlaze2) && Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Melee.Broadswords.YianBlade>())
            {
                modifiers.FinalDamage -= ArtoriasArmor.DmgMultWhileRolling;
            }
            if (ShunpoTimer > 0 && (proj.type == ProjectileID.JoustingLance || proj.type == ProjectileID.HallowJoustingLance || proj.type == ProjectileID.ShadowJoustingLance))
            {
                modifiers.FinalDamage *= 0.15f;
            }
            if (modifiers.DamageType == DamageClass.Ranged && InfinityEdge)
            {
                modifiers.CritDamage += Items.Accessories.Ranged.InfinityEdge.CritDmgIncrease / 100f;
            }
            if (((proj.type == ProjectileID.MoonlordArrow) || (proj.type == ProjectileID.MoonlordArrowTrail)) && Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Bows.CernosPrime>())
            {
                modifiers.FinalDamage *= 0.55f;
            }
            if (proj.DamageType == DamageClass.SummonMeleeSpeed && MorgulWhipEffect)
            {
                target.AddBuff(ModContent.BuffType<MorgulPoisoning>(), 240);
            }
            if (Goredrinker && proj.DamageType == DamageClass.SummonMeleeSpeed && !Player.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && GoredrinkerSwung && ProjectileID.Sets.IsAWhip[proj.type])
            {
                modifiers.SourceDamage += Items.Accessories.Summon.Goredrinker.WhipDmgRange / 100f / 3f;
            }
            if (ProjectileID.Sets.IsAWhip[proj.type] && WhipTipHit(proj, proj.WhipPointsForCollision, target.Hitbox))
            {
                modifiers.SourceDamage += WhipTipHitBonusDamage / 100f;
            }
            if (BurningAura || BurningStone && target.onFire == true && proj.type != ModContent.ProjectileType<Projectiles.HomingFireball>())
            {
                modifiers.TargetDamageMultiplier *= 1f + Items.Accessories.Damage.BurningStone.DamageIncrease / 100f;
            }
            if (proj.type == ProjectileID.StardustDragon1 || proj.type == ProjectileID.StardustDragon2 || proj.type == ProjectileID.StardustDragon3 || proj.type == ProjectileID.StardustDragon4)
            {
                float DragonStacks = Player.ownedProjectileCounts[ProjectileID.StardustDragon1] + Player.ownedProjectileCounts[ProjectileID.StardustDragon2] + Player.ownedProjectileCounts[ProjectileID.StardustDragon3] + Player.ownedProjectileCounts[ProjectileID.StardustDragon4];
                modifiers.SourceDamage *= MathF.Max(SummonerEdits.StardustDragonBaseDmgMult - DragonStacks / 100f, 0.2f);
            }
            if (!proj.IsMinionOrSentryRelated)
            {
                OverCrit(proj.CritChance, proj.DamageType, ref modifiers, out CritColorTier);
            }
            if (ProjectileID.Sets.MinionSacrificable[proj.type])
            {
                ShunpoCooldownPerHit = -4;
            }
        }
        public override void ModifyItemScale(Item item, ref float scale)
        {
            if (Player.GetModPlayer<tsorcRevampPlayer>().TitanPotion && item.DamageType == DamageClass.Melee)
            {
                scale += Player.GetModPlayer<tsorcRevampPlayer>().TitanSizeScaling * TitanMeleeSize / 100f;
            }
        }
        public bool WhipTipHit(in Projectile projectile, in List<Vector2> points, in Rectangle targetHitbox)
        {
            Player player = Main.player[projectile.owner];
            if (Goredrinker && !Player.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && GoredrinkerSwung)
            {
                return true;
            }
            Vector2 TipBase = tsorcRevamp.WhipTipBases[projectile.type];
            if (Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 2], TipBase * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier * player.GetModPlayer<tsorcRevampPlayer>().WhipTipHitboxSize).Intersects(targetHitbox) || 
                Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 1], TipBase * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier * player.GetModPlayer<tsorcRevampPlayer>().WhipTipHitboxSize).Intersects(targetHitbox))
            {
                return true;
            }
            return false;
        }
        public void OverCrit(in int CritChance, DamageClass damageType, ref NPC.HitModifiers modifiers, out int critColorTier)
        {
            int critLevel = (int)(Math.Floor(CritChance / 100f));
            critColorTier = 0;
            if (critLevel != 0 && damageType != DamageClass.Summon && damageType != DamageClass.SummonMeleeSpeed)
            {
                if (critLevel > 1)
                {
                    for (int i = 1; i < critLevel; i++)
                    {
                        modifiers.CritDamage += 1;
                        modifiers.HideCombatText();
                        critColorTier++;
                    }
                }
                if (Main.rand.Next(1, 101) <= (float)CritChance - (100 * critLevel))
                {
                    modifiers.CritDamage += 1;
                    modifiers.HideCombatText();
                    critColorTier++;
                }
            }
            else if (critLevel != 0 && (damageType == DamageClass.Summon | damageType == DamageClass.SummonMeleeSpeed))
            {
                modifiers.SetCrit();
                if (critLevel > 1)
                {
                    for (int i = 1; i < critLevel; i++)
                    {
                        modifiers.CritDamage += 1;
                        modifiers.HideCombatText();
                        critColorTier++;
                    }
                }
                if (Main.rand.Next(1, 101) <= (float)CritChance - (100 * critLevel))
                {
                    modifiers.CritDamage += 1;
                    modifiers.HideCombatText();
                    critColorTier++;
                }
            }
            /*else if (IsWhip)
            {
                if (WhipTipCrit(projectile, projectile.WhipPointsForCollision, targetHitbox) || (Goredrinker && !Player.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && GoredrinkerSwung))
                {
                    modifiers.SetCrit();
                    if (critLevel > 0)
                    {
                        for (int i = 0; i < critLevel; i++)
                        {
                            modifiers.CritDamage += 1;
                            modifiers.HideCombatText();
                            critColorTier++;
                        }
                    }
                    if (Main.rand.Next(1, 101) <= (float)CritChance - (100 * critLevel))
                    {
                        modifiers.CritDamage += 1;
                        modifiers.HideCombatText();
                        critColorTier++;
                    }
                }
            }*/
            else
            {
                if (Main.rand.Next(1, 101) <= (float)CritChance - (100 * critLevel))
                {
                    modifiers.SetCrit();
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (MagmaArmor && target.HasBuff(BuffID.OnFire) || target.HasBuff(BuffID.OnFire3))
            {
                target.AddBuff(ModContent.BuffType<Ignited>(), 5 * 60);
            }
            SpendArtoriasAbysswalkerPoise(target, hit);
            if (PhoenixSkull && Player.HasBuff(ModContent.BuffType<PhoenixRebirthBuff>()) && (int)(Items.Accessories.Defensive.PhoenixSkull.LifeSteal * damageDone / 100f) > 0)
            {
                Player.HealEffect((int)(Items.Accessories.Defensive.PhoenixSkull.LifeSteal * damageDone / 100f));
                Player.statLife += ((int)(Items.Accessories.Defensive.PhoenixSkull.LifeSteal * damageDone / 100f));
            }
            if (MiakodaFull)
            { //Miakoda Full Moon
                if (MiakodaEffectsTimer > Items.Pets.MiakodaFull.HealCooldown * 60)
                {
                    if (hit.Crit) //summoner has decent options for crits now
                    {
                        Player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal1 = true;
                        Player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal2 = true;

                        //2 per 100 max hp, plus 2
                        int HealAmount = (int)((Math.Floor((double)(Player.statLifeMax2 / 100)) * Items.Pets.MiakodaFull.MaxHPHealPercent) + Items.Pets.MiakodaFull.BaseHealing);
                        Player.statLife += HealAmount;
                        Player.HealEffect(HealAmount, false);
                        if (Player.statLife > Player.statLifeMax2)
                        {
                            Player.statLife = Player.statLifeMax2;
                        }

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f }, Player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }

            if (MiakodaCrescent)
            { //Miakoda Crescent Moon
                if (MiakodaEffectsTimer > Items.Pets.MiakodaCrescent.BoostCooldown * 60)
                {
                    if (hit.Crit) //summoner has decent options for crits now
                    {
                        Player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust1 = true;
                        Player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust2 = true;
                        Player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentBoost = true;

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item100 with { Volume = 0.75f }, Player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }

            if (MiakodaNew)
            { //Miakoda New Moon
                if (MiakodaEffectsTimer > Items.Pets.MiakodaNew.BoostCooldown * 60)
                {
                    if (hit.Crit)
                    {
                        Player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewDust1 = true;
                        Player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewDust2 = true;
                        Player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewBoost = true;

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item81 with { Volume = 0.75f }, Player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }
        }
        public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
        {
            if (item.type == ModContent.ItemType<Items.Weapons.Melee.Spears.PilgrimSpontoon>() && PilgrimSpontoonBuff)
            {
                Player.GetAttackSpeed(DamageClass.Melee) *= 27f / 21f; //reduced the use time of pilgrim spontoon
            }
        }
        public void CustomCombatText(in Rectangle targetHitbox, in int damageDealt, in int CritColorTier, in bool isCrit, bool isWhipTipCrit = false)
        {
            Color ColorOfCrit = Color.Orange;
            switch (CritColorTier)
            {
                case 1:
                    {
                        ColorOfCrit = Color.Blue;
                        break;
                    }
                case 2:
                    {
                        ColorOfCrit = Color.Purple;
                        break;
                    }
                case 3:
                {
                    ColorOfCrit = Color.White;
                        break;
                    }
                case 4:
                    {
                        ColorOfCrit = Color.Black;
                        break;
                    }
                case 5:
                {
                    ColorOfCrit = Color.Red;
                    break;
                }
                default:
                    {
                        if (isCrit)
                        {
                            ColorOfCrit = Color.OrangeRed;
                        }
                        break;
                    }
            }
            CombatText.NewText(targetHitbox, ColorOfCrit, damageDealt + (isWhipTipCrit ? "!" : ""), isCrit, false);
        }
        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)/* tModPorter If you don't need the Item, consider using OnHitNPC instead */
        {
            if (item.DamageType != DamageClass.Default)
            {
                CustomCombatText(target.Hitbox, damageDone, CritColorTier, hit.Crit); 
            }
            if (MeleeArmorVamp10)
            {
                if (Main.rand.NextBool(10))
                {
                    Player.HealEffect(10);
                    Player.statLife += 10;
                }
            }
            if (DemonPower && hit.DamageType == DamageClass.SummonMeleeSpeed && Main.myPlayer == Player.whoAmI)
            {
                Projectile SummonMeleeBoom = Projectile.NewProjectileDirect(Projectile.GetSource_None(), target.Bottom, 
                    Vector2.Zero, ProjectileID.DD2ExplosiveTrapT1Explosion, 
                    (int)Player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(AncientDemonArmor.ExplosionBaseDmg), 0, Player.whoAmI, 1);
                SummonMeleeBoom.position -= new Vector2(0, SummonMeleeBoom.height / 2f);
                SummonMeleeBoom.netUpdate = true;
            }
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)/* tModPorter If you don't need the Projectile, consider using OnHitNPC instead */
        {
            Player owner = Main.player[proj.owner];
            if (ProjectileID.Sets.IsAWhip[proj.type])
            {
                CustomCombatText(target.Hitbox, damageDone, CritColorTier, hit.Crit, WhipTipHit(proj, proj.WhipPointsForCollision, target.Hitbox));
                
                if (DemonPower && WhipTipHit(proj, proj.WhipPointsForCollision, target.Hitbox) && Main.myPlayer == Player.whoAmI)
                {
                    Projectile WhipTipBoom = Projectile.NewProjectileDirect(Projectile.GetSource_None(), target.Bottom, 
                        Vector2.Zero, ProjectileID.DD2ExplosiveTrapT1Explosion, 
                        (int)Player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(AncientDemonArmor.ExplosionBaseDmg), 0, Player.whoAmI, 1);
                    WhipTipBoom.position -= new Vector2(0, WhipTipBoom.height / 2f);
                    WhipTipBoom.netUpdate = true;
                }
            }
            else if (!proj.IsMinionOrSentryRelated && proj.DamageType != DamageClass.Default)
            {
                CustomCombatText(target.Hitbox, damageDone, CritColorTier, hit.Crit);
            }
            if (LudensTempest && hit.DamageType == DamageClass.Magic && !owner.HasBuff(ModContent.BuffType<LudensTempestCooldown>()) && !owner.DeadOrGhost)
            {
                int? closest = UsefulFunctions.GetClosestEnemyNPC(target.Center);
                if (closest.HasValue && (Main.npc[closest.Value].type != NPCID.TargetDummy || Main.npc[closest.Value].Distance(target.Center) < 2000))
                {
                    Vector2 velocity = UsefulFunctions.Aim(target.Bottom, Main.npc[closest.Value].Top, 3);
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_None(), target.Center, velocity + new Vector2(-1, -2), ModContent.ProjectileType<LudensTempestFire>(), (int)(hit.SourceDamage * Items.Accessories.Magic.LudensTempest.ProcDmg), 0, Main.myPlayer, 0);
                        Projectile.NewProjectile(Projectile.GetSource_None(), target.Center, velocity + new Vector2(0, -3), ModContent.ProjectileType<LudensTempestFire>(), (int)(hit.SourceDamage * Items.Accessories.Magic.LudensTempest.ProcDmg), 0, Main.myPlayer, 0);
                        Projectile.NewProjectile(Projectile.GetSource_None(), target.Center, velocity + new Vector2(1, -2), ModContent.ProjectileType<LudensTempestFire>(), (int)(hit.SourceDamage * Items.Accessories.Magic.LudensTempest.ProcDmg), 0, Main.myPlayer, 0);
                    }
                    Main.player[proj.owner].AddBuff(ModContent.BuffType<LudensTempestCooldown>(), Items.Accessories.Magic.LudensTempest.Cooldown * 60);
                }
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/LudensTempest") with { Volume = 0.25f }, target.Center);
            }
            else if (LudensTempest && hit.DamageType == DamageClass.Magic && owner.HasBuff(ModContent.BuffType<LudensTempestCooldown>()) && proj.type != ModContent.ProjectileType<LudensTempestFire>() && proj.type != ModContent.ProjectileType<LudensTempestFirelet>())
            {
                UsefulFunctions.AddPlayerBuffDuration(owner, ModContent.BuffType<LudensTempestCooldown>(), -20);
            }
            if (Goredrinker && proj.DamageType == DamageClass.SummonMeleeSpeed && ProjectileID.Sets.IsAWhip[proj.type] && !owner.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && GoredrinkerSwung)
            {
                Player.statLife += (int)MathF.Max(MathF.Min((Player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(Items.Accessories.Summon.Goredrinker.HealBaseValue) * Player.statLifeMax2 / Player.statLife), 20) / (int)((float)GoredrinkerHits * 1.5f + 1), 1);
                Player.HealEffect((int)MathF.Max(MathF.Min((Player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(Items.Accessories.Summon.Goredrinker.HealBaseValue) * Player.statLifeMax2 / Player.statLife), 20) / (int)((float)GoredrinkerHits * 1.5f + 1), 1));
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/GoredrinkerHit") with { Volume = 0.25f }, target.Center);
                GoredrinkerHits++;
            }
            else if (Goredrinker && proj.DamageType == DamageClass.SummonMeleeSpeed && ProjectileID.Sets.IsAWhip[proj.type] && owner.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()))
            {
                int buffIndex = 0;
                foreach (int buffType in owner.buffType)
                {
                    if (buffType == ModContent.BuffType<GoredrinkerCooldown>())
                    {
                        if (Player.buffTime[buffIndex] < 15)
                        {
                            GoredrinkerHits = 0;
                        }
                        Player.buffTime[buffIndex] -= 15;
                    }
                    buffIndex++;
                }
            }

            if (proj.type == ModContent.ProjectileType<Projectiles.Ranged.PiercingPlasma>())
            {
                PiercingGazeCharge++;
                if (PiercingGazeCharge == 16)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113, Player.Center);
                    UsefulFunctions.DustRing(Player.Center, 70, DustID.FireworkFountain_Red, 100, 18);
                }
            }
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            if (UndeadTalisman)
            {
                if (tsorcRevamp.UndeadNPCs.Contains(npc.type))
                {
                    modifiers.FinalDamage.Flat -= Items.Accessories.Defensive.UndeadTalisman.FlatDR;
                }
            }

        }

        public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
        {
            //Todo: All of these accessories should use Player.GetSource_Accessory() as their source
            //They don't because that requires getting the inventory item casuing this effect. I'll do it later if I remember.
            if (Player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge && Main.myPlayer == Player.whoAmI)
            {
                for (int b = 0; b < 5; b++)
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("Bone Revenge"), Player.position, new Vector2(Main.rand.NextFloat(-3f, 3f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), hurtInfo.Damage * 2, 4f, Player.whoAmI, 0, 1);
                }
            }

            if (Player.GetModPlayer<tsorcRevampPlayer>().SoulSickle && Main.myPlayer == Player.whoAmI)
            {
                if (!Main.hardMode)
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("Soul Sickle"), Player.Center, new Vector2(Player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), hurtInfo.SourceDamage * 2, 7f, Player.whoAmI);
                }
                else
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("Soul Sickle"), Player.Center, new Vector2(Player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), hurtInfo.SourceDamage * 4, 9f, Player.whoAmI);
                }
            }
            if (npc.type == NPCID.SkeletronPrime && Main.rand.NextBool(2))
            {
                Player.AddBuff(BuffID.Bleeding, 1800);
                Player.AddBuff(BuffID.OnFire, 600);
            }

            if (Player.HasBuff(ModContent.BuffType<MagicPlating>()))
            {
                MagicPlatingStacks = 0;
            }
        }

        public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo)
        {
            if (Player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge && Main.myPlayer == Player.whoAmI)
            {
                for (int b = 0; b < 5; b++)
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("Bone Revenge"), Player.position, new Vector2(Main.rand.NextFloat(-3f, 3f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), hurtInfo.Damage * 2, 4f, Player.whoAmI, 0, 1);
                }
            }

            if (Player.GetModPlayer<tsorcRevampPlayer>().SoulSickle && Main.myPlayer == Player.whoAmI)
            {
                if (!Main.hardMode)
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("Soul Sickle"), Player.Center, new Vector2(Player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), hurtInfo.SourceDamage * 2, 6f, Player.whoAmI);
                }
                else
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("Soul Sickle"), Player.Center, new Vector2(Player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), hurtInfo.SourceDamage * 4, 8f, Player.whoAmI);
                }
            }
            if (proj.type == ProjectileID.DeathLaser && Main.rand.NextBool(2))
            {
                Player.AddBuff(BuffID.BrokenArmor, 180);
                Player.AddBuff(BuffID.OnFire, 180);
            }

            if (hurtInfo.Damage >= Player.statLife)
            {
                if (proj.type == ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnifeSmall>() && proj.damage > 999)
                {
                    Player.GetModPlayer<tsorcRevampPlayer>().DeathTextOverride = LangUtils.GetTextValue("DeathText.Tonberry");
                }
            }
        }

        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            if (UndeadTalisman)
            {
                if (proj.type == ProjectileID.SkeletonBone || proj.type == ProjectileID.Skull)
                {
                    modifiers.FinalDamage.Flat -= Items.Accessories.Defensive.UndeadTalisman.FlatDR;
                }
            }
        }

        public override void ArmorSetBonusActivated()
        {
            Player player = Main.player[Main.myPlayer];
            if (Shunpo)
            {
                DoShunpo(player);
            }
            
            if (Kraken)
            {
                DoKrakenCast(player);
            }
            
            if (!Player.HasBuff(ModContent.BuffType<WitchkingScreamCooldown>()) && Witch)
            {
                DoWitchScream(player);
            }
        }

        public void ShunpoTooltip(List<TooltipLine> tooltips)
        {
            var ShunpoKeybind = tsorcRevamp.Shunpo.GetAssignedKeys();
            string ShunpoString = ShunpoKeybind.Count > 0 ? ShunpoKeybind[0] : LangUtils.GetTextValue("Keybinds.Shunpo.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            string ArmorSetBonusKeybind = LangUtils.GetTextValue(Main.ReversedUpDownArmorSetBonuses ? "CommonItemTooltip.UpKeybind" : "CommonItemTooltip.DownKeybind") ;
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex1 != -1)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", ArmorSetBonusKeybind 
                    + Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.ShunpoKeybind1") + ShunpoString + LangUtils.GetTextValue("CommonItemTooltip.ShunpoKeybind2")));
            }
        }


        public void DoShunpo(Player player)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                Vector2 MouseHitboxSize = new Vector2(100, 100);

                if (other.active && !tsorcRevamp.UntargetableNPCs.Contains(other.type) && !other.friendly && other.Hitbox.Intersects(Utils.CenteredRectangle(Main.MouseWorld, MouseHitboxSize)) && !player.HasBuff(ModContent.BuffType<ShunpoBlinkCooldown>()))
                {
                    player.immune = true;
                    player.SetImmuneTimeForAllTypes((int)(ShunpoBlink.ShunpoBlinkImmunityTime * 60));
                    ShunpoVelocity = player.DirectionTo(other.Center) * other.Center.Distance(player.Center);
                    player.AddBuff(ModContent.BuffType<ShunpoBlink>(), (int)(ShunpoBlink.ShunpoBlinkImmunityTime * 60 * 2 + 2));
                    player.AddBuff(ModContent.BuffType<ShunpoBlinkCooldown>(), ShunpoBlink.Cooldown * 60);
                    if (Main.rand.NextBool(2))
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Shunpo1") with { Volume = 1f });
                    }
                    else
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Shunpo2") with { Volume = 1f });
                    }
                    ShunpoTimer = 3;
                }
            }
        }
        public void DoKrakenCast(Player player)
        {
            if (Main.myPlayer == player.whoAmI)
            {
                Projectile Tsunami = Projectile.NewProjectileDirect(Projectile.GetSource_None(), Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<KrakenTsunami>(), (int)player.GetTotalDamage(DamageClass.Ranged).ApplyTo(KrakenCarcass.TsunamiBaseDmg), player.GetTotalKnockback(DamageClass.Ranged).ApplyTo(KrakenCarcass.TsunamiBaseKnockback), player.whoAmI);
            }
        }

        public void DoWitchScream(Player player)
        {
                        player.AddBuff(ModContent.BuffType<WitchkingScreamCooldown>(), 20 * 60);
                        
                        if (Main.myPlayer == Player.whoAmI)
                        {
                            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Lotr/WitchkingScream"), Player.Center);

                            Projectile.NewProjectile(
                                Player.GetSource_Misc("WitchScream"),
                                Player.Center,
                                Vector2.Zero,
                                ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(),
                                0,
                                0,
                                Player.whoAmI,
                                550, // ai0
                                20  // ai1
                            );
                            Projectile.NewProjectile(
                                Player.GetSource_Misc("WitchScream"),
                                Player.Center,
                                Vector2.Zero,
                                ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(),
                                0,
                                0,
                                Player.whoAmI,
                                520, // ai0
                                60   // ai1
                            );
                            for (int i = 0; i < 30; i++) 
                            {
                                Vector2 dustSpeed = Main.rand.NextVector2Circular(30f, 30f);
                                int dustIndex = Dust.NewDust(player.Center, 0, 0, 114, dustSpeed.X, dustSpeed.Y, 0, default(Color), 1.9f);
                                Main.dust[dustIndex].noGravity = true; 
                            }
                            for (int i = 0; i < 30; i++) 
                            {
                                Vector2 dustSpeed = Main.rand.NextVector2Circular(30f, 30f);
                                int dustIndex = Dust.NewDust(player.Center, 0, 0, 130, dustSpeed.X, dustSpeed.Y, 0, default(Color), 1.9f);
                                Main.dust[dustIndex].noGravity = true; 
                            }

                            float radius = 30 * 16; 
                            for (int i = 0; i < Main.maxNPCs; i++)
                            {
                                NPC npc = Main.npc[i];
                                if (npc.active && !npc.friendly && npc.Distance(Player.Center) <= radius)
                                {
                                    npc.AddBuff(ModContent.BuffType<WitchkingCurse>(), 6 * 60); // 6 seconds
                                    npc.AddBuff(BuffID.Confused, 4 * 60);
                                    int baseDamage = (int)Player.GetTotalDamage(DamageClass.Summon).ApplyTo(800);
                                    int finalDamage = Main.DamageVar(baseDamage);
                                    
                                    npc.StrikeNPC(new NPC.HitInfo
                                    {
                                        Damage = finalDamage,
                                        Knockback = 0,
                                        HitDirection = 0,
                                        Crit = false,
                                        DamageType = DamageClass.Summon
                                    }, false, false); // noPlayerInteraction = false, dontTriggerSound = false
                                }
                            }
                        }
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            Player player = Main.player[Main.myPlayer];
            Vector2 unitVectorTowardsMouse = player.Center.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * player.direction);

            if (tsorcRevamp.PrintPosition.JustPressed && ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                string text = "Player position: X = " + ((int)Player.position.X / 16).ToString() + ", Y = " + ((int)Player.position.Y / 16).ToString();
                UsefulFunctions.BroadcastText(text);
            }

            if (tsorcRevamp.toggleDragoonBoots.JustPressed)
            {
                DragoonBootsEnable = !DragoonBootsEnable;
            }

            // Guard against the toggle firing while typing into the Storage search bar (default key is 'T').
            if (tsorcRevamp.StorageKey.JustPressed && !Main.blockInput && !Main.drawingPlayerChat)
            {
                ToggleStorage();
            }
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                Vector2 MouseHitboxSize = new Vector2(100, 100);

                if ((tsorcRevamp.Shunpo.JustReleased) && other.active && !tsorcRevamp.UntargetableNPCs.Contains(other.type) && !other.friendly && other.Hitbox.Intersects(Utils.CenteredRectangle(Main.MouseWorld, MouseHitboxSize)) && player.GetModPlayer<tsorcRevampPlayer>().Shunpo && !player.HasBuff(ModContent.BuffType<ShunpoBlinkCooldown>()))
                {
                    player.immune = true;
                    player.SetImmuneTimeForAllTypes((int)(ShunpoBlink.ShunpoBlinkImmunityTime * 60));
                    ShunpoVelocity = player.DirectionTo(other.Center) * other.Center.Distance(player.Center);
                    player.AddBuff(ModContent.BuffType<ShunpoBlink>(), (int)(ShunpoBlink.ShunpoBlinkImmunityTime * 60 * 2 + 2));
                    player.AddBuff(ModContent.BuffType<ShunpoBlinkCooldown>(), ShunpoBlink.Cooldown * 60);
                    if (Main.rand.NextBool(2))
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Shunpo1") with { Volume = 1f });
                    }
                    else
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Shunpo2") with { Volume = 1f });
                    }
                    ShunpoTimer = 3;
                }
            }
            if (tsorcRevamp.reflectionShiftKey.JustPressed)
            {
                if (ReflectionShiftEnabled)
                {
                    if (Player.controlUp)
                    {
                        ReflectionShiftState.Y = -1;
                    }
                    if (Player.controlLeft)
                    {
                        ReflectionShiftState.X = -1;
                    }
                    if (Player.controlRight)
                    {
                        ReflectionShiftState.X = 1;
                    }
                    if (Player.controlDown)
                    {
                        ReflectionShiftState.Y = 1;
                    }
                }
            }
            if (tsorcRevamp.WolfRing.JustReleased)
            {
                if (Player.GetModPlayer<tsorcRevampPlayer>().WolfRing && !Player.HasBuff(ModContent.BuffType<RejuvenationCooldown>()))
                {
                    Player.AddBuff(ModContent.BuffType<Rejuvenation>(), 5 * 60);
                    Player.AddBuff(ModContent.BuffType<RejuvenationCooldown>(), 25 * 60);
                }
            }
            if (tsorcRevamp.WitchScream.JustReleased && !Player.HasBuff(ModContent.BuffType<WitchkingScreamCooldown>()) && Witch)
                {
                    DoWitchScream(player);
                }

            if (tsorcRevamp.KrakensCast.JustReleased && Kraken)
            {
                DoKrakenCast(player);
            }

            if (tsorcRevamp.specialAbility.JustReleased)
            {
                #region Sweeping Blade & Firewall
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC other = Main.npc[i];

                    if (other.active && !tsorcRevamp.UntargetableNPCs.Contains(other.type) && !other.friendly && other.Hitbox.Intersects(Utils.CenteredRectangle(Main.MouseWorld, MouseHitboxSize)) & other.Distance(Player.Center) <= 400 && !other.HasBuff(ModContent.BuffType<PlasmaWhirlwindDashCooldown>()) && player.HeldItem.type == ModContent.ItemType<PlasmaWhirlwind>() && !player.HasBuff(ModContent.BuffType<PlasmaWhirlwindDash>()))
                    {
                        player.immune = true;
                        player.SetImmuneTimeForAllTypes((int)(PlasmaWhirlwind.DashDuration * 60f * 5));
                        SweepingBladeVelocity = player.DirectionTo(other.Center) * 17;
                        player.AddBuff(ModContent.BuffType<PlasmaWhirlwindDash>(), (int)(PlasmaWhirlwind.DashDuration * 60f * 2));
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/Dash") with { Volume = 1f });
                        if (Main.myPlayer == player.whoAmI)
                        {
                            Projectile DashHitbox = Projectile.NewProjectileDirect(Projectile.GetSource_None(), player.Center, Vector2.Zero, ModContent.ProjectileType<PlasmaWhirlwindDashHitbox>(), player.HeldItem.damage, 0, player.whoAmI);
                        }
                    } //cooldown is added by On-Hit in the dash projectile hitbox
                    if (!(Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) && other.active && !tsorcRevamp.UntargetableNPCs.Contains(other.type) && !other.friendly && other.Hitbox.Intersects(Utils.CenteredRectangle(Main.MouseWorld, MouseHitboxSize)) & other.Distance(Player.Center) <= 400 && !other.HasBuff(ModContent.BuffType<NightbringerDashCooldown>()) && player.HeldItem.type == ModContent.ItemType<Nightbringer>() && !player.HasBuff(ModContent.BuffType<NightbringerDash>()))
                    {
                        player.immune = true;
                        SweepingBladeVelocity = player.DirectionTo(other.Center) * 17;
                        player.SetImmuneTimeForAllTypes((int)(PlasmaWhirlwind.DashDuration * 60f * 5));
                        player.AddBuff(ModContent.BuffType<NightbringerDash>(), (int)(PlasmaWhirlwind.DashDuration * 60f * 2));
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/Dash") with { Volume = 1f });
                        if (Main.myPlayer == player.whoAmI)
                        {
                            Projectile DashHitbox = Projectile.NewProjectileDirect(Projectile.GetSource_None(), player.Center, Vector2.Zero, ModContent.ProjectileType<NightbringerDashHitbox>(), player.HeldItem.damage, 0, player.whoAmI);
                        }
                    } //cooldown is added by On-Hit in the dash projectile hitbox
                }
                if ((Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) && Player.HeldItem.type == ModContent.ItemType<Nightbringer>() && !Player.HasBuff(ModContent.BuffType<NightbringerFirewallCooldown>()))
                {
                    if (Main.myPlayer == player.whoAmI)
                    {
                        Projectile Firewall = Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), player.Center, unitVectorTowardsMouse * 5f, ModContent.ProjectileType<NightbringerFirewall>(), player.HeldItem.damage / 3, 0, Main.myPlayer);
                    }
                    Player.AddBuff(ModContent.BuffType<NightbringerFirewallCooldown>(), 30 * 60);
                }
                #endregion

                #region Scouts Boost & Nuclear Mushrooms
                if (!player.HasBuff(ModContent.BuffType<ScoutsBoost2Cooldown>()) && (Player.HeldItem.type == ModContent.ItemType<ToxicShot>() | Player.HeldItem.type == ModContent.ItemType<AlienGun>()))
                {
                    player.AddBuff(ModContent.BuffType<ScoutsBoost2>(), ToxicShot.ScoutsBoost2Duration * 60);
                    player.AddBuff(ModContent.BuffType<ScoutsBoost2Cooldown>(), ToxicShot.ScoutsBoost2Cooldown * 60);
                }
                if (!(Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) && !player.HasBuff(ModContent.BuffType<ScoutsBoost2CooldownOmega>()) && Player.HeldItem.type == ModContent.ItemType<OmegaSquadRifle>())
                {
                    player.AddBuff(ModContent.BuffType<ScoutsBoost2Omega>(), ToxicShot.ScoutsBoost2Duration * 60);
                    player.AddBuff(ModContent.BuffType<ScoutsBoost2CooldownOmega>(), ToxicShot.ScoutsBoost2Cooldown * 60);
                }
                if ((Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) && !Player.HasBuff(ModContent.BuffType<NuclearMushroomCooldown>()) && Player.HeldItem.type == ModContent.ItemType<OmegaSquadRifle>() && player.statMana >= (int)(OmegaSquadRifle.BaseShroomManaCost * player.manaCost))
                {
                    if (Main.myPlayer == player.whoAmI)
                    {
                        Projectile Shroom = Projectile.NewProjectileDirect(Projectile.GetSource_None(), Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<Projectiles.Ranged.Runeterra.NuclearMushroom>(), player.GetWeaponDamage(player.HeldItem), player.GetWeaponKnockback(player.HeldItem), Main.myPlayer);
                    }
                    Player.AddBuff(ModContent.BuffType<NuclearMushroomCooldown>(), OmegaSquadRifle.ShroomCooldown * 60);
                }
                #endregion

                #region Spirit Rush
                if (player.HeldItem.type == ModContent.ItemType<OrbOfSpirituality>() && player.statMana >= (player.GetManaCost(player.HeldItem) * OrbOfSpirituality.DashCostMultiplier) && !player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDashCooldown>()))
                {
                    player.AddBuff(ModContent.BuffType<OrbOfSpiritualityDash>(), OrbOfSpirituality.DashBuffDuration * 60);
                    player.statMana -= player.GetManaCost(player.HeldItem) * OrbOfSpirituality.DashCostMultiplier;
                }
                if (player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDash>()) && SpiritRushCooldown <= 0f && SpiritRushCharges > 0)
                {
                    player.immune = true;
                    SpiritRushVelocity = player.DirectionTo(Main.MouseWorld) * 25f;
                    SpiritRushTimer = 0.3f;
                    SpiritRushCooldown = 1f;
                    player.SetImmuneTimeForAllTypes(60);
                    if (SpiritRushSoundStyle == 0)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/Dash1") with { Volume = RuneterraOrb.OrbSoundVolume });
                        SpiritRushSoundStyle += 1;
                    }
                    else
                    if (SpiritRushSoundStyle == 1)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/Dash2") with { Volume = RuneterraOrb.OrbSoundVolume });
                        SpiritRushSoundStyle += 1;
                    }
                    else
                    if (SpiritRushSoundStyle == 2)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/Dash3") with { Volume = RuneterraOrb.OrbSoundVolume });
                        SpiritRushSoundStyle = 0;
                    }
                    SpiritRushCharges--;
                }
                #endregion

                #region Turboboost
                bool holdingControlsAndOrSummonWeapon = (player.HasItem(ModContent.ItemType<InterstellarVesselGauntlet>()) || player.HasItem(ModContent.ItemType<CenterOfTheUniverse>())) 
                                                        && (player.HeldItem.DamageType == DamageClass.SummonMeleeSpeed || player.HeldItem.DamageType == DamageClass.Summon);
                bool hasBuff = player.HasBuff(ModContent.BuffType<InterstellarCommander>()) || Player.HasBuff(ModContent.BuffType<CenterOfTheUniverseBuff>());
                bool hasBoost = player.HasBuff(ModContent.BuffType<Turboboost>())
                                          || player.HasBuff(ModContent.BuffType<TurboboostUniversal>());
                bool hasCooldown = player.HasBuff(ModContent.BuffType<TurboboostCooldown>())
                                   || player.HasBuff(ModContent.BuffType<TurboboostUniversalCooldown>());
                
                if (holdingControlsAndOrSummonWeapon && hasBuff && !(Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) 
                    && !hasBoost && !hasCooldown && TurboboostControlsCooldown < 0)
                {
                    bool hasCenterOfTheUniverse = player.HasBuff(ModContent.BuffType<CenterOfTheUniverseBuff>());
                    
                    if (!hasCenterOfTheUniverse)
                    {
                        player.AddBuff(ModContent.BuffType<Turboboost>(), RuneterraGauntlets.BoostDuration * 60);
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/InterstellarVessel/BoostActivation") with { Volume = InterstellarVesselGauntlet.SoundVolume });
                    }

                    if (hasCenterOfTheUniverse)
                    {
                        player.AddBuff(ModContent.BuffType<TurboboostUniversal>(), RuneterraGauntlets.BoostDuration * 60);
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/CenterOfTheUniverse/BoostActivation") with { Volume = CenterOfTheUniverse.SoundVolume });
                    }
                }
                else if (holdingControlsAndOrSummonWeapon && hasBuff 
                         && !(Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) && hasBoost)
                {
                    bool hasCenterOfTheUniverse = player.HasBuff(ModContent.BuffType<CenterOfTheUniverseBuff>());
                    
                    if (!hasCenterOfTheUniverse)
                    {
                        int buffIndex = player.FindBuffIndex(ModContent.BuffType<Turboboost>());
                        Buffs.Runeterra.Summon.Turboboost.OnRemoval(buffIndex, player);
                        player.DelBuff(buffIndex);
                    }

                    if (hasCenterOfTheUniverse)
                    {
                        int buffIndex = player.FindBuffIndex(ModContent.BuffType<TurboboostUniversal>());
                        Buffs.Runeterra.Summon.TurboboostUniversal.OnRemoval(buffIndex, player);
                        player.DelBuff(buffIndex);
                    }
                }
                #endregion

            }



            if (tsorcRevamp.specialAbility.Current && (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) 
                                                   && (Player.HeldItem.type == ModContent.ItemType<ScorchingPoint>() 
                                                       || Player.HeldItem.type == ModContent.ItemType<InterstellarVesselGauntlet>() 
                                                       || Player.HeldItem.type == ModContent.ItemType<CenterOfTheUniverse>()))
            {
                if (player.direction == 1)
                {
                    MinionCircleRadius -= 1.5f;
                    if (MinionCircleRadius < MinimumMinionCircleRadius)
                    {
                        MinionCircleRadius = MinimumMinionCircleRadius;
                    }
                }
                else
                {
                    MinionCircleRadius += 1.5f;
                    if (MinionCircleRadius > MaximumMinionCircleRadius)
                    {
                        MinionCircleRadius = MaximumMinionCircleRadius;
                    }
                }
                Dust.NewDustDirect(Player.Center, 10, 10, DustID.FlameBurst, 0.5f, 0.5f, 0, Color.Firebrick, 0.5f);
                TurboboostControlsCooldown = 61; //so you don't accidentally activate Interstellar Boost when you jsut wanted to adjust the circle radius
            }
        }


        //On hit, subtract the mana cost and disable natural mana regen for a short period
        //The latter is absolutely necessary, because natural mana regen scales with your base mana
        //Even as melee there are mana boosting accessories you can stack, as well as armor like Dragoon that makes mana regen obscenely powerful.
        //This means you can tank until your mana bar is exhausted, then have to back off for a bit and actually dodge
        public override void OnHurt(Player.HurtInfo info)
        {
            if (Player.HasBuff(ModContent.BuffType<ArtoriasAbysswalkerPoise>()))
            {
                Player.ClearBuff(ModContent.BuffType<ArtoriasAbysswalkerPoise>());
                Player.AddBuff(ModContent.BuffType<ArtoriasAbysswalkerPoiseCooldown>(), ArtoriasAbysswalkerPoiseCooldown);
                ArtoriasAbysswalkerCounterType = 0;
            }
            if (manaShield == 1)
            {
                if (Player.statMana >= Items.Accessories.Defensive.Shields.ManaShield.manaCost)
                {
                    SpendManaOnHit(Items.Accessories.Defensive.Shields.ManaShield.manaCost); // also applies the Unkindled mana-regen delay
                    Player.manaRegenDelay = Items.Accessories.Defensive.Shields.ManaShield.regenDelay * 60;
                    Player.maxRegenDelay = Items.Accessories.Defensive.Shields.ManaShield.regenDelay * 60;
                }
            }
            if (manaShield == 2)
            {
                if (Player.statMana >= Items.Accessories.Defensive.Celestriad.manaCost)
                {
                    SpendManaOnHit(Items.Accessories.Defensive.Celestriad.manaCost);
                    Player.manaRegenDelay = Items.Accessories.Defensive.Celestriad.regenDelay * 60;
                    Player.maxRegenDelay = Items.Accessories.Defensive.Celestriad.regenDelay * 60;
                }
            }
            // stamina shield code
            if (staminaShield == 1)
            {
                if (Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 75)
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= Items.Accessories.Defensive.Shields.DragonCrestShield.staminaCost;
                    //return;
                }
            }
            //Trinity Accessory
            if (Trinity)
            {
                Player.AddBuff(BuffID.RapidHealing, 120);
                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && !npc.friendly && npc.Distance(Player.Center) <= 320f) 
                    {
                        npc.AddBuff(BuffID.Venom, 240);

                        if (Main.rand.NextFloat() < 0.33f)
                        {
                            npc.AddBuff(BuffID.Frozen, 90);
                        }
                    }
                }
            }

            if (HasSporePowder) 
            {
                Vector2 center = Player.Center;
                float radius = 120f; 

                for (int i = 0; i < 190; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                    int dust = Dust.NewDust(center + offset, 1, 1, 44, 0f, 0f, 100, default, 1.4f); 
                    Main.dust[dust].velocity = offset.SafeNormalize(Vector2.Zero) * 1f;
                    Main.dust[dust].noGravity = false;
                }

                for (int n = 0; n < Main.maxNPCs; n++)
                {
                    NPC npc = Main.npc[n];
                    if (npc.active && !npc.friendly && !npc.dontTakeDamage)
                    {
                        if (Vector2.Distance(npc.Center, center) <= radius)
                        {
                            int baseDamage = (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(60);
                            int finalDamage = Main.DamageVar(baseDamage);
                            npc.StrikeNPC(new NPC.HitInfo
                            {
                                Damage = finalDamage,
                                Knockback = 2f,
                                HitDirection = 0,
                                Crit = false,
                                DamageType = DamageClass.Generic
                            }, false, false); // noPlayerInteraction = false, dontTriggerSound = false

                            npc.AddBuff(BuffID.Poisoned, 240); // 4 seconds
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.8f }, Player.Center);
                        }
                    }
                }
            }

            /*if (HasVenomPowder) 
            {
                Vector2 center = Player.Center;
                float radius = 150f; 

                for (int i = 0; i < 280; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                    int dust = Dust.NewDust(center + offset, 1, 1, 171, 0f, 0f, 100, default, 1.6f);
                    Main.dust[dust].velocity = offset.SafeNormalize(Vector2.Zero) * 1.5f;
                    Main.dust[dust].noGravity = true;
                }

                for (int n = 0; n < Main.maxNPCs; n++)
                {
                    NPC npc = Main.npc[n];
                    if (npc.active && !npc.friendly && !npc.dontTakeDamage)
                    {
                        if (Vector2.Distance(npc.Center, center) <= radius)
                        {
                            int baseDamage = (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(90);
                            int finalDamage = Main.DamageVar(baseDamage);
                            npc.StrikeNPC(new NPC.HitInfo
                            {
                                Damage = finalDamage,
                                Knockback = 4f,
                                HitDirection = 0,
                                Crit = false,
                                DamageType = DamageClass.Generic
                            }, false, false); // noPlayerInteraction = false, dontTriggerSound = false

                            npc.AddBuff(BuffID.Venom, 240); // 4 seconds
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.9f }, Player.Center);
                        }
                    }
                }
            }*/
        }

        //Reduces the mana restored from potions and such to zero
        public override bool CanConsumeAmmo(Item weapon, Item ammo)
        {
            if (normansRingAmmoSave && Main.rand.NextBool(20))
            {
                return false;
            }

            return base.CanConsumeAmmo(weapon, ammo);
        }


        public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
        {
            base.ModifyMaxStats(out health, out mana);

            StartingClass resolvedStartingClass = GetResolvedStartingClass();
            switch (resolvedStartingClass)
            {
                case StartingClass.Melee:
                    health.Base += 20;
                    mana.Base -= 10;
                    break;
                case StartingClass.Ranged:
                    health.Base -= 10;
                    break;
                case StartingClass.Magic:
                    health.Base -= 20;
                    mana.Base += 30;
                    break;
                case StartingClass.Summoner:
                    mana.Base += 10;
                    break;
            }

            // Capture the counters BEFORE reconciling. PlayerLoader.ModifyMaxStats calls ResetMaxStatsToVanilla
            // immediately before this hook, so statLifeMax was already rebuilt from *these* values — subtracting
            // anything else below would leave the cancellation off by 20 per crystal for a frame, and since
            // statLife is clamped down to the new maximum on that same frame, that would be real damage rather
            // than a flicker.
            int consumedLifeCrystals = Player.ConsumedLifeCrystals;
            int consumedManaCrystals = Player.ConsumedManaCrystals;

            ReconcileCrystalState(consumedLifeCrystals, consumedManaCrystals);
            NormalizeCrystalCounters();

            // The cancellation. ResetMaxStatsToVanilla rebuilds statLifeMax as
            // `100 + 20 * ConsumedLifeCrystals + 5 * ConsumedLifeFruit` every frame; subtracting the counter's
            // contribution and adding our own valuation cancels the 20-per-crystal term outright, leaving
            //     statLifeMax = 100 + classOffset + EffectiveLifeGrant + 5 * ConsumedLifeFruit
            // no matter what vanilla currently believes the crystal count to be. That last part is the point: a
            // load can legitimately re-decompose the saved number into a different (crystals, fruit) split than
            // the one we wrote, and this subtraction absorbs the difference instead of drifting.
            health.Base += EffectiveLifeGrant - consumedLifeCrystals * 20;
            mana.Base += EffectiveManaGrant - consumedManaCrystals * 20;
        }

        internal int SoulsLifeGrantCap => SoulsModeMaxLife - GetStartingClassBaseLife();
        internal int SoulsManaGrantCap => SoulsModeMaxMana - GetStartingClassBaseMana();

        /// The same crystals, valued by the rules of whichever mode you are currently in: SoulsMode pays the
        /// reduced per-crystal gain that was banked at the moment each one was eaten, Classic pays vanilla's
        /// flat +20. Both are clamped to the same class ceiling (400 life / 200 mana), so Classic doesn't blow
        /// past it — it just gets there on far fewer crystals.
        ///
        /// This is why lifeCrystalsEaten has to exist alongside soulsLifeGranted. Storing only the granted total
        /// left Classic with nothing to re-value: it fell back to vanilla's ConsumedLifeCrystals counter, which
        /// saturates at 15 and so under-reports anyone who ate more than that under SoulsMode's cheaper rate.
        internal int EffectiveLifeGrant => SoulsMode
            ? Math.Min(SoulsLifeGrantCap, soulsLifeGranted)
            : Math.Min(SoulsLifeGrantCap, lifeCrystalsEaten * 20);

        internal int EffectiveManaGrant => SoulsMode
            ? Math.Min(SoulsManaGrantCap, soulsManaGranted)
            : Math.Min(SoulsManaGrantCap, manaCrystalsEaten * 20);

        internal bool LifeCrystalsMaxed => EffectiveLifeGrant >= SoulsLifeGrantCap;
        internal bool ManaCrystalsMaxed => EffectiveManaGrant >= SoulsManaGrantCap;

        /// Repairs our totals from vanilla's counters when those know about crystals we don't — a character that
        /// predates this system, or one that ate crystals through the vanilla path. Guarded on "not already
        /// maxed" because NormalizeCrystalCounters deliberately parks the life counter *above* the eaten count
        /// once maxed (see below), which would otherwise look like untracked crystals and ratchet forever.
        private void ReconcileCrystalState(int consumedLifeCrystals, int consumedManaCrystals)
        {
            if (!LifeCrystalsMaxed && consumedLifeCrystals > lifeCrystalsEaten)
            {
                // Valued at the SoulsMode rate, same as if they'd been eaten under this system — a crystal is
                // worth what a crystal is worth, regardless of which mode it was swallowed in.
                int missing = consumedLifeCrystals - lifeCrystalsEaten;
                lifeCrystalsEaten = consumedLifeCrystals;
                soulsLifeGranted = Math.Min(SoulsLifeGrantCap, soulsLifeGranted + missing * 10);
            }

            if (!ManaCrystalsMaxed && consumedManaCrystals > manaCrystalsEaten)
            {
                int missing = consumedManaCrystals - manaCrystalsEaten;
                manaCrystalsEaten = consumedManaCrystals;
                soulsManaGranted = Math.Min(SoulsManaGrantCap, soulsManaGranted + missing * SoulsModeManaCrystalGain);
            }

            // Deliberately NOT clamping the stored totals to the cap here. The cap depends on the starting class,
            // and GetResolvedStartingClass falls back to inferring from inventory — if that ever came back None
            // for a frame, a blanket clamp would permanently truncate a Magic character's 320 down to 300. The
            // caps are applied non-destructively where the totals are read, in EffectiveLifeGrant.
        }

        /// Push as much of the current mode's valuation into the vanilla counters as those counters can legally
        /// hold, so the progress survives Player.Serialize. Anything that doesn't fit is re-applied by the
        /// cancellation in ModifyMaxStats.
        ///
        /// The forced 15 once Life Crystals are maxed is load-bearing, not cosmetic. A maxed Melee is granted
        /// only 280 HP, which fills just 14 counters, so its saved number would be 100 + 280 = 380 — and
        /// Deserialize can only recover a Life Fruit count from a saved number *above* 400
        /// (`ConsumedLifeFruit = (statLifeMax - 400) / 5`). Every fruit a Melee ate would therefore evaporate
        /// 5 HP at a time on each reload. Parking the counter at 15 puts the saved number at 400 + 5 * fruit,
        /// where fruit round-trips exactly; the resulting negative remainder (280 - 300 = -20) keeps the total
        /// at the intended 400. It also means vanilla's own `ConsumedLifeCrystals == 15` gate unlocks Life
        /// Fruit at the right moment for every class, with no patch needed.
        internal void NormalizeCrystalCounters()
        {
            Player.ConsumedLifeCrystals = LifeCrystalsMaxed
                ? Terraria.Player.LifeCrystalMax
                : Math.Min(Terraria.Player.LifeCrystalMax, EffectiveLifeGrant / 20);

            Player.ConsumedManaCrystals = Math.Min(Terraria.Player.ManaCrystalMax, EffectiveManaGrant / 20);
        }

        /// Called from the ItemCheck_UseLifeCrystal detour in MethodSwaps, which has already checked
        /// LifeCrystalsMaxed. Runs in both modes — the mod owns crystal consumption outright now, because
        /// Classic has to keep lifeCrystalsEaten up to date too.
        internal void GrantLifeCrystal()
        {
            int before = EffectiveLifeGrant;

            lifeCrystalsEaten++;
            soulsLifeGranted = Math.Min(SoulsLifeGrantCap, soulsLifeGranted + GetSoulsModeLifeCrystalGain());

            // The visible gain is whatever the current mode's valuation actually moved by: +10 in SoulsMode
            // (party size permitting), +20 in Classic, or just the remainder on the crystal that reaches the
            // ceiling. UseHealthMaxIncreasingItem bumps statLife and fires the heal popup with that real number,
            // so the "+10" the player sees is genuine rather than a "+20" rewritten after the fact (which only
            // ever worked on the local client). Its statLifeMax write is transient — ModifyMaxStats rebuilds
            // that from scratch next frame.
            int gain = EffectiveLifeGrant - before;
            NormalizeCrystalCounters();
            if (gain > 0)
            {
                Player.UseHealthMaxIncreasingItem(gain);
            }
        }

        internal void GrantManaCrystal()
        {
            int before = EffectiveManaGrant;

            manaCrystalsEaten++;
            soulsManaGranted = Math.Min(SoulsManaGrantCap, soulsManaGranted + SoulsModeManaCrystalGain);

            int gain = EffectiveManaGrant - before;
            NormalizeCrystalCounters();
            if (gain > 0)
            {
                Player.UseManaMaxIncreasingItem(gain);
            }
        }

        public override void GetHealMana(Item item, bool quickHeal, ref int healValue)
        {
            if (manaShield >= 1)
            {
                healValue = 0;
            }
        }

        internal void ApplyStartingClassStats(bool force = false, bool clearPending = true)
        {
            if (!force && appliedStartingClassStatsVersion >= StartingClassStatsVersion)
            {
                return;
            }

            GetResolvedStartingClass();

            if (startingClass == StartingClass.None)
            {
                return;
            }

            int maxLife = GetStartingClassBaseLife();
            int maxMana = GetStartingClassBaseMana();
            float maxStamina = GetStartingClassBaseStamina();

            Player.statLifeMax2 = maxLife;
            Player.statLife = maxLife;
            Player.statManaMax2 = maxMana;
            Player.statMana = maxMana;

            tsorcRevampStaminaPlayer staminaPlayer = Player.GetModPlayer<tsorcRevampStaminaPlayer>();
            staminaPlayer.staminaResourceMax = maxStamina;
            staminaPlayer.staminaResourceCurrent = maxStamina;

            appliedStartingClassStats = true;
            appliedStartingClassStatsVersion = StartingClassStatsVersion;
            if (clearPending)
            {
                tsorcRevamp.PendingStartingClass = StartingClass.None;
            }
        }

        private StartingClass GetResolvedStartingClass()
        {
            if (startingClass != StartingClass.None)
            {
                return startingClass;
            }

            startingClass = ResolveStartingClassForStats();
            return startingClass;
        }

        private StartingClass ResolveStartingClassForStats()
        {
            if (tsorcRevamp.PendingStartingClass != StartingClass.None)
            {
                return tsorcRevamp.PendingStartingClass;
            }

            return InferStartingClassFromInventory();
        }

        private StartingClass InferStartingClassFromInventory()
        {
            for (int i = 0; i < Player.inventory.Length; i++)
            {
                int itemType = Player.inventory[i]?.type ?? ItemID.None;
                if (itemType == ModContent.ItemType<ApprenticesWand>() || itemType == ItemID.BorealWoodSword)
                {
                    return StartingClass.Magic;
                }
                if (itemType == ModContent.ItemType<RustedChain>() || itemType == ItemID.BabyBirdStaff)
                {
                    return StartingClass.Summoner;
                }
                if (itemType == ModContent.ItemType<Items.Weapons.Ranged.Crossbows.Crossbow>() || itemType == ItemID.IronShortsword)
                {
                    return StartingClass.Ranged;
                }
                if (itemType == ModContent.ItemType<ForgottenRuneAxe>() || itemType == ItemID.WoodenBoomerang)
                {
                    return StartingClass.Melee;
                }
            }

            return StartingClass.None;
        }
        // Class starting stamina. Stamina needs no crystal-style bookkeeping — staminaResourceMax is written
        // straight to our own tag data and never passes through vanilla's save path — but it follows the same
        // shape: every class converges on StaminaVessel.PermanentStaminaCap (200), and the starting value only
        // decides how many vessels it takes to get there (Melee 14, Ranged 15, Summoner 16, Magic 17).
        internal float GetStartingClassBaseStamina() => GetBaseStaminaForClass(GetResolvedStartingClass());

        internal int GetStartingClassBaseLife() => GetBaseLifeForClass(GetResolvedStartingClass());

        internal int GetStartingClassBaseMana() => GetBaseManaForClass(GetResolvedStartingClass());

        // Static, class-keyed versions of the above so UI (e.g. the starting-class selection tooltip) can read
        // a class's base stats without needing a resolved player instance. Deprived deliberately has no case in
        // any of these three switches — it falls to the shared "_" default (Life 100 / Mana 20 / Stamina 125),
        // which is also what an unresolved/None class gets. That's intentional: Deprived's whole identity is
        // "no specialization," so it sits on the same unbiased baseline the game already uses as its default,
        // rather than needing invented numbers of its own.
        internal static float GetBaseStaminaForClass(StartingClass startingClass)
        {
            return startingClass switch
            {
                StartingClass.Melee => 130,
                StartingClass.Magic => 115,
                StartingClass.Summoner => 120,
                _ => tsorcRevampStaminaPlayer.DefaultStaminaResourceMax
            };
        }

        internal static int GetBaseLifeForClass(StartingClass startingClass)
        {
            return startingClass switch
            {
                StartingClass.Melee => 120,
                StartingClass.Ranged => 90,
                StartingClass.Magic => 80,
                _ => 100
            };
        }

        internal static int GetBaseManaForClass(StartingClass startingClass)
        {
            return startingClass switch
            {
                StartingClass.Melee => 10,
                StartingClass.Ranged => 20,
                StartingClass.Magic => 50,
                StartingClass.Summoner => 30,
                _ => 20
            };
        }

        private void MoveStartingPickaxeToFirstSlot()
        {
            if (Player.inventory[0] == null || !Player.inventory[0].IsAir)
            {
                return;
            }

            for (int i = 1; i < 10; i++)
            {
                Item item = Player.inventory[i];
                if (item != null && !item.IsAir && item.pick > 0)
                {
                    Player.inventory[0] = item.Clone();
                    item.TurnToAir();
                    return;
                }
            }
        }

        public override void OnEnterWorld()
        {
            if (!gotDarksign)
            { // Fresh character: start in Unkindled mode and strip junk starter items.
                gotDarksign = true;
                if (!BearerOfTheCurse) Unkindled = true;

                // Everyone starts with just the vanilla Copper Pickaxe - no bonus Copper Axe, and no Copper
                // Shortsword unless you're Deprived, whose whole starting weapon IS the copper shortsword.
                // A better pickaxe (Diamond) is earned later as an Emerald Herald gift instead of handed out here.
                for (int i = 0; i < Player.inventory.Length; i++)
                {
                    bool isJunkAxe = Player.inventory[i].type == ItemID.CopperAxe;
                    bool isJunkSword = Player.inventory[i].type == ItemID.CopperShortsword && startingClass != StartingClass.Deprived;
                    if (isJunkAxe || isJunkSword)
                        Player.inventory[i].TurnToAir();
                }

                MoveStartingPickaxeToFirstSlot();
            }
        }



        public override void OnRespawn()
        {
            unkindledManaDelayTimer = 0;
            Player.statLife = Player.statLifeMax2;

            // Restore non-permanent completed events (SaveOnCompletion=false) so their spawn rings reappear.
            // They were parked in DisabledEvents on completion instead of the 5-second QueuedEvents timer.
            if (Player.whoAmI == Main.myPlayer && tsorcScriptedEvents.DisabledEvents != null && tsorcScriptedEvents.QueuedEvents != null)
            {
                foreach (var ev in tsorcScriptedEvents.DisabledEvents)
                    tsorcScriptedEvents.QueuedEvents.Add(ev);
                tsorcScriptedEvents.DisabledEvents.Clear();
            }
            if (BearerOfTheCurse) Player.AddBuff(ModContent.BuffType<Hollowed>(), 2);
            Player.AddBuff(ModContent.BuffType<Invincible>(), 360);

            tsorcRevampPlayer modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();

            if (modPlayer.HadBuffAmmoBox)
            {
                Player.AddBuff(BuffID.AmmoBox, 1);
            }
            if (modPlayer.HadBuffBewitched)
            {
                Player.AddBuff(BuffID.Bewitched, 1);
            }
            if (modPlayer.HadBuffClairvoyance)
            {
                Player.AddBuff(BuffID.Clairvoyance, 1);
            }
            if (modPlayer.HadBuffSharpened) 
            {
                Player.AddBuff(BuffID.Sharpened, 1);
            }
            if (modPlayer.HadBuffStrategist)
            {
                Player.AddBuff(BuffID.WarTable, 1);
            }
        }

        public override void OnConsumeMana(Item item, int manaConsumed)
        {
            if (Unkindled && manaConsumed > 0)
            {
                unkindledManaDelayTimer = 3600; // 60 seconds (3600 ticks at 60fps)
            }
        }

        /// <summary>
        /// Spend mana from a source that ISN'T a normal item use (shields/wards blocking, on-hit drains, etc.).
        /// Vanilla's OnConsumeMana — which triggers Unkindled's 30s mana-regen delay — only fires for item use,
        /// so anything subtracting statMana directly must call this to apply the same Unkindled penalty.
        /// </summary>
        public void SpendManaOnHit(int amount)
        {
            if (amount <= 0)
            {
                return;
            }
            Player.statMana = System.Math.Max(0, Player.statMana - amount);
            if (Unkindled)
            {
                unkindledManaDelayTimer = 3600;
            }
        }

        public static float CheckReduceDefense(Vector2 Position, int Width, int Height, bool fireWalk)
        {
            int playerTileXLeft = (int)(Position.X / 16f) - 1;
            int playerTileXRight = (int)((Position.X + Width) / 16f) + 2;
            int playerTileYBottom = (int)(Position.Y / 16f) - 1;
            int playerTileYTop = (int)((Position.Y + Height) / 16f) + 2;

            #region sanity
            if (playerTileXLeft < 0)
            {
                playerTileXLeft = 0;
            }
            if (playerTileXRight > Main.maxTilesX)
            {
                playerTileXRight = Main.maxTilesX;
            }
            if (playerTileYBottom < 0)
            {
                playerTileYBottom = 0;
            }
            if (playerTileYTop > Main.maxTilesY)
            {
                playerTileYTop = Main.maxTilesY;
            }
            #endregion

            for (int i = playerTileXLeft; i < playerTileXRight; i++)
            {
                for (int j = playerTileYBottom; j < playerTileYTop; j++)
                {
                    if (Main.tile[i, j] != null && Main.tile[i, j].HasTile)
                    {
                        Vector2 TilePos;
                        TilePos.X = i * 16;
                        TilePos.Y = j * 16;

                        int type = Main.tile[i, j].TileType;

                        if (DamageDir.ContainsKey(type) && !(fireWalk && type == 76))
                        {
                            float a = DamageDir[type];
                            float z = 0.5f;
                            if (Position.X + Width > TilePos.X - z &&
                                Position.X < TilePos.X + 16f + z &&
                                Position.Y + Height > TilePos.Y - z &&
                                Position.Y < TilePos.Y + 16f + z)
                            {
                                return a;
                            }
                        }
                    }
                }
            }
            return 0;
        }

        public static float CheckSoulsMultiplier(Player player)
        {
            float multiplier = 1f;
            if (player.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing)
            {
                multiplier += CovetousSilverSerpentRing.SoulAmplifier / 100f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SoulSerpentRing)
            {
                multiplier += CovetousSoulSerpentRing.SoulAmplifier / 100f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SoulSiphon)
            {
                multiplier += SoulSiphonPotion.SoulAmplifier / 100f * player.GetModPlayer<tsorcRevampPlayer>().SoulSiphonScaling;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SOADrain)
            {
                multiplier += SymbolOfAvarice.SoulAmplifier / 100f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().VOEGDrain)
            {
                multiplier += VaultOfEndlessGreed.SoulAmplifier / 100f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                multiplier += Darksign.BotCSoulDropAmplifier / 100f;
            }
            return multiplier;
        }

        public void DoPortableChest<T>(ref int whoAmI, ref bool toggle) where T : BonfireProjectiles, new()
        {
            int projectileType = ModContent.ProjectileType<T>();
            T instance = ModContent.GetInstance<T>();
            int bankID = instance.ChestType;
            SoundStyle useSound = instance.UseSound;

            if (Main.projectile[whoAmI].active && Main.projectile[whoAmI].type == projectileType)
            {
                int oldChest = Player.chest;
                Player.chest = bankID;
                toggle = true;

                int num17 = (int)((Player.position.X + Player.width * 0.5) / 16.0);
                int num18 = (int)((Player.position.Y + Player.height * 0.5) / 16.0);
                Player.chestX = (int)Main.projectile[whoAmI].Center.X / 16;
                Player.chestY = (int)Main.projectile[whoAmI].Center.Y / 16;
                if ((oldChest != bankID && oldChest != -1) || num17 < Player.chestX - Player.tileRangeX || num17 > Player.chestX + Player.tileRangeX + 1 || num18 < Player.chestY - Player.tileRangeY || num18 > Player.chestY + Player.tileRangeY + 1)
                {
                    whoAmI = -1;
                    if (Player.chest != -1)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(useSound);
                    }

                    if (oldChest != bankID)
                        Player.chest = oldChest;
                    else
                        Player.chest = -1;

                    Recipe.FindRecipes();
                }
            }
            else
            {


                whoAmI = -1;
                Player.chest = -1; //none
                Recipe.FindRecipes();
            }
        }

        internal void SendSingleItemPacket(int message, Item item, int toWho, int fromWho)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)message);
            packet.Write((byte)Player.whoAmI);
            ItemIO.Send(item, packet);
            packet.Send(toWho, fromWho);
        }
    }
}
