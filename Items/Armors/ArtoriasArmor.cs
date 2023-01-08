using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [LegacyName("ArmorOfArtorias")]
    [AutoloadEquip(EquipType.Body)]
    public class ArtoriasArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Artorias' Armor");
            Tooltip.SetDefault("Enchanted armor of Artorias of the Abyss." +
                "\nIncreases your damage dealt by 21% and life regeneration by 8" +
                "\nGrants knockback and fall damage immunity" +
                "\nSet Bonus: Increases maximum stamina and it's regeneration by 20%");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 30;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.21f;
            player.lifeRegen += 8;
            player.noKnockback = true;
            player.noFallDmg = true;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<ArtoriasHelmet>() && legs.type == ModContent.ItemType<ArtoriasGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1.2f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1.2f;

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
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
