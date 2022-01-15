using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class Ice3Tome : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ice 3 Tome");
            Tooltip.SetDefault("A lost tome fabled to deal great damage.\n" +
                                "Only mages will be able to realize this tome's full potential. \n" +
                                "Can be upgraded with 80,000 Dark Souls");

        }

        //This stores the original, true mana cost of the item. We have to change item.mana later to cause it to use less/none while it's not actually firing
        int storeManaCost3;
        public override void SetDefaults() {
            item.autoReuse = true; //why was it the only one without autoreuse?
            item.damage = 32;
            item.height = 10;
            item.knockBack = 0f;
            item.maxStack = 1;
            item.rare = ItemRarityID.LightRed;
            item.scale = 1;
            item.channel = true;
            item.shootSpeed = 10;
            item.magic = true;
            item.noMelee = true;
            item.mana = 30;
            storeManaCost3 = item.mana;
            item.useAnimation = 10;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 10;
            item.value = PriceByRarity.LightRed_4;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.Ice3Ball>();
        }

        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Ice3Ball>()] < 5)
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
            recipe.AddIngredient(mod.GetItem("Ice2Tome"), 1);
            recipe.AddIngredient(ItemID.SoulofLight, 15);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 25000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
