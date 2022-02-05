using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class Ice2Tome : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ice 2 Tome");
            Tooltip.SetDefault("A lost tome for artisans, with a high rate of casting." +
                                "\nCan be upgraded with 25,000 Dark Souls and 15 Souls of Light.");
        }

        //This stores the original, true mana cost of the item. We have to change item.mana later to cause it to use less/none while it's not actually firing
        int storeManaCost2;
        public override void SetDefaults() {
            item.damage =  15;
            item.height = 10;
            item.knockBack = 0f;
            item.rare = ItemRarityID.Orange;
            item.channel = true;
            item.autoReuse = true;
            item.shootSpeed = 10;
            item.magic = true;
            item.noMelee = true;
            item.mana = 14;
            storeManaCost2 = item.mana;
            item.useAnimation = 19;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 19;
            item.value = PriceByRarity.Orange_3;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.Ice2Ball>();
        }

        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Ice2Ball>()] < 5 && player == Main.LocalPlayer)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("Ice1Tome"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 8000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
