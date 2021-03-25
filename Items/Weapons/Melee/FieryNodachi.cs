using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    public class FieryNodachi : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Fiery Nodachi");
            Tooltip.SetDefault("");

	}

        public override void SetDefaults()
        {
            
            //item.prefixType=121;
            item.rare = ItemRarityID.Orange;
            item.damage = 36;
            item.width = 48;
            item.height = 44;
            item.knockBack = (float)6;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1.1;
            item.useAnimation = 35;
            item.UseSound = SoundID.Item1;
            item.useStyle =ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 25000;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.HellstoneBar, 33);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1100);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void OnHitNPC( Player player, NPC npc, int damage, float knockBack, bool crit )
        {
            if (Main.rand.Next(2) == 0)
            { //50% chance to occur
                npc.AddBuff(24, 360, false); //Light 'em on fire! 
                                             //24 is for onFire buff, 20 is for poisoned buff
            }
        }
        public override void MeleeEffects( Player player, Rectangle rectangle )
        {
            Color color = new Color();
            //This is the same general effect done with the Fiery Greatsword
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, color, 1.9f);
            Main.dust[dust].noGravity = true;
        }

    }
}
