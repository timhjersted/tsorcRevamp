using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [LegacyName("HelmetOfArtorias")]
    [AutoloadEquip(EquipType.Head)]
    public class ArtoriasHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Artorias' Helmet");
            Tooltip.SetDefault("Enchanted helmet of Artorias.\nLonger invincibility when hit plus 30% critical chance.");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 20;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.longInvince = true;
            player.GetCritChance(DamageClass.Generic) += 30;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ArtoriasArmor>() && legs.type == ModContent.ItemType<ArtoriasGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.lavaImmune = true;
            player.fireWalk = true;
            player.breath = 9999999;
            player.waterWalk = true;
            player.noKnockback = true;
            player.GetDamage(DamageClass.Generic) += 0.37f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.37f;
            player.moveSpeed += 0.50f;
            player.manaCost -= 0.37f;
            player.lifeRegen += 8;
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
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("SoulOfArtorias").Type, 2);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
