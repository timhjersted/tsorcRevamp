using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles;

namespace tsorcRevamp.Buffs.Debuffs
{
    // PLACEHOLDER sprite: copy of MadnessBuildup.png, for the user to replace.
    public class BlightBuildup : ModBuff
    {
        public const int MaximumBuildup = 100;
        public const int BuildupPerHit = 15;
        public const int DecayIntervalTicks = 60;
        public const int MarkerDurationTicks = 120 * 60;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip = Description.WithFormatArgs(Main.LocalPlayer.GetModPlayer<BlightPlayer>().BlightLevel).Value;
        }

        // Direct hits normally add 15. Persistent hazards pass a smaller authored amount.
        public static void Apply(Player player, int buildup = BuildupPerHit)
        {
            if (player == null || !player.active || player.dead || buildup <= 0) return;
            player.GetModPlayer<BlightPlayer>().RequestBuildup(buildup);
        }

        public static bool FromGreatBlackKnight(Projectile projectile)
            => projectile.GetGlobalProjectile<tsorcGlobalProjectile>().SourceNPCType
                == ModContent.NPCType<NPCs.Enemies.GreatBlackKnight>();
    }
}
