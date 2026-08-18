using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.ArcaneSorcery;

public class ManaBurn : ModBuff
{
    public const int Duration = 5;
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