using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class WitchkingHelmet : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 30;
            Item.value = 21000;
            Item.rare = ItemRarityID.LightRed;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<WitchkingTop>() && legs.type == ModContent.ItemType<WitchkingBottoms>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "+20% magic and ranged damage, +10% movement speed, -30% mana cost," + "\nnightvision +3 HP regen, no knockback, fall damage or firewalk damage";
            player.fireWalk = true;
            player.noKnockback = true;
            player.GetDamage(DamageClass.Magic) += 0.20f;
            player.GetDamage(DamageClass.Ranged) += 0.20f;
            player.moveSpeed += 0.10f;
            player.manaCost -= 0.30f;
            player.lifeRegen += 3;
            player.nightVision = true;
            player.noFallDmg = true;

            int i2 = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int j2 = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(i2, j2, 0.92f, 0.8f, 0.65f);
        }

        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
    }
}
