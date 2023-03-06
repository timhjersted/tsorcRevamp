using tsorcRevamp.Projectiles.Magic.Runeterra;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    public class OrbofFlame : ModItem
    {
        public static int useOrbofFlame = 0;
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Orb of Flame");
            /* Tooltip.SetDefault("Throws a flaming orb which will return to you after a certain distance" +
                "\nYou cannot throw more than one orb at a time" +
                "\nYou can recast with mana to force it to return early" +
                "\nThe orb deals more damage on the way back" +
                "\nEach hit gathers a stack of Essence Thief, crits gather 2" +
                "\nUpon reaching 9 stacks, the next cast will have 10% lifesteal" +
                "\nand spawn a homing flame on-hit(2 on-crit)"); */

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
            Item.damage = 115;
            Item.autoReuse = false;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = Item.buyPrice(0, 30, 0, 0);
            Item.mana = 50;
            Item.DamageType = DamageClass.Magic;
            Item.knockBack = 1f;
        }

        public override bool? UseItem(Player player)
        {
            if(useOrbofFlame == 0)
            {
                useOrbofFlame = 1;
            } else
            if (useOrbofFlame == 1)
            {
                useOrbofFlame = 2;
            }
            return true;
        }
        public override void HoldItem(Player player)
        {
            bool OrbofFlameProjExists = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<OrbofFlameProj>() && Main.projectile[i].owner == player.whoAmI)
                {
                    OrbofFlameProjExists = true;
                    break;
                }
            }
            if (!OrbofFlameProjExists)
            {
                var projectile = Projectile.NewProjectileDirect(Projectile.GetSource_None(), player.Center, Vector2.Zero, ModContent.ProjectileType<OrbofFlameProj>(), Item.damage, Item.knockBack, Main.myPlayer);
                projectile.originalDamage = (int)player.GetTotalDamage(DamageClass.Magic).ApplyTo(Item.damage);
            }
        }/*
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<OrbofDeception>());
            recipe.AddIngredient(ItemID.HallowedBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }*/
    }
}