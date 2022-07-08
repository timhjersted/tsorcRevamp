using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    public class YianBlade : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Yian Blade");
            Tooltip.SetDefault("Random chance to steal life from attacks");

        }

        public override void SetDefaults()
        {

            Item.width = 40;
            Item.height = 40;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 21;
            Item.autoReuse = true;
            Item.useTime = 21;
            Item.maxStack = 1;
            Item.damage = 17;
            Item.knockBack = (float)5;
            Item.useTurn = true;
            Item.scale = (float)1.1;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.Blue_1;
            Item.DamageType = DamageClass.Melee;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.GoldBroadsword, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void ModifyHitNPC(Player P, NPC target, ref int damage, ref float knockBack, ref bool crit)
        {
            if (Main.rand.NextBool(15))
            {
                P.HealEffect(damage / 2);
                P.statLife += (damage / 2);
            }
        }

        public override void MeleeEffects(Player player, Rectangle rectangle)
        {
            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 15, (player.velocity.X * 0.2f) + (player.direction * 2), player.velocity.Y * 0.2f, 100, color, 1f);
            Main.dust[dust].noGravity = false;
        }

    }
}
