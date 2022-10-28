using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class ShatteredPrism : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Holding this item surrounds you in a prism that gathers power from grazing enemy attacks" +
                "\nFiring it unleahes this energy in a furious blast");

        }

        public override void SetDefaults()
        {

            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTurn = true;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.maxStack = 1;
            Item.damage = 170;
            Item.autoReuse = true;
            Item.knockBack = (float)4;
            Item.scale = (float)1;
            Item.UseSound = SoundID.Item34;
            //item.projectile=Sandstorm;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = (float)10;
            Item.crit = 2;
            Item.mana = 14;
            Item.noMelee = true;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.Magic.ShatteredPrism>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            //recipe.AddIngredient(ItemID.MeteoriteBar, 25);
            //recipe.AddIngredient(ItemID.SandBlock, 150);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe.AddIngredient(Mod.Find<ModItem>("FlameOfTheAbyss").Type, 20);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 120000);
            recipe.AddTile(TileID.DemonAltar);

            //recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            int spread = 10;
            float num48 = 14f;

            Vector2 speed, position = player.position;

            speed.X = ((Main.mouseX + Main.screenPosition.X) - (player.position.X + player.width * 0.5f)) + Main.rand.Next(-spread, spread);
            speed.Y = ((Main.mouseY + Main.screenPosition.Y) - (player.position.Y + player.height * 0.5f)) + Main.rand.Next(-spread, spread);
            float num51 = (float)Math.Sqrt((double)((speed.X * speed.X) + (speed.Y * speed.Y)));
            num51 = num48 / num51;
            speed.X *= num51;
            speed.Y *= num51;

            if ((player.direction == -1) && ((Main.mouseX + Main.screenPosition.X) > (player.position.X + player.width * 0.5f)))
            {
                player.direction = 1;
            }
            if ((player.direction == 1) && ((Main.mouseX + Main.screenPosition.X) < (player.position.X + player.width * 0.5f)))
            {
                player.direction = -1;
            }

            if (player.direction == 1)
            {
                player.itemRotation = (float)Math.Atan2((Main.mouseY + Main.screenPosition.Y) - (player.position.Y + player.height * 0.5f), (Main.mouseX + Main.screenPosition.X) - (player.position.X + player.width * 0.5f));
            }
            else
            {
                player.itemRotation = (float)Math.Atan2((player.position.Y + player.height * 0.5f) - (Main.mouseY + Main.screenPosition.Y), (player.position.X + player.width * 0.5f) - (Main.mouseX + Main.screenPosition.X));
            }

            position.X += player.width * 0.5f;
            position.Y += player.height * 0.5f;
            int damage = (int)(player.GetTotalDamage(DamageClass.Magic).ApplyTo(Item.damage));
            float knockback = player.inventory[player.selectedItem].knockBack;

            Projectile.NewProjectile(player.GetSource_ItemUse(Item), position, speed, ModContent.ProjectileType<Projectiles.Sandstorm>(), damage, knockback, player.whoAmI);

            return true;
        }
    }
}
