using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    public class Bolt4Tome : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Bolt 4 Tome");
            /* Tooltip.SetDefault("A lost legendary tome. You command the forces of the sky.\n" +
                                "\nElectrifies and paralyzes enemies" +
                                "\nOnly the most powerful mages will be able to cast this spell."); */

        }

        public override void SetDefaults()
        {
            Item.damage = 140;
            Item.height = 10;
            Item.width = 34;
            Item.knockBack = 0.1f;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Lime;
            Item.shootSpeed = 24f;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 70;
            Item.scale = 0.8f;
            Item.useAnimation = 60;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 60;
            Item.value = PriceByRarity.Lime_7;
            Item.shoot = ModContent.ProjectileType<Projectiles.Bolt4Ball>();
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
            recipe.AddIngredient(ModContent.ItemType<Bolt3Tome>());
            recipe.AddIngredient(ItemID.SoulofMight, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 60000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}
