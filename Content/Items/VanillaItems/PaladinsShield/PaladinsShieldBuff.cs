using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.VanillaItems.PaladinsShield;

public class PaladinsShieldBuff : ModBuff
{
    public override string Texture => UsefulFunctions.VanillaTextureFilepath(BuffID.PaladinsShield, "Buff");
    public override LocalizedText DisplayName => Language.GetText("BuffName.PaladinsShield");
    public override LocalizedText Description => Language.GetText("BuffDescription.PaladinsShield");
    public override void Update(Player player, ref int buffIndex)
    {
        player.defendedByPaladin = true;
    }
}