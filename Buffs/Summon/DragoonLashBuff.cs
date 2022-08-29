using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class DragoonLashBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Dragoon Awakening");
			Description.SetDefault("+66% whip speed");
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}

        public override void Update(Player player, ref int buffIndex)
        {
			player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += 0.66f;
            if (Main.GameUpdateCount % 1 == 0)
            {
                WhipDebuffs.DragoonLashDebuffNPC.fireBreathTimer += 0.0167f;
            }
        }
    }
}
