using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Summon.Runeterra;
using tsorcRevamp.Buffs.Runeterra.Summon;

namespace tsorcRevamp.Items.Weapons.Summon.Runeterra
{
    [Autoload(true)]
    public class ScorchingPoint : ModItem
	{
		public static List<ScorchingPointFireball> projectiles = null;
		public static int processedProjectilesCount = 0;

		public override void SetStaticDefaults()
		{
			// DisplayName.SetDefault("Scorching Point");
			/* Tooltip.SetDefault("Summons fireballs to rotate around you and damage enemies in their way" +
								"\nUses half a minion slot" +
								"\nWhile holding this item, increase their radius by holding Special Ability" +
								"\nOr hold Shift + Special Ability to shrink their radius" +
                                "\nApplies the Scorching debuff, which grants minions crit chance based on a fraction of your critical strike chance" +
                				"\n'By the Ashen Lord of Runeterra!'"); */


			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
        }
    public override void SetDefaults()
		{
			projectiles = new List<ScorchingPointFireball>(){};

			Item.damage = 20;
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
			Item.buffType = ModContent.BuffType<CenterOfTheHeat>();
			Item.shoot = ModContent.ProjectileType<ScorchingPointFireball>();
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
            projectiles.Add((ScorchingPointFireball)projectile.ModProjectile);
            projectile.originalDamage = Item.damage;

            // Since we spawned the projectile manually already, we do not need the game to spawn it for ourselves anymore, so return false
            return false;
		}

        public override void HoldItem(Player player)
        {
			Lighting.AddLight(player.Center, new Vector3(0.1f, 0.08f, 0.05f));
		}

        public static void ReposeProjectiles(Player player) 
		{
            // repose projectiles relatively to the first one so they are evenly spread on the radial circumference
            List<ScorchingPointFireball> projectileList = new List<ScorchingPointFireball>();
            processedProjectilesCount = player.ownedProjectileCounts[ModContent.ProjectileType<ScorchingPointFireball>()];
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type == ModContent.ProjectileType<ScorchingPointFireball>() && Main.projectile[i].owner == player.whoAmI)
                {
                    projectileList.Add((ScorchingPointFireball)Main.projectile[i].ModProjectile);
                }
            }

            for (int i = 1; i < processedProjectilesCount; ++i)
            {
                projectileList[i].currentAngle = projectileList[i - 1].currentAngle + 2f * (float)Math.PI / processedProjectilesCount;
            }
        }
		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();

			recipe.AddIngredient(ItemID.FeralClaws);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);

			recipe.AddTile(TileID.DemonAltar);

			recipe.Register();
		}
	}
}