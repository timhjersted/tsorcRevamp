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
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using TerraUI.Objects;
using tsorcRevamp.Buffs;
using tsorcRevamp.Items;
using tsorcRevamp.Projectiles.Pets;
using tsorcRevamp.UI;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Buffs.Runeterra.Ranged;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Buffs.Runeterra.Magic;
using Terraria.Localization;
using tsorcRevamp.Items.Accessories.Summon;
using tsorcRevamp.Items.Accessories.Expert;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Tools;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Armors.Summon;
using tsorcRevamp.Buffs.Summon.WhipDebuffs;
using tsorcRevamp.Buffs.Armor;
using tsorcRevamp.Projectiles.Melee.Runeterra;
using tsorcRevamp.Utilities;

namespace tsorcRevamp
{
    public partial class tsorcRevampPlayer : ModPlayer
    {
        public static readonly int PermanentBuffCount = 58;
        public static List<int> startingItemsList;
        public List<int> bagsOpened;
        public Dictionary<ItemDefinition, int> consumedPotions;

        public override void Initialize()
        {
            PermanentBuffToggles = new bool[PermanentBuffCount]; //todo dont forget to increment this if you add buffs to the dictionary
            DamageDir = new Dictionary<int, float> {
                { 48, 4 }, //spike
                { 76, 4 }, //hellstone
                { 232, 4 } //wooden spike, in case tim decides to use them
            };

            SoulSlot = new UIItemSlot(Vector2.Zero, 52, ItemSlot.Context.InventoryItem, LaUtils.GetTextValue("UI.DarkSouls"), null, SoulSlotCondition, DrawSoulSlotBackground, null, null, false, true);
            SoulSlot.BackOpacity = 0.8f;
            SoulSlot.Item = new Item();
            SoulSlot.Item.SetDefaults(0, true);

            chestBankOpen = false;
            chestBank = -1;

            chestPiggyOpen = false;
            chestPiggy = -1;


            bagsOpened = new List<int>();
        }

        public override void CopyClientState(ModPlayer clientClone)/* tModPorter Suggestion: Replace Item.Clone usages with Item.CopyNetStateTo */
        {
            tsorcRevampPlayer clone = clientClone as tsorcRevampPlayer;
            if (clone == null) { return; }

            clone.SoulSlot.Item = SoulSlot.Item.Clone();
        }
        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            tsorcRevampPlayer oldClone = clientPlayer as tsorcRevampPlayer;
            if (oldClone == null) { return; }

            if (oldClone.SoulSlot.Item.IsNotSameTypePrefixAndStack(SoulSlot.Item))
            {
                SendSingleItemPacket(tsorcPacketID.SyncSoulSlot, SoulSlot.Item, -1, Player.whoAmI);
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
            tag.Add("warpX", warpX);
            tag.Add("warpY", warpY);
            tag.Add("warpWorld", warpWorld);
            tag.Add("warpSet", warpSet);
            tag.Add("townWarpX", townWarpX);
            tag.Add("townWarpY", townWarpY);
            tag.Add("townWarpWorld", townWarpWorld);
            tag.Add("townWarpSet", townWarpSet);
            tag.Add("gotPickaxe", gotPickaxe);
            tag.Add("FirstEncounter", FirstEncounter);
            tag.Add("ReceivedGift", ReceivedGift);
            tag.Add("BearerOfTheCurse", BearerOfTheCurse);
            tag.Add("soulSlot", ItemIO.Save(SoulSlot.Item));
            tag.Add("MaxAcquiredHP", MaxAcquiredHP);
            tag.Add("SoulVessel", SoulVessel);

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

            consumedPotions ??= new Dictionary<ItemDefinition, int>();
            tag.Add("consumedPotionsKeys", consumedPotions.Keys.ToList());
            tag.Add("consumedPotionsValues", consumedPotions.Values.ToList());
        }

