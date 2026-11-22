using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Bases;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs
{
    public class ScoutsBoost : ModBuff
    {
        public override LocalizedText Description => base.Description.WithFormatArgs(ToxicShot.ScoutsBoostMoveSpeedMult, ToxicShot.ScoutsBoostStaminaRegenMult);
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<RuneterraDartsPlayer>().ScoutsBoostPassive = true;
        }
    }
}
