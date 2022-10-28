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
                "\nPossesses the same power as the Titan Glove." +
                "\nSet Bonus: +21% damage and +24% attack speed, +50% move speed, -13% mana costs, +8 life regen" +
                "\nWater, Lava, Fire walking and immunity, increased breath, knockback and fall damage immunity, night vision");
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
            player.kbGlove = true;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<ArtoriasHelmet>() && legs.type == ModContent.ItemType<ArtoriasGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.21f;
            player.GetAttackSpeed(DamageClass.Generic) += 0.24f;
            player.moveSpeed += 0.5f;
            player.manaCost -= 0.13f;
            player.lifeRegen += 8;
            player.waterWalk = true;
            player.fireWalk = true;
            player.lavaImmune = true;
            player.breath = 9999999;
            player.noKnockback = true;
            player.nightVision = true;
            player.noFallDmg = true;

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
            recipe.AddIngredient(Mod.Find<ModItem>("SoulOfArtorias").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
