using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Damage.Trinity;

public class TrinityPlayer : ModPlayer
{
    public bool Equipped;
    public DamageClass Buffed1;
    public DamageClass Buffed2;
    public override void ResetEffects()
    {
        Equipped = false;
    }
    
}