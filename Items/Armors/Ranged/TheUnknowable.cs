using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Head)]
    public class TheUnknowable : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("25% chance to not consume ammo" +
                "\nIncreases ranged critical strike chance by 17%" +
                "\nSet Bonus: Grants Holy Dodge, stats provided by this armor set are doubled while Holy Dodge is active" +
                "\nDefense and ammo consumption chance are not affected by this");
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
            player.ammoCost75 = true;

            player.GetCritChance(DamageClass.Ranged) += 17;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.GetCritChance(DamageClass.Ranged) += 17;
            }
        }

        public override void UpdateArmorSet(Player player)
        {
            player.onHitDodge = true;
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
