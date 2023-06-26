using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Melee;
using Terraria.DataStructures;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Runeterra;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria.Localization;
using Humanizer;

namespace tsorcRevamp.Items.Weapons.Melee.Runeterra
{
    public class Nightbringer: ModItem
    {
        public int SwingSoundStyle = 0;
        public float SwingSoundVolume = 0.3f;
        public const int BaseDamage = 220;
        public int AttackSpeedScalingDuration = 240;
        public const int DashCooldown = 6;
        public const int WindwallDuration = 5;
        public const int WindwallCooldown = 30;
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.damage = BaseDamage;
            Item.crit = SteelTempest.CritChance;
            Item.width = 94;
            Item.height = 110;
            Item.knockBack = 5f;
            Item.scale = 1f;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shootSpeed = 5f;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
        }

        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            crit *= 2;
        }
        public override void HoldItem(Player player)
        {
            AttackSpeedScalingDuration = (int)(4 / player.GetTotalAttackSpeed(DamageClass.Melee) * 60); //3 seconds divided by player's melee speed
            if (AttackSpeedScalingDuration <= 80)
            {
                AttackSpeedScalingDuration = 80; //1.33 seconds minimum
            }
            Vector2 playerCenter = new Vector2(-13, 0);
            if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2 && player.ownedProjectileCounts[ModContent.ProjectileType<NightbringerTornado>()] < 1)
            {
                Dust.NewDust(player.TopLeft + playerCenter, 50, 50, DustID.DesertTorch, Scale: 2);
            }
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (SwingSoundStyle == 1) //Shoot will always run before this can occur, so they have to be incremented by 1
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/SwingHit1") with { Volume = SwingSoundVolume }, player.Center);
            }
            else
            if (SwingSoundStyle == 2)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/SwingHit2") with { Volume = SwingSoundVolume }, player.Center);
            }
            else
            if (SwingSoundStyle == 0)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/SwingHit3") with { Volume = SwingSoundVolume }, player.Center);
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2) //shoot Nothing
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                if (SwingSoundStyle == 0)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/Swing1") with { Volume = SwingSoundVolume }, player.Center);
                    SwingSoundStyle += 1;
                }
                else
                if (SwingSoundStyle == 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/Swing2") with { Volume = SwingSoundVolume }, player.Center);
                    SwingSoundStyle += 1;
                }
                else
                if (SwingSoundStyle == 2)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/Swing3") with { Volume = SwingSoundVolume }, player.Center);
                    SwingSoundStyle = 0;
                }
                return true;
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks < 2 && !player.HasBuff(ModContent.BuffType<NightbringerDash>()))
            {
                Item.useStyle = ItemUseStyleID.Rapier;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                player.AddBuff(ModContent.BuffType<NightbringerThrustCooldown>(), AttackSpeedScalingDuration);
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/Thrust") with { Volume = 1f }, player.Center);
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<NightbringerThrust>(), damage, knockback * 2, player.whoAmI);
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks < 2 && player.HasBuff(ModContent.BuffType<NightbringerDash>()))
            {
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/Spin") with { Volume = 1f }, player.Center);
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<NightbringerSpin>(), damage, knockback * 2, player.whoAmI);
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2 && !player.HasBuff(ModContent.BuffType<NightbringerDash>()))
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                player.AddBuff(ModContent.BuffType<NightbringerThrustCooldown>(), AttackSpeedScalingDuration);
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/TornadoCast") with { Volume = 1f }, player.Center);
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<NightbringerTornado>(), damage, knockback * 2, player.whoAmI);
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2 && player.HasBuff(ModContent.BuffType<NightbringerDash>()))
            {
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/Spin") with { Volume = 1f }, player.Center);
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<NightbringerSpin>(), damage, knockback * 2, player.whoAmI);
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/TornadoCast") with { Volume = 1f }, player.Center);
                Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<NightbringerTornado>(), damage, knockback * 2, player.whoAmI);
            }
            return true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var SpecialAbilityKey = tsorcRevamp.specialAbility.GetAssignedKeys();
            string SpecialAbilityString = SpecialAbilityKey.Count > 0 ? SpecialAbilityKey[0] : "Special Ability: <NOT BOUND>";
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex1 != -1)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.Items.PlasmaWhirlwind.Keybind1") + SpecialAbilityString + Language.GetTextValue("Mods.tsorcRevamp.Items.PlasmaWhirlwind.Keybind2")));
            }
            int ttindex2 = tooltips.FindIndex(t => t.Name == "Tooltip5");
            if (ttindex2 != -1)
            {
                tooltips.RemoveAt(ttindex2);
                tooltips.Insert(ttindex2, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.Items.Nightbringer.Keybind1") + SpecialAbilityString + Language.GetTextValue("Mods.tsorcRevamp.Items.Nightbringer.Keybind2")));
            }
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", Language.GetTextValue("Mods.tsorcRevamp.Items.Nightbringer.Details").FormatWith((float)AttackSpeedScalingDuration / 60f, PlasmaWhirlwind.PercentHealthDamage, PlasmaWhirlwind.HealthDamageCap, DashCooldown, WindwallDuration, WindwallCooldown)));
                }
            }
            else
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Shift", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.Details")));
                }
            }
        }
        public override bool AltFunctionUse(Player player)
        {
            if (!player.HasBuff(ModContent.BuffType<NightbringerThrustCooldown>()))
                return true;
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<PlasmaWhirlwind>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}