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

        /// <summary>
        /// Colour-code for attacks that build curse: one bright-pink flame mote layered over the attack's own palette.
        /// Untinted PinkTorch is the regular-curse pink; powerful curse uses <see cref="PowerfulCurseBuildup.SpawnAccentDust"/>
        /// (same flame, plum-tinted). Callers roll their own chance. Visual only, so it no-ops on the server.
        /// </summary>
        public static Dust SpawnAccentDust(Vector2 position, int width, int height, Vector2 velocity, float scale = 1.4f)
        {
            if (Main.dedServ)
            {
                return null;
            }

            Dust accent = Dust.NewDustDirect(position, width, height, DustID.PinkTorch, velocity.X, velocity.Y, 60, default, scale);
            accent.noGravity = true;
            return accent;
        }

        public const int CurseBurstDustCount = 50;

        /// <summary>
        /// The moment a curse lands: CurseBurstDustCount flame motes thrown outward from the player's centre all at once,
        /// pink for the regular curse and plum for the powerful one (the same accent dusts the curse-building attacks use).
        /// Speeds 3-7 px/t in random directions so it reads as a burst out of the body. Visual only, no-ops on the server.
        /// </summary>
        public static void SpawnCurseBurst(Player player, bool powerful)
        {
            Vector2 burstOrigin = player.Center - new Vector2(4f, 4f);

            for (int i = 0; i < CurseBurstDustCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 7f);
                float scale = Main.rand.NextFloat(1.6f, 2.4f);

                if (powerful)
                {
                    PowerfulCurseBuildup.SpawnAccentDust(burstOrigin, 8, 8, velocity, scale);
                }
                else
                {
                    SpawnAccentDust(burstOrigin, 8, 8, velocity, scale);
                }
            }
        }

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

            // No trigger buffs any more (Invincible, Green Blossom, Strength removed 2026-10-18): the curse is the whole effect.
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.75f, Pitch = -0.35f, PitchVariance = 0.1f }, player.Center);
            SpawnCurseBurst(player, powerful: false);
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
