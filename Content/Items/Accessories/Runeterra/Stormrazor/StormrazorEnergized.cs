using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.Stormrazor;

public class StormrazorProc : ModBuff
{
    public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Boost));

    public override void SetStaticDefaults()
    {
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.moveSpeed += StormrazorItem.MovespeedBoost / 100f;
    }
}