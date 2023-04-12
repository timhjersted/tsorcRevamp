using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using tsorcRevamp.UI;
using Terraria.ModLoader.Config;

namespace tsorcRevamp.Items
{
    public class tsorcGlobalItem : GlobalItem
    {
        public static List<int> potionList;
        public static List<int> ammoList;
        public static List<int> torchList;
        public static List<int> hasSoulRecipe;

        public override bool CanUseItem(Item item, Player player)
        {
            if(item.type == ItemID.MagicMirror || item.type == ItemID.RecallPotion)
            {
                if (tsorcRevampWorld.BossAlive)
                {
                    Main.NewText("Can not be used while a boss is alive!", Color.Yellow);
                    return false;
                }
            }
            if(item.type == ItemID.Picksaw && !tsorcRevampWorld.SuperHardMode && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return false;
            }

            if(item.type == ItemID.SlimySaddle && !NPC.downedBoss2)
            {
                return false;
            }
            if(item.type == ItemID.QueenSlimeMountSaddle && !NPC.downedMechBoss3)
            {
                return false;
            }
            if(tsorcRevamp.RestrictedHooks.Contains(item.type) && !NPC.downedBoss3)
            {
                return false;
            }

            if (item.type == ItemID.MechanicalEye)
            {
                if (!Main.dayTime)
                {
                    if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Cataluminance>()) && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.RetinazerV2>()) && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SpazmatismV2>()))
                    {
                        //Triad
                        UsefulFunctions.BroadcastText("The Triad has spotted you", Color.MediumPurple);
                        NPC.NewNPCDirect(item.GetSource_FromThis(), (int)player.Center.X, (int)player.Center.Y - 1000, ModContent.NPCType<NPCs.Bosses.Cataluminance>());
                        NPC.NewNPCDirect(item.GetSource_FromThis(), (int)player.Center.X - 1500, (int)player.Center.Y, ModContent.NPCType<NPCs.Bosses.RetinazerV2>());
                        NPC.NewNPCDirect(item.GetSource_FromThis(), (int)player.Center.X + 1500, (int)player.Center.Y, ModContent.NPCType<NPCs.Bosses.SpazmatismV2>());
                        Projectile.NewProjectileDirect(player.GetSource_ItemUse(item), player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Triad.TriadDeath>(), 0, 0, player.whoAmI, 0, UsefulFunctions.ColorToFloat(Color.White));

                    }
                    else
                    {
                        UsefulFunctions.BroadcastText("The Triad already has you in their sights...", Color.MediumPurple);
                    }
                }
                else
                {
                    UsefulFunctions.BroadcastText("The Triad only awakens at night...", Color.MediumPurple);
                }
                return false;
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (player.GetModPlayer<tsorcRevampPlayer>().isDodging || player.GetModPlayer<tsorcRevampEstusPlayer>().isDrinking)
                {
                    return false;
                }

                if (item.damage >= 1 && item.useAnimation * .8f > player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent == player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2)
                {
                    return true;
                }
                else if (item.damage >= 1 && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < item.useAnimation * .8f && !item.CountsAsClass(DamageClass.Melee) && !(item.type == ModContent.ItemType<Weapons.Ranged.Bows.SagittariusBow>() || item.type == ModContent.ItemType<Weapons.Ranged.Bows.ArtemisBow>() || item.type == ModContent.ItemType<Weapons.Ranged.Bows.CernosPrime>()))
                {
                    return false;
                }
                else if (item.damage >= 1 && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < item.useAnimation * player.GetAttackSpeed(DamageClass.Melee) * .8f && item.CountsAsClass(DamageClass.Melee))
                {
                    return false;
                }

                if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 50 && (item.type == ModContent.ItemType<Weapons.Magic.DivineSpark>() || (item.type == ModContent.ItemType<Weapons.Magic.DivineBoomCannon>())))
                {
                    return false;
                }

                if (item.healLife > 0)
                {
                    return false;
                }
            }

            return true;

        }

        
        public override bool CanEquipAccessory(Item item, Player player, int slot, bool modded)
        {
            if(item.wingSlot < ArmorIDs.Wing.Sets.Stats.Length && item.wingSlot > 0 && !player.HasItem(ModContent.ItemType<Weapons.DebugTome>()))
            {
                if (item.type != ItemID.CreativeWings && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>())))
                {
                    return false;
                }
            }
            return base.CanEquipAccessory(item, player, slot, modded);
        }
        
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (hasSoulRecipe.Contains(item.type))
            {
                tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "RecipeTooltip", $"[i:{ModContent.ItemType<DarkSoul>()}][c/66fc03:Dark Soul recipe material]"));
            }

            if (item.wingSlot < ArmorIDs.Wing.Sets.Stats.Length && item.wingSlot > 0)
            {
                if (item.type != ItemID.CreativeWings && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>())))
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "These wings have been [c/383838:cursed] by a ferocious [c/009400:Hunter]"));
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "They can not be used until it is defeated"));
                }
            }
            

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                if (item.type == ItemID.ObsidianSkinPotion)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "No Quick Buff", "Your supply of these is finite, so they are never used by Quick Buff"));
                }

                if (item.createWall > 0)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "[c/fc1c03:Can not be placed in adventure mode!]"));
                }

                if(item.createTile > -1)
                {
                    if (tsorcRevamp.PlaceAllowed.Contains(item.createTile) || tsorcRevamp.CrossModTiles.Contains(item.createTile) || tsorcRevamp.PlaceAllowedModTiles.Contains(item.createTile))
                    {
                        tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Enabled", "[c/1cfc03:Can be placed in adventure mode!]"));
                    }
                    else
                    {
                        tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "[c/fc1c03:Can not be placed in adventure mode!]"));
                    }
                }

                if (item.type == ItemID.DirtRod)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "[c/fc1c03:This item is disabled in adventure mode!]."));
                }

                if (item.type == ItemID.Picksaw && !tsorcRevampWorld.SuperHardMode)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "This item has been [c/383838:cursed] by [c/aa00ff:Attraidies]"));
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "The only thing that will break it is his death..."));
                }

                if (tsorcRevamp.RestrictedHooks.Contains(item.type) && !NPC.downedBoss3)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "This item has been [c/383838:cursed] and can't be used yet"));
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "You can see a strange [c/878787:skull] symbol glowing on its surface..."));
                }
                if(item.type == ItemID.SlimySaddle && !NPC.downedBoss2)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "This item has been [c/383838:cursed] by the [c/701070:corruption] and can't be used yet"));
                }
                if (item.type == ItemID.QueenSlimeMountSaddle && !NPC.downedMechBoss3)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "This item has been [c/383838:cursed] by a ferocious [c/009400:Hunter]"));
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "It can not be used until it is defeated"));
                }
            }
        }

        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            return base.PreDrawInInventory(item, spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }



        public override void SetDefaults(Item item)
        {
            base.SetDefaults(item);

            //Let all accessories be used in vanity slots (Remove in 1.4.4 where this becomes vanilla behavior)
            if (item.accessory)
            {
                item.hasVanityEffects = true;
            }

            //Let all items be auto-used (Remove in 1.4.4 where this becomes toggleable vanilla behavior)
            if (item.damage >= 1 && !item.channel)
            {
                item.autoReuse = true;
            }

            if (potionList == null)
            {
                populatePotions();
            }
            if (ammoList == null)
            {
                populateAmmo();
            }
            if (torchList == null)
            {
                populateTorches();
            }
            if (potionList.Contains(item.type))
            {
                item.maxStack = 9999;
            }
            else if (ammoList.Contains(item.type))
            {
                item.maxStack = 9999;
            }
            if (torchList.Contains(item.type))
            {
                item.maxStack = 9999;
            }
        }

        public override void GrabRange(Item item, Player player, ref int grabRange)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().bossMagnet && item.type != ModContent.ItemType<DarkSoul>())
            { //bossMagnet is set on every player when a boss is killed, in NPCLoot
                grabRange *= 20;
            }

        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].aiStyle == 19 && Main.projectile[i].owner == player.whoAmI)
                {
                    Main.projectile[i].Kill();
                }
            }


            return true;
        }

        /*
        public override void UpdateVanity(Item item, Player player)
        {
            base.UpdateVanity(item, player);
        }*/

        public override bool GrabStyle(Item item, Player player)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().bossMagnet)
            { //pulling items is faster and more consistent
                Vector2 vectorItemToPlayer = player.Center - item.Center;
                Vector2 movement = vectorItemToPlayer.SafeNormalize(default) * 0.4f;
                item.velocity += movement;
            }
            return base.GrabStyle(item, player);
        }

        public override void HoldItem(Item item, Player player)
        {
            /*if (item.Prefix(mod.PrefixType("Blessed"))) //THIS LITERALY BLESSES EVERYTHING YOU TOUCH
            {
				player.lifeRegen += 1;
            }*/

            if (item.prefix == ModContent.PrefixType<Prefixes.Blessed>())
            {
                player.lifeRegen += 1;
            }
        }

        public override void MeleeEffects(Item item, Player player, Rectangle hitbox)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (modPlayer.MiakodaCrescentBoost)
            {
                int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 164, player.velocity.X * 1.2f, player.velocity.Y * 1.2f, 80, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }

            if (modPlayer.MiakodaNewBoost)
            {
                int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 57, player.velocity.X * 1.2f, player.velocity.Y * 1.2f, 120, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }

            if (modPlayer.MagicWeapon)
            {
                Lighting.AddLight(new Vector2(hitbox.X, hitbox.Y), 0.3f, 0.3f, 0.45f);
                for (int i = 0; i < 4; i++)
                {
                    int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 68, player.velocity.X * 1, player.velocity.Y * 1, 30, default(Color), .9f);
                    Main.dust[dust].noGravity = true;
                }
                {
                    int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 15, player.velocity.X * 1, player.velocity.Y * 1, 30, default(Color), .9f);
                    Main.dust[dust].noGravity = true;
                }
            }

            if (modPlayer.GreatMagicWeapon)
            {
                Lighting.AddLight(new Vector2(hitbox.X, hitbox.Y), 0.3f, 0.3f, 0.55f);
                for (int i = 0; i < 3; i++)
                {
                    int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 172, player.velocity.X * 1, player.velocity.Y * 1, 30, default(Color), .9f);
                    Main.dust[dust].noGravity = true;
                }
                {
                    int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 68, player.velocity.X * 1, player.velocity.Y * 1, 30, default(Color), .9f);
                    Main.dust[dust].noGravity = true;
                }
                {
                    int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 172, player.velocity.X * 1, player.velocity.Y * 1, 30, default(Color), 1.3f);
                    Main.dust[dust].noGravity = true;
                }
            }

            if (modPlayer.CrystalMagicWeapon)
            {
                Lighting.AddLight(new Vector2(hitbox.X, hitbox.Y), 0.3f, 0.3f, 0.55f);
                for (int i = 0; i < 2; i++)
                {
                    int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 221, player.velocity.X * 1, player.velocity.Y * 1, 30, default(Color), .9f);
                    Main.dust[dust].noGravity = true;
                }
                {
                    int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 68, player.velocity.X * 1, player.velocity.Y * 1, 30, default(Color), .9f);
                    Main.dust[dust].noGravity = true;
                }
                {
                    int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 172, player.velocity.X * 1, player.velocity.Y * 1, 30, default(Color), 1.3f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }

        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (modPlayer.MiakodaCrescentBoost)
            {
                target.AddBuff(ModContent.BuffType<Buffs.CrescentMoonlight>(), 240);
            }

            if (modPlayer.MiakodaNewBoost)
            {
                target.AddBuff(BuffID.Midas, 300);
            }

            if (modPlayer.MagicWeapon || modPlayer.GreatMagicWeapon)
            {
                SoundStyle WeaponSound = SoundID.NPCHit44;
                WeaponSound.Volume = 0.3f;
                SoundEngine.PlaySound(WeaponSound, target.position);
            }

            if (modPlayer.CrystalMagicWeapon)
            {
                SoundStyle WeaponSound = SoundID.Item27;
                WeaponSound.Volume = 0.3f;
                SoundEngine.PlaySound(WeaponSound, target.position);
            }

            if (item.type == ItemID.DD2SquireBetsySword)
            {
                target.AddBuff(BuffID.BetsysCurse, 600);
            }
        }

        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (item.CountsAsClass(DamageClass.Melee))
            {
                /*Main.NewText("magicDamage: " + player.GetDamage(DamageClass.Magic));
				Main.NewText("magicDamageMult: " + player.GetDamage(DamageClass.Magic)Mult);
				Main.NewText((player.GetDamage(DamageClass.Magic) - player.GetDamage(DamageClass.Magic)Mult) * .5f);*/

                if (modPlayer.MagicWeapon)
                {
                    float bonusDamage = ((player.GetDamage(DamageClass.Magic).Additive * player.GetDamage(DamageClass.Magic).Multiplicative) - 1) * .5f;
                    if (bonusDamage >= 0)
                    {
                        player.GetDamage(DamageClass.Melee) += bonusDamage; 
                    }
                }
                if (modPlayer.GreatMagicWeapon)
                {
                    float bonusDamage = ((player.GetDamage(DamageClass.Magic).Additive * player.GetDamage(DamageClass.Magic).Multiplicative) - 1) * .75f;
                    if (bonusDamage >= 0)
                    {
                        player.GetDamage(DamageClass.Melee) += bonusDamage; 
                    }
                }
                if (modPlayer.CrystalMagicWeapon)
                {
                    float bonusDamage = (player.GetDamage(DamageClass.Magic).Additive * player.GetDamage(DamageClass.Magic).Multiplicative) * 1f;
                    if (bonusDamage >= 0)
                    {
                        player.GetDamage(DamageClass.Melee) += bonusDamage;
                    }
                }
            }

            if (modPlayer.BearerOfTheCurse && item.pick != 0)
            {
                damage *= 0.5f;
            }
        }

        public override bool OnPickup(Item item, Player player)
        {
            if (PotionBagUIState.IsValidPotion(item) && player.HasItem(ModContent.ItemType<PotionBag>()))
            {
                Item[] PotionItems = player.GetModPlayer<tsorcRevampPlayer>().PotionBagItems;
                int? emptySlot = null;
                for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
                {
                    if (PotionItems[i].type == 0 && emptySlot == null)
                    {
                        emptySlot = i;
                    }
                    if (PotionItems[i].type == item.type && (PotionItems[i].stack + item.stack) <= PotionItems[i].maxStack)
                    {
                        PotionItems[i].stack += item.stack;
                        string itemText = item.Name;
                        if (item.stack > 1)
                        {
                            itemText += " (" + item.stack + ")";
                        }
                        CombatText.NewText(player.Hitbox, Color.Purple, itemText);
                        SoundEngine.PlaySound(SoundID.Grab);
                        SoundEngine.PlaySound(SoundID.Item8);
                        return false;
                    }
                }

                //If it got here, that means there's no existing stacks with room
                //So go through it again, finding the first empty slot instead
                if (emptySlot != null)
                {
                    PotionItems[emptySlot.Value] = item;
                    string itemText = item.Name;
                    if (item.stack > 1)
                    {
                        itemText += " (" + item.stack + ")";
                    }
                    CombatText.NewText(player.Hitbox, Color.Purple, itemText);
                    SoundEngine.PlaySound(SoundID.Grab);
                    SoundEngine.PlaySound(SoundID.Item8);
                    return false;
                }
            }
                       
            return base.OnPickup(item, player);
        }

        #region PrefixChance (taken from Example Mod, leaving most original comments in)

        public override bool? PrefixChance(Item item, int pre, UnifiedRandom rand)
        {
            // pre: The prefix being applied to the item, or the roll mode
            // -1 is when an item is naturally generated in a chest, crafted, purchased from an NPC, looted from a grab bag (excluding presents), or dropped by a slain enemy
            // -2 is when an item is rolled in the tinkerer
            // -3 determines if an item can be placed in the tinkerer slot

            // To prevent putting an item in the tinkerer slot, return false when pre is -3
            /*if (pre == -3 && item.type == ItemID.LaserRifle)
			{
				// This will make the Laser Rifle not be reforgeable at all (useful if you want your item to preserve its custom name color)
				return false;
			}*/

            // To make an item reset its prefix when reforging
            /*if (pre == -2)
			{
				if (Main.LocalPlayer.HasBuff(BuffID.Tipsy))
				{
					// If the player is drunk, make it remove the prefix
					return false;
				}
			}*/

            // To prevent rolling of a prefix on spawn, return false when pre is -1
            if (pre == -1)
            {
                if (item.ModItem?.Mod == Mod)
                {
                    // All weapons/accesories from tsorcRevamp can have a prefix when they are crafted, bought, taken from a generated chest, opened, or dropped by an enemy
                    return true;
                }
            }

            // For the following code, this is useful to know (from the terraria wiki):
            // Nearly all weapons and accessories have a 75% chance of receiving a random modifier upon the item's creation
            // (naturally generated in a chest, crafted, purchased from an NPC, looted from a grab bag (excluding presents), or dropped by a slain enemy).

            // To change the chance of a prefix being rolled or not, return true or false depending on some condition
            /*if (pre == -1 && item.type == ItemID.Shackle)
			{
				// Force rolling
				// return true;

				// When using random numbers, make sure to use the rand object passed into this method, and not Main.rand.
				// This will make it consistent with worldgen should this item be spawned in a chest
				if (rand.NextFloat() < 0.5f)
				{
					// Increase the chance of not receiving any prefix on spawn by 50%
					return false;
				}
				// Keep in mind that if the code arrives here, there is still a 25% chance that it won't get a modifier.
				// If you want a more controlled approach, return true in an else block
			}*/

            return null;
        }

        #endregion

        private void populatePotions()
        {
            potionList = new List<int>()
            {
                ItemID.LesserHealingPotion,
				ItemID.LesserManaPotion,
				ItemID.LesserRestorationPotion,
                ItemID.HealingPotion,
			    ItemID.ManaPotion,
				ItemID.RestorationPotion,
				ItemID.GreaterHealingPotion,
				ItemID.GreaterManaPotion,
				ItemID.SuperHealingPotion,
				ItemID.SuperManaPotion,

				ItemID.BowlofSoup,
                ItemID.SwiftnessPotion,
                ItemID.AmmoReservationPotion,
                ItemID.ArcheryPotion,
                ItemID.BattlePotion,
                ItemID.BuilderPotion,
                ItemID.CalmingPotion,
                ItemID.CratePotion,
                ItemID.TrapsightPotion,
                ItemID.EndurancePotion,
                ItemID.FeatherfallPotion,
                ItemID.FishingPotion,
                ItemID.FlipperPotion,
                ItemID.GillsPotion,
                ItemID.GravitationPotion,
                ItemID.HeartreachPotion,
                ItemID.HunterPotion,
                ItemID.InfernoPotion,
                ItemID.InvisibilityPotion,
                ItemID.IronskinPotion,
                ItemID.LifeforcePotion,
                ItemID.MagicPowerPotion,
                ItemID.ManaRegenerationPotion,
                ItemID.MiningPotion,
                ItemID.NightOwlPotion,
                ItemID.ObsidianSkinPotion,
                ItemID.RagePotion,
                ItemID.RegenerationPotion,
                ItemID.ShinePotion,
                ItemID.SonarPotion,
                ItemID.SpelunkerPotion,
                ItemID.SummoningPotion,
                ItemID.SwiftnessPotion,
                ItemID.ThornsPotion,
                ItemID.TitanPotion,
                ItemID.WarmthPotion,
                ItemID.WaterWalkingPotion,
                ItemID.WrathPotion,

                ItemID.FlaskofCursedFlames,
                ItemID.FlaskofFire,
                ItemID.FlaskofGold,
                ItemID.FlaskofIchor,
                ItemID.FlaskofNanites,
                ItemID.FlaskofParty,
                ItemID.FlaskofPoison,
                ItemID.FlaskofVenom,

                ItemID.GenderChangePotion,
                ItemID.RecallPotion,
                ItemID.TeleportationPotion,
                ItemID.WormholePotion,
                ItemID.RedPotion
            };
        }
        private void populateAmmo()
        {
            ammoList = new List<int>()
            {
                ItemID.MusketBall,
                ItemID.MeteorShot,
                ItemID.SilverBullet,
                ItemID.CursedBullet,
                ItemID.CrystalBullet,
                ItemID.ChlorophyteBullet,
                ItemID.HighVelocityBullet,
                ItemID.IchorBullet,
                ItemID.VenomBullet,
                ItemID.PartyBullet,
                ItemID.NanoBullet,
                ItemID.ExplodingBullet,
                ItemID.GoldenBullet,
                ItemID.MoonlordBullet,

                ItemID.WoodenArrow,
                ItemID.FlamingArrow,
                ItemID.UnholyArrow,
                ItemID.JestersArrow,
                ItemID.HellfireArrow,
                ItemID.HolyArrow,
                ItemID.CursedArrow,
                ItemID.FrostburnArrow,
                ItemID.ChlorophyteArrow,
                ItemID.IchorArrow,
                ItemID.VenomArrow,
                ItemID.BoneArrow,
                ItemID.MoonlordArrow,

                ItemID.RocketI,
                ItemID.RocketII,
                ItemID.RocketIII,
                ItemID.RocketIV,

                ItemID.PoisonDart,
                ItemID.CrystalDart,
                ItemID.CursedDart,
                ItemID.IchorDart,

                ItemID.FallenStar,
                ItemID.Gel,
                ItemID.Seed,
                ItemID.StyngerBolt,
                ItemID.CandyCorn,
                ItemID.ExplosiveJackOLantern,
                ItemID.Stake,
                ItemID.Flare,
                ItemID.BlueFlare,
                ItemID.Snowball,
                ItemID.Nail,

				//yes clearly this is not ammo but im gonna have a stroke if i have to carry stacks of 99 torches
				ItemID.Torch
            };
        }
        private void populateTorches()
        {
            torchList = new List<int> {
                ItemID.Torch,
                ItemID.PurpleTorch,
                ItemID.YellowTorch,
                ItemID.BlueTorch,
                ItemID.GreenTorch,
                ItemID.RedTorch,
                ItemID.OrangeTorch,
                ItemID.WhiteTorch,
                ItemID.IceTorch,
                ItemID.PinkTorch,
                ItemID.BoneTorch,
                ItemID.UltrabrightTorch,
                ItemID.DemonTorch,
                ItemID.CursedTorch,
                ItemID.IchorTorch,
                ItemID.RainbowTorch,
                ItemID.DesertTorch,
                ItemID.CoralTorch,
                ItemID.CorruptTorch,
                ItemID.CrimsonTorch,
                ItemID.HallowedTorch,
                ItemID.JungleTorch
            };
        }

        public override void OnConsumeItem(Item item, Player player) {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.consumedPotions ??= new Dictionary<ItemDefinition, int>();

            bool isPotion = false;
            if (potionList.Contains(item.type)) {
                isPotion = true;
            }

            if (!isPotion) {
                if (item.buffType > 0 && item.consumable) {
                    isPotion = true;
                }
            }
            

            if (isPotion) {
                ItemDefinition pot = new(item.type);
                if (modPlayer.consumedPotions.ContainsKey(pot)) {
                    modPlayer.consumedPotions[pot] += 1;
                }
                else {
                    modPlayer.consumedPotions.Add(pot, 1);
                }
            }
        }

        public static void populateSoulRecipes()
        {
            hasSoulRecipe = new List<int>();

            for(int i = 0; i < Recipe.numRecipes; i++)
            {
                if (Main.recipe[i].HasIngredient<DarkSoul>())
                {
                    for (int j = 1; j < ItemLoader.ItemCount; j++)
                    {
                        if (Main.recipe[i].HasIngredient(j))
                        {
                            hasSoulRecipe.Add(j);
                        }
                    }
                }
            }
        }
    }
}