using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Head)]
    public class TheUnknowable : ModItem
    {
        public static int AmmoChance = 25; //changing this number has no effect since an ammo consumption chance stat doesn't exist
        public static float CritChance = 15f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AmmoChance, CritChance);
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 15;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<TheUnforseeable>() && legs.type == ModContent.ItemType<TheUntouchable>();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Ranged) += CritChance;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.GetCritChance(DamageClass.Ranged) += CritChance;
            }
        }

        public override void UpdateArmorSet(Player player)
        {
            player.onHitDodge = true;
            player.ammoCost75 = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedHelmet, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
