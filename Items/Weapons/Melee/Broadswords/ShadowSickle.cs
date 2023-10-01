using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Items.Materials;
using Terraria.Localization;
using tsorcRevamp.Buffs.Weapons.Melee;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class ShadowSickle : ModItem
    {
        public const int BaseDamage = 51;
        public static int ManaRefund = 15;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ManaRefund);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Blue;
            Item.damage = BaseDamage;
            Item.width = 32;
            Item.height = 32;
            Item.knockBack = 6f;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = 13500;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.DarkMagenta;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<SickleSlashes>(), 5 * 60);
            player.statMana += ManaRefund;
        }

        public override void UpdateInventory(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().HasShadowSickle = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.DemoniteBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
