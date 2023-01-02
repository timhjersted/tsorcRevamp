using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Hammers
{
    [LegacyName("ForgottenDiamondMace")]
    class DiamondMace : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A mace made of a large diamond. Has a 1 in 15 chance to heal 20 life.");
        }

        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.rare = ItemRarityID.Pink;
            Item.damage = 90;
            Item.crit += 46;
            Item.width = 47;
            Item.height = 47;
            Item.knockBack = 10;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.UseSound = SoundID.Item1;
            Item.value = PriceByRarity.Pink_5;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
        }
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            if (Main.rand.NextBool(15))
            {
                player.statLife += 20;
                if (player.statLife > player.statLifeMax2)
                {
                    player.statLife = player.statLifeMax2;
                }
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteBar, 3);
            recipe.AddIngredient(ItemID.Diamond, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 44000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
