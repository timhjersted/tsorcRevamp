using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class PowerfulCurseBuildup : ModBuff
    {
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
            player.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel += Main.rand.Next(180, 240); // +180-240, aka 3 hits for proc

            for (int i = 0; i < 8; i++)
            {
                var dust = Dust.NewDustDirect(player.position, player.width, player.height, 21, 0, 0, 200, Color.Violet, Main.rand.NextFloat(2f, 3f));
                dust.noGravity = true;
            }

            return true;
        }
    }
}
