using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Weapons.Melee.Axes
{
    class ForgottenRuneAxe : ModItem
    {
        public const int BaseDmg = 15;
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.White;
            Item.damage = BaseDmg;
            Item.knockBack = 5f;
            Item.useAnimation = 28;
            Item.useTime = 28;
            Item.width = 56;
            Item.height = 46;
            Item.DamageType = DamageClass.Melee;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.White_0;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Gray;
        }
        public override void UpdateInventory(Player player)
        {
            if (Item.prefix == PrefixID.Dull)
            {
                Item.damage = BaseDmg - 5;
            }
            else
            {
                Item.damage = BaseDmg;
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Item.prefix == PrefixID.Dull)
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "CanBeBlessed", LangUtils.GetTextValue("CommonItemTooltip.CanBeBlessed")));
                }
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Wood, 10);
            recipe.AddIngredient(ItemID.StoneBlock, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
