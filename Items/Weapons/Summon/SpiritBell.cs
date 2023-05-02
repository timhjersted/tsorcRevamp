using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon
{
    [Autoload(true)]
    public class SpiritBell : ModItem
	{
		public override void SetStaticDefaults()
		{
			// Tooltip.SetDefault("Summons a Barrow Wight to fight for you");

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
			ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
			ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
        }
		public override void SetDefaults()
		{
			Item.damage = 24;
			Item.knockBack = 3f;
			Item.mana = 10;
			Item.width = 50;
			Item.height = 50;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.value = Item.buyPrice(0, 2, 80, 0);
			Item.rare = ItemRarityID.Blue;
			Item.UseSound = SoundID.Item44;


			Item.noMelee = true;
			Item.DamageType = DamageClass.Summon;
			Item.buffType = ModContent.BuffType<Buffs.Summon.BarrowWightBuff>();
			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.SpiritBell.BarrowWightMinion>();
		}
		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
		{
			position = Main.MouseWorld;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(Item.buffType, 2);

            var projectile = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer);
            projectile.originalDamage = Item.damage;

            return false;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AbigailsFlower);
            recipe.AddIngredient(ModContent.ItemType<EphemeralDust>(), 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 7000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}