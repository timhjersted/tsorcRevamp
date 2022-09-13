using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class DragoonLashBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Dragoon Awakening");
			Description.SetDefault("+33% summon attack speed");
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}

        public override void Update(Player player, ref int buffIndex)
        {
			player.GetAttackSpeed(DamageClass.Summon) += 0.33f;
            if (Main.GameUpdateCount % 1 == 0 & player.whoAmI == Main.myPlayer)
            {
                WhipDebuffs.DragoonLashDebuffNPC.fireBreathTimer += 0.0167f;
				Projectiles.Summon.Whips.DragoonLashProjectile.DragoonLashHitTimer -= 0.0167f;
            }
        }
    }
}
