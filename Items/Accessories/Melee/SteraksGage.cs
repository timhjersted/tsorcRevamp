using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Melee.Hammers;

namespace tsorcRevamp.Items.Accessories.Melee
{
    public class SteraksGage : ModItem
    {
        public static float MeleeDmg = 5f;
        public static int UseTimeRatio = 2;
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
            recipe.AddIngredient(ItemID.LifeCrystal);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3200);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SteraksGage = true;
            player.GetDamage(DamageClass.Melee) += MeleeDmg / 100f;
            player.GetDamage(DamageClass.Melee) += MathF.Min((player.HeldItem.useAnimation - 20) / UseTimeRatio / 100f, MaxDmg);
            player.statLifeMax2 += MaxLife;
        }
    }
}