using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class Ice4Tome : ModItem {


        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ice 4 Tome");
            Tooltip.SetDefault("A lost legendary tome.");
        }

        //This stores the original, true mana cost of the item. We have to change item.mana later to cause it to use less/none while it's not actually firing
        int storeManaCost4;
        public override void SetDefaults() {
            item.damage = 120;
            item.height = 10;
            item.knockBack = 0f;
            item.maxStack = 1;
            item.rare = ItemRarityID.Green;
            item.channel = true;
            item.autoReuse = true;
            item.shootSpeed = 11;
            item.magic = true;
            item.noMelee = true;
            item.mana = 100;
            storeManaCost4 = item.mana;
            item.useAnimation = 20;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 20;
            item.value = 600000;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.Ice4Ball>();
        }

        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Ice4Ball>()] < 5)
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
            recipe.AddIngredient(mod.GetItem("Ice3Tome"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 80000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
