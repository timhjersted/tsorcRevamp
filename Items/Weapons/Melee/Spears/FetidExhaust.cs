using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class FetidExhaust : ModItem
    {
        public const float BaseHitCooldown = 25f;
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Expel a close-range blast of searing noxious gas");
        }

        public override void SetDefaults()
        {
            Item.damage = 160;
            Item.knockBack = 15f;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 11;
            Item.useTime = 1;
            Item.shootSpeed = 8;

            Item.height = 74;
            Item.width = 74;

            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.useTurn = true;

            Item.value = PriceByRarity.Yellow_8;
            Item.rare = ItemRarityID.Yellow;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<FetidExhaustProjectile>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.ownedProjectileCounts[type] > 0)
            {
                return false;
            }
            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SetAuraState(tsorcAuraState.Spazmatism);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DamagedFlameNozzle>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15000);
            recipe.AddIngredient(ItemID.SoulofSight, 5);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
