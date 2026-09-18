using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    /// <summary>
    /// Seven-second aftermath of a full Madness meter. The initial damage and stagger are
    /// applied once by MadnessPlayer; this buff owns the continuing offensive benefits.
    /// </summary>
    public class Madness : ModBuff
    {
        public const int DurationTicks = 7 * 60;
        public const float MeleeAttackSpeedBonus = 0.25f;
        public const int ManaRegenerationMultiplier = 10;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetAttackSpeed(DamageClass.Melee) += MeleeAttackSpeedBonus;
        }
    }
}
