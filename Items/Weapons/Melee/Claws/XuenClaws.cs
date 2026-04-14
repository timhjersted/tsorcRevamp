using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Claws
{
    class XuenClaws : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.damage = 180;
            Item.width = 22;
            Item.height = 22;
            Item.scale = 1.05f;
            Item.knockBack = 3;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 8;
            Item.useTime = 8;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Purple_11;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Cyan;
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Buffs.ElectrocutedBuff3>(), 4 * 60);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BurningFist>());
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>());
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 15);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
