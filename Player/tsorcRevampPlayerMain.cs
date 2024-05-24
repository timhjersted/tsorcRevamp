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
using tsorcRevamp.Items.Weapons.Magic;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
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
        public static readonly int PermanentBuffCount = 58;
        public static List<int> startingItemsList;
        public List<int> bagsOpened;
        public Dictionary<int, int> consumedPotions;

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

            SoulSlot.Item.CopyNetStateTo(clone.SoulSlot.Item);
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
            tag.Add("greatMirrorWarp", greatMirrorWarpPoint);
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
            gotPickaxe = tag.GetBool("gotPickaxe");
            FirstEncounter = tag.GetBool("FirstEncounter");
            ReceivedGift = tag.GetBool("ReceivedGift");
            BearerOfTheCurse = tag.GetBool("BearerOfTheCurse");
            Item soulSlotSouls = ItemIO.Load(tag.GetCompound("soulSlot"));
            SoulSlot.Item = soulSlotSouls.Clone();
            CurseActive = tag.GetBool("Curse");
            CurseMaxLifeMultiplier = tag.GetFloat("CurseMaxLifeMult");
            CurseLifeRegenerationBonus = tag.GetFloat("CurseLifeRegen");
            CurseDefenseBonus = tag.GetFloat("CurseDefense");
            CurseResistanceBonus = tag.GetFloat("CurseResist");
            CurseDamageBonus = tag.GetFloat("CurseDmg");
            CurseAttackSpeedBonus = tag.GetFloat("CurseAtkSpd");
            CurseMovementSpeedBonus = tag.GetFloat("CurseMoveSpd");
            powerfulCurseActive = tag.GetBool("powerfulCurse");
            CurseMaxLifeMultiplier = tag.GetFloat("CurseMaxLifeMult");
            powerfulCurseLifeRegenerationBonus = tag.GetFloat("powerfulCurseLifeRegen");
            powerfulCurseDefenseBonus = tag.GetFloat("powerfulCurseDefense");
            powerfulCurseResistanceBonus = tag.GetFloat("powerfulCurseResist");
            powerfulCurseDamageBonus = tag.GetFloat("powerfulCurseDmg");
            powerfulCurseAttackSpeedBonus = tag.GetFloat("powerfulCurseAtkSpd");
            powerfulCurseMovementSpeedBonus = tag.GetFloat("powerfulCurseMoveSpd");
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
                    consumedPotions.Add(potKey[i].Type, potValue[i]);
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
            if (info.Damage > 1)
            {
                Player.AddBuff(ModContent.BuffType<InCombat>(), 600); //10s  
            }
        }

        public override void OnHitAnything(float x, float y, Entity victim)
        {
            if (Shunpo && Player.titaniumStormCooldown >= 0)
            {
                int TitaniumShardBaseDmg = 50; //50 is the base dmg of vanilla Titanium Shards
                int TitaniumShardScaledBonusDmg = (int)((Player.GetDamage(DamageClass.Generic).ApplyTo(TitaniumShardBaseDmg) + Player.GetDamage(DamageClass.Melee).ApplyTo(TitaniumShardBaseDmg) 
                 + Player.GetDamage(DamageClass.Ranged).ApplyTo(TitaniumShardBaseDmg) + Player.GetDamage(DamageClass.Magic).ApplyTo(TitaniumShardBaseDmg) 
                 + Player.GetDamage(DamageClass.Summon).ApplyTo(TitaniumShardBaseDmg) + Player.GetDamage(DamageClass.Throwing).ApplyTo(TitaniumShardBaseDmg)) - (6f * TitaniumShardBaseDmg));
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
                    if (Player.HasBuff(BuffID.Smolstar))
                    {
                        UsefulFunctions.AddPlayerBuffDuration(Player, ModContent.BuffType<ShunpoBlinkCooldown>(), -4);
                    }
                    else
                    {
                        UsefulFunctions.AddPlayerBuffDuration(Player, ModContent.BuffType<ShunpoBlinkCooldown>(), -40);
                    }
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

            Item ForgottenRuneAxe = new Item();
            ForgottenRuneAxe.SetDefaults(ModContent.ItemType<ForgottenRuneAxe>());
            ForgottenRuneAxe.prefix = PrefixID.Dull;
            startingItems.Add(ForgottenRuneAxe);

            Item Crossbow = new Item();
            Crossbow.SetDefaults(ModContent.ItemType<Items.Weapons.Ranged.Specialist.Crossbow>());
            Crossbow.prefix = PrefixID.Awful;
            startingItems.Add(Crossbow);

            Item ApprenticesWand = new Item();
            ApprenticesWand.SetDefaults(ModContent.ItemType<ApprenticesWand>());
            ApprenticesWand.prefix = PrefixID.Ignorant;
            startingItems.Add(ApprenticesWand);

            Item RustyChain = new Item();
            RustyChain.SetDefaults(ModContent.ItemType<RustedChain>());
            RustyChain.prefix = PrefixID.Terrible;
            startingItems.Add(RustyChain);

            Item Bolt = new Item();
            Bolt.SetDefaults(ModContent.ItemType<Bolt>());
            Bolt.stack = 50;
            startingItems.Add(Bolt);

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
        public static float AmmoReservationRangedCritDamage = 10f;
        public static float SharpenedMeleeArmorPen = 50f;
        public static float MythrilOcrichalcumCritDmg = 25f;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (SmoughAttackSpeedReduction)
            {
                if (modifiers.DamageType != DamageClass.SummonMeleeSpeed)
                {
                    modifiers.SetCrit();
                }
                modifiers.CritDamage -= SmoughArmor.BadCritDmg / 100f;
            }
            if (modifiers.DamageType == DamageClass.MagicSummonHybrid)
            {
                modifiers.CritDamage -= 0.25f;
            }
            if (CanUseItemsWhileDodging && isDodging && (modifiers.DamageType == DamageClass.Melee || modifiers.DamageType == DamageClass.MeleeNoSpeed))
            {
                modifiers.FinalDamage += ArtoriasArmor.DmgMultWhileRolling;
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
            OverCrit(Player.GetWeaponCrit(Player.HeldItem), item.DamageType, ref modifiers, out CritColorTier, false, null, target.Hitbox);
        }
        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)/* tModPorter If you don't need the Projectile, consider using ModifyHitNPC instead */
        {
            if (SmoughAttackSpeedReduction && modifiers.DamageType == DamageClass.SummonMeleeSpeed)
            {
                if (!ProjectileID.Sets.IsAWhip[proj.type])
                {
                    modifiers.SetCrit();
                }
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
            if (Goredrinker && proj.DamageType == DamageClass.SummonMeleeSpeed && !Player.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && GoredrinkerSwung && ProjectileID.Sets.IsAWhip[proj.type])
            {
                modifiers.SourceDamage += Items.Accessories.Summon.Goredrinker.WhipDmgRange / 100f / 3f; //guaranteed crit is in overcrit
            }
            if (BurningAura || BurningStone && target.onFire == true && proj.type != ModContent.ProjectileType<Projectiles.HomingFireball>())
            {
                modifiers.TargetDamageMultiplier *= 1f + Items.Accessories.Damage.BurningStone.DamageIncrease / 100f;
            }
            if (proj.type == ProjectileID.StardustDragon1 || proj.type == ProjectileID.StardustDragon2 || proj.type == ProjectileID.StardustDragon3 || proj.type == ProjectileID.StardustDragon4)
            {
                float DragonStacks = Player.ownedProjectileCounts[ProjectileID.StardustDragon1] + Player.ownedProjectileCounts[ProjectileID.StardustDragon2] + Player.ownedProjectileCounts[ProjectileID.StardustDragon3] + Player.ownedProjectileCounts[ProjectileID.StardustDragon4];
                modifiers.SourceDamage *= MathF.Max(0.5f - DragonStacks / 100f, 0.2f);
            }
            if (!proj.IsMinionOrSentryRelated)
            {
                OverCrit(proj.CritChance, proj.DamageType, ref modifiers, out CritColorTier, ProjectileID.Sets.IsAWhip[proj.type], proj, target.Hitbox);
            }
        }
        public bool WhipTipCrit(in Projectile projectile, in List<Vector2> points, in Rectangle targetHitbox)
        {
            Player player = Main.player[projectile.owner];
            Vector2 TipBase = tsorcRevamp.WhipTipBases[projectile.type];
            if (Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 2], TipBase * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier * player.GetModPlayer<tsorcRevampPlayer>().WhipCritHitboxSize).Intersects(targetHitbox) || 
                Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 1], TipBase * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier * player.GetModPlayer<tsorcRevampPlayer>().WhipCritHitboxSize).Intersects(targetHitbox))
            {
                return true;
            }
            return false;
        }
        public void OverCrit(in int CritChance, DamageClass damageType, ref NPC.HitModifiers modifiers, out int critColorTier, in bool IsWhip, Projectile projectile, Rectangle targetHitbox)
        {
            int critLevel = (int)(Math.Floor(CritChance / 100f));
            critColorTier = 0;
            if (critLevel != 0 && damageType != DamageClass.Summon && damageType != DamageClass.SummonMeleeSpeed)
            {
                if (critLevel > 1)
                {
                    for (int i = 1; i < critLevel; i++)
                    {
                        modifiers.CritDamage *= 2;
                        modifiers.HideCombatText();
                        critColorTier++;
                    }
                }
                if (Main.rand.Next(1, 101) <= (float)CritChance - (100 * critLevel))
                {
                    modifiers.CritDamage *= 2;
                    modifiers.HideCombatText();
                    critColorTier++;
                }
            }
            else if (critLevel != 0 && (damageType == DamageClass.Summon || damageType == DamageClass.SummonMeleeSpeed) && !IsWhip)
            {
                modifiers.SetCrit();
                if (critLevel > 1)
                {
                    for (int i = 1; i < critLevel; i++)
                    {
                        modifiers.CritDamage *= 2;
                        modifiers.HideCombatText();
                        critColorTier++;
                    }
                }
                if (Main.rand.Next(1, 101) <= (float)CritChance - (100 * critLevel))
                {
                    modifiers.CritDamage *= 2;
                    modifiers.HideCombatText();
                    critColorTier++;
                }
            }
            else if (IsWhip)
            {
                if (WhipTipCrit(projectile, projectile.WhipPointsForCollision, targetHitbox) || (Goredrinker && !Player.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && GoredrinkerSwung))
                {
                    modifiers.SetCrit();
                    if (critLevel > 0)
                    {
                        for (int i = 0; i < critLevel; i++)
                        {
                            modifiers.CritDamage *= 2;
                            modifiers.HideCombatText();
                            critColorTier++;
                        }
                    }
                    if (Main.rand.Next(1, 101) <= (float)CritChance - (100 * critLevel))
                    {
                        modifiers.CritDamage *= 2;
                        modifiers.HideCombatText();
                        critColorTier++;
                    }
                }
            }
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
            if (CritColorTier > 0 && hit.DamageType != DamageClass.MagicSummonHybrid)
            {
                OverCritColor(target.Hitbox, damageDone, CritColorTier);
            }
            if (MagmaArmor && target.HasBuff(BuffID.OnFire) || target.HasBuff(BuffID.OnFire3))
            {
                target.AddBuff(ModContent.BuffType<Ignited>(), 5 * 60);
            }
            if (DemonPower && hit.DamageType == DamageClass.SummonMeleeSpeed && hit.Crit && Main.myPlayer == Player.whoAmI)
            {
                Projectile WhipCritBoom = Projectile.NewProjectileDirect(Projectile.GetSource_None(), target.Center - new Vector2(0, target.height / 2), Vector2.Zero, ProjectileID.DD2ExplosiveTrapT1Explosion, (int)Player.GetTotalDamage(DamageClass.Summon).ApplyTo(AncientDemonArmor.ExplosionBaseDmg), 0, Player.whoAmI, 1);
            }
            if (Lich && hit.Damage >= target.life)
            {
                LichKills++;
            }
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
        public void OverCritColor(in Rectangle targetHitbox, in int damageDealt, in int CritColorTier)
        {
            Color ColorOfCrit = Color.Red;
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
                        ColorOfCrit = Color.DarkViolet;
                        break;
                    }
                case 4:
                    {
                        ColorOfCrit = Color.White;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            CombatText.NewText(targetHitbox, ColorOfCrit, damageDealt, true, false);
        }
        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)/* tModPorter If you don't need the Item, consider using OnHitNPC instead */
        {
            if (MeleeArmorVamp10)
            {
                if (Main.rand.NextBool(10))
                {
                    Player.HealEffect(10);
                    Player.statLife += 10;
                }
            }

        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)/* tModPorter If you don't need the Projectile, consider using OnHitNPC instead */
        {
            Player owner = Main.player[proj.owner];
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

            if (hurtInfo.Damage >= Player.statLife)
            {
                DeathText = PickDeathText();
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
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                Vector2 MouseHitboxSize = new Vector2(100, 100);

                if (tsorcRevamp.Shunpo.JustReleased && other.active && !tsorcRevamp.UntargetableNPCs.Contains(other.type) && !other.friendly && other.Hitbox.Intersects(Utils.CenteredRectangle(Main.MouseWorld, MouseHitboxSize)) && player.GetModPlayer<tsorcRevampPlayer>().Shunpo && !player.HasBuff(ModContent.BuffType<ShunpoBlinkCooldown>()))
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
            if (tsorcRevamp.NecromancersSpell.JustReleased)
            {
                if (Lich && LichKills > 0)
                {
                    for (int i = 0; i < LichKills; i++)
                    {
                        if (Main.myPlayer == player.whoAmI)
                        {
                            Projectile Skull = Projectile.NewProjectileDirect(Projectile.GetSource_None(), player.Center + Main.rand.NextVector2CircularEdge(30, 30), Vector2.Zero, ProjectileID.BookOfSkullsSkull, (int)player.GetTotalDamage(DamageClass.MagicSummonHybrid).ApplyTo(NecromancersShirt.SkullBaseDmg), player.GetTotalKnockback(DamageClass.MagicSummonHybrid).ApplyTo(NecromancersShirt.SkullBaseKnockback), player.whoAmI, 1);
                        }
                    }
                    LichKills = 0;
                }
            }
            if (tsorcRevamp.KrakensCast.JustReleased)
            {
                if (Kraken)
                {
                    if (Main.myPlayer == player.whoAmI)
                    {
                        Projectile Tsunami = Projectile.NewProjectileDirect(Projectile.GetSource_None(), Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<KrakenTsunami>(), (int)player.GetTotalDamage(DamageClass.Ranged).ApplyTo(KrakenCarcass.TsunamiBaseDmg), player.GetTotalKnockback(DamageClass.Ranged).ApplyTo(KrakenCarcass.TsunamiBaseKnockback), player.whoAmI);
                    }
                }
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
                if (player.HeldItem.type == ModContent.ItemType<OrbOfSpirituality>() && player.statMana >= (player.GetManaCost(player.HeldItem) * OrbOfSpirituality.DashCostMultiplier) && !Player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDashCooldown>()) && !Player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDash>()))
                {
                    player.AddBuff(ModContent.BuffType<OrbOfSpiritualityDash>(), OrbOfSpirituality.DashBuffDuration * 60);
                    player.statMana -= player.GetManaCost(player.HeldItem) * OrbOfSpirituality.DashCostMultiplier;
                }
                if (player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDash>()) && SpiritRushCooldown <= 0f && SpiritRushCharges > 0)
                {
                    player.immune = true;
                    SpiritRushVelocity = player.DirectionTo(Main.MouseWorld) * 20f;
                    SpiritRushTimer = 0.3f;
                    SpiritRushCooldown = 1f;
                    player.SetImmuneTimeForAllTypes((int)(SpiritRushCooldown * 60f));
                    if (SpiritRushSoundStyle == 0)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/Dash1") with { Volume = 1f });
                        SpiritRushSoundStyle += 1;
                    }
                    else
                    if (SpiritRushSoundStyle == 1)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/Dash2") with { Volume = 1f });
                        SpiritRushSoundStyle += 1;
                    }
                    else
                    if (SpiritRushSoundStyle == 2)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/Dash3") with { Volume = 1f });
                        SpiritRushSoundStyle = 0;
                    }
                    SpiritRushCharges--;
                }
                #endregion

                #region Interstellar Boost
                bool holdingControlsAndOrSummonWeapon = (Player.HasItem(ModContent.ItemType<InterstellarVesselGauntlet>()) || Player.HasItem(ModContent.ItemType<CenterOfTheUniverse>())) && (player.HeldItem.DamageType == DamageClass.SummonMeleeSpeed || player.HeldItem.DamageType == DamageClass.Summon);
                bool hasBuff = Player.HasBuff(ModContent.BuffType<InterstellarCommander>()) || Player.HasBuff(ModContent.BuffType<CenterOfTheUniverseBuff>());
                if (holdingControlsAndOrSummonWeapon && hasBuff && !(Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) && InterstellarBoostCooldown < 0)
                {
                    player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost = !player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost;
                    bool HasCenterOfTheUniverse = player.HasItem(ModContent.ItemType<CenterOfTheUniverse>());
                    if (!HasCenterOfTheUniverse && player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/InterstellarVessel/BoostActivation") with { Volume = InterstellarVesselGauntlet.SoundVolume });
                    }
                    else if (!HasCenterOfTheUniverse && !player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/InterstellarVessel/BoostDeactivation") with { Volume = InterstellarVesselGauntlet.SoundVolume });
                    }

                    if (HasCenterOfTheUniverse && player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/CenterOfTheUniverse/BoostActivation") with { Volume = CenterOfTheUniverse.SoundVolume });
                    }
                    else if (HasCenterOfTheUniverse && !player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/CenterOfTheUniverse/BoostDeactivation") with { Volume = CenterOfTheUniverse.SoundVolume });
                    }

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



            if (tsorcRevamp.specialAbility.Current && (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) && (Player.HeldItem.type == ModContent.ItemType<ScorchingPoint>() || Player.HeldItem.type == ModContent.ItemType<InterstellarVesselGauntlet>() || Player.HeldItem.type == ModContent.ItemType<CenterOfTheUniverse>()))
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
                InterstellarBoostCooldown = 61; //so you don't accidentally activate Interstellar Boost when you jsut wanted to adjust the circle radius
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
                if (Player.statMana >= Items.Accessories.Defensive.Shields.ManaShield.manaCost)
                {
                    Player.statMana -= Items.Accessories.Defensive.Shields.ManaShield.manaCost;
                    Player.manaRegenDelay = Items.Accessories.Defensive.Shields.ManaShield.regenDelay * 60;
                    Player.maxRegenDelay = Items.Accessories.Defensive.Shields.ManaShield.regenDelay * 60;
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
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= Items.Accessories.Defensive.Shields.DragonCrestShield.staminaCost;
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
