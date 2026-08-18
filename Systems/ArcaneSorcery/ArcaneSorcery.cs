using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.ArcaneSorcery;

public class ArcaneSorcery : ModBuff
{
    public const bool Enabled = true; //for easy switching on or off of the system
    
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        //Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        var arcanePlayer = player.GetModPlayer<ArcaneSorceryPlayer>();
    }
}