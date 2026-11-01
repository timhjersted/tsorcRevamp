using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Melee.Halberds;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Halberds
{
    public class DarkrootHalberd : ModItem
    {
        public const int BaseDamage = 360;

        public override void SetDefaults()
        {
            Item.width = Item.height = 74;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 8f;
            Item.crit = 4;
            Item.useTime = Item.useAnimation = 44;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.value = PriceByRarity.Purple_11;
            Item.shoot = ModContent.ProjectileType<DarkrootHalberdHeld>();
            Item.shootSpeed = 1f;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            if (!ModContent.GetInstance<tsorcRevampConfig>().ShowStaminaTooltip
                || !player.GetModPlayer<tsorcRevampPlayer>().UsesWeaponStamina) return;
            float mult = player.GetModPlayer<tsorcRevampPlayer>().WeaponStaminaMult(Item);
            tooltips.Add(new TooltipLine(Mod, "DarkrootStamina", LangUtils.GetTextValue("Items.DarkrootHalberd.Stamina",
                Math.Round(24 * mult, 1), Math.Round(26 * mult, 1), Math.Round(30 * mult, 1))));
        }

        public override bool CanUseItem(Player player)
        {
            int step = player.altFunctionUse == 2 ? DarkrootHalberdMotion.SpinWindup : player.GetModPlayer<DarkrootHalberdPlayer>().NextStep;
            return player.ownedProjectileCounts[Item.shoot] == 0 && CanPay(player, step);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.whoAmI != Main.myPlayer) return false;
            int step = player.altFunctionUse == 2 ? DarkrootHalberdMotion.SpinWindup : player.GetModPlayer<DarkrootHalberdPlayer>().NextStep;
            if (!TryPay(player, step)) return false;
            Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            int duration = Duration(player, step);
            int index = Projectile.NewProjectile(source, player.MountedCenter, aim, type, damage, knockback, player.whoAmI, step, 0, duration);
            if (index >= 0 && index < Main.maxProjectiles)
                Main.projectile[index].CritChance = (int)player.GetWeaponCrit(Item);
            return false;
        }

        internal static int Duration(Player player, int step)
        {
            float speed = Math.Max(0.1f, player.GetAttackSpeed(player.HeldItem.DamageType));
            float prefixTempo = Math.Max(0.25f, player.HeldItem.useAnimation / 44f);
            return Math.Max(DarkrootHalberdMotion.MinimumDuration(step),
                (int)Math.Round(DarkrootHalberdMotion.BaseDuration(step) * prefixTempo / speed));
        }

        internal static float Cost(Player player, int step) => DarkrootHalberdMotion.Stamina(step)
            * player.GetModPlayer<tsorcRevampPlayer>().WeaponStaminaMult(player.HeldItem);

        internal static bool CanPay(Player player, int step)
        {
            if (player.whoAmI != Main.myPlayer) return true;
            var stamina = player.GetModPlayer<tsorcRevampStaminaPlayer>();
            return !player.GetModPlayer<tsorcRevampPlayer>().UsesWeaponStamina || stamina.IsManaBurnActive
                || (stamina.staminaDebt <= 0 && stamina.staminaResourceCurrent >= Cost(player, step));
        }

        internal static bool TryPay(Player player, int step)
        {
            if (!CanPay(player, step)) return false;
            var stamina = player.GetModPlayer<tsorcRevampStaminaPlayer>();
            if (player.GetModPlayer<tsorcRevampPlayer>().UsesWeaponStamina && !stamina.IsManaBurnActive)
            {
                stamina.staminaResourceCurrent -= Cost(player, step);
                stamina.PauseStaminaRegen(stamina.SpendRegenDelay);
            }
            return true;
        }
    }

    public class DarkrootHalberdPlayer : ModPlayer
    {
        int nextStep;
        ulong expires;
        public int NextStep => Main.GameUpdateCount <= expires ? nextStep : 0;

        internal void Complete(int step)
        {
            nextStep = step < 2 ? step + 1 : 0;
            expires = Main.GameUpdateCount + 45;
        }

        internal void ResetCombo() { nextStep = 0; expires = 0; }

        public override void ResetEffects()
        {
            if (Player.dead || Player.HeldItem.type != ModContent.ItemType<DarkrootHalberd>()
                || Player.GetModPlayer<tsorcRevampPlayer>().isDodging)
                ResetCombo();
        }
    }
}
