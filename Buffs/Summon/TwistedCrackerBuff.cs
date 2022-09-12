using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class TwistedCrackerBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Night's Crack");
			Description.SetDefault("+18% whip speed");
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}

        public override void Update(Player player, ref int buffIndex)
        {
			player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += 0.18f;
		}
    }
}
