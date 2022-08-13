/*
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Weapons.Runeterra.Magic
{
    public class OoDItem3 : ModItem
    {
        public static int useOoDItem3 = 0;
        public static bool OoDOrb3Exists = false;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Orb of Deception");
            Tooltip.SetDefault("Throws a magic orb which will return to you after a certain distance" +
                "\nYou cannot throw more than one orb at a time" +
                "\nYou can recast with mana to force it to return early" +
                "\nThe orb deals more damage on the way back" +
                "\nEach hit gathers a stack of Essence Thief, crits gather 2" +
                "\nUpon reaching 9 stacks, the next cast will have 10% lifesteal" +
                "\nand spawn a homing flame on-hit(2 on-crit)" +
                "\nRight click to dash in mouse direction and spawn homing flames while dashing" +
                "\nThis has a 90 second cooldown");

        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.channel = true;
            Item.useTurn = false;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.holdStyle = ItemHoldStyleID.HoldFront;
            Item.useAnimation = 10;
            Item.useTime = 10;
            Item.damage = 100;
            Item.autoReuse = false;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.mana = 60;
            Item.DamageType = DamageClass.Magic;
            Item.knockBack = 1f;
        }

        public override bool? UseItem(Player player)
        {
            if(useOoDItem3 == 0)
            {
                useOoDItem3 = 1;
            } else
            if (useOoDItem3 == 1)
            {
                useOoDItem3 = 2;
            }
            return true;
        }
        public override void HoldItem(Player player)
        {

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<OoDOrb3>() && Main.projectile[i].owner == player.whoAmI)
                {
                    OoDOrb3Exists = true;
                    break;
                }
            }
            if (!OoDOrb3Exists)
            {
                var projectile = Projectile.NewProjectileDirect(Projectile.GetSource_None(), player.Center, Vector2.Zero, ModContent.ProjectileType<OoDOrb3>(), Item.damage, Item.knockBack, Main.myPlayer);
                projectile.originalDamage = (int)player.GetTotalDamage(DamageClass.Magic).ApplyTo(Item.damage);
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<OoDItem2>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}*/