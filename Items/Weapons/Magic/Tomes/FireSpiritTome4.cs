using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    class FireSpiritTome4 : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Tome of the Dying Star");
            /* Tooltip.SetDefault("Leave nothing but ash in your wake." +
                "\nLeft click to charge a detonating core" +
                "\nRight click to fire a rapid barrage of solar flares"); */
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.damage = 2000;
            Item.knockBack = 90;
            Item.autoReuse = true;
            Item.scale = 1.3f;
            Item.rare = ItemRarityID.Purple;
            Item.shootSpeed = 44;
            Item.mana = 40;
            Item.value = PriceByRarity.Purple_11;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.DyingStarHoldout>();
            Item.channel = true;
            Item.noMelee = true;

        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            player.manaRegenDelay = 180;
            mult = 0;
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

        public override bool? UseItem(Player player)
        {
            return base.UseItem(player);
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<FireSpiritTome3>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfBlight>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Items.Weapons.Magic.Tomes.FlareTome>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
