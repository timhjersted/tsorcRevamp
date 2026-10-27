using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Defensive.RubyCrystal;
using tsorcRevamp.Content.Items.Accessories.Runeterra.Goredrinker;
using tsorcRevamp.Content.Items.Accessories.Runeterra.LudensTempest;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.ImperialMandate;

public class ImperialMandateItem : ModItem
{
    public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(LudensTempestItem)); //temp
    public const float SummonDamage = 5f;
    public const float AtkSpeed = 4f;
    public const float CurrentPercentLifeDmg = 2.5f;
    public const int Duration = 4;
    public const int Cooldown = 10;

    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SummonDamage, AtkSpeed, CurrentPercentLifeDmg, Cooldown, Duration);
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
        recipe.AddIngredient(ItemID.SpellTome, 2);
        recipe.AddIngredient(ItemID.MagicMirror);
        recipe.AddIngredient(ModContent.ItemType<WorldRune>());
        recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 3000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.GetModPlayer<ImperialMandatePlayer>().Equipped = true;
        player.GetDamage(DamageClass.Summon) += SummonDamage / 100f;
        player.GetAttackSpeed(DamageClass.Summon) += AtkSpeed / 100f;
    }
}