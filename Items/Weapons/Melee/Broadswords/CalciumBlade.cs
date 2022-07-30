using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    [LegacyName("BoneBlade")]
    public class CalciumBlade : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("'A blade of sharpened bone'" +
                "\nShoots out a bone upon hitting enemies with the blade");
        }
        public bool canitshoot = false;

        public override void SetDefaults()
        {
            Item.width = 61;
            Item.height = 74;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.maxStack = 1;
            Item.damage = 33;
            Item.knockBack = 7f;
            Item.scale = 0.9f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.Green_2;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ProjectileID.Bone;
            Item.shootSpeed = 10f;
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            canitshoot = true;
        }
        public override bool CanShoot(Player player)
        {
            if (canitshoot == false)
            {
                return false;
            }
            else
            {
                canitshoot = false;
                return true;
            }
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.Bone, 35);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}
