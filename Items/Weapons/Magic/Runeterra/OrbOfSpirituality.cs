using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Magic.Runeterra;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    public class OrbOfSpirituality : ModItem
    {
        public static Color FilledColor = Color.YellowGreen;
        public static int DashBuffDuration = 15;
        public static int DashCD = 60;
        public static int DashCostMultiplier = 4;
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 8));
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = false;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.damage = 330;
            Item.crit = 6;
            Item.mana = 60;
            Item.knockBack = 8;
            Item.UseSound = null;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = OrbOfDeception.ShootSpeed;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Cyan_9;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<OrbOfSpiritualityOrb>();
            Item.holdStyle = ItemHoldStyleID.HoldLamp;
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfSpiritualityOrb>()] != 0)
            {
                type = ModContent.ProjectileType<OrbOfSpiritualityFlame>();
            }
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfSpiritualityOrb>()] == 0 && player.GetModPlayer<tsorcRevampPlayer>().EssenceThief < 9)
            {
                type = ModContent.ProjectileType<OrbOfSpiritualityOrb>();
            }
            if (player.altFunctionUse == 2)
            {
                type = ModContent.ProjectileType<OrbOfSpiritualityCharm>();
            }
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (Main.mouseRight & !Main.mouseLeft & !player.HasBuff(ModContent.BuffType<OrbOfSpiritualityCharmCooldown>())) //cooldown gets applied on projectile spawn
            {
                player.altFunctionUse = 2;
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
            }
        }
        public override void HoldItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfSpiritualityOrb>()] == 0 && player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfSpiritualityOrbIdle>()] == 0 && !player.dead)
            {
                Projectile.NewProjectile(Projectile.InheritSource(player), player.Center, Vector2.Zero, ModContent.ProjectileType<OrbOfSpiritualityOrbIdle>(), 0, 0, player.whoAmI);
            }
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2 || !player.HasBuff(ModContent.BuffType<OrbOfSpiritualityCharmCooldown>()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            var SpecialAbilityKey = tsorcRevamp.specialAbility.GetAssignedKeys();
            string SpecialAbilityString = SpecialAbilityKey.Count > 0 ? SpecialAbilityKey[0] : Language.GetTextValue("Mods.tsorcRevamp.Keybinds.Special Ability.DisplayName") + Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.NotBound");
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip5");
            if (ttindex1 != -1)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.Items.OrbOfSpirituality.Keybind1") + SpecialAbilityString + Language.GetTextValue("Mods.tsorcRevamp.Items.OrbOfSpirituality.Keybind2")));
            }
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", Language.GetTextValue("Mods.tsorcRevamp.Items.OrbOfSpirituality.Details").FormatWith(OrbOfDeception.OrbDmgMod - 100f, (OrbOfDeception.OrbReturnDmgMod * OrbOfDeception.OrbDmgMod) / 100f - 100f, OrbOfDeception.EssenceThiefOnKillChance, OrbOfDeception.FilledOrbDmgMod - 100f, OrbOfFlame.FireballDmgMod / 100f, OrbOfFlame.FireballCD, OrbOfFlame.FireballHPPercentDmg, OrbOfFlame.FireballHPDmgCap, OrbOfFlame.MagicSunder, DashBuffDuration, DashCD, (int)(Item.mana * player.manaCost * DashCostMultiplier))));
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
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<OrbOfFlame>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
