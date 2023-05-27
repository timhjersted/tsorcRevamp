using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class DragoonLashBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			// DisplayName.SetDefault("Dragoon Awakening");
			// Description.SetDefault("+33% summon attack speed");
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}

        public override void Update(Player player, ref int buffIndex)
        {
			player.GetAttackSpeed(DamageClass.Summon) += 0.33f;
            if (Main.GameUpdateCount % 1 == 0 & player.whoAmI == Main.myPlayer)
            {
                player.GetModPlayer<tsorcRevampPlayer>().DragoonLashFireBreathTimer += 0.0167f;
				player.GetModPlayer<tsorcRevampPlayer>().DragoonLashHitTimer -= 0.0167f;
            }
        }
    }
}
