using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs;

class CowardsAffliction : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Coward's Affliction");
        Description.SetDefault("Do not flee from the Lord of Cinder.");
        Main.debuff[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
        Terraria.ID.BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetModPlayer<tsorcRevampPlayer>().CowardsAffliction = true;
    }
}
