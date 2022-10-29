using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Hammers
{
    public class Mjolnir : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Shatter the earth" +
                                "\nBreaks walls and trees with amazing speed" +
                                "\nAlso retains the pickaxe strength of the Molten Pickaxe" +
                                "\nHold the cursor away from you to wield only as a weapon" +
                                "\nSummons electrospheres upon hitting an enemy");
        }
        public override void SetDefaults()
        {
            Item.width = 41;
            Item.height = 42;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 30;
            Item.useTime = 10;
            Item.pick = 100;
            Item.axe = 120;
            Item.hammer = 120;
            Item.damage = 64;
            Item.knockBack = 15;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.scale = 1.4f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Melee;
        }
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), target.Center, Vector2.Zero, ProjectileID.Electrosphere, damage, knockBack, Main.myPlayer);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MoltenPickaxe, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 12000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void MeleeEffects(Player player, Rectangle rectangle)
        {
            Color color = new Color();
            //This is the same general effect done with the Fiery Greatsword
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 15, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, color, 1.0f);
            Main.dust[dust].noGravity = true;
        }

    }
}
