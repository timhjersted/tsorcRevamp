using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.Stormrazor;

public class StormrazorEnergy : ModBuff
{
    public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Boost));
    public override void SetStaticDefaults()
    {
        BuffID.Sets.TimeLeftDoesNotDecrease[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        var modPlayer = player.GetModPlayer<StormrazorPlayer>();
        if (!modPlayer.Equipped)
        {
            player.DelBuff(buffIndex);
        }
    }
}