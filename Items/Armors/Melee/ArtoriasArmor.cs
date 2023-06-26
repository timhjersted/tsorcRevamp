using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using Terraria.Localization;

namespace tsorcRevamp.Items.Armors.Melee
{
    [LegacyName("ArmorOfArtorias")]
    [AutoloadEquip(EquipType.Body)]
    public class ArtoriasArmor : ModItem
    {
        public static int SoulCost = 70000;
        public static float DmgMult = 24f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DmgMult);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 51;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) *= 1f + DmgMult / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<ArtoriasHelmet>() && legs.type == ModContent.ItemType<ArtoriasGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {

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
            recipe.AddIngredient(ItemID.BeetleScaleMail);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.BeetleShell);
            recipe2.AddIngredient(ModContent.ItemType<SoulOfArtorias>());
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
