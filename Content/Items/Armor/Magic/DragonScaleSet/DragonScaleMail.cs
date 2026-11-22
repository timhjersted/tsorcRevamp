using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Magic.DragonScaleSet
{
    [AutoloadEquip(EquipType.Body)]
    public class DragonScaleMail : ModItem
    {
        public static float Dmg = 22f;
        public static float Thorns = 100f;
        public static int ManaRegen = 6;
        public static float LifeThreshold = 25f;
        public static float SetBonusDmgCritChance = 11f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, Thorns, ManaRegen, LifeThreshold, SetBonusDmgCritChance, tsorcRevampPlayer.MythrilOcrichalcumCritDmg);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 8;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic) += Dmg / 100f;
            player.thorns += Thorns / 100f;
            player.manaRegenBonus += ManaRegen;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<DragonScaleHelmet>() && legs.type == ModContent.ItemType<DragonScaleGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            var modPlayer = player.GetModPlayer<DragonScaleSetPlayer>();
            modPlayer.Equipped = true;
            bool isVisible = player.armor[10].IsAir && player.armor[11].IsAir && player.armor[12].IsAir;
            if (isVisible)
            {
                modPlayer.Visual = true;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilChainmail);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 6000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.MythrilChainmail);
            recipe2.AddIngredient(ItemID.OrichalcumBreastplate);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
