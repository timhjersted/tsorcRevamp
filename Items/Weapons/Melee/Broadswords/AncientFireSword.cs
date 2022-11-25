using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class AncientFireSword : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("The blade is a magic flame, slicing quickly. \n" +
                                "Will set enemies ablaze and do damage over time.");

        }

        public override void SetDefaults()
        {

            Item.stack = 1;
            //item.prefixType=121;
            Item.rare = ItemRarityID.Green;
            Item.damage = 22;
            Item.width = 34;
            Item.height = 38;
            Item.knockBack = 5.5f;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 17;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 17;
            Item.value = PriceByRarity.Green_2;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.GoldBroadsword, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void OnHitNPC(Player player, NPC npc, int damage, float knockBack, bool crit)
        {
            if (Main.rand.NextBool(2))
            { //50% chance to occur
                npc.AddBuff(BuffID.OnFire, 360, false);
            }
        }

        public override void MeleeEffects(Player player, Rectangle rectangle)
        {
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }

    }
}
