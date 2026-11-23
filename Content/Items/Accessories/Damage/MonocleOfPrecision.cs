using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Accessories.Damage
{
    public class MonocleOfPrecision : ModItem
    {
        public static float CriticalStrikeChance = 7;
        public static float WhipTipHitboxSize = 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CriticalStrikeChance, WhipTipHitboxSize);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetCritChance(DamageClass.Generic) += CriticalStrikeChance;
            player.GetModPlayer<WhipTipHit>().WhipTipHitboxSize = WhipTipHitboxSize;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
            if (ttindex != -1 && Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "TipHits", Language.GetTextValue(Tooltip.Key + "0", WhipTipHitboxSize)));
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BlackLens);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 2000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
