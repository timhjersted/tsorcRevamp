using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Ranged
{
    class ScoutsBoost : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Scouts Boost");
            Description.SetDefault("Multiplies movement speed by 20% and stamina regen by 10%");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.moveSpeed *= 1.2f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 1.1f;
        }
    }
}
