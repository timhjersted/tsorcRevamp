using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class MeteorTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A legendary spell tome that calls down a meteor storm");
        }
        public override void SetDefaults()
        {
            Item.damage = 90;
            Item.height = 10;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.Pink;
            Item.shootSpeed = 6;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 70;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 10;
            Item.useAnimation = 120;
            Item.value = PriceByRarity.Pink_5;
            Item.width = 34;
            Item.autoReuse = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofMight, 2);
            recipe.AddIngredient(Mod.Find<ModItem>("MeteorShower").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 45000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), (float)(Main.mouseX + Main.screenPosition.X) - 100 + Main.rand.Next(200), player.position.Y - 800.0f,
               (float)(-40 + Main.rand.Next(80)) / 10, 14.9f, ModContent.ProjectileType<Projectiles.Meteor>(), (int)(Item.damage * player.GetDamage(DamageClass.Magic)), 2.0f, player.whoAmI);
            return true;
        }
    }
}
