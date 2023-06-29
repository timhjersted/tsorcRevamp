using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon
{
    public class PhotonicDownpour : ModItem
	{
		public override void SetStaticDefaults()
		{

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
			ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
			ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
        }
		public override void SetDefaults()
		{
			Item.damage = 100;
			Item.knockBack = 3f;
			Item.mana = 10;
			Item.width = 42;
			Item.height = 28;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.value = Item.buyPrice(1, 50, 0, 0);
			Item.rare = ItemRarityID.Expert;
			Item.expert = true;
			Item.UseSound = SoundID.Item44;

			Item.noMelee = true;
			Item.DamageType = DamageClass.Summon;
			Item.buffType = ModContent.BuffType<Buffs.Summon.PhotonicDownpourBuff>();
			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.PhotonicDownpourLaserDrone>();
		}

		int triadType;
		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
		{
			// Here you can change where the minion is spawned. Most vanilla minions spawn at the cursor position
			position = Main.MouseWorld;

			if (Main.MouseWorld.Distance(player.Center) > 300)
			{
				type = ModContent.ProjectileType<Projectiles.Summon.PhotonicDownpourLaserDrone>();
            }
			else
			{
				type = ModContent.ProjectileType<Projectiles.Summon.PhotonicDownpourDefenseDrone>();
			}
		}

        public override void HoldItem(Player player)
        {
			if (Main.myPlayer == player.whoAmI)
			{
				float minionCount = 0;
				for (int i = 0; i < Main.maxProjectiles; i++)
				{
					if (Main.projectile[i].active && Main.projectile[i].minion && Main.projectile[i].owner == player.whoAmI)
					{
						minionCount += Main.projectile[i].minionSlots;
					}
				}
				if (minionCount < player.maxMinions) {
					int dustType = DustID.CursedTorch;
					if (Main.MouseWorld.Distance(player.Center) < 300)
					{
						dustType = DustID.Torch;
					}
					UsefulFunctions.DustRing(Main.MouseWorld, 70, dustType, 15);
				} }
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
		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ModContent.ItemType<DamagedMechanicalScrap>());
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25000);
			recipe.AddIngredient(ItemID.SoulofFright, 5);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}