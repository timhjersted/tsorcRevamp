using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors.Magic;
using tsorcRevamp.Items.Armors.Ranged;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class YianBlade : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Yian Blade");
            // Tooltip.SetDefault("Random chance to steal life from attacks");

        }

        public override void SetDefaults()
        {

            Item.width = 44;
            Item.height = 44;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 21;
            Item.autoReuse = true;
            Item.useTime = 21;
            Item.maxStack = 1;
            Item.damage = 18;
            Item.knockBack = (float)5;
            Item.useTurn = false;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.Blue_1;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.DarkMagenta;

        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.GoldBroadsword, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                player.ManaEffect(3);
                player.statMana += (3);
            }
            else
            {
                player.ManaEffect(5);
                player.statMana += (5);
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
