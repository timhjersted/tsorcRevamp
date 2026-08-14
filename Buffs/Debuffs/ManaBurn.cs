using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Buffs.Debuffs;

public class ManaBurn : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        var arcanePlayer = player.GetModPlayer<ArcaneSorceryPlayer>();
        arcanePlayer.ManaBurn = true; //adds mana cost later
        player.endurance -= arcanePlayer.ManaBurnBadResistance / 100f;
    }
}