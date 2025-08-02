using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Axes
{
    public class ForgottenPoisonAxe : ModItem
    {
        public const int CoinPrice = 2500;
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("The blade has been dipped in poison.");
        }
        public override void SetDefaults()
        {

            Item.rare = ItemRarityID.Pink;
            Item.damage = 72;
            Item.width = 63;
            Item.height = 58;
            Item.knockBack = 5.4f;
            Item.DamageType = DamageClass.Melee;
            Item.axe = 110 / 5; // same as the Pickaxe axe
            Item.scale = 1.2f;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 25;
            Item.value = PriceByRarity.Pink_5;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.YellowGreen;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextFloat() < 0.20f)
            {
                target.AddBuff(BuffID.Venom, 6 * 60, false);
            }
            else
            {
                target.AddBuff(BuffID.Poisoned, 5 * 60, false);
            }
        }
        
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<ForgottenRuneAxe>());
            recipe.AddIngredient(ItemID.SoulofNight, 6);
            recipe.AddIngredient(ItemID.Stinger, 6);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
