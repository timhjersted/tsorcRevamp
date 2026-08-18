using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Debug;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.LegacyCode;
using tsorcRevamp.NPCs.Bosses.WyvernMage;
using tsorcRevamp.Systems;
using tsorcRevamp.Systems.ArcaneSorcery;
using tsorcRevamp.UI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items
{
    public class tsorcGlobalItem : GlobalItem
    {
        // Magic Weapon imbue bonuses
        public static float BonusDamage1 = 30f; // MagicWeapon
        public static float BonusDamage2 = 50f; // GreatMagicWeapon
        public static float BonusDamage3 = 75f; // CrystalMagicWeapon

        public static List<int> potionList;
        public static List<int> ammoList;
        public static List<int> torchList;
        public static List<int> hasSoulRecipe;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ShimmerTransformToItem[ItemID.LunarHook] = ItemID.LunarHook;
        }
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

            if (item.type == ItemID.SlimySaddle && !NPC.downedQueenBee)
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

            tsorcRevampPlayer soulsPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            // Carried over from the Bearer-of-the-Curse gate further below, which has always exempted
            // these three. They were never stamina-blocked, so the new universal gate must not start
            // blocking them without that being a deliberate call.
            bool exemptFromStaminaGate =
                item.type == ModContent.ItemType<Weapons.Ranged.Bows.SagittariusBow>()
                || item.type == ModContent.ItemType<Weapons.Ranged.Bows.ArtemisBow>()
                || item.type == ModContent.ItemType<Weapons.Ranged.Bows.CernosPrime>();

            // Dark Souls gate: any stamina at all lets you commit to the swing, even when it costs more
            // than you have. Overdrawing dumps you to zero and the regen delay locks you out — that
            // lockout is the punishment, not a refusal to swing and not an exhaustion debuff.
            //
            // Requiring the full cost up front was tried and removed: it made a weapon silently
            // unresponsive at partial stamina, and it prevented the very overdraw it was meant to price.
            if (soulsPlayer.UsesWeaponStamina && item.damage >= 1 && !item.accessory && !exemptFromStaminaGate)
            {
                tsorcRevampStaminaPlayer staminaPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                var arcanePlayer = player.GetModPlayer<ArcaneSorceryPlayer>();
                if (staminaPlayer.staminaResourceCurrent <= 0f && !player.HasBuff(ModContent.BuffType<ManaBurn>()))
                {
                    // No "Tired" floating text — the weapon simply not swinging, plus the empty stamina bar,
                    // already communicates this. The text fired on every attempted swing while empty, which is
                    // exactly when a player is mashing the button, so it spammed hardest when least wanted.
                    return false;
                }
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if ((player.GetModPlayer<tsorcRevampPlayer>().isDodging && !player.GetModPlayer<tsorcRevampPlayer>().CanUseItemsWhileDodging) || player.GetModPlayer<tsorcRevampEstusPlayer>().isDrinking || player.GetModPlayer<CeruleanFlaskPlayer>().IsDrinking)
                {
                    return false;
                }

                // The old Bearer-of-the-Curse cost gate (block unless stamina >= useAnimation * 0.8) has
                // been removed. It required the full price up front, which is the opposite of the Dark
                // Souls rule now applied above to both Souls classes, and being BotC-only it would have
                // left BotC unable to overdraw while Unkindled could.

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

        // Apply the Unkindled heal multiplier to every healing item (mod + vanilla).
        // Bearer of the Curse never reaches this because CanUseItem above already blocks healLife > 0.
        // Classic players get the unmodified vanilla amount; Unkindled players get half via ApplyHealing.
        public override void GetHealLife(Item item, Player player, bool quickHeal, ref int healValue)
        {
            if (healValue <= 0) return;
            healValue = player.GetModPlayer<tsorcRevampPlayer>().ApplyHealing(healValue);
        }


        public override bool CanEquipAccessory(Item item, Player player, int slot, bool modded)
        {

            // Cannot equip wings until the hunter has been defeated unless you're in debug mode.
            if (item.wingSlot < ArmorIDs.Wing.Sets.Stats.Length && item.wingSlot > 0 && !player.HasItem(ModContent.ItemType<DebugTome>()) && !ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                if (item.type != ItemID.CreativeWings && item.type != ItemID.AngelWings && item.type != ItemID.DemonWings && item.type != ItemID.HarpyWings && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>())))
                {
                    return false;
                }
                if ((item.type == ItemID.AngelWings || item.type == ItemID.DemonWings || item.type == ItemID.HarpyWings) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WyvernMage>())))
                {
                    return false;
                }
            }
            return base.CanEquipAccessory(item, player, slot, modded);
        }

        public static int[] badPrefixes = { 7, 8, 9, 10, 11, 13, 14, 22, 23, 24, 29, 30, 31, 39, 40, 41, 47, 48, 49, 50, 56 };
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (hasSoulRecipe.Contains(item.type))
            {
                tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "RecipeTooltip", $"[i:{ModContent.ItemType<DarkSoul>()}]" + Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.RecipeTooltip")));
            }

            // Life Crystal: replace the vanilla "...by 20" line with the actual party-scaled gain
            // so SoulsMode players see what they'll actually get. Classic players see the unchanged
            // vanilla tooltip.
            if (item.type == ItemID.LifeCrystal && Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulsMode)
            {
                int activePlayers = 0;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active) activePlayers++;
                }
                int gain = activePlayers >= 4 ? 20 : (activePlayers >= 2 ? 15 : 10);

                // Find the vanilla "permanently increases maximum life" line and rewrite the number.
                // Falls back to appending a new line if the vanilla one isn't found (other mods etc.).
                bool replaced = false;
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i].Mod == "Terraria" && tooltips[i].Text.Contains("20"))
                    {
                        tooltips[i].Text = Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.LifeCrystalSoulsMode", gain);
                        replaced = true;
                        break;
                    }
                }
                if (!replaced)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "LifeCrystalSoulsMode",
                        Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.LifeCrystalSoulsMode", gain)));
                }
            }

            if (item.type == ItemID.ManaCrystal && Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulsMode)
            {
                bool replaced = false;
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i].Mod == "Terraria" && tooltips[i].Text.Contains("20"))
                    {
                        tooltips[i].Text = Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.ManaCrystalSoulsMode", 10);
                        replaced = true;
                        break;
                    }
                }
                if (!replaced)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "ManaCrystalSoulsMode",
                        Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.ManaCrystalSoulsMode", 10)));
                }
            }
            if (badPrefixes.Contains<int>(item.prefix) && !NPC.AnyNPCs(NPCID.GoblinTinkerer))
            {
                tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "ReforgeTooltip", $"[i:{ItemID.LivingFireBlock}]" + Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.ReforgeTooltip")));
            }

            if (item.wingSlot < ArmorIDs.Wing.Sets.Stats.Length && item.wingSlot > 0 && !ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                if (item.type != ItemID.CreativeWings && item.type != ItemID.AngelWings && item.type != ItemID.DemonWings && item.type != ItemID.HarpyWings && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>())))
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.WingsDisabled")));
                }
                if ((item.type == ItemID.AngelWings || item.type == ItemID.DemonWings || item.type == ItemID.HarpyWings) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WyvernMage>())))
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.WingsDisabled2")));
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
                if (item.type == ItemID.SlimySaddle && !NPC.downedQueenBee)
                {
                    tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Disabled", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.QueenBeeCursed")));
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

            if (player.whoAmI == Main.myPlayer
                && modPlayer.UsesWeaponStamina)
            {
                player.GetModPlayer<tsorcRevampStaminaPlayer>().RecordWeaponOutputDamage(item.type, damageDone);
            }

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
                target.AddBuff(BuffID.BetsysCurse, 240);
            }

            if (item.type == ItemID.InfluxWaver && NPC.downedMartians)
            {
                target.AddBuff(ModContent.BuffType<Buffs.ElectrocutedBuff3>(), 5 * 60);
            }
            #region Lethal Tempo
            if ((item.DamageType == DamageClass.Melee || item.DamageType == DamageClass.MeleeNoSpeed) && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (modPlayer.LethalTempoStacks < modPlayer.LethalTempoMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.002f }, player.Center);
                }
                else if (modPlayer.LethalTempoStacks == modPlayer.LethalTempoMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.003f }, player.Center);
                }
                player.AddBuff(ModContent.BuffType<LethalTempo>(), player.GetModPlayer<tsorcRevampPlayer>().LethalTempoDuration * 60);
            }
            #endregion
            #region Conqueror
            if (item.DamageType == DamageClass.SummonMeleeSpeed && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (modPlayer.ConquerorStacks < modPlayer.ConquerorMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.0054f }, player.Center);
                }
                else if (modPlayer.ConquerorStacks == modPlayer.ConquerorMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, player.Center);
                }
                player.AddBuff(ModContent.BuffType<Conqueror>(), player.GetModPlayer<tsorcRevampPlayer>().ConquerorDuration * 60);
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && tsorcRevamp.EnemiesOOA.Contains(target.type))
            {
                if (modPlayer.ConquerorStacks < modPlayer.ConquerorMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.0054f }, player.Center);
                }
                else if (modPlayer.ConquerorStacks == modPlayer.ConquerorMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, player.Center);
                }
                player.AddBuff(ModContent.BuffType<Conqueror>(), player.GetModPlayer<tsorcRevampPlayer>().ConquerorDuration * 60);
            }
            #endregion
            // Pick/axe/hammer combat surcharge: the swing itself already paid ToolSwingStaminaMult in
            // PreItemCheck (tsorcRevampPlayerDodgeRoll.cs) regardless of what it hit — tile, air, or NPC —
            // so fighting with one of these tools costs a bit more than idly chopping wood or mining
            // without reintroducing the old "only pay when you connect" behaviour that let the swing cost
            // be dodged entirely by whiffing. Trees/tiles never reach OnHitNPC, so pure wood-chopping only
            // ever pays the swing cost.
            //
            // Skipped entirely for weapon-classified tools (tsorcRevamp.WeaponClassifiedTools) — those
            // already pay the plain legacy weapon cost in PreItemCheck, same as any sword, and this
            // surcharge exists specifically to compensate the FLAT tool rate for not knowing about
            // combat; a real weapon doesn't need that compensation and would just be double-charged by it.
            if (modPlayer.UsesWeaponStamina && (item.pick != 0 || item.axe != 0 || item.hammer != 0)
                && (tsorcRevamp.WeaponClassifiedTools == null || !tsorcRevamp.WeaponClassifiedTools.Contains(item.type)))
            {
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -=
                    tsorcRevampStaminaPlayer.ToolCombatHitSurcharge * modPlayer.WeaponStaminaMult;
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

            // Dark Souls Storage: auto-file-on-pickup is disabled — all picked-up items go to the normal
            // inventory like vanilla. Storage is still reachable manually (opener slot / keybind, and
            // shift-click while the Storage UI is open via tsorcRevampPlayerMain.ShiftClickSlot).
            // if (player.whoAmI == Main.myPlayer)
            // {
            //     tsorcRevampPlayer mp = player.GetModPlayer<tsorcRevampPlayer>();
            //     if (mp.IsStorageDepositable(item))
            //     {
            //         if (mp.DepositToStorage(item))
            //         {
            //             SoundEngine.PlaySound(SoundID.Grab);
            //             return false; // fully stored — suppress the normal "got item" pickup
            //         }
            //     }
            // }

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
            // Life Crystal nerf moved to UseItem (below) � vanilla's Life Crystal handling in
            // Player.ItemCheck bumps statLifeMax/statLife directly and consumes the stack manually,
            // bypassing ItemLoader.ConsumeItem entirely. That means this OnConsumeItem hook never
            // fired for Life Crystals, and the nerf was silently inert. UseItem runs immediately
            // before vanilla's +20 effect, so we pre-subtract the nerf there and let vanilla's
            // unconditional +20 produce the intended net gain.
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

            // Life Crystal nerf moved to tsorcRevampPlayer.PostUpdate as a statLifeMax-spike detector.
            // Attempts to pre-subtract here in UseItem (so vanilla's +20 nets to +10) didn't reduce
            // statLifeMax reliably � the user observed +20 max HP still landing despite the hook
            // running. The PostUpdate monitor watches statLifeMax across frames and claws back the
            // nerf the frame after a Life Crystal raises it, which is timing-independent.

            return base.UseItem(item, player);
        }
        public override bool CanRightClick(Item item)
        {
            if ((item.type == ItemID.OasisCrate || item.type == ItemID.OasisCrateHard || item.type == ItemID.DungeonFishingCrate || item.type == ItemID.DungeonFishingCrateHard) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>())))
            {
                return false;
            }
            return base.CanRightClick(item);
        }
    }
}