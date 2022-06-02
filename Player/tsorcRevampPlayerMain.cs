using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
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

namespace tsorcRevamp
{
    public partial class tsorcRevampPlayer : ModPlayer
    {

        public static List<int> startingItemsList;

        public override void Initialize()
        {
            PermanentBuffToggles = new bool[54]; //todo dont forget to increment this if you add buffs to the dictionary
            DamageDir = new Dictionary<int, float> {
                { 48, 4 }, //spike
                { 76, 4 }, //hellstone
                { 232, 4 } //wooden spike, in case tim decides to use them
            };

            SoulSlot = new UIItemSlot(Vector2.Zero, 52, ItemSlot.Context.InventoryItem, "Dark Souls", null, SoulSlotCondition, DrawSoulSlotBackground, null, null, false, true);
            SoulSlot.BackOpacity = 0.8f;
            SoulSlot.Item = new Item();
            SoulSlot.Item.SetDefaults(0, true);

            chestBankOpen = false;
            chestBank = -1;

            chestPiggyOpen = false;
            chestPiggy = -1;

        }

        public override void clientClone(ModPlayer clientClone)
        {
            tsorcRevampPlayer clone = clientClone as tsorcRevampPlayer;
            if (clone == null) { return; }

            clone.SoulSlot.Item = SoulSlot.Item.Clone();
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            tsorcRevampPlayer oldClone = clientPlayer as tsorcRevampPlayer;
            if (oldClone == null) { return; }

            if (oldClone.SoulSlot.Item.IsNotTheSameAs(SoulSlot.Item))
            {
                SendSingleItemPacket(1, SoulSlot.Item, -1, Player.whoAmI);
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

        public override TagCompound SaveData()
        {
            TagCompound tsorcTagCompound = new TagCompound
            {
                {"warpX", warpX},
                {"warpY", warpY},
                {"warpWorld", warpWorld},
                {"warpSet", warpSet},
                {"townWarpX", townWarpX},
                {"townWarpY", townWarpY},
                {"townWarpWorld", townWarpWorld},
                {"townWarpSet", townWarpSet},
                {"gotPickaxe", gotPickaxe},
                {"FirstEncounter", FirstEncounter},
                {"ReceivedGift", ReceivedGift},
                {"BearerOfTheCurse", BearerOfTheCurse},
                {"soulSlot", ItemIO.Save(SoulSlot.Item)},
                {"MaxAcquiredHP", MaxAcquiredHP}

            };

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

            tsorcTagCompound.Add("PotionBag", PotionBagList);

            return tsorcTagCompound;
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
        public override bool PreHurt(bool pvp, bool quiet, ref int damage, ref int hitDirection, ref bool crit, ref bool customDamage, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (Player.HasBuff(ModContent.BuffType<Invincible>())) return false;
            /*
            player.AddBuff(ModContent.BuffType<InCombat>(), 600); //10s 
            */
            return base.PreHurt(pvp, quiet, ref damage, ref hitDirection, ref crit, ref customDamage, ref playSound, ref genGore, ref damageSource);
        }

        public override void OnHitAnything(float x, float y, Entity victim)
        {
            /*
            player.AddBuff(ModContent.BuffType<InCombat>(), 600); //10s 
            */
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

            //you died sound                    \
            Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("Sounds/DarkSouls/you-died") with { Volume = 0.4f }, NPC.Center);

            if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && Player.statLifeMax > 200)
            {
                Player.statLifeMax -= 20;
            }



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
                    UsefulFunctions.BroadcastText("The Solar Pillar returns to full strength...", Color.OrangeRed);
                }
                if (NPC.AnyNPCs(NPCID.LunarTowerStardust))
                {
                    NPC.ShieldStrengthTowerStardust = NPC.ShieldStrengthTowerMax;
                    UsefulFunctions.BroadcastText("The Stardust Pillar returns to full strength...", Color.Cyan);
                }
                if (NPC.AnyNPCs(NPCID.LunarTowerVortex))
                {
                    NPC.ShieldStrengthTowerVortex = NPC.ShieldStrengthTowerMax;
                    UsefulFunctions.BroadcastText("The Vortex Pillar returns to full strength...", Color.Teal);
                }
                if (NPC.AnyNPCs(NPCID.LunarTowerNebula))
                {
                    NPC.ShieldStrengthTowerNebula = NPC.ShieldStrengthTowerMax;
                    UsefulFunctions.BroadcastText("The Nebula Pillar returns to full strength...", Color.Pink);
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
                            PotionBagItems[emptySlot.Value] = item.DeepClone();
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
                            Player.inventory[emptySlot.Value] = item.DeepClone();
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

        public override void SetupStartInventory(IList<Item> items, bool mediumcoreDeath)
        {
            Item item = new Item();
            item.SetDefaults(ModContent.ItemType<Darksign>());
            items.Add(item);

            Item PotionBagItem = new Item();
            PotionBagItem.SetDefaults(ModContent.ItemType<PotionBag>());
            items.Add(PotionBagItem);
        }

        public override void OnHitNPC(Item item, NPC target, int damage, float knockback, bool crit)
        {
            if (MeleeArmorVamp10)
            {
                if (Main.rand.Next(10) == 0)
                {
                    Player.HealEffect(10);
                    Player.statLife += (10);

                }
            }
            if (NUVamp)
            {
                if (Main.rand.Next(5) == 0)
                {
                    Player.HealEffect(damage / 4);
                    Player.statLife += (damage / 4);
                }
            }
            if (MiakodaFull)
            { //Miakoda Full Moon
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit)
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
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit)
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
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit)
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

        public override void OnHitNPCWithProj(Projectile proj, NPC target, int damage, float knockback, bool crit)
        {
            if (MiakodaFull)
            { //Miakoda Full Moon
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit || (proj.minion && Main.player[proj.owner].HeldItem.DamageType == DamageClass.Summon))
                    {
                        Player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal1 = true;
                        Player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal2 = true;



                        //2 per 100 max hp, plus 2
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
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit || (proj.minion && Main.player[proj.owner].HeldItem.DamageType == DamageClass.Summon))
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
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit || (proj.minion && Main.player[proj.owner].HeldItem.CountsAsClass(DamageClass.Summon)))
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

        public override void ModifyHitNPC(Item item, NPC target, ref int damage, ref float knockback, ref bool crit)
        {
            if (OldWeapon)
            {
                float damageMult = Main.rand.NextFloat(0.0f, 0.8696f);
                damage = (int)(damage * damageMult);
            }
            if (crit)
            {
                if (item.DamageType == DamageClass.Melee)
                {
                    DoMultiCrits(ref damage, Player.GetCritChance(DamageClass.Melee));
                }
                else if (item.DamageType == DamageClass.Magic)
                {
                    DoMultiCrits(ref damage, Player.GetCritChance(DamageClass.Magic));
                }
                else if (item.DamageType == DamageClass.Ranged)
                {
                    DoMultiCrits(ref damage, Player.GetCritChance(DamageClass.Ranged));
                }
                else if (item.DamageType == DamageClass.Throwing)
                {
                    DoMultiCrits(ref damage, Player.GetCritChance(DamageClass.Throwing)); //lol
                }
            }
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (OldWeapon)
            {
                float damageMult = Main.rand.NextFloat(0.0f, 0.8696f);
                damage = (int)(damage * damageMult);
            }
            if (((proj.type == ProjectileID.MoonlordArrow) || (proj.type == ProjectileID.MoonlordArrowTrail)) && Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.CernosPrime>())
            {
                damage = (int)(damage * 0.55);
            }

            if (crit)
            {
                if (proj.CountsAsClass(DamageClass.Melee))
                {
                    DoMultiCrits(ref damage, Player.GetCritChance(DamageClass.Melee));
                }
                else if (proj.CountsAsClass(DamageClass.Magic))
                {
                    DoMultiCrits(ref damage, Player.GetCritChance(DamageClass.Magic));
                }
                else if (proj.CountsAsClass(DamageClass.Ranged))
                {
                    DoMultiCrits(ref damage, Player.GetCritChance(DamageClass.Ranged));
                }
                else if (proj.CountsAsClass(DamageClass.Throwing))
                {
                    DoMultiCrits(ref damage, Player.GetCritChance(DamageClass.Throwing)); //lol
                }
            }
        }

        public override void ModifyHitByNPC(NPC npc, ref int damage, ref bool crit)
        {
            int NT = npc.type;
            if (DragonStone)
            {
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
                    damage = 0;
                }
            }
            if (UndeadTalisman)
            {
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
                    /* || NT == mod.NPCType("MagmaSkeleton") || NT == mod.NPCType("Troll") || NT == mod.NPCType("HeavyZombie") || NT == mod.NPCType("IceSkeleton") || NT == mod.NPCType("IrateBones")*/)
                {
                    damage -= 15;

                    if (damage < 0) damage = 0;
                }
            }

        }

