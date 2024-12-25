using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Summon.Whips;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    public class SupremeDragoonLash : ModItem
    {
        public static float SummonTagCrit = 8;
        public const int BaseDamage = 80;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
        }

        public override void SetDefaults()
        {
            Item.height = 48;
            Item.width = 60;

            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.damage = BaseDamage;
            Item.knockBack = 3.8f;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 70, 0, 0);

            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.SupremeDragoonLashProjectile>();
            Item.shootSpeed = 4;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
            if (ttindex != -1)
            {
                tooltips.Insert(ttindex, new TooltipLine(Mod, "TagDmg", Language.GetTextValue(Tooltip.Key + "0", (int)(SummonTagCrit * Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SummonTagStrength))));
            }
        }
        public override bool MeleePrefix()
        {
            return true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DragoonLash>(), 1);
            recipe.AddIngredient(ModContent.ItemType<FlameOfTheAbyss>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 5);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 20);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 90000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}