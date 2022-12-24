using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    class RingOfClarity : ModItem
    {

        public override string Texture => "tsorcRevamp/Items/Accessories/Defensive/PoisonbloodRing";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Prevents a wide variety of debuffs, including many DoT effects. \n+3% damage, +4 regeneration, and 9 defense");
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.useAnimation = 100;
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
            recipe.AddIngredient(ModContent.ItemType<Items.Accessories.Defensive.CrackedDragonStone>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Generic) += 0.03f;
            player.noKnockback = true;
            player.fireWalk = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.Burning] = true;
            player.buffImmune[BuffID.Chilled] = true;
            player.buffImmune[BuffID.CursedInferno] = true;
            player.buffImmune[BuffID.Ichor] = true;
            player.buffImmune[BuffID.BrokenArmor] = true;
            player.buffImmune[BuffID.Slow] = true;
            player.buffImmune[BuffID.Confused] = true;
            player.buffImmune[BuffID.Gravitation] = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[ModContent.BuffType<Buffs.Chilled>()] = true;

            player.lifeRegen += 4;
            player.statDefense += 9;
        }
    }
}
