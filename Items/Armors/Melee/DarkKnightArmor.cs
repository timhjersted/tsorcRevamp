using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Body)]
    public class DarkKnightArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+30 % Melee Critical Chance\nThe fiercest armor for melee warriors\nSet bonus grants waterwalk, no knockback, plus 30% boost to Melee Speed, Melee Damage and Move Speed\nSmall chance to regain life from melee strikes");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 25;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += 30;
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().MeleeArmorVamp10 = true;
        }

        /*
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            if (Main.rand.NextBool(2))
            {
                player.HealEffect(damage / 10);
                player.statLife += (damage / 10);
            }
        }*/

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedPlateMail, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
