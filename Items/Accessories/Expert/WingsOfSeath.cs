using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace tsorcRevamp.Items.Accessories.Expert
{
    [AutoloadEquip(EquipType.Wings)]
    [LegacyName("DragonWings")]
    public class WingsOfSeath : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wings of Seath");
            Tooltip.SetDefault("The wings of Seath the Scaleless" +
                               "\nAllow for superior control of movement in the air, including hovering(hovering doesn't work funny tmod)" +
                                "\nProvide immunity to all fire and lava damage, as well as perfect sight and hunting abilities." +
                                "\nAlso provide immunity to knockback");
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(6000, 10f, 1.4f, true);
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 42;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.expert = true;
        }

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising,
                    ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 1.1f;
            ascentWhenRising = 0.35f;
            maxCanAscendMultiplier = 1.3f;
            maxAscentMultiplier = 3.6f;
            constantAscend = 0.2f;
        }
        public override void HorizontalWingSpeeds(Player player, ref float speed, ref float acceleration)
        {
            speed = 13f;
            acceleration = 0.45f;
        }

        public override void UpdateEquip(Player player)
        {
            //todo: make these wings alternative to supersonic that can hover
            player.jumpBoost = true;
            player.jumpSpeedBoost = 1.5f;
            player.lavaImmune = true;
            player.fireWalk = true;
            player.noKnockback = true;
            player.buffImmune[BuffID.Darkness] = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.nightVision = true;
            player.AddBuff(BuffID.Hunter, 1);
        }
    }
}
