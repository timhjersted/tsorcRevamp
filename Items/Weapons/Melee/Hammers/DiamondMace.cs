using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Hammers
{
    [LegacyName("ForgottenDiamondMace")]
    class DiamondMace : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("A mace made of a large diamond. Has a 1 in 15 chance to heal 20 life.");
        }

        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.rare = ItemRarityID.Pink;
            Item.damage = 120;
            Item.scale = 1.15f;
            Item.crit += 46;
            Item.width = 47;
            Item.height = 47;
            Item.knockBack = 8;
            Item.hammer = 80; // a bit more than molten hamaxe
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.UseSound = SoundID.Item1;
            Item.value = PriceByRarity.Pink_5;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.White;
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(10))
            {
                player.statLife += 15;
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 40000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
