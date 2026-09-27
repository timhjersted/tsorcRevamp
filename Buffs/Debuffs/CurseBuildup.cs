using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class CurseBuildup : ModBuff
    {
        public const int DefaultBuildupPerHit = 35;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip = base.Description.WithFormatArgs(100, Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().CurseLevel).Value;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (!modPlayer.CurseBuildupInitialized)
            {
                modPlayer.CurseBuildupInitialized = true;
                // AddBuff does not call ReApply on the first hit. Start generic sources at 35,
                // while an attack that already assigned its own amount remains authoritative.
                if (modPlayer.CurseLevel == 1) modPlayer.CurseLevel = DefaultBuildupPerHit;
            }

            if (modPlayer.CurseLevel >= 100)
            {
                TriggerCurse(player);
            }
        }

        public static void TriggerCurse(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.CalculateCurseStats(false);
            modPlayer.CurseActive = true;
            player.AddBuff(ModContent.BuffType<Curse>(), 2);

            modPlayer.CurseLevel = 0; // Reset it to 0

            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.75f, Pitch = -0.35f, PitchVariance = 0.1f }, player.Center);
            player.AddBuff(ModContent.BuffType<Invincible>(), 8 * 60, false); // 8 seconds
            player.AddBuff(ModContent.BuffType<GreenBlossom>(), 60 * 60, false);
            player.AddBuff(ModContent.BuffType<Strength>(), 60 * 60, false);

            for (int i = 0; i < 30; i++)
            {
                var dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.VilePowder, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 200, Color.Pink, Main.rand.NextFloat(2f, 5f));
                dust.noGravity = true;
            }
        }

        public override bool ReApply(Player player, int time, int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (!modPlayer.SuppressDefaultCurseBuildup)
            {
                if (!modPlayer.CurseBuildupInitialized)
                {
                    modPlayer.CurseBuildupInitialized = true;
                    if (modPlayer.CurseLevel == 1) modPlayer.CurseLevel = DefaultBuildupPerHit;
                }
                modPlayer.CurseLevel += DefaultBuildupPerHit;
            }

            for (int i = 0; i < 10; i++)
            {
                var dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.VilePowder, 0, 0, 200, Color.Pink, Main.rand.NextFloat(2f, 3f));
                dust.noGravity = true;
            }

            return true;
        }

        // For attacks with an authored amount. Suppress ReApply's generic 35 and assign the
        // specific amount exactly once, including the first application of the marker buff.
        public static void ApplyExplicit(Player player, int amount, int durationTicks)
        {
            int type = ModContent.BuffType<CurseBuildup>();
            if (player == null || !player.active || player.dead || amount <= 0 || player.buffImmune[type]) return;
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            bool wasActive = player.HasBuff(type);
            modPlayer.SuppressDefaultCurseBuildup = true;
            try { player.AddBuff(type, durationTicks, false); }
            finally { modPlayer.SuppressDefaultCurseBuildup = false; }
            if (!player.HasBuff(type)) return;
            if (!wasActive) modPlayer.CurseLevel = amount;
            else modPlayer.CurseLevel += amount;
            modPlayer.CurseBuildupInitialized = true;
        }
    }
}
