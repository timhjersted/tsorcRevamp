using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Debug;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.UI;

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
            if (item.type == ItemID.MagicMirror || item.type == ItemID.RecallPotion)
            {
                if (tsorcRevampWorld.BossAlive)
                {
                    Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.UnusableDuringBoss"), Color.Yellow);
                    return false;
                }
            }
            if (item.type == ItemID.Picksaw && !tsorcRevampWorld.SuperHardMode && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return false;
            }

            if (item.type == ItemID.SlimySaddle && !NPC.downedBoss2)
            {
                return false;
            }
            if (item.type == ItemID.QueenSlimeMountSaddle && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>())))
            {
                return false;
            }
            if (tsorcRevamp.RestrictedHooks.Contains(item.type) && !NPC.downedBoss3)
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
                        UsefulFunctions.BroadcastText(Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MechanicalEye.OnUseText1"), Color.MediumPurple);
                        NPC.NewNPCDirect(item.GetSource_FromThis(), (int)player.Center.X, (int)player.Center.Y - 1000, ModContent.NPCType<NPCs.Bosses.Cataluminance>());
                        NPC.NewNPCDirect(item.GetSource_FromThis(), (int)player.Center.X - 1500, (int)player.Center.Y, ModContent.NPCType<NPCs.Bosses.RetinazerV2>());
                        NPC.NewNPCDirect(item.GetSource_FromThis(), (int)player.Center.X + 1500, (int)player.Center.Y, ModContent.NPCType<NPCs.Bosses.SpazmatismV2>());
                        Projectile.NewProjectileDirect(player.GetSource_ItemUse(item), player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, player.whoAmI, 0, UsefulFunctions.ColorToFloat(Color.White));

                    }
                    else
                    {
                        UsefulFunctions.BroadcastText(Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MechanicalEye.OnUseText2"), Color.MediumPurple);
                    }
                }
                else
                {
                    UsefulFunctions.BroadcastText(Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MechanicalEye.OnUseText3"), Color.MediumPurple);
                }
                return false;
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if ((player.GetModPlayer<tsorcRevampPlayer>().isDodging && !player.GetModPlayer<tsorcRevampPlayer>().CanUseItemsWhileDodging) || player.GetModPlayer<tsorcRevampEstusPlayer>().isDrinking || player.GetModPlayer<tsorcRevampCeruleanPlayer>().isDrinking)
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
            if (item.wingSlot < ArmorIDs.Wing.Sets.Stats.Length && item.wingSlot > 0 && !player.HasItem(ModContent.ItemType<DebugTome>()))
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
                tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "RecipeTooltip", $"[i:{ModContent.ItemType<DarkSoul>()}]" + Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.RecipeTooltip")));
            }

            if (item.wingSlot < ArmorIDs.Wing.Sets.Stats.Length && item.wingSlot > 0)
            {
                if (item.type != ItemID.CreativeWings && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>())))
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.WingsDisabled")));
                }
            }


            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                if (item.type == ItemID.ObsidianSkinPotion || item.type == ItemID.WaterWalkingPotion)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "No Quick Buff", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.ObsidianSkinPotion")));
                }

                if (item.createWall > 0)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.TileDisabled")));
                }

                if (item.createTile > -1)
                {
                    if (tsorcRevamp.PlaceAllowed.Contains(item.createTile) || tsorcRevamp.CrossModTiles.Contains(item.createTile) || tsorcRevamp.PlaceAllowedModTiles.Contains(item.createTile))
                    {
                        tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Enabled", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.TileEnabled")));
                    }
                    else
                    {
                        tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.TileDisabled")));
                    }
                }

                if (item.type == ItemID.DirtRod)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.ItemDisabled")));
                }

                if (item.type == ItemID.Picksaw && !tsorcRevampWorld.SuperHardMode)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.AttraidiesCursed")));
                }

                if (tsorcRevamp.RestrictedHooks.Contains(item.type) && !NPC.downedBoss3)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.Cursed")));
                }
                if (item.type == ItemID.SlimySaddle && !NPC.downedBoss2)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.CorruptionCursed")));
                }
                if (item.type == ItemID.QueenSlimeMountSaddle && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>())))
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.WingsDisabled")));
                }
            }
        }

        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            return base.PreDrawInInventory(item, spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }



        public override void SetDefaults(Item item)
        {

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
                item.maxStack = Item.CommonMaxStack;
            }
            else if (ammoList.Contains(item.type))
            {
                item.maxStack = Item.CommonMaxStack;
            }
            if (torchList.Contains(item.type))
            {
                item.maxStack = Item.CommonMaxStack;
            }
        }

        public override void GrabRange(Item item, Player player, ref int grabRange)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().bossMagnet && item.type != ModContent.ItemType<DarkSoul>())
            { //bossMagnet is set on every player when a boss is killed, in NPCLoot
                grabRange *= 20;
            }
            if (player.manaMagnet && item.type == ItemID.ManaCloakStar)
            {
                grabRange = Item.manaGrabRange;
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

            //Spawn the melee slash vfx
            if (player.ItemAnimationJustStarted && player.whoAmI == Main.myPlayer)
            {
                //No slash effect if any of these is true
                if (!(item.useStyle != ItemUseStyleID.Swing || item.noMelee || item.noUseGraphic || item.pick > 0 || item.createTile >= TileID.Dirt || item.createWall >= 0 || item.damage <= 0))
                {
                    Projectile.NewProjectile(item.GetSource_FromThis(), player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.Slash>(), 0, 1, player.whoAmI);
                }
            }

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
            /*for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];

                if ((item.DamageType == DamageClass.Melee || item.DamageType == DamageClass.MeleeNoSpeed) && modPlayer.BearerOfTheCurse
                    && other.active && !other.friendly && other.hostile && UsefulFunctions.IsProjectileSafeToFuckWith(i) && other.type != ModContent.ProjectileType<Nothing>() && other.type != ModContent.ProjectileType<Slash>()
                    && !other.GetGlobalProjectile<tsorcGlobalProjectile>().AppliedLethalTempo && hitbox.Intersects(other.Hitbox))
                {
                    if (modPlayer.BotCLethalTempoStacks < modPlayer.BotCLethalTempoMaxStacks - 1)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.4f }, player.Center);
                    }
                    else if (modPlayer.BotCLethalTempoStacks == modPlayer.BotCLethalTempoMaxStacks - 1)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 2f }, player.Center);
                    }
                    player.AddBuff(ModContent.BuffType<LethalTempo>(), modPlayer.BotCLethalTempoDuration * 60);
                    other.GetGlobalProjectile<tsorcGlobalProjectile>().AppliedLethalTempo = true;
                }
            }*/
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
                target.AddBuff(BuffID.Midas, 5 * 60);
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
            #region Lethal Tempo
            if ((item.DamageType == DamageClass.Melee || item.DamageType == DamageClass.MeleeNoSpeed) && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (modPlayer.BotCLethalTempoStacks < modPlayer.BotCLethalTempoMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.00375f }, player.Center);
                }
                else if (modPlayer.BotCLethalTempoStacks == modPlayer.BotCLethalTempoMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.0045f }, player.Center);
                }
                player.AddBuff(ModContent.BuffType<LethalTempo>(), player.GetModPlayer<tsorcRevampPlayer>().BotCLethalTempoDuration * 60);
            }
            #endregion
            #region Conqueror
            if (item.DamageType == DamageClass.SummonMeleeSpeed && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (modPlayer.BotCConquerorStacks < modPlayer.BotCConquerorMaxStacks - 1 && !hit.Crit)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.0054f }, player.Center);
                }
                else if (modPlayer.BotCConquerorStacks < modPlayer.BotCConquerorMaxStacks - 2 && hit.Crit)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.008f }, player.Center);
                }
                else if (modPlayer.BotCConquerorStacks == modPlayer.BotCConquerorMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, player.Center);
                }
                else if (modPlayer.BotCConquerorStacks == modPlayer.BotCConquerorMaxStacks - 2 && hit.Crit)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, player.Center);
                }
                player.AddBuff(ModContent.BuffType<Conqueror>(), player.GetModPlayer<tsorcRevampPlayer>().BotCConquerorDuration * 60);
                if (hit.Crit)
                {
                    player.AddBuff(ModContent.BuffType<Conqueror>(), player.GetModPlayer<tsorcRevampPlayer>().BotCConquerorDuration * 60);
                }
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.ZoneOldOneArmy)
            {
                if (modPlayer.BotCConquerorStacks < modPlayer.BotCConquerorMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.0054f }, player.Center);
                }
                else if (modPlayer.BotCConquerorStacks == modPlayer.BotCConquerorMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, player.Center);
                }
                player.AddBuff(ModContent.BuffType<Conqueror>(), player.GetModPlayer<tsorcRevampPlayer>().BotCConquerorDuration * 60);
            }
            #endregion
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && (item.pick != 0 || item.axe != 0 || item.hammer != 0))
            {
                tsorcRevampStaminaPlayer StaminaPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                int scaledUseAnimation = (int)(item.useAnimation / player.GetAttackSpeed(item.DamageType));
                StaminaPlayer.staminaResourceCurrent -= tsorcRevampPlayer.ReduceStamina(scaledUseAnimation);
            }
        }
        public static float BonusDamage1 = 30f;
        public static float BonusDamage2 = 50f;
        public static float BonusDamage3 = 75f;
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
                    float bonusDamage = ((player.GetDamage(DamageClass.Magic).Additive * player.GetDamage(DamageClass.Magic).Multiplicative) - 1) * BonusDamage1 / 100f;
                    if (bonusDamage >= 0)
                    {
                        player.GetDamage(DamageClass.Melee) += bonusDamage;
                    }
                }
                if (modPlayer.GreatMagicWeapon)
                {
                    float bonusDamage = ((player.GetDamage(DamageClass.Magic).Additive * player.GetDamage(DamageClass.Magic).Multiplicative) - 1) * BonusDamage2 / 100f;
                    if (bonusDamage >= 0)
                    {
                        player.GetDamage(DamageClass.Melee) += bonusDamage;
                    }
                }
                if (modPlayer.CrystalMagicWeapon)
                {
                    float bonusDamage = (player.GetDamage(DamageClass.Magic).Additive * player.GetDamage(DamageClass.Magic).Multiplicative) * BonusDamage3 / 100f;
                    if (bonusDamage >= 0)
                    {
                        player.GetDamage(DamageClass.Melee) += bonusDamage;
                    }
                }
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

        public override void OnConsumeItem(Item item, Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.consumedPotions ??= new Dictionary<int, int>();

            bool isPotion = false;
            if (potionList.Contains(item.type))
            {
                isPotion = true;
            }

            if (!isPotion)
            {
                if (item.buffType > 0 && item.consumable)
                {
                    isPotion = true;
                }
            }


            if (isPotion && item.buffType != 0)
            {
                if (modPlayer.consumedPotions.ContainsKey(item.buffType))
                {
                    modPlayer.consumedPotions[item.buffType] += 1;
                }
                else
                {
                    modPlayer.consumedPotions.Add(item.buffType, 1);
                }
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && item.type == ItemID.ManaCrystal)
            {
                player.statMana += 20;
            }
        }

        public static void populateSoulRecipes()
        {
            hasSoulRecipe = new List<int>();

            for (int i = 0; i < Recipe.numRecipes; i++)
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

                    //Disable decrafting of anything with a dark soul in its recipe
                    Main.recipe[i].DisableDecraft();
                }

                //Enable decrafting of anything if moon lord is dead (purely an example)
                //The 'NPC.downedMoonlord' part can be replaced by any expression, even a complex one, that ultimately spits out 'true' or 'false'
                //Main.recipe[i].AddDecraftCondition(new Condition(Language.GetText("Conditions.Blah"), () => NPC.downedMoonlord));
            }
        }
        public override bool? UseItem(Item item, Player player)
        {
            if (item.type == ItemID.TorchGodsFavor)
            {
                player.QuickSpawnItem(item.GetSource_Misc("meep"), ModContent.ItemType<WorldRune>());
                player.QuickSpawnItem(item.GetSource_Misc("meep"), ItemID.MagicLantern);
                if (Main.masterMode)
                {
                    player.QuickSpawnItem(item.GetSource_Misc("meep"), ModContent.ItemType<DarkSoul>(), (int)(1500 * 1.2f * tsorcRevampPlayer.CheckSoulsMultiplier(player)));
                }
                else
                {
                    player.QuickSpawnItem(item.GetSource_Misc("meep"), ModContent.ItemType<DarkSoul>(), (int)(1500 * tsorcRevampPlayer.CheckSoulsMultiplier(player)));
                }
                return true;
            }
            return base.UseItem(item, player);
        }
        public override bool CanRightClick(Item item)
        {
            if ((item.type == ItemID.OasisCrate || item.type == ItemID.OasisCrateHard || item.type == ItemID.DungeonFishingCrate || item.type == ItemID.DungeonFishingCrateHard) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>())))
            {
                return true;
            }
            return base.CanRightClick(item);
        }
    }
}