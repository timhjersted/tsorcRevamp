using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class DevilsScythe : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Devil's Scythe");
            Tooltip.SetDefault("Casts a hellfire scythe.");
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.damage = 40;
            Item.knockBack = 5;
            Item.scale = 0.9f;
            Item.UseSound = SoundID.Item8;
            Item.crit = 8;
            Item.rare = ItemRarityID.Orange;
            Item.mana = 14;
            Item.noMelee = true;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Magic;
        }

        public override bool? UseItem(Player player)
        {
            float num48 = .6f;
            float speedX = ((Main.mouseX + Main.screenPosition.X) - (player.position.X + player.width * 0.5f));
            float speedY = ((Main.mouseY + Main.screenPosition.Y) - (player.position.Y + player.height * 0.5f));
            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
            num51 = num48 / num51;
            speedX *= num51;
            speedY *= num51;
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), new Vector2(player.position.X + (player.width * 0.5f), player.position.Y + (player.height * 0.5f)), new Vector2((float)speedX, (float)speedY), ModContent.ProjectileType<Projectiles.DevilSickle>(), (int)(player.GetTotalDamage(DamageClass.Magic).ApplyTo(player.inventory[player.selectedItem].damage)), 3, player.whoAmI);
            return true;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.DemonScythe, 1);
            recipe.AddIngredient(ItemID.HellstoneBar, 30);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
