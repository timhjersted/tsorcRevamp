using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Magic.RTQ2Set
{
    [AutoloadEquip(EquipType.Body)]
    public class RTQ2Chestplate : ModItem
    {
        public static float CritChance = 16f;
        public static float AtkSpeed = 10f;
        public static float LifeThreshold = 40f;
        public static int Defense = 15;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance, AtkSpeed, LifeThreshold, Defense);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 8;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += CritChance;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<RTQ2Helmet>() && legs.type == ModContent.ItemType<RTQ2Leggings>();
        }
        public override void UpdateArmorSet(Player player)
        {
            var modPlayer = player.GetModPlayer<RTQ2SetPlayer>();
            modPlayer.Equipped = true;
            bool isVisible = player.armor[10].IsAir && player.armor[11].IsAir && player.armor[12].IsAir;
            if (isVisible)
            {
                modPlayer.Visual = true;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MeteorSuit, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
