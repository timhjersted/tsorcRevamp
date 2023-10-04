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
using tsorcRevamp.Items.Materials;
using Terraria.Localization;
using Terraria.Audio;
using Microsoft.Xna.Framework.Input;

namespace tsorcRevamp.Items.Weapons.Summon.Runeterra
{
    [LegacyName("InterstellarVesselControls")]
    public class InterstellarVesselGauntlet : ModItem
	{
		public static List<InterstellarVesselShip> projectiles = null;
		public static int processedProjectilesCount = 0;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ScorchingPoint.SummonTagDmgMult);
        public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 0.5f;
        }
		public override void SetDefaults()
		{
			projectiles = new List<InterstellarVesselShip>(){};

			Item.damage = 60;
			Item.knockBack = 5f;
			Item.mana = 10;
			Item.width = 32;
			Item.height = 34;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.holdStyle = ItemHoldStyleID.HoldFront;
			Item.noUseGraphic = true;
			Item.useTurn = false;
			Item.value = Item.buyPrice(0, 30, 0, 0);
			Item.rare = ItemRarityID.LightPurple;

			Item.noMelee = true;
			Item.DamageType = DamageClass.Summon;
			Item.buffType = ModContent.BuffType<InterstellarCommander>();
			Item.shoot = ModContent.ProjectileType<InterstellarVesselShip>();
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
			projectiles.Add((InterstellarVesselShip)projectile.ModProjectile);
			projectile.originalDamage = Item.damage;

            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.Runeterra.Dragons.ASOL13>()] == 0)
            {
                Projectile.NewProjectileDirect(source, position, Vector2.Zero, ModContent.ProjectileType<Projectiles.Summon.Runeterra.Dragons.ASOL13>(), 0, 0, Main.myPlayer);
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/InterstellarVessel/DragonCast") with { Volume = 1f });
            }
            else if (Main.rand.NextBool(2))
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/InterstellarVessel/ShipCast1") with { Volume = 1f });
            }
            else
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/InterstellarVessel/ShipCast2") with { Volume = 1f });
            }

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
			List<InterstellarVesselShip> projectileList = new List<InterstellarVesselShip>(); 
			processedProjectilesCount = player.ownedProjectileCounts[ModContent.ProjectileType<InterstellarVesselShip>()];
			for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type == ModContent.ProjectileType<InterstellarVesselShip>() && Main.projectile[i].owner == player.whoAmI)
                {
					projectileList.Add((InterstellarVesselShip)Main.projectile[i].ModProjectile);
				}
            }

			for (int i = 1; i < processedProjectilesCount; ++i) {
				projectileList[i].currentAngle2 = projectileList[i - 1].currentAngle2 + 2f * (float)Math.PI / processedProjectilesCount;
			}
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var SpecialAbilityKey = tsorcRevamp.specialAbility.GetAssignedKeys();
            string SpecialAbilityString = SpecialAbilityKey.Count > 0 ? SpecialAbilityKey[0] : Language.GetTextValue("Mods.tsorcRevamp.Keybinds.Special Ability.DisplayName") + Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.NotBound");
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex1 != -1)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.Items.ScorchingPoint.Keybind1") + SpecialAbilityString + Language.GetTextValue("Mods.tsorcRevamp.Items.ScorchingPoint.Keybind2")));
            }
            int ttindex2 = tooltips.FindIndex(t => t.Name == "Tooltip5");
            if (ttindex2 != -1)
            {
                tooltips.RemoveAt(ttindex2);
                tooltips.Insert(ttindex2, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.Items.InterstellarVesselGauntlet.Keybind1") + SpecialAbilityString + Language.GetTextValue("Mods.tsorcRevamp.Items.InterstellarVesselGauntlet.Keybind2")));
            }
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", Language.GetTextValue("Mods.tsorcRevamp.Items.InterstellarVesselGauntlet.Details", ScorchingPoint.MarkChance, ScorchingPoint.SuperBurnDuration, ScorchingPoint.SummonTagCrit)));
                }
            }
            else
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Shift", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.Details")));
                }
            }
        }
        public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();

			recipe.AddIngredient(ModContent.ItemType<ScorchingPoint>());
			recipe.AddIngredient(ItemID.ChlorophyteBar, 11);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);

			recipe.AddTile(TileID.DemonAltar);

			recipe.Register();
		}
	}
}