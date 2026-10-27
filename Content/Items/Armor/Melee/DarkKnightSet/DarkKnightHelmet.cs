using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Melee.DarkKnightSet
{
    [AutoloadEquip(EquipType.Head)]
    public class DarkKnightHelmet : ModItem
    {
        public const float CritChance = 17f;
        public const float Resistance = 15f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance);
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 15;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += CritChance;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.GetCritChance(DamageClass.Melee) += CritChance;
            }
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<DarkKnightArmor>() && legs.type == ModContent.ItemType<DarkKnightGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = Language.GetTextValue(Tooltip.Key + "SetBonus", Resistance);
            player.onHitDodge = true;

            player.endurance += Resistance / 100f;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().MeleeArmorVamp10 = true;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height,
                    DustID.Shadowflame, (player.velocity.X) + (player.direction * 3), player.velocity.Y, 100, Color.BlueViolet, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedMask, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();


            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.AncientHallowedMask, 1);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 20000);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
