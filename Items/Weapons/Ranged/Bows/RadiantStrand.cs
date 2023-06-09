using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.Bows
{
    public class RadiantStrand : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Fires arrows of light which leave a damaging trail of energy as they fly");
        }
        public override void SetDefaults()
        {
            Item.height = 50;
            Item.width = 32;
            Item.DamageType = DamageClass.Ranged;
            Item.noMelee = true;
            Item.useTime = Item.useAnimation = 12;
            Item.damage = 350;
            Item.knockBack = 1;
            Item.autoReuse = true;
            Item.shootSpeed = 16;
            Item.useAmmo = AmmoID.Arrow;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.Purple_11;
            Item.shoot = 10;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = SoundID.Item43;

        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-3, 0);
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            type = ModContent.ProjectileType<Projectiles.Ranged.RadiantStrand>();
            velocity = velocity.RotatedByRandom(0.5);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.FairyQueenRangedItem);
            recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 90000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.SHM9Downed);

            recipe.Register();
        }
    }
}
