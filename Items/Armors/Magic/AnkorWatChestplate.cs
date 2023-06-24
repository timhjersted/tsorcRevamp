using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using Terraria.Localization;

namespace tsorcRevamp.Items.Armors.Magic
{
    [AutoloadEquip(EquipType.Body)]
    public class AnkorWatChestplate : ModItem
    {
        public static float CritChance = 14f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 14;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += CritChance;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.GetCritChance(DamageClass.Magic) += CritChance;
            }
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<AnkorWatHelmet>() && legs.type == ModContent.ItemType<AnkorWatLeggings>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.onHitDodge = true;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 60, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Red, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
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
}
