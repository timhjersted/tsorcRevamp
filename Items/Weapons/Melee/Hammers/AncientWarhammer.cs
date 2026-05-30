using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Items.Weapons.Melee.Hammers
{
    class AncientWarhammer : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ancient Warhammer");
            // Tooltip.SetDefault("An old choice for advancing druids");

        }

        public override void SetDefaults()
        {
            Item.hammer = 60; // Same as meteor hamaxe
            Item.rare = ItemRarityID.Green;
            Item.DamageType = DamageClass.Melee;
            Item.damage = 56;
            Item.scale = 1.2f;
            Item.width = 42;
            Item.height = 42;
            Item.knockBack = 9f;
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.useAnimation = 38;
            Item.useTime = 38;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Green_2;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.DarkGray;
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<DefenseCrush>(), 600);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.TheBreaker);
            recipe.AddIngredient(ItemID.PlatinumBar, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
