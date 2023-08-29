using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Ranged;

class ScoutsBoost2 : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Scouts Boost II");
        Description.SetDefault("Multiplies movement speed by 40% and stamina regen by 20%");
        Main.debuff[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.moveSpeed *= 1.4f;
        player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 1.2f;
    }
}
