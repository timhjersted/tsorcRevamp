using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class Bolt2Tome : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bolt 2 Tome");
            Tooltip.SetDefault("A lost tome for artisans." +
                                "\nDrops a lightning strike upon collision" +
                                "\nHas a chance to electrify enemies" +
                                "\nCan be upgraded with 25,000 Dark Souls and 15 Soul of Light.");

        }

        public override void SetDefaults()
        {

            Item.damage = 17;
            Item.height = 10;
            Item.knockBack = 0f;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Orange;
            Item.shootSpeed = 7f;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 20;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 30;
            Item.value = PriceByRarity.Orange_3;
            Item.width = 34;
            Item.shoot = ModContent.ProjectileType<Projectiles.Bolt2Ball>();
        }
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if (player.wet)
            {
                player.AddBuff(BuffID.Electrified, 90);
            }

            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("Bolt1Tome").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 8000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}
