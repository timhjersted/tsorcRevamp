using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class FireSpiritTome4 : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tome of the Dying Star");
            Tooltip.SetDefault("Leave nothing but ash in your wake." +
                "\nLeft click to charge a detonating core" +
                "\nRight click to fire a rapid barrage of solar flares");
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 1;
            Item.useTime = 1;
            Item.maxStack = 1;
            Item.damage = 1000;
            Item.knockBack = 90;
            Item.autoReuse = true;
            Item.scale = 1.3f;
            Item.rare = ItemRarityID.Purple;
            Item.shootSpeed = 44;
            Item.mana = 0;
            Item.value = PriceByRarity.Purple_11;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.DyingStarHoldout>();
            Item.channel = true;
            
        }
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2 && player.statMana <= (int)(5 * player.manaCost))
            {
                return false;
            }
            if (player.altFunctionUse != 2 && player.statMana <= (int)(50 * player.manaCost))
            {
                return false;
            }
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.DyingStarHoldout>()] <= 0;
        }

        public override bool AltFunctionUse(Player player)
        {            
            return true;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("FireSpiritTome3").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<Items.BequeathedSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Items.SoulOfChaos>(), 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 115000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
