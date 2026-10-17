using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class PowerfulCurseBuildup : ModBuff
    {
        // Plum overlay colour of the powerful-curse meter (sampled from CurseMeter_powerfulFull). Attacks that build
        // powerful curse tint their VFX with it, so the telegraph matches the meter it fills.
        public static readonly Color PowerfulCurseColor = new Color(146, 0, 138);

        /// <summary>
        /// Colour-code for attacks that build powerful curse: one plum flame mote (pink flame multiplied by
        /// <see cref="PowerfulCurseColor"/>) layered over the attack's own palette. Regular curse uses the untinted
        /// version, <see cref="CurseBuildup.SpawnAccentDust"/>. Callers roll their own chance. No-ops on the server.
        /// </summary>
        public static Dust SpawnAccentDust(Vector2 position, int width, int height, Vector2 velocity, float scale = 1.6f)
        {
            if (Main.dedServ)
            {
                return null;
            }

            Dust accent = Dust.NewDustDirect(position, width, height, DustID.PinkTorch, velocity.X, velocity.Y, 40, PowerfulCurseColor, scale);
            accent.noGravity = true;
            return accent;
        }

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip = base.Description.WithFormatArgs(500, Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel).Value;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (player.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel >= 500)
            {
                modPlayer.CalculateCurseStats(true);
                modPlayer.powerfulCurseActive = true;

                modPlayer.PowerfulCurseLevel = 0; // Reset to 0
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.75f, Pitch = -0.35f, PitchVariance = 0.1f }, player.Center);
                player.AddBuff(ModContent.BuffType<Invincible>(), 5 * 60); 
                player.AddBuff(ModContent.BuffType<Strength>(), 120 * 60); 
                player.AddBuff(ModContent.BuffType<GreenBlossom>(), 120 * 60); 
                player.AddBuff(ModContent.BuffType<CrimsonDrain>(), 1 * 60); 
                player.AddBuff(BuffID.Clairvoyance, 120 * 60);

                for (int i = 0; i < 50; i++)
                {
                    var dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.VilePowder, Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(-2.5f, 2.5f), 200, Color.Violet, Main.rand.NextFloat(2f, 5f));
                    dust.noGravity = true;
                }
            }
        }

        public override bool ReApply(Player player, int time, int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel += 160;

            for (int i = 0; i < 8; i++)
            {
                var dust = Dust.NewDustDirect(player.position, player.width, player.height, 21, 0, 0, 200, Color.Violet, Main.rand.NextFloat(2f, 3f));
                dust.noGravity = true;
            }

            return true;
        }
    }
}
