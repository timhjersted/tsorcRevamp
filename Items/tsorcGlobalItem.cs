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

namespace tsorcRevamp.Items
{
    public class tsorcGlobalItem : GlobalItem
    {
        public static List<int> potionList;
        public static List<int> ammoList;
        public static List<int> torchList;

        public override bool CanUseItem(Item item, Player player)
        {
            if(item.type == ItemID.Picksaw && !tsorcRevampWorld.SuperHardMode)
            {
                return false;
            }
            if (item.type == ItemID.SlimeHook && !tsorcRevampWorld.SuperHardMode)
            {
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
                else if (item.damage >= 1 && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < item.useAnimation * .8f && !item.CountsAsClass(DamageClass.Melee) && !(item.type == ModContent.ItemType<Weapons.Ranged.SagittariusBow>() || item.type == ModContent.ItemType<Weapons.Ranged.ArtemisBow>() || item.type == ModContent.ItemType<Weapons.Ranged.CernosPrime>()))
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
            if(item.wingSlot < ArmorIDs.Wing.Sets.Stats.Length && item.wingSlot > 0)
            {
                if (!NPC.downedMechBoss3)
                {
                    return false;
                }
            }
            return base.CanEquipAccessory(item, player, slot, modded);
        }
        
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.Picksaw && !tsorcRevampWorld.SuperHardMode)
            {
                tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "This item has been [c/383838:cursed] by [c/aa00ff:Attraidies]"));
                tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "It can not be used while he still lives..."));
            }

            if (item.wingSlot < ArmorIDs.Wing.Sets.Stats.Length && item.wingSlot > 0)
            {
                if (!NPC.downedMechBoss3)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "These wings have been [c/383838:cursed] by a fierce [c/949494:Machine]"));
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "They are now heavier than steel, and can not be used until it is defeated"));
                }
            }

            
            if (item.type == ItemID.SlimeHook && !tsorcRevampWorld.SuperHardMode)
            {
                tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "This item has been [c/383838:cursed] and can't be used"));
                tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", "You can see a strange [c/878787:skull] symbol glowing on its surface..."));
            }
        }

        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            return base.PreDrawInInventory(item, spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }

        public override void SetDefaults(Item item)
        {
            base.SetDefaults(item);
            if (item.accessory)
            {
                item.canBePlacedInVanityRegardlessOfConditions = true;
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
            if (item.type == ItemID.NebulaBlaze)
            {
                item.damage = (int)Math.Round(0.7f * item.damage);
            }
            if (item.type == ItemID.NebulaArcanum)
            {
                item.damage = (int)Math.Round(0.5f * item.damage);
            }
            if (item.type == ItemID.VortexBeater || item.type == ItemID.Phantasm)
            {
                item.damage = (int)Math.Round(0.7f * item.damage);
            }
            if (item.type == ItemID.DayBreak || item.type == ItemID.SolarEruption)
            {
                item.damage = (int)Math.Round(0.5f * item.damage);
            }

            if (item.damage >= 1 && !item.channel)
            {
                item.autoReuse = true;
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

            if (item.prefix == Mod.Find<ModPrefix>("Blessed").Type)
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

        public override void OnHitNPC(Item item, Player player, NPC target, int damage, float knockBack, bool crit)
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
                    float bonusDamage = (player.GetDamage(DamageClass.Magic).Additive - player.GetDamage(DamageClass.Magic).Multiplicative) * .5f;
                    if (bonusDamage >= 0) { player.GetDamage(DamageClass.Melee) *= (1 + bonusDamage); }

                    if (player.statManaMax2 >= 100)
                    {
                        player.GetDamage(DamageClass.Melee).Flat += (player.statManaMax2 - 100) / 60;
                    }
                }
                if (modPlayer.GreatMagicWeapon)
                {
                    float bonusDamage = (player.GetDamage(DamageClass.Magic).Additive - player.GetDamage(DamageClass.Magic).Multiplicative) * .75f;
                    if (bonusDamage >= 0) { player.GetDamage(DamageClass.Melee) *= (1 + bonusDamage); }

                    if (player.statManaMax2 >= 100)
                    {
                        player.GetDamage(DamageClass.Melee).Flat += (player.statManaMax2 - 100) / 40;
                    }
                }
                if (modPlayer.CrystalMagicWeapon)
                {
                    float bonusDamage = (player.GetDamage(DamageClass.Magic).Additive - player.GetDamage(DamageClass.Magic).Multiplicative) * 1f;
                    if (bonusDamage >= 0) { player.GetDamage(DamageClass.Melee) *= (1 + bonusDamage); }

                    if (player.statManaMax2 >= 100)
                    {
                        player.GetDamage(DamageClass.Melee).Flat += (player.statManaMax2 - 100) / 20;
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
    }
}