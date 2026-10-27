using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Melee.ArtoriasSet
{
    [AutoloadEquip(EquipType.Head)]
    public class ArtoriasHelmet : ModItem
    {
        public const int SoulCost = 70000;
        public static float CritChance = 30f;
        public const float DmgMultWhileRolling = 2.25f;
        public const float Resistance = 20f;
        public const int AggroBoost = 800;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance);
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 28;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.value = PriceByRarity.Purple_11;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += CritChance;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ArtoriasArmor>() && legs.type == ModContent.ItemType<ArtoriasGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus =
                Language.GetTextValue(Tooltip.Key + "SetBonus", DmgMultWhileRolling, Resistance);
            
            player.endurance += Resistance / 100f;
            player.aggro += AggroBoost;
            
            player.GetModPlayer<tsorcRevampPlayer>().CanUseItemsWhileDodging = true;

            int dust = Dust.NewDust(new Vector2((float)player.position.X - 5, (float)player.position.Y), player.width + 10, player.height, 77, player.velocity.X, -2, 180, default, 1.25f);
            Main.dust[dust].noGravity = true;

            int i2 = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int j2 = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(i2, j2, 0.7f, 0.6f, 0.8f);
        }
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
            player.armorEffectDrawOutlinesForbidden = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
