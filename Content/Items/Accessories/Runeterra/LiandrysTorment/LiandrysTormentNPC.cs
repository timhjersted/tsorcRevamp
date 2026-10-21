using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.LiandrysTorment;

public class LiandrysTormentNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public bool Burning;
    public override void ResetEffects(NPC npc)
    {
        Burning = false;
    }
}