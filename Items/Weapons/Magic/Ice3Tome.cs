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
            item.rare = ItemRarityID.Green;
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
            item.value = 200000;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.Ice3Ball>();
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            item.mana = storeManaCost3;
            //How many projectiles exist that are being channeled (controlled) by the player using this tome
            int projCount = 0;

            //Iterate through the projectile array
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                //For each, check if it's modded. If so, check if it's the Ice3Ball. If so, check if it's owned by this player (to prevent other players projectiles counting against it)
                if (Main.projectile[i].modProjectile != null && Main.projectile[i].modProjectile is Projectiles.Ice3Ball && (Main.projectile[i].owner == player.whoAmI))
                {
                    //Cast it to an Ice3Ball so we can check if it's currently being channeled
                    if (((Projectiles.Ice3Ball)Main.projectile[i].modProjectile).isChanneled && Main.projectile[i].active)
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
                item.mana = 1;
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
