using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Defensive.RubyCrystal;
using tsorcRevamp.Content.Items.Accessories.Runeterra.LiandrysTorment;
using tsorcRevamp.Content.Items.Accessories.Runeterra.LudensTempest;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.BlackCleaver;

public class BlackCleaverItem : ModItem
{
    public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(LudensTempestItem)); //temp
    public const float Dmg = 4f;
    public const int Health = 45;
    public const float AtkSpeed = 5f;
    public const int MaxStacks = 6;
    public const int DefenseReductionPerStack = 2;
    public const int DebuffDuration = 3;
    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, AtkSpeed, Health, MaxStacks, DebuffDuration, DefenseReductionPerStack);
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
        recipe.AddIngredient(ItemID.WoodenHammer);
        recipe.AddIngredient(ItemID.IronPickaxe);
        recipe.AddIngredient(ModContent.ItemType<RubyCrystalItem>());
        recipe.AddIngredient(ModContent.ItemType<WorldRune>());
        recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 2200);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.GetModPlayer<BlackCleaverPlayer>().Equipped = true;
        player.GetDamage(DamageClass.Ranged) += Dmg / 100f;
        player.statLifeMax2 += Health;
        player.GetAttackSpeed(DamageClass.Ranged) += AtkSpeed / 100f;
    }
}