        public override void LoadData(TagCompound tag)
        {
            warpX = tag.GetInt("warpX");
            warpY = tag.GetInt("warpY");
            warpWorld = tag.GetInt("warpWorld");
            warpSet = tag.GetBool("warpSet");
            townWarpX = tag.GetInt("townWarpX");
            townWarpY = tag.GetInt("townWarpY");
            townWarpWorld = tag.GetInt("townWarpWorld");
            townWarpSet = tag.GetBool("townWarpSet");
            gotPickaxe = tag.GetBool("gotPickaxe");
            FirstEncounter = tag.GetBool("FirstEncounter");
            ReceivedGift = tag.GetBool("ReceivedGift");
            BearerOfTheCurse = tag.GetBool("BearerOfTheCurse");
            Item soulSlotSouls = ItemIO.Load(tag.GetCompound("soulSlot"));
            SoulSlot.Item = soulSlotSouls.Clone();
            MaxAcquiredHP = tag.GetInt("MaxAcquiredHP");
            SoulVessel = tag.GetInt("SoulVessel");

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
            if (permaBuffs.Count == 0) {
                for (int i = 0; i < PermanentBuffCount; i++) {
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

            consumedPotions ??= new Dictionary<ItemDefinition, int>();
            if (tag.ContainsKey("consumedPotionsKeys")) {
                List<ItemDefinition> potKey = tag.GetList<ItemDefinition>("consumedPotionsKeys") as List<ItemDefinition>;
                List<int> potValue = tag.GetList<int>("consumedPotionsValues") as List<int>;
                for (int i = 0; i < potKey.Count; i++) {
                    consumedPotions.Add(potKey[i], potValue[i]);
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
            if (Player.HasBuff(ModContent.BuffType<Invincible>()))
            {
                return true;
            }
            if (DragonStoneImmunity)
            {
                int NT = Main.npc[damageSource.SourceNPCIndex].type;
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
                    || NT == NPCID.WyvernHead
                    || NT == NPCID.Harpy
                    || NT == NPCID.CultistDragonHead
                    || NT == NPCID.SandElemental)
                {
                    return true;
                }
            }
                return false;
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers) 
        {
            float REDUCE = CheckReduceDefense(Player.position, Player.width, Player.height, Player.fireWalk);
            if (REDUCE != 0) 
            {
            }
            modifiers.FinalDamage.ApplyTo(modifiers.SourceDamage.Base);
            Player.AddBuff(ModContent.BuffType<InCombat>(), 600); //10s  
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

        public override void OnHitAnything(float x, float y, Entity victim)
        {
            if (Shunpo && Player.titaniumStormCooldown >= 0)
            {
                Player.titaniumStormCooldown = 10;
                Player.AddBuff(BuffID.TitaniumStorm, 10 * 60);
                if (Player.ownedProjectileCounts[ProjectileID.TitaniumStormShard] < 15)
                {
                    Player.ownedProjectileCounts[ProjectileID.TitaniumStormShard]++;
                    Projectile.NewProjectile(Player.GetSource_OnHit(victim), Player.Center, Vector2.Zero, ProjectileID.TitaniumStormShard, 50, 15f, Player.whoAmI);
                } else
                {
                    int buffIndex = 0;
                    foreach (int buffType in Player.buffType)
                    {
                        if (buffType == ModContent.BuffType<ShunpoDashCooldown>())
                        {
                            Player.buffTime[buffIndex] -= 20;
                        }
                        buffIndex++;
                    }
                }
            }
            if (CelestialCloak) 
            {
                if (Main.rand.NextBool(20))
                {
                    Vector2 starvector1 = new Vector2(-40, -200) + victim.Center;
                    Vector2 starvector2 = new Vector2(40, -200) +victim.Center;
                    Vector2 starvector3 = new Vector2(0, -200) + victim.Center;
                    Vector2 starmove1 = new Vector2(+4, 20);
                    Vector2 starmove2 = new Vector2(-4, 20);
                    Vector2 starmove3 = new Vector2(0, 20);
                    Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), starvector1, starmove1, ProjectileID.ManaCloakStar, Player.statManaMax2 / 5, 2f, Main.myPlayer);
                    Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), starvector2, starmove2, ProjectileID.ManaCloakStar, Player.statManaMax2 / 5, 2f, Main.myPlayer);
                    Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), starvector3, starmove3, ProjectileID.ManaCloakStar, Player.statManaMax2 / 5, 2f, Main.myPlayer);
                }
            }
            if (Main.rand.NextBool(9) & MagicPlatingStacks <= 22 & Player.HasBuff(ModContent.BuffType<MagicPlating>()))
            {
                MagicPlatingStacks += 7;
            }
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
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
            Projectile.NewProjectile(Player.GetSource_Misc("Bloodsign"), Player.Bottom, new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Bloodsign>(), 0, 0, Player.whoAmI);
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

