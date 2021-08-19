using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class Ice1Tome : ModItem {

        bool LegacyMode = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

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
            item.damage = LegacyMode ? 10 : 11;
            item.height = 10;
            item.knockBack = 0f;
            item.channel = true;
            item.autoReuse = true;
            item.rare = ItemRarityID.Green;
            item.shootSpeed = 9;
            item.magic = true;
            item.noMelee = true;
            //item.mana = LegacyMode ? 5 : 8;
            item.mana = 8;
            storeManaCost = item.mana;
            item.useAnimation = LegacyMode ? 10 : 19;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = LegacyMode ? 10 : 19;
            item.value = 200000;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.Ice1Ball>();
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            item.mana = storeManaCost;
            //How many projectiles exist that are being channeled (controlled) by the player using this tome
            int projCount = 0;

            //Iterate through the projectile array
            for(int i = 0; i < Main.projectile.Length; i++)
            {
                //For each, check if it's modded. If so, check if it's the Ice1Ball. If so, check if it's owned by this player (to prevent other players projectiles counting against it)
                if (Main.projectile[i].modProjectile != null && Main.projectile[i].modProjectile is Projectiles.Ice1Ball && (Main.projectile[i].owner == player.whoAmI))
                {
                    //Cast it to an Ice1Ball so we can check if it's currently being channeled
                    if (((Projectiles.Ice1Ball)Main.projectile[i].modProjectile).isChanneled && Main.projectile[i].active)
                    {
                        //If so, then up the count
                        projCount++;
                    }
                }
            }

            //If there's 5, don't fire any more
            if (projCount < 5) return true;
            else
            {
                //This is how much mana it will use while channeling when it can not fire another projectile
                //Setting this to 0 would make it consume no mana
                item.mana = 3;
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
