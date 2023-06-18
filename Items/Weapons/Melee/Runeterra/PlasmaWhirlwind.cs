using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Runeterra;
using tsorcRevamp.Projectiles;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria.Localization;
using Humanizer;

namespace tsorcRevamp.Items.Weapons.Melee.Runeterra
{
    public class PlasmaWhirlwind : ModItem
    {
        public int SwingSoundStyle = 0;
        public float SwingSoundVolume = 0.25f;
        public const int BaseDamage = 60;
        public int AttackSpeedScalingDuration = 240;
        public const float DashDuration = 0.2f;
        public const int DashCooldown = 6;
        public static float PercentHealthDamage = 0.001f;
        public static int HealthDamageCap = 450;
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightPurple;
            Item.value = Item.buyPrice(0, 30, 0, 0);
            Item.damage = BaseDamage;
            Item.crit = SteelTempest.CritChance;
            Item.width = 70;
            Item.height = 68;
            Item.knockBack = 4f;
            Item.scale = 0.85f;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shootSpeed = 5f;
            Item.shoot = ModContent.ProjectileType<Nothing>();
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
            if (Main.mouseLeft)
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
            }
            Vector2 playerCenter = new Vector2(-13, 0);
            if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2 && player.ownedProjectileCounts[ModContent.ProjectileType<PlasmaWhirlwindTornado>()] < 1)
            {
                Dust.NewDust(player.TopLeft + playerCenter, 50, 50, DustID.ApprenticeStorm, Scale: 1);
            }
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.altFunctionUse == 2 && player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2)
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                player.AddBuff(ModContent.BuffType<PlasmaWhirlwindThrustCooldown>(), AttackSpeedScalingDuration);
            }
            else
            if (player.altFunctionUse == 2 && player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks < 2)
            {
                Item.useStyle = ItemUseStyleID.Rapier;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                player.AddBuff(ModContent.BuffType<PlasmaWhirlwindThrustCooldown>(), AttackSpeedScalingDuration);
            }
            if (player.altFunctionUse == 2 && player.HasBuff(ModContent.BuffType<PlasmaWhirlwindDash>()))
            {
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.noUseGraphic = true;
                Item.noMelee = true;
            }
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (SwingSoundStyle == 1) //Shoot will always run before this can occur, so they have to be incremented by 1
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/SwingHit1") with { Volume = SwingSoundVolume }, player.Center);
            }
            else
            if (SwingSoundStyle == 2)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/SwingHit2") with { Volume = SwingSoundVolume }, player.Center);
            }
            else
            if (SwingSoundStyle == 0)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/SwingHit3") with { Volume = SwingSoundVolume }, player.Center);
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2) //shoot Nothing
            {
                if (SwingSoundStyle == 0)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/Swing1") with { Volume = SwingSoundVolume }, player.Center);
                    SwingSoundStyle += 1;
                }
                else
                if (SwingSoundStyle == 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/Swing2") with { Volume = SwingSoundVolume }, player.Center);
                    SwingSoundStyle += 1;
                }
                else
                if (SwingSoundStyle == 2)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/Swing3") with { Volume = SwingSoundVolume }, player.Center);
                    SwingSoundStyle = 0;
                }
                return true;
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks < 2 && !player.HasBuff(ModContent.BuffType<PlasmaWhirlwindDash>()))
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/Thrust") with { Volume = 1f }, player.Center);
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<PlasmaWhirlwindThrust>(), damage, knockback * 2, player.whoAmI);
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks < 2 && player.HasBuff(ModContent.BuffType<PlasmaWhirlwindDash>()))
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/Spin") with { Volume = 1f }, player.Center);
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<PlasmaWhirlwindSpin>(), damage, knockback * 2, player.whoAmI);
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2 && !player.HasBuff(ModContent.BuffType<PlasmaWhirlwindDash>()))
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/TornadoCast") with { Volume = 1f }, player.Center);
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<PlasmaWhirlwindTornado>(), damage, knockback * 2, player.whoAmI);
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2 && player.HasBuff(ModContent.BuffType<PlasmaWhirlwindDash>()))
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/Spin") with { Volume = 1f }, player.Center);
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<PlasmaWhirlwindSpin>(), damage, knockback * 2, player.whoAmI);
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/TornadoCast") with { Volume = 1f }, player.Center);
                Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<PlasmaWhirlwindTornado>(), damage, knockback * 2, player.whoAmI);
            }
            return false;
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
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", Language.GetTextValue("Mods.tsorcRevamp.Items.PlasmaWhirlwind.Details").FormatWith((float)AttackSpeedScalingDuration / 60f, PercentHealthDamage, HealthDamageCap, DashCooldown)));
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
            if (!player.HasBuff(ModContent.BuffType<PlasmaWhirlwindThrustCooldown>()))
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

            recipe.AddIngredient(ModContent.ItemType<SteelTempest>());
            recipe.AddIngredient(ItemID.ChlorophyteBar, 11);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}