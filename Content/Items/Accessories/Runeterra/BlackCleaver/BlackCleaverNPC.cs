using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.BlackCleaver;

public class BlackCleaverNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public bool HasBuff;
    public int Stacks = 0;
}