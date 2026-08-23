using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Systems.ArcaneSorcery;

namespace tsorcRevamp.Systems.Electrocute;

public class Electrocute : ModBuff
{
    public const bool Enabled = true; //for easy switching on or off of the system
    public const int Cooldown = 10;
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetDamage(DamageClass.Ranged) *= 1f - (ElectrocutePlayer.BadRangedDmg / 100f);
    }
}