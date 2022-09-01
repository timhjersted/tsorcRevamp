using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Runeterra.Summon
{
	public class CotUItem1 : ModItem
	{
		public static List<CotUStar1> projectiles = null;
		public static int processedProjectilesCount = 0;
		public static float holdTimer1 = 0f;

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Scorching Point");
			Tooltip.SetDefault("Summons fireballs to rotate around you and damage enemies in their way" +
								"\nIncrease their radius by holding the Special Ability hotkey" +
								"\nReset their range by clearing the buff" +
                				"\n'By the Ashen Lord of Runeterra!'");


			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}
    public override void SetDefaults()
		{
			projectiles = new List<CotUStar1>(){};

			Item.damage = 18;
			Item.knockBack = 3f;
			Item.mana = 10;
			Item.width = 32;
			Item.height = 34;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.holdStyle = ItemHoldStyleID.HoldFront;
			Item.noUseGraphic = true;
			Item.useTurn = false;
			Item.value = Item.buyPrice(0, 10, 0, 0);
			Item.rare = ItemRarityID.Green;
			Item.UseSound = SoundID.Item117;

			Item.noMelee = true;
			Item.DamageType = DamageClass.Summon;
			Item.buffType = ModContent.BuffType<CotUBuff1>();
			Item.shoot = ModContent.ProjectileType<CotUStar1>();
		}

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
		{
			// Here you can change where the minion is spawned. Most vanilla minions spawn at the cursor position
			position = player.Bottom;
		}
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			// This is needed so the buff that keeps your minion alive and allows you to despawn it properly applies
			player.AddBuff(Item.buffType, 2);

			// Minions have to be spawned manually, then have originalDamage assigned to the damage of the summon item
			var projectile = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer);
			projectile.originalDamage = Item.damage;

			// Since we spawned the projectile manually already, we do not need the game to spawn it for ourselves anymore, so return false
			return false;
		}
		public override void HoldItem(Player player)
		{
			holdTimer1 = 0.5f;
        }
		public override void UpdateInventory(Player player)
		{
            if (Main.GameUpdateCount % 20 == 0)
            {
                holdTimer1 -= 0.267f;
            }
			Main.NewText(holdTimer1);
        }
		public static void ReposeProjectiles(Player player) 
		{
			// repose projectiles relatively to the first one so they are evenly spread on the radial circumference
			processedProjectilesCount = player.ownedProjectileCounts[ModContent.ProjectileType<CotUStar1>()];
			for (int i = 1; i < processedProjectilesCount; ++i) {
				projectiles[i].currentAngle = projectiles[i - 1].currentAngle + 2f * (float)Math.PI / processedProjectilesCount;
			}
		}
		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();

			recipe.AddIngredient(ItemID.IronBar, 10);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);

			recipe.AddTile(TileID.DemonAltar);

			recipe.Register();
		}
	}
}