            if (!onePlayerAlive)
            {
                if (NPC.AnyNPCs(NPCID.LunarTowerSolar))
                {
                    NPC.ShieldStrengthTowerSolar = NPC.ShieldStrengthTowerMax;
                    UsefulFunctions.BroadcastText(LaUtils.GetTextValue("NPCs.SolarPillar"), Color.OrangeRed);
                }
                if (NPC.AnyNPCs(NPCID.LunarTowerStardust))
                {
                    NPC.ShieldStrengthTowerStardust = NPC.ShieldStrengthTowerMax;
                    UsefulFunctions.BroadcastText(LaUtils.GetTextValue("NPCs.StardustPillar"), Color.Cyan);
                }
                if (NPC.AnyNPCs(NPCID.LunarTowerVortex))
                {
                    NPC.ShieldStrengthTowerVortex = NPC.ShieldStrengthTowerMax;
                    UsefulFunctions.BroadcastText(LaUtils.GetTextValue("NPCs.VortexPillar"), Color.Teal);
                }
                if (NPC.AnyNPCs(NPCID.LunarTowerNebula))
                {
                    NPC.ShieldStrengthTowerNebula = NPC.ShieldStrengthTowerMax;
                    UsefulFunctions.BroadcastText(LaUtils.GetTextValue("NPCs.NebulaPillar"), Color.Pink);
                }
            }
        }

        public override bool ShiftClickSlot(Item[] inventory, int context, int slot)
        {
            if (!PotionBagUIState.Visible) return false;
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
            return false;
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

            if (ModLoader.TryGetMod("MagicStorage", out Mod MagicStorage)) {
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

            }

            return startingItems;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (MagmaArmor && target.HasBuff(BuffID.OnFire) || target.HasBuff(BuffID.OnFire3))
            {
                target.AddBuff(ModContent.BuffType<Ignited>(), 5 * 60);
            }
            if (MeleeArmorVamp10)
            {
                if (Main.rand.NextBool(10))
                {
                    Player.HealEffect(10);
                    Player.statLife += (10);
                }
            }
            if (NUVamp)
            {
                if (Main.rand.NextBool(5))
                {
                    Player.HealEffect(damageDone / 4);
                    Player.statLife += (damageDone / 4);
                }
            }
            if (MiakodaFull)
            { //Miakoda Full Moon
                if (MiakodaEffectsTimer > Items.Pets.MiakodaFull.HealCooldown)
                {
                    if (hit.Crit)
                    {

                        Player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal1 = true;
                        Player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal2 = true;

                        int HealAmount = (int)((Math.Floor((double)(Player.statLifeMax2 / 100)) * 2) + 2);
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
                if (MiakodaEffectsTimer > Items.Pets.MiakodaCrescent.BoostCooldown)
                {
                    if (hit.Crit)
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

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)/* tModPorter If you don't need the Item, consider using OnHitNPC instead */
        {
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)/* tModPorter If you don't need the Projectile, consider using OnHitNPC instead */
        {
            if (MagmaArmor && target.HasBuff(BuffID.OnFire) || target.HasBuff(BuffID.OnFire3))
            {
                target.AddBuff(ModContent.BuffType<Ignited>(), 5 * 60);
            }
            if (DemonPower && hit.DamageType == (DamageClass.SummonMeleeSpeed) && hit.Crit)
            {
                Projectile WhipCritBoom = Projectile.NewProjectileDirect(Projectile.GetSource_None(), target.Center - new Vector2(0, target.height / 2), Vector2.Zero, ProjectileID.DD2ExplosiveTrapT1Explosion, (int)Main.player[proj.owner].GetTotalDamage(DamageClass.Summon).ApplyTo(AncientDemonArmor.ExplosionBaseDmg), 0, proj.owner);
                WhipCritBoom.ArmorPenetration = 10;
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


            if(proj.type == ModContent.ProjectileType<Projectiles.Ranged.PiercingPlasma>())
            {
                PiercingGazeCharge++;
                if(PiercingGazeCharge == 16)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113, Player.Center);
                    UsefulFunctions.DustRing(Player.Center, 70, DustID.FireworkFountain_Red, 100, 18);
                }
            }
        }
        public static float AmmoReservationRangedCritDamage = 10f;
        public static float SharpenedMeleeArmorPen = 50f;
        public static float MythrilOcrichalcumCritDmg = 25f;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (modifiers.DamageType == DamageClass.Ranged)
            {
                modifiers.NonCritDamage -= Items.Accessories.Ranged.InfinityEdge.NonCritDmgReduction / 100f;
                modifiers.CritDamage += Items.Accessories.Ranged.InfinityEdge.CritDmgIncrease / 100f;
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
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)/* tModPorter If you don't need the Item, consider using ModifyHitNPC instead */
        {
            int critLevel = (int)(Math.Floor(Player.GetWeaponCrit(Player.HeldItem) / 100f));
            if ((BurningAura || BurningStone) && target.onFire == true)
            {
                modifiers.TargetDamageMultiplier *= 1.05f;
            }
            if (critLevel != 0)
            {
                if (critLevel > 1)
                {
                    for (int i = 1; i < critLevel; i++)
                    {
                        modifiers.CritDamage *= 2;
                    }
                }
                if (Main.rand.Next(1, 101) <= (float)Player.GetWeaponCrit(Player.HeldItem) - (100 * critLevel))
                {
                    modifiers.CritDamage *= 2;
                }
            }
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)/* tModPorter If you don't need the Projectile, consider using ModifyHitNPC instead */
        {
            int critLevel = (int)(Math.Floor(proj.CritChance / 100f));
            if (((proj.type == ProjectileID.MoonlordArrow) || (proj.type == ProjectileID.MoonlordArrowTrail)) && Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Bows.CernosPrime>())
            {
                modifiers.FinalDamage *= 0.55f;
            }
            if (Player.GetModPlayer<tsorcRevampPlayer>().ChallengersGloveCritDamage && proj.DamageType == DamageClass.SummonMeleeSpeed)
            {
                modifiers.CritDamage += ChallengersGlove.WhipCritDamage / 100f;
            }
            if (BurningAura || BurningStone && target.onFire == true && proj.type != ModContent.ProjectileType<Projectiles.HomingFireball>())
            {
                modifiers.TargetDamageMultiplier *= 1f + Items.Accessories.Expert.BurningStone.DamageIncrease / 100f;
            }
            if (critLevel != 0)
            {
                if (critLevel > 1)
                {
                    for (int i = 1; i < critLevel; i++)
                    {
                        modifiers.CritDamage *= 2;
                    }
                }
                if (Main.rand.Next(1, 101) <= (float)proj.CritChance - (100 * critLevel))
                {
                    modifiers.CritDamage *= 2;
                }
            }
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            if (UndeadTalisman)
            {
                if (NPCID.Sets.Skeletons[npc.type]
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
                    /* || NT == mod.NPCType("MagmaSkeleton") || NT == mod.NPCType("Troll") || NT == mod.NPCType("HeavyZombie") || NT == mod.NPCType("IceSkeleton") || NT == mod.NPCType("IrateBones")*/)
                {
                    modifiers.FinalDamage.Flat -= Items.Accessories.Defensive.UndeadTalisman.FlatDR;
                }
            }

        }

        public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
        {
            //Todo: All of these accessories should use Player.GetSource_Accessory() as their source
            //They don't because that requires getting the inventory item casuing this effect. I'll do it later if I remember.
            if (Player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge)
            {
                for (int b = 0; b < 8; b++)
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("Bone Revenge"), Player.position, new Vector2(Main.rand.NextFloat(-3f, 3f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), hurtInfo.SourceDamage, 4f, 0, 0, 0);
                }
            }

            if (Player.GetModPlayer<tsorcRevampPlayer>().SoulSickle)
            {
                if (!Main.hardMode)
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("Soul Sickle"), Player.Center, new Vector2(Player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), 40, 7f, 0, 0, 0);
                }
                else
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("Soul Sickle"), Player.Center, new Vector2(Player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), 80, 9f, 0, 0, 0);
                }
            }
            if (npc.type == NPCID.SkeletronPrime && Main.rand.NextBool(2))
            {
                Player.AddBuff(BuffID.Bleeding, 1800);
                Player.AddBuff(BuffID.OnFire, 600);
            }

            if (hurtInfo.Damage >= Player.statLife)
            {
                DeathText = PickDeathText();
            }
            if(Player.HasBuff(ModContent.BuffType<MagicPlating>()))
            {
                MagicPlatingStacks = 0;
            }
        }

        public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo)
        {
            if (Player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge)
            {
                for (int b = 0; b < 8; b++)
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("Bone Revenge"), Player.position, new Vector2(Main.rand.NextFloat(-3f, 3f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), hurtInfo.SourceDamage, 4f, 0, 0, 0);
                }
            }

            if (Player.GetModPlayer<tsorcRevampPlayer>().SoulSickle)
            {
                if (!Main.hardMode)
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("Soul Sickle"), Player.Center, new Vector2(Player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), 40, 6f, 0, 0, 0);
                }
                else
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("Soul Sickle"), Player.Center, new Vector2(Player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), 60, 8f, 0, 0, 0);
                }
            }
            if (proj.type == ProjectileID.DeathLaser && Main.rand.NextBool(2))
            {
                Player.AddBuff(BuffID.BrokenArmor, 180);
                Player.AddBuff(BuffID.OnFire, 180);
            }


            if (hurtInfo.Damage >= Player.statLife)
            {
                DeathText = PickDeathText(proj);
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

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            Player player = Main.player[Main.myPlayer];
            Vector2 unitVectorTowardsMouse = player.Center.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * player.direction);
            if (tsorcRevamp.toggleDragoonBoots.JustPressed)
            {
                DragoonBootsEnable = !DragoonBootsEnable;
            }
            if (tsorcRevamp.Shunpo.JustReleased && player.GetModPlayer<tsorcRevampPlayer>().Shunpo && !player.HasBuff(ModContent.BuffType<ShunpoDashCooldown>()))
            {
                player.AddBuff(ModContent.BuffType<ShunpoDash>(), (int)(ShunpoDash.ShunpoDashDuration * 60 * 2));
                ShunpoTimer = ShunpoDash.ShunpoDashDuration;
                player.AddBuff(ModContent.BuffType<ShunpoDashCooldown>(), ShunpoDash.Cooldown * 60);
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

           if (tsorcRevamp.specialAbility.JustReleased)
            {
                #region Sweeping Blade
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC other = Main.npc[i];
                    Vector2 MouseHitboxSize = new Vector2(100, 100);

                    if (!(Main.keyState.IsKeyDown(Keys.LeftShift)) && other.active && !other.friendly && other.Hitbox.Intersects(Utils.CenteredRectangle(Main.MouseWorld, MouseHitboxSize)) & other.Distance(Player.Center) <= 400 && !other.HasBuff(ModContent.BuffType<PlasmaWhirlwindDashCooldown>()) && player.HeldItem.type == ModContent.ItemType<PlasmaWhirlwind>())
                    {
                        SweepingBladeVelocity = player.DirectionTo(other.Center) * 17;
                        player.AddBuff(ModContent.BuffType<PlasmaWhirlwindDash>(), (int)(PlasmaWhirlwind.DashDuration * 60f * 2));
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/Dash") with { Volume = 1f }, player.Center);
                        Projectile DashHitbox = Projectile.NewProjectileDirect(Projectile.GetSource_None(), player.Center, Vector2.Zero, ModContent.ProjectileType<PlasmaWhirlwindDashHitbox>(), PlasmaWhirlwind.BaseDamage, 0, player.whoAmI);
                        DashHitbox.OriginalCritChance = SteelTempest.CritChance;
                    } //cooldown is added by On-Hit in the dash projectile hitbox
                    if (!(Main.keyState.IsKeyDown(Keys.LeftShift)) && other.active && !other.friendly && other.Hitbox.Intersects(Utils.CenteredRectangle(Main.MouseWorld, MouseHitboxSize)) & other.Distance(Player.Center) <= 400 && !other.HasBuff(ModContent.BuffType<NightbringerDashCooldown>()) && player.HeldItem.type == ModContent.ItemType<Nightbringer>())
                    {
                        SweepingBladeVelocity = player.DirectionTo(other.Center) * 17;
                        player.AddBuff(ModContent.BuffType<NightbringerDash>(), (int)(PlasmaWhirlwind.DashDuration * 60f * 2));
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/Dash") with { Volume = 1f }, player.Center);
                        Projectile DashHitbox = Projectile.NewProjectileDirect(Projectile.GetSource_None(), player.Center, Vector2.Zero, ModContent.ProjectileType<NightbringerDashHitbox>(), Nightbringer.BaseDamage, 0, player.whoAmI);
                        DashHitbox.OriginalCritChance = SteelTempest.CritChance;
                    } //cooldown is added by On-Hit in the dash projectile hitbox
                }
                #endregion

                #region Firewall
                if (Main.keyState.IsKeyDown(Keys.LeftShift) && Player.HeldItem.type == ModContent.ItemType<Nightbringer>() && !Player.HasBuff(ModContent.BuffType<NightbringerFirewallCooldown>()))
                {
                    Projectile Firewall = Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), player.Center, unitVectorTowardsMouse * 5f, ModContent.ProjectileType<Projectiles.Melee.Runeterra.NightbringerFirewall>(), player.HeldItem.damage / 3, 0, Main.myPlayer);
                    Firewall.OriginalCritChance = SteelTempest.CritChance;
                    Player.AddBuff(ModContent.BuffType<NightbringerFirewallCooldown>(), 30 * 60);
                }
                #endregion

                #region Scouts Boost
                if (!(Main.keyState.IsKeyDown(Keys.LeftShift)) && !player.HasBuff(ModContent.BuffType<ScoutsBoost2Cooldown>()) && (Player.HeldItem.type == ModContent.ItemType<ToxicShot>() | Player.HeldItem.type == ModContent.ItemType<AlienGun>()))
                {
                    player.AddBuff(ModContent.BuffType<ScoutsBoost2>(), ToxicShot.ScoutsBoost2Duration * 60);
                    player.AddBuff(ModContent.BuffType<ScoutsBoost2Cooldown>(), ToxicShot.ScoutsBoost2Cooldown * 60);
                }
                if (!(Main.keyState.IsKeyDown(Keys.LeftShift)) && !player.HasBuff(ModContent.BuffType<ScoutsBoost2CooldownOmega>()) && Player.HeldItem.type == ModContent.ItemType<OmegaSquadRifle>())
                {
                    player.AddBuff(ModContent.BuffType<ScoutsBoost2Omega>(), ToxicShot.ScoutsBoost2Duration * 60);
                    player.AddBuff(ModContent.BuffType<ScoutsBoost2CooldownOmega>(), ToxicShot.ScoutsBoost2Cooldown * 60);
                }
                if (Main.keyState.IsKeyDown(Keys.LeftShift) && !Player.HasBuff(ModContent.BuffType<NuclearMushroomCooldown>()) && Player.HeldItem.type == ModContent.ItemType<OmegaSquadRifle>())
                {
                    Projectile Shroom = Projectile.NewProjectileDirect(Projectile.GetSource_None(), Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<Projectiles.Ranged.Runeterra.NuclearMushroom>(), player.GetWeaponDamage(player.HeldItem), player.GetWeaponKnockback(player.HeldItem), Main.myPlayer);
                    Shroom.OriginalCritChance = 100;
                    Player.AddBuff(ModContent.BuffType<NuclearMushroomCooldown>(), OmegaSquadRifle.ShroomCooldown * 60);
                }
                #endregion

                #region Spirit Rush
                if (player.HeldItem.type == ModContent.ItemType<OrbOfSpirituality>() && player.statMana >= (player.GetManaCost(player.HeldItem) * OrbOfSpirituality.DashCostMultiplier) && !Player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDashCooldown>()) && !Player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDash>()))
                {
                    player.AddBuff(ModContent.BuffType<OrbOfSpiritualityDash>(), OrbOfSpirituality.DashBuffDuration * 60);
                    player.statMana -= player.GetManaCost(player.HeldItem) * OrbOfSpirituality.DashCostMultiplier;
                }
                if (player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDash>()) && SpiritRushCooldown <= 0f && SpiritRushCharges > 0)
                {
                    SpiritRushVelocity = player.DirectionTo(Main.MouseWorld) * 20f;
                    SpiritRushTimer = 0.3f;
                    SpiritRushCooldown = 1f;
                    if (SpiritRushSoundStyle == 0)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/Dash1") with { Volume = 1f }, player.Center);
                        SpiritRushSoundStyle += 1;
                    }
                    else
                    if (SpiritRushSoundStyle == 1)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/Dash2") with { Volume = 1f }, player.Center);
                        SpiritRushSoundStyle += 1;
                    }
                    else
                    if (SpiritRushSoundStyle == 2)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/Dash3") with { Volume = 1f }, player.Center);
                        SpiritRushSoundStyle = 0;
                    }
                    SpiritRushCharges--;
                }
                #endregion

                #region Interstellar Boost
                bool holdingControls = Player.HeldItem.type == ModContent.ItemType<InterstellarVesselGauntlet>() || Player.HeldItem.type == ModContent.ItemType<CenterOfTheUniverse>();
                bool hasBuff = Player.HasBuff(ModContent.BuffType<InterstellarCommander>()) || Player.HasBuff(ModContent.BuffType<CenterOfTheUniverseBuff>());
                if (!holdingControls && hasBuff && !(Main.keyState.IsKeyDown(Keys.LeftShift)))
                {
                    player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost = !player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost;

                    //Every time the player releases the button, sync this info to everyone else
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        ModPacket minionPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                        minionPacket.Write(tsorcPacketID.SyncMinionRadius);
                        minionPacket.Write((byte)Player.whoAmI);
                        minionPacket.Write(MinionCircleRadius);
                        minionPacket.Write(InterstellarBoost);
                        minionPacket.Send();
                    }
                }
                #endregion

            }



            if (tsorcRevamp.specialAbility.Current && (Player.HeldItem.type == ModContent.ItemType<ScorchingPoint>() || Player.HeldItem.type == ModContent.ItemType<InterstellarVesselGauntlet>() || Player.HeldItem.type == ModContent.ItemType<CenterOfTheUniverse>()))
            {
                if (Main.keyState.IsKeyDown(Keys.LeftShift))
                {
                    MinionCircleRadius -= 1.5f;
                    if (MinionCircleRadius < 50)
                    {
                        MinionCircleRadius = 50;
                    }
                }
                else
                {
                    MinionCircleRadius += 1.5f;
                    if (MinionCircleRadius > 500)
                    {
                        MinionCircleRadius = 500;
                    }
                }
                Dust.NewDustDirect(Player.Center, 10, 10, DustID.FlameBurst, 0.5f, 0.5f, 0, Color.Firebrick, 0.5f);
            }
        }


        //On hit, subtract the mana cost and disable natural mana regen for a short period
        //The latter is absolutely necessary, because natural mana regen scales with your base mana
        //Even as melee there are mana boosting accessories you can stack, as well as armor like Dragoon that makes mana regen obscenely powerful.
        //This means you can tank until your mana bar is exhausted, then have to back off for a bit and actually dodge
        public override void OnHurt(Player.HurtInfo info)
        {
            
            if (manaShield == 1)
            {
                if (Player.statMana >= Items.Accessories.Defensive.ManaShield.manaCost)
                {
                    Player.statMana -= Items.Accessories.Defensive.ManaShield.manaCost;
                    Player.manaRegenDelay = Items.Accessories.Defensive.ManaShield.regenDelay * 60;
                    Player.maxRegenDelay = Items.Accessories.Defensive.ManaShield.regenDelay * 60;
                }
            }
            if (manaShield == 2)
            {
                if (Player.statMana >= Items.Accessories.Defensive.Celestriad.manaCost)
                {
                    Player.statMana -= Items.Accessories.Defensive.Celestriad.manaCost;
                    Player.manaRegenDelay = Items.Accessories.Defensive.Celestriad.regenDelay * 60;
                    Player.maxRegenDelay = Items.Accessories.Defensive.Celestriad.regenDelay * 60;
                }
            }
            // stamina shield code
            if (staminaShield == 1)
            {
                if (Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 75)
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= Items.Accessories.Expert.DragonCrestShield.staminaCost;
                    //return;
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

        public override void OnEnterWorld()
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && !gotPickaxe)
            { //sandbox mode only, and only once
                Player.QuickSpawnItem(Player.GetSource_Loot(), ModContent.ItemType<DiamondPickaxe>());
                gotPickaxe = true;
            }
            DeathText = PickDeathText();
        }



        public override void OnRespawn()
        {
            Player.statLife = Player.statLifeMax2;
            if (BearerOfTheCurse) Player.AddBuff(ModContent.BuffType<Hollowed>(), 2);
            Player.AddBuff(ModContent.BuffType<Invincible>(), 360);
            DeathText = PickDeathText();
        }

        public static bool CheckBossZen()
        {
            for (int i = 0; i < 200; i++)
            {
                if (Main.npc[i].active && Main.npc[i].boss)
                {
                    return true;
                }
            }
            return false;
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
