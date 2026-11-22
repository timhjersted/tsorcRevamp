using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;

namespace tsorcRevamp.Content.Items.VanillaItems.FrozenTurtleShell;

public class IceBarrierBuff : ModBuff
{
    public override string Texture => UsefulFunctions.VanillaTextureFilepath(BuffID.IceBarrier, "Buff");
    public override LocalizedText DisplayName => Language.GetText("BuffName.IceBarrier");
    public override LocalizedText Description => Language.GetText("Mods.tsorcRevamp.CommonItemTooltip.DRStat").WithFormatArgs(IceBarrierResistance);
    public const float IceBarrierResistance = 25 + 8;
    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
    }


    public override void Update(Player player, ref int buffIndex)
    {
        player.endurance += IceBarrierResistance / 100f;
        player.iceBarrier = true;
        Lighting.AddLight((int)(player.Center.X / 16f), (int)(player.Center.Y / 16f), 0.1f, 0.2f, 0.45f);
        player.iceBarrierFrameCounter++;
        if (player.iceBarrierFrameCounter > 2)
        {
            player.iceBarrierFrameCounter = 0;
            player.iceBarrierFrame++;
            if (player.iceBarrierFrame >= 12)
            {
                player.iceBarrierFrame = 0;
            }
        }
    }
}