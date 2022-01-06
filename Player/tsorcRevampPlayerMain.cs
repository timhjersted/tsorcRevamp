using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Items;
using tsorcRevamp.Buffs;
using System;
using TerraUI.Objects;
using Terraria.UI;
using Terraria.Audio;
using tsorcRevamp.Projectiles.Pets;
using Terraria.Localization;
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

        public override void clientClone(ModPlayer clientClone) {
            tsorcRevampPlayer clone = clientClone as tsorcRevampPlayer;
            if (clone == null) { return; }

            clone.SoulSlot.Item = SoulSlot.Item.Clone();
        }

        public override void SendClientChanges(ModPlayer clientPlayer) {
            tsorcRevampPlayer oldClone = clientPlayer as tsorcRevampPlayer;
            if (oldClone == null) { return; }

            if (oldClone.SoulSlot.Item.IsNotTheSameAs(SoulSlot.Item)) {
                SendSingleItemPacket(1, SoulSlot.Item, -1, player.whoAmI);
            }
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {

            //Sync soul slot
            ModPacket packet = mod.GetPacket();
            packet.Write((byte)tsorcPacketID.SyncSoulSlot);
            packet.Write((byte)player.whoAmI);
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

        public Item[] PotionBagItems = new Item[PotionBagUIState.POTION_BAG_SIZE];
        public override TagCompound Save()
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
                {"soulSlot", ItemIO.Save(SoulSlot.Item)}
            };

            List<Item> PotionBagList = new List<Item>();
            foreach (Item thisItem in PotionBagItems)
            {
                PotionBagList.Add(thisItem);
            }

            tsorcTagCompound.Add("PotionBag", PotionBagList);

            return tsorcTagCompound;
        }

        public override void Load(TagCompound tag)
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

            IList<Item> PotionBagIList = tag.GetList<Item>("PotionBag");
            List<Item> PotionTagList = (List<Item>)PotionBagIList;

            for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
            {
                if(PotionTagList == null || PotionTagList.Count <= i || PotionTagList[i] == null)
                {
                    PotionBagItems[i] = new Item();
                    PotionBagItems[i].SetDefaults(0);
                }
                else
                {
                    PotionBagItems[i] = PotionTagList[i];
                    if (PotionBagItems[i] == null)
                    {
                        PotionBagItems[i] = new Item();
                        PotionBagItems[i].SetDefaults(0);
                    }
                }
            }
        }

        public void SetDirection() => SetDirection(false);

        private void SetDirection(bool resetForcedDirection) {
            if (!Main.dedServ && Main.gameMenu) {
                player.direction = 1;

                return;
            }

            if (!player.pulley && (!player.mount.Active || player.mount.AllowDirectionChange) && (player.itemAnimation <= 1)) {
                if (forcedDirection != 0) {
                    player.direction = forcedDirection;

                    if (resetForcedDirection) {
                        forcedDirection = 0;
                    }
                }
            }
        }
        public override bool PreHurt(bool pvp, bool quiet, ref int damage, ref int hitDirection, ref bool crit, ref bool customDamage, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource) {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                player.AddBuff(ModContent.BuffType<InCombat>(), 600); //10s 
            }
            return base.PreHurt(pvp, quiet, ref damage, ref hitDirection, ref crit, ref customDamage, ref playSound, ref genGore, ref damageSource);
        }

        public override void OnHitAnything(float x, float y, Entity victim) {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                player.AddBuff(ModContent.BuffType<InCombat>(), 600); //10s 
            }
            base.OnHitAnything(x, y, victim);
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
            Projectile.NewProjectile(player.Bottom, new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Bloodsign>(), 0, 0, player.whoAmI);
            Main.PlaySound(SoundID.NPCDeath58.WithVolume(0.8f).WithPitchVariance(.3f), player.position);


            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.statLifeMax > 200)
            {
                player.statLifeMax -= 20;
            }

            bool onePlayerAlive = false;
            for(int i = 0; i < Main.maxPlayers; i++)
            {
                if(Main.player[i].active && !Main.player[i].dead)
                {
                    onePlayerAlive = true;
                }
            }

            if (!onePlayerAlive)
            {
                NPC.ShieldStrengthTowerNebula = NPC.ShieldStrengthTowerMax;
                NPC.ShieldStrengthTowerSolar = NPC.ShieldStrengthTowerMax;
                NPC.ShieldStrengthTowerVortex = NPC.ShieldStrengthTowerMax;
                NPC.ShieldStrengthTowerStardust = NPC.ShieldStrengthTowerMax;
            }
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
                    player.HealEffect(10);
                    player.statLife += (10);

                }
            }
            if (NUVamp)
            {
                if (Main.rand.Next(5) == 0)
                {
                    player.HealEffect(damage / 4);
                    player.statLife += (damage / 4);
                }
            }
            if (MiakodaFull)
            { //Miakoda Full Moon
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit)
                    {

                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal2 = true;

                        int HealAmount = (int)((Math.Floor((double)(player.statLifeMax2 / 100)) * 2) + 2);
                        player.statLife += HealAmount;
                        player.HealEffect(HealAmount, false);
                        if (player.statLife > player.statLifeMax2)
                        {
                            player.statLife = player.statLifeMax2;
                        }

                        Main.PlaySound(SoundID.Item30.WithVolume(.7f), player.Center);

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
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust2 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentBoost = true;

                        Main.PlaySound(SoundID.Item100.WithVolume(.75f), player.Center);

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
            if (MiakodaFull)
            { //Miakoda Full Moon
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit || (proj.minion && Main.player[proj.owner].HeldItem.summon))
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaFullHeal2 = true;



                        //2 per 100 max hp, plus 2
                        int HealAmount = (int)((Math.Floor((double)(player.statLifeMax2 / 100)) * 2) + 2);
                        player.statLife += HealAmount;
                        player.HealEffect(HealAmount, false);
                        if (player.statLife > player.statLifeMax2)
                        {
                            player.statLife = player.statLifeMax2;
                        }

                        Main.PlaySound(SoundID.Item30.WithVolume(.7f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }

            if (MiakodaCrescent)
            { //Miakoda Crescent Moon
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit || (proj.minion && Main.player[proj.owner].HeldItem.summon))
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust2 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentBoost = true;

                        Main.PlaySound(SoundID.Item100.WithVolume(.75f), player.Center);

                        MiakodaEffectsTimer = 0;
                    }
                }
            }

            if (MiakodaNew)
            { //Miakoda New Moon
                if (MiakodaEffectsTimer > 720)
                {
                    if (crit || (proj.minion && Main.player[proj.owner].HeldItem.summon))
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewDust1 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewDust2 = true;
                        player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewBoost = true;

                        Main.PlaySound(SoundID.Item81.WithVolume(.75f), player.Center);

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
            if (crit) {
                if (item.melee) {
                    DoMultiCrits(ref damage, player.meleeCrit);
                }
                else if (item.magic) {
                    DoMultiCrits(ref damage, player.magicCrit);
                }
                else if (item.ranged) {
                    DoMultiCrits(ref damage, player.rangedCrit);
                }
                else if (item.thrown) {
                    DoMultiCrits(ref damage, player.thrownCrit); //lol
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
            if (((proj.type == ProjectileID.MoonlordArrow) || (proj.type == ProjectileID.MoonlordArrowTrail)) && player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.CernosPrime>())
            {
                damage = (int)(damage * 0.55);
            }

            if (crit) {
                if (proj.melee) {
                    DoMultiCrits(ref damage, player.meleeCrit);
                }
                else if (proj.magic) {
                    DoMultiCrits(ref damage, player.magicCrit);
                }
                else if (proj.ranged) {
                    DoMultiCrits(ref damage, player.rangedCrit);
                }
                else if (proj.thrown) {
                    DoMultiCrits(ref damage, player.thrownCrit); //lol
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
            if (player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge)
            {
                if (!Main.hardMode)
                {
                    for (int b = 0; b < 8; b++)
                    {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3f, 3f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 20, 4f, 0, 0, 0);
                    }
                }
                else
                {
                    for (int b = 0; b < 12; b++)
                    {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 40, 5f, 0, 0, 0);
                    }
                }
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().SoulSickle)
            {
                if (!Main.hardMode)
                {
                    Projectile.NewProjectile(player.Center, new Vector2(player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), 40, 7f, 0, 0, 0);
                }
                else
                {
                    Projectile.NewProjectile(player.Center, new Vector2(player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), 80, 9f, 0, 0, 0);
                }
            }
            if (npc.type == NPCID.SkeletronPrime && Main.rand.Next(2) == 0)
            {
                player.AddBuff(BuffID.Bleeding, 1800);
                player.AddBuff(BuffID.OnFire, 600);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, bool crit)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge)
            {
                if (!Main.hardMode)
                {
                    for (int b = 0; b < 8; b++)
                    {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3f, 3f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 20, 4f, 0, 0, 0);
                    }
                }
                else
                {
                    for (int b = 0; b < 12; b++)
                    {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 40, 5f, 0, 0, 0);
                    }
                }
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().SoulSickle)
            {
                if (!Main.hardMode)
                {
                    Projectile.NewProjectile(player.Center, new Vector2(player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), 40, 6f, 0, 0, 0);
                }
                else
                {
                    Projectile.NewProjectile(player.Center, new Vector2(player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<Projectiles.SoulSickle>(), 60, 8f, 0, 0, 0);
                }
            }
            if (projectile.type == ProjectileID.DeathLaser && Main.rand.Next(2) == 0)
            {
                player.AddBuff(BuffID.BrokenArmor, 180);
                player.AddBuff(BuffID.OnFire, 180);
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
            if (tsorcRevamp.reflectionShiftKey.JustPressed) { 
                if (ReflectionShiftEnabled)
                {
                    if (player.controlUp)
                    {
                        ReflectionShiftState.Y = -1;
                    }
                    if (player.controlLeft)
                    {
                        ReflectionShiftState.X = -1;
                    }
                    if (player.controlRight)
                    {
                        ReflectionShiftState.X = 1;
                    }
                    if (player.controlDown)
                    {
                        ReflectionShiftState.Y = 1;
                    }
                }
        }}

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

        public override void OnEnterWorld(Player player)
        {
            if (Main.worldID == VariousConstants.CUSTOM_MAP_WORLD_ID)
            {
                if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                {
                    Main.NewText("Custom map detected. Adventure Mode auto-enabled.", Color.GreenYellow);
                    ModContent.GetInstance<tsorcRevampConfig>().AdventureMode = true;
                }
                if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
                {
                    Main.NewText("Warning!! The setting 'Adventure Mode: Recipes and Items' is disabled!!", Color.OrangeRed);
                    Main.NewText("To prevent issues with the custom map, please enable this setting and reload mods!", Color.OrangeRed);
                }
            }
            else
            {
                if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                {
                    Main.NewText("Randomly-generated map detected. Adventure Mode auto-disabled.", Color.GreenYellow);
                    ModContent.GetInstance<tsorcRevampConfig>().AdventureMode = false;
                }
                if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
                {
                    Main.NewText("Warning!! The setting 'Adventure Mode: Recipes and Items' is enabled!!", Color.OrangeRed);
                    Main.NewText("This is intended for the custom map. To prevent issues, please disable this setting and reload mods!", Color.OrangeRed);
                }
            }

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    tsorcRevampWorld.CampfireToBonfire();
                }
            }

            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && !gotPickaxe)
            { //sandbox mode only, and only once
                player.QuickSpawnItem(ModContent.ItemType<DiamondPickaxe>());
                gotPickaxe = true;
            }
            if (Main.worldID == VariousConstants.CUSTOM_MAP_WORLD_ID && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.EmeraldHerald>()))
            {
                NPC.NewNPC((int)player.position.X + 3000, (int)player.position.Y, ModContent.NPCType<NPCs.Friendly.EmeraldHerald>());
            }
        }

        public override void OnRespawn(Player player)
        {
            tsorcScriptedEvents.RefreshEvents();
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
                multiplier += 0.2f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SOADrain) {
                multiplier += 0.4f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                multiplier += 0.2f;
            }
            return multiplier;
        }

        public void DoPortableChest<T>(ref int whoAmI, ref bool toggle) where T : BonfireProjectiles, new() {
            int projectileType = ModContent.ProjectileType<T>();
            T instance = ModContent.GetInstance<T>();
            int bankID = instance.ChestType;
            LegacySoundStyle useSound = instance.UseSound;

            if (Main.projectile[whoAmI].active && Main.projectile[whoAmI].type == projectileType) {
                int oldChest = player.chest;
                player.chest = bankID;
                toggle = true;

                int num17 = (int)((player.position.X + player.width * 0.5) / 16.0);
                int num18 = (int)((player.position.Y + player.height * 0.5) / 16.0);
                player.chestX = (int)Main.projectile[whoAmI].Center.X / 16;
                player.chestY = (int)Main.projectile[whoAmI].Center.Y / 16;
                if ((oldChest != bankID && oldChest != -1) || num17 < player.chestX - Player.tileRangeX || num17 > player.chestX + Player.tileRangeX + 1 || num18 < player.chestY - Player.tileRangeY || num18 > player.chestY + Player.tileRangeY + 1) {
                    whoAmI = -1;
                    if (player.chest != -1) {
                        Main.PlaySound(useSound);
                    }

                    if (oldChest != bankID)
                        player.chest = oldChest;
                    else
                        player.chest = -1;

                    Recipe.FindRecipes();
                }
            }
            else {


                whoAmI = -1;
                player.chest = -1; //none
                Recipe.FindRecipes();
            }
        }

        internal void SendSingleItemPacket(int message, Item item, int toWho, int fromWho) {
            ModPacket packet = mod.GetPacket();
            packet.Write((byte)message);
            packet.Write((byte)player.whoAmI);
            ItemIO.Send(item, packet);
            packet.Send(toWho, fromWho);
        }

        public void DoMultiCrits(ref int damage, int critType) {
            int critLevel = (int)(Math.Floor(critType / 100f));
            if (critLevel != 0) {
                if (critLevel > 1) {
                    for (int i = 1; i < critLevel; i++) {
                        damage *= 2;
                    }
                }
                if (Main.rand.Next(1, 101) <= critType - (100 * critLevel)) {
                    damage *= 2;
                } 
            }
        }
    }
}
