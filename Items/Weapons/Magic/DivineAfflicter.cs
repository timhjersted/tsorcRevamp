using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Magic;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class DivineAfflicter : ModItem
    {

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 24;
            Item.useTime = 24;
            Item.channel = true;
            Item.damage = 44;
            Item.knockBack = 4;
            Item.UseSound = SoundID.Item20;
            Item.rare = ItemRarityID.LightRed;
            Item.crit = 4; //it has 4 extra crit I guess, sure?
            Item.mana = 200;
            Item.noMelee = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<DivineAfflicterProjectile>();
        }
        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<DivineAfflicterProjectile>()] > 0 && player.channel)
            {
                return false;
            }

            return true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Flamelash);
            recipe.AddIngredient(ItemID.Ichor, 5);
            recipe.AddIngredient(ItemID.SoulofNight, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
