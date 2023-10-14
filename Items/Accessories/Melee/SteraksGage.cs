using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Melee.Hammers;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Accessories.Melee
{
    public class SteraksGage : ModItem
    {
        public static float MeleeDmg = 10f;
        public static float UseTimeRatio = 1.5f;
        public static float MaxDmg = 20f;
        public static int MaxLife = 45;
        public static float LifeThreshold = 30f;
        public static int ShieldHeal = 50;
        public static int Cooldown = 90;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MeleeDmg, MaxDmg, MaxLife, LifeThreshold, ShieldHeal, Cooldown);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.value = PriceByRarity.Orange_3;
            Item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronPickaxe);
            recipe.AddIngredient(ModContent.ItemType<AncientWarhammer>());
            recipe.AddIngredient(ItemID.LifeCrystal, 2);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3200);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SteraksGage = true;
            player.GetDamage(DamageClass.Melee) += MeleeDmg / 100f;
            player.GetDamage(DamageClass.Melee) += MathF.Min((float)(player.HeldItem.useAnimation - 18) / UseTimeRatio / 100f, MaxDmg / 100f); 
            player.statLifeMax2 += MaxLife;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
            if (ttindex != -1 && Main.LocalPlayer.HeldItem.damage > 0 && (player.HeldItem.DamageType == DamageClass.Melee || player.HeldItem.DamageType == DamageClass.MeleeNoSpeed))
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Bonus", LangUtils.GetTextValue("Items.SteraksGage.Bonus", (int)MathF.Min((player.HeldItem.useAnimation - 18) / UseTimeRatio, MaxDmg))));
            }
        }
    }
}