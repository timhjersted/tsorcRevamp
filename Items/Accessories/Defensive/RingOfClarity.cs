using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Items.Accessories.Defensive;

class RingOfClarity : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Immunity to On Fire, Burning, Chilled, Cursed Inferno, Ichor," +
                           " Gravitation, Bleeding, Poisoned and knockback" +
                           "\nIncreases damage dealt by 3% and life regeneration by 2");
    }
    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 24;
        Item.accessory = true;
        Item.useAnimation = 100;
        Item.defense = 8;
        Item.useTime = 100;
        Item.maxStack = 1;
        Item.rare = ItemRarityID.Orange;
        Item.value = PriceByRarity.Orange_3;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<PoisonbloodRing>());
        recipe.AddIngredient(ItemID.HallowedBar, 5);
        recipe.AddIngredient(ModContent.ItemType<Items.Accessories.Expert.CrackedDragonStone>());
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.noKnockback = true;
        player.fireWalk = true;
        player.buffImmune[BuffID.OnFire] = true;
        player.buffImmune[BuffID.Burning] = true;
        player.buffImmune[BuffID.Chilled] = true;
        player.buffImmune[BuffID.CursedInferno] = true;
        player.buffImmune[BuffID.Ichor] = true;
        player.buffImmune[BuffID.Gravitation] = true;
        player.buffImmune[BuffID.Bleeding] = true;
        player.buffImmune[BuffID.Poisoned] = true;
        player.buffImmune[ModContent.BuffType<Chilled>()] = true;

        player.GetDamage(DamageClass.Generic) += 0.03f;
        player.lifeRegen += 2;
    }
}
