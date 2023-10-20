using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Body)]
    public class DarkKnightArmor : ModItem
    {
        public static float Dmg = 17f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg);
        public override void SetStaticDefaults()
        {
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
            player.GetDamage(DamageClass.Melee) += Dmg / 100f;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.GetDamage(DamageClass.Melee) += Dmg / 100f;
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


            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.AncientHallowedPlateMail, 1);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
