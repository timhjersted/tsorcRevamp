using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    [AutoloadEquip(EquipType.Front)]
    public class RingOfTheBlueEye : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ring Of The Blue Eye");
            Tooltip.SetDefault("Bones fly out in retaliation when player is hurt");
        }
        public override void SetDefaults()
        {
            item.width = 24;
            item.height = 24;
            item.alpha = 0;
            item.accessory = true;
            item.value = 1000000;
            item.rare = ItemRarityID.Cyan;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual && player.direction == -1)
            {
                int dust = Dust.NewDust(player.position + new Vector2(1, 9), 2, 2, 111, 0f, 0f, 100, default(Color), .6f); 
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.1f;
                Main.dust[dust].velocity.Y = Main.dust[dust].velocity.Y - 1f;
                Main.dust[dust].velocity.X = Main.dust[dust].velocity.X + 2f;
            }
        }

        public override void UpdateVanity(Player player, EquipType type)
        {
            if (player.direction == -1)
            {
                int dust = Dust.NewDust(player.position + new Vector2(1, 9), 2, 2, 111, 0f, 0f, 100, default(Color), .6f); 
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.1f;
                Main.dust[dust].velocity.Y = Main.dust[dust].velocity.Y - 1f;
                Main.dust[dust].velocity.X = Main.dust[dust].velocity.X + 2f;
            }
        }
    }
}