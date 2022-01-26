using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    public class ArtemisBow : ModItem {

        bool LegacyMode = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A bow forged to slay demon-gods.");

        }

        public override void SetDefaults() {
            if (LegacyMode) {
                item.ranged = true;
                item.shoot = ProjectileID.PurificationPowder;

                item.damage = 400;
                item.width = 24;
                item.height = 60;
                item.knockBack = 19;
                item.maxStack = 1;
                item.noMelee = true;
                item.rare = ItemRarityID.Pink;
                item.scale = (float)0.8;
                item.shootSpeed = 16;
                item.useAmmo = AmmoID.Arrow;
                item.UseSound = SoundID.Item5;
                item.useStyle = ItemUseStyleID.HoldingOut;
                item.useAnimation = 50;
                item.useTime = 50;
                item.value = 3100000; 
            }

            //revamp
            else {
                item.ranged = true;
                item.shoot = ModContent.ProjectileType<Projectiles.ArtemisBowHeld>();
                item.channel = true;

                item.damage = 370;
                item.width = 14;
                item.height = 28;
                item.useTime = 60;
                item.useAnimation = 60;
                item.useStyle = ItemUseStyleID.HoldingOut;
                item.noMelee = true;
                item.noUseGraphic = true;
                item.knockBack = 15f;
                item.value = PriceByRarity.LightRed_4;
                item.rare = ItemRarityID.LightRed;
                item.UseSound = SoundID.Item7;

                item.shootSpeed = 18f;
            }
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.GoldBow, 1);
            recipe.AddIngredient(ItemID.ChlorophyteBar, 12);
            recipe.AddIngredient(ItemID.SoulofLight, 18);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 75000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                //find the knockback tooltip line
                int ttindex = tooltips.FindLastIndex(t => t.mod == "Terraria" && t.Name == "Knockback");
                if (ttindex != -1) {// if we find one
                    //insert the extra tooltip line
                    tooltips.Insert(ttindex + 1, new TooltipLine(mod, "RevampArtemis1", "Hold FIRE to charge."));
                    tooltips.Insert(ttindex + 2, new TooltipLine(mod, "RevampArtemis2", "Arrows are faster and more accurate when the bow is charged."));
                }
            }
        }

    }
}
