using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
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
            
            item.width = 40;
            item.height = 40;
            item.useStyle =ItemUseStyleID.SwingThrow;
            //item.prefixType=368;
            item.useAnimation = 21;
            item.autoReuse = true;
            item.useTime = 21;
            item.maxStack = 1;
            item.damage = 17;
            item.knockBack = (float)5;
            item.useTurn = true;
            item.scale = (float)1.1;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.Green;
            item.value = 10000;
            item.melee = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.GoldBroadsword, 1);
            recipe.AddIngredient(ModLoader.GetMod("DarkSouls"), "DarkSoul", 3000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void ModifyHitNPC( Terraria.Player P, NPC target, ref int damage, ref float knockBack, ref bool crit )
        {
            if (Main.rand.Next(15) == 0)
            {
                P.HealEffect(damage / 2);
                P.statLife += (damage / 2);
            }
        }

        public override void MeleeEffects( Terraria.Player player, Rectangle rectangle )
        {
            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 15, (player.velocity.X * 0.2f) + (player.direction * 2), player.velocity.Y * 0.2f, 100, color, 1f);
            Main.dust[dust].noGravity = false;
        }

    }
}
