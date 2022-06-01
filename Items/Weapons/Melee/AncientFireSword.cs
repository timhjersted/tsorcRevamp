using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class AncientFireSword : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The blade is a magic flame, slicing quickly. \n" +
                                "Will set enemies ablaze and do damage over time.");

        }

        public override void SetDefaults() {

            Item.stack = 1;
            //item.prefixType=121;
            Item.rare = ItemRarityID.Green;
            Item.damage = 22;
            Item.height = 32;
            Item.knockBack = (float)5.5;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = (float)1.05;
            Item.useAnimation = 17;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 17;
            Item.value = PriceByRarity.Green_2;
            Item.width = 40;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);

            recipe.AddIngredient(ItemID.GoldBroadsword, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 3000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void OnHitNPC(Player player, NPC npc, int damage, float knockBack, bool crit) {
            if (Main.rand.Next(2) == 0) { //50% chance to occur
                npc.AddBuff(BuffID.OnFire, 360, false);
            }
        }

        public override void MeleeEffects(Player player, Rectangle rectangle) { 
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }

    }
}
