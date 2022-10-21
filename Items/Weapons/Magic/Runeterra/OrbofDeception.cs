using tsorcRevamp.Projectiles.Magic.Runeterra;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    public class OrbofDeception : ModItem
    {
        public static int useOrbofDeception = 0;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Orb of Deception");
            Tooltip.SetDefault("Throws a magic orb which will return to you after a certain distance" +
                "\nYou cannot throw more than one orb at a time" +
                "\nYou can recast with mana to force it to return early" +
                "\nThe orb deals more damage on the way back" +
                "\nEach hit gathers a stack of Essence Thief, crits gather 2" +
                "\nUpon reaching 9 stacks, the next cast will have 10% lifesteal");

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
            Item.damage = 40;
            Item.autoReuse = false;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.mana = 25;
            Item.DamageType = DamageClass.Magic;
            Item.knockBack = 1f;
        }

        public override bool? UseItem(Player player)
        {
            if(useOrbofDeception == 0)
            {
                useOrbofDeception = 1;
            } else
            if (useOrbofDeception == 1)
            {
                useOrbofDeception = 2;
            }
            return true;
        }
        public override void HoldItem(Player player)
        {
            bool OrbofDeceptionProjExists = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<OrbofDeceptionProj>() && Main.projectile[i].owner == player.whoAmI)
                {
                    OrbofDeceptionProjExists = true;
                    break;
                }
            }
            if (!OrbofDeceptionProjExists)
            {
                var projectile = Projectile.NewProjectileDirect(Projectile.GetSource_None(), player.Center, Vector2.Zero, ModContent.ProjectileType<OrbofDeceptionProj>(), Item.damage, Item.knockBack, Main.myPlayer);
                projectile.originalDamage = (int)player.GetTotalDamage(DamageClass.Magic).ApplyTo(Item.damage);
            }
        }/*
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.ShadowOrb, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 7000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }*/
    }
}