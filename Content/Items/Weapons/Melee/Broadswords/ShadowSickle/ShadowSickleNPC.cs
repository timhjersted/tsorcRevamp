using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.ShadowSickle;

public class ShadowSickleNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public Player LastHitter;
}