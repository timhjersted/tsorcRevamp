using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
    public class DragoonLashBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}

        public override void Update(Player player, ref int buffIndex)
        {
			player.GetAttackSpeed(DamageClass.Summon) += 0.33f;
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.DragoonLashFireBreathTimer += 0.0167f;
        }
    }
}
