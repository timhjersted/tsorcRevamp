using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Whips;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    public class DragoonLashBuff : ModBuff
	{
        public override LocalizedText Description => base.Description.WithFormatArgs(DragoonLash.AtkSpeed);
        public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}

        public override void Update(Player player, ref int buffIndex)
        {
			player.GetAttackSpeed(DamageClass.Summon) += DragoonLash.AtkSpeed / 100f;
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.DragoonLashFireBreathTimer += 0.0167f;
        }
    }
}
