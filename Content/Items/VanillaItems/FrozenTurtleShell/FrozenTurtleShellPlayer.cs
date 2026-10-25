using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.VanillaItems.FrozenTurtleShell;

public class FrozenTurtleShellPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public void ApplyIceBarrier()
    {
        Player.AddBuff(ModContent.BuffType<IceBarrierBuff>(), 5);
    }

    public override void Load()
    {
        On_Player.ApplyEquipFunctional += VanillaFrozenTurtleShellBlock;
    }

    private void VanillaFrozenTurtleShellBlock(On_Player.orig_ApplyEquipFunctional orig, Player self, Item currentItem, bool hideVisual)
    {
        orig(self, currentItem, hideVisual);
        if (self.HasBuff(BuffID.IceBarrier))
        {
            self.ClearBuff(BuffID.IceBarrier);
        }
    }
    
}