using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Damage.Trinity
{
    public class TrinityBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<TrinityPlayer>();
            player.GetCritChance(modPlayer.Buffed1) += TrinityItem.CritChance;
            player.GetCritChance(modPlayer.Buffed2) += TrinityItem.CritChance;
            
            if (player.buffTime[buffIndex] <= 0)
            {
                player.AddBuff(ModContent.BuffType<TrinityCooldown>(), TrinityItem.Cooldown * 60);
            }
        }

        public override bool RightClick(int buffIndex)
        {
            Main.LocalPlayer.AddBuff(ModContent.BuffType<TrinityCooldown>(), TrinityItem.Cooldown * 60);
            return base.RightClick(buffIndex);
        }
    }
}