        public override void OnHitByNPC(NPC npc, int damage, bool crit)
        {
            //Todo: All of these accessories should use Player.GetSource_Accessory() as their source
            //They don't because that requires getting the inventory item casuing this effect. I'll do it later if I remember.
            if (Player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge)
            {
                if (!Main.hardMode)
                {
                    for (int b = 0; b < 8; b++)
                    {
                        Projectile.NewProjectile(Player.GetSource_Misc("Bone Revenge"), Player.position, new Vector2(Main.rand.NextFloat(-3f, 3f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 20, 4f, 0, 0, 0);
                    }
                }
                else
                {
                    for (int b = 0; b < 12; b++)
                    {
                        Projectile.NewProjectile(Player.GetSource_Misc("Bone Revenge"), Player.position, new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 40, 5f, 0, 0, 0);
                    }
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
            if (npc.type == NPCID.SkeletronPrime && Main.rand.Next(2) == 0)
            {
                Player.AddBuff(BuffID.Bleeding, 1800);
                Player.AddBuff(BuffID.OnFire, 600);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, bool crit)
        {
            if (Player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge)
            {
                if (!Main.hardMode)
                {
                    for (int b = 0; b < 8; b++)
                    {
                        Projectile.NewProjectile(Player.GetSource_Misc("Bone Revenge"), Player.position, new Vector2(Main.rand.NextFloat(-3f, 3f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 20, 4f, 0, 0, 0);
                    }
                }
                else
                {
                    for (int b = 0; b < 12; b++)
                    {
                        Projectile.NewProjectile(Player.GetSource_Misc("Bone Revenge"), Player.position, new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 40, 5f, 0, 0, 0);
                    }
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
            if (projectile.type == ProjectileID.DeathLaser && Main.rand.Next(2) == 0)
            {
                Player.AddBuff(BuffID.BrokenArmor, 180);
                Player.AddBuff(BuffID.OnFire, 180);
            }
        }

        public override void ModifyHitByProjectile(Projectile proj, ref int damage, ref bool crit)
        {
            if (UndeadTalisman)
            {
                if (proj.type == ProjectileID.SkeletonBone || proj.type == ProjectileID.Skull)
                {
                    if (!Main.expertMode)
                    {
                        damage -= 8;
                    }
                    if (Main.expertMode)
                    {
                        damage -= 4;
                    }

                    if (damage < 0) damage = 0;
                }
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (tsorcRevamp.toggleDragoonBoots.JustPressed)
            {
                DragoonBootsEnable = !DragoonBootsEnable;
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
                if (Player.statMana >= Items.Accessories.ManaShield.manaCost)
                {
                    Player.statMana -= Items.Accessories.ManaShield.manaCost;
                    Player.manaRegenDelay = Items.Accessories.ManaShield.regenDelay;
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

        public override void OnEnterWorld(Player player)
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && !gotPickaxe)
            { //sandbox mode only, and only once
                player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<DiamondPickaxe>());
                gotPickaxe = true;
            }
        }



        public override void OnRespawn(Player player)
        {
            tsorcScriptedEvents.RefreshEvents();
            player.statLife = player.statLifeMax2;
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
                multiplier += 0.20f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SoulSiphon)
            {
                multiplier += 0.2f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SOADrain)
            {
                multiplier += 0.4f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                multiplier += 0.2f;
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

        public void DoMultiCrits(ref int damage, float critType)
        {
            int critLevel = (int)(Math.Floor(critType / 100f));
            if (critLevel != 0)
            {
                if (critLevel > 1)
                {
                    for (int i = 1; i < critLevel; i++)
                    {
                        damage *= 2;
                    }
                }
                if (Main.rand.Next(1, 101) <= critType - (100 * critLevel))
                {
                    damage *= 2;
                }
            }
        }
    }
}
