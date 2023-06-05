using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    public class MeteorShower : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Causes meteorites to rain from the sky.");
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item8;
            Item.rare = ItemRarityID.Orange;
            Item.knockBack = 3;
            Item.mana = 10;
            Item.damage = 40;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Magic;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.MeteoriteBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 12000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            Projectile.NewProjectile(player.GetSource_ItemUse(Item),
               (float)(Main.mouseX + Main.screenPosition.X) - 100 + Main.rand.Next(200),
               (float)player.position.Y - 800.0f,
               (float)(Main.rand.Next(-40, 40)) / 10,
               14.9f,
               ModContent.ProjectileType<Projectiles.MeteorShower>(),
               50,
               2.0f,
               player.whoAmI);
            return true;
        }
    }
}
