using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Damage.ShadowmoonCloak;

public class ShadowmoonCloakPlayer : ModPlayer
{
    public bool ShadowmoonCloak;
    public override void ResetEffects()
    {
        ShadowmoonCloak = false;
    }
}