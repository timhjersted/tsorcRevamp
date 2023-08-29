using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs;

class BossZenBuff : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Boss Zen");
        Description.SetDefault("The active boss is blocking enemy spawns!");
        Main.debuff[Type] = false;
        Main.buffNoTimeDisplay[Type] = true;
        Terraria.ID.BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetModPlayer<tsorcRevampPlayer>().BossZenBuff = true;
    }

}
