using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class Ice1Tome : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ice 1 Tome");
            Tooltip.SetDefault("A lost beginner's tome" +
                "\nMultiple shots can be controlled and stacked" +
                "\nallowing for high burst damage" +
                "\nDrops a small icicle upon collision" +
                "\nCan be upgraded");
        }

        //This stores the original, true mana cost of the item. We have to change item.mana later to cause it to use less/none while it's not actually firing
        int storeManaCost;
        public override void SetDefaults() {
            item.damage = 11;
            item.height = 10;
            item.knockBack = 0f;
            item.channel = true;
            item.autoReuse = true;
            item.rare = ItemRarityID.Green;
            item.shootSpeed = 9;
            item.magic = true;
            item.noMelee = true;
            item.mana = 8;
            storeManaCost = item.mana;
            item.useAnimation = 19;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 19;
            item.value = PriceByRarity.Green_2;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.Ice1Ball>();
        }

        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Ice1Ball>()] < 5)
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
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
