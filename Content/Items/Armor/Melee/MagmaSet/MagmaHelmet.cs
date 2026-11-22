using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Melee.MagmaSet
{
    [AutoloadEquip(EquipType.Head)]
    public class MagmaHelmet : ModItem
    {
        public const float Crit = 9f;
        public const int OnHitDmg = 3;
        public const int BaseDoT = 20;
        public const int DebuffDuration = 10;
        public const float Resistance = 9f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Crit);
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 9;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += Crit;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<MagmaBreastplate>() && legs.type == ModContent.ItemType<MagmaGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = Language.GetTextValue(Tooltip.Key + "SetBonus", OnHitDmg, Resistance);
            player.endurance += Resistance / 100f;
            player.GetModPlayer<MagmaSetPlayer>().Equipped = true;
            player.buffImmune[BuffID.OnFire] = true;

            if (Main.rand.NextBool(3))
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height,
                    DustID.Torch, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 200, color, 1.0f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].noLight = false;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MoltenHelmet, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
