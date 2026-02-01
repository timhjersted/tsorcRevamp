using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    class AbyssalDeluge : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("A lost legendary tome.");
        }

        public override void SetDefaults()
        {
            Item.damage = 290;
            Item.height = 10;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.Purple;
            Item.shootSpeed = 3;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 35;
            Item.UseSound = SoundID.Item20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 33;
            Item.useAnimation = 33;
            Item.value = PriceByRarity.Purple_11;
            Item.width = 34;
            Item.autoReuse = true;
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
            if (Main.myPlayer == player.whoAmI)
            {
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), new Vector2(player.position.X + (player.width * 0.5f), player.position.Y + (player.height * 0.5f)), new Vector2((float)speedX, (float)speedY), ModContent.ProjectileType<Projectiles.Magic.AbyssalDelugeBall>(), (int)(player.GetTotalDamage(DamageClass.Magic).ApplyTo(player.inventory[player.selectedItem].damage)), 3, player.whoAmI);
            }
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 15);
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
