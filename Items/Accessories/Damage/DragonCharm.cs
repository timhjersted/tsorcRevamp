using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Armor;
using tsorcRevamp.Items.Materials;
using tsorcRevamp;

namespace tsorcRevamp.Items.Accessories.Damage
{
    public class DragonCharm : ModItem
    {
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            CursedDragonArmorPlayer.StaminaRegenBonus * 100f,
            CursedDragonArmorPlayer.BreathManaCost,
            CursedDragonArmorPlayer.BreathDamage);

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.Cyan_9;
            Item.rare = ItemRarityID.Cyan;
            Item.expert = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CursedDragonArmorPlayer dragonPlayer = player.GetModPlayer<CursedDragonArmorPlayer>();
            dragonPlayer.CursedDragonSet = true;
            dragonPlayer.CursedDragonVisuals = true;
            player.AddBuff(ModContent.BuffType<DragonForm>(), 30);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DragonStone>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}