using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class SagittariusBow : ModItem {
        bool LegacyMode = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetDefaults() {
            if (LegacyMode) {
                item.autoReuse = true;
                item.damage = 500;
                item.height = 28;
                item.knockBack = 12;
                item.noMelee = true;
                item.ranged = true;
                item.rare = ItemRarityID.LightPurple;
                item.shootSpeed = 16;
                item.useAnimation = 60;
                item.useTime = 60;
                item.UseSound = SoundID.Item5;
                item.shoot = ProjectileID.WoodenArrowFriendly;
                item.useStyle = ItemUseStyleID.HoldingOut;
                item.value = 19000000;
                item.width = 14;
                item.useAmmo = AmmoID.Arrow; 
            }

            //revamp sagittarius
            else {
                item.ranged = true;
                item.shoot = ModContent.ProjectileType<Projectiles.SagittariusBowHeld>();
                item.channel = true;

                item.damage = 477;
                item.width = 14;
                item.height = 28;
                item.useTime = 60;
                item.useAnimation = 60;
                item.useStyle = ItemUseStyleID.HoldingOut;
                item.noMelee = true;
                item.noUseGraphic = true;
                item.knockBack = 5f;
                item.value = Item.sellPrice(platinum: 4);
                item.rare = ItemRarityID.LightPurple;
                item.UseSound = SoundID.Item7;

                item.shootSpeed = 20f;
            }
        }

        public override bool CanUseItem(Player player) {
            if (LegacyMode) {
                return base.CanUseItem(player);
            }
            else return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.SagittariusBowHeld>()] <= 0;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            if (LegacyMode) {
                recipe.AddIngredient(mod.GetItem("ArtemisBow"), 1);
                recipe.AddIngredient(mod.GetItem("CursedSoul"), 70);
                recipe.AddIngredient(mod.GetItem("BlueTitanite"), 25);
                recipe.AddIngredient(mod.GetItem("FlameOfTheAbyss"), 40);
                recipe.AddIngredient(mod.GetItem("DarkSoul"), 250000);
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this, 1);
                recipe.AddRecipe(); 
            }

            else {
                recipe.AddIngredient(mod.GetItem("ArtemisBow"), 1);
                recipe.AddIngredient(mod.GetItem("BlueTitanite"), 5);
                recipe.AddIngredient(mod.GetItem("DarkSoul"), 90000);
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this, 1);
                recipe.AddRecipe(); 
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                //find the knockback tooltip line
                int ttindex = tooltips.FindLastIndex(t => t.mod == "Terraria" && t.Name == "Knockback");
                if (ttindex != -1) {// if we find one
                    //insert the extra tooltip line
                    tooltips.Insert(ttindex + 1, new TooltipLine(mod, "RevampSagittarius1", "Fires two arrows."));
                    tooltips.Insert(ttindex + 2, new TooltipLine(mod, "RevampSagittarius2", "Hold FIRE to charge."));
                    tooltips.Insert(ttindex + 3, new TooltipLine(mod, "RevampSagittarius3", "Arrows are faster and more accurate when the bow is charged."));
                }
            }
        }
    }
}
