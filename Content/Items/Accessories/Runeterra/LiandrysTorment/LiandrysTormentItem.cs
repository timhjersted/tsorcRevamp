using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Defensive.RubyCrystal;
using tsorcRevamp.Content.Items.Accessories.Runeterra.LudensTempest;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Magic.Wands;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.LiandrysTorment;

public class LiandrysTormentItem : ModItem
{
    public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(LudensTempestItem)); //temp
    public const float Dmg = 2f;
    public const int Mana = 20;
    public const int Health = 40;
    public const float MaxHealthDmgPerSec = 0.15f; //at this number the debuff will need 667 seconds to get through the entire enemy health by itself
    public const float MinDmgPerSec = 5;
    public const int BurnDuration = 3;
    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, Health, Mana, MaxHealthDmgPerSec, MinDmgPerSec, BurnDuration);
    public override void SetDefaults()
    {
        Item.width = 42;
        Item.height = 48;
        Item.accessory = true;
        Item.value = PriceByRarity.Orange_3;
        Item.rare = ItemRarityID.Orange;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.SpellTome, 2);
        recipe.AddIngredient(ItemID.ManaCrystal);
        recipe.AddIngredient(ModContent.ItemType<RubyCrystalItem>());
        recipe.AddIngredient(ModContent.ItemType<WorldRune>());
        recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 2200);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.GetModPlayer<LiandrysTormentPlayer>().Equipped = true;
        player.GetDamage(DamageClass.Magic) += Dmg / 100f;
        player.statLifeMax2 += Health;
        player.statManaMax2 += Mana;
    }
}