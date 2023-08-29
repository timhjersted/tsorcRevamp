using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee;

[AutoloadEquip(EquipType.Body)]
public class DarkKnightArmor : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("\nOne of the fiercest armors for melee warriors" +
            "\nIncreases melee damage by 15%" +
            "\nSet Bonus: Grants Holy Dodge, stats provided by this armor set are doubled while Holy Dodge is active" +
            "\nDefense is not affected by this" +
            "\nSmall chance to regain life from melee strikes while Holy Dodge is active");
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
        player.GetDamage(DamageClass.Melee) += 0.15f;

        if (player.HasBuff(BuffID.ShadowDodge))
        {
            player.GetDamage(DamageClass.Melee) += 0.15f;
        }
    }

    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        return head.type == ModContent.ItemType<DarkKnightHelmet>() && legs.type == ModContent.ItemType<DarkKnightGreaves>();
    }
    public override void UpdateArmorSet(Player player)
    {
        player.onHitDodge = true;

        if (player.HasBuff(BuffID.ShadowDodge))
        {
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().MeleeArmorVamp10 = true;

            int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 27, (player.velocity.X) + (player.direction * 3), player.velocity.Y, 100, Color.BlueViolet, 1.0f);
            Main.dust[dust].noGravity = true;
        }
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.HallowedPlateMail, 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
