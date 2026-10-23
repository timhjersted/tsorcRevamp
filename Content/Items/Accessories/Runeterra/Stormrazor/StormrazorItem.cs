using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Defensive.RubyCrystal;
using tsorcRevamp.Content.Items.Accessories.Runeterra.LiandrysTorment;
using tsorcRevamp.Content.Items.Accessories.Runeterra.LudensTempest;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.Stormrazor;

public class StormrazorItem : ModItem
{
    public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(LudensTempestItem)); //temp
    public const float Dmg = 3f;
    public const float Crit = 3f;
    public const float AtkSpeed = 3f;
    public const int MaxEnergizedStacks = 100;
    public const float MovespeedBoost = 40f;
    public const int MovespeedDuration = 4;
    public const int OnHitFlatBonusDmg = 40;
    public const float EnergizedStackDivisor = 20f; //higher means less stacks gained, longer charging
    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, Crit, AtkSpeed, OnHitFlatBonusDmg, MovespeedBoost, MovespeedDuration);
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
        recipe.AddIngredient(ItemID.BoneSword);
        recipe.AddIngredient(ItemID.HunterCloak);
        recipe.AddIngredient(ItemID.FeralClaws);
        recipe.AddIngredient(ModContent.ItemType<WorldRune>());
        recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 3300);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.GetModPlayer<StormrazorPlayer>().Equipped = true;
        player.GetDamage(DamageClass.Melee) += Dmg / 100f;
        player.GetCritChance(DamageClass.Melee) += Crit;
        player.GetAttackSpeed(DamageClass.Melee) += AtkSpeed / 100f;
    }
}