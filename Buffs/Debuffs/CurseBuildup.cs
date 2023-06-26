using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class CurseBuildup : ModBuff
    {
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

            if (modPlayer.CurseLevel >= 100 && player.statLifeMax > 100)
            {
                if (player.statLifeMax <= 100)
                {
                    Main.NewText(LaUtils.GetTextValue("Buffs.Curse.CurseText1"));
                }

                if (player.statLifeMax >= 120)
                {
                    player.statLifeMax -= 20;
                    Main.NewText(LaUtils.GetTextValue("Buffs.Curse.CurseLifeLoss", 20));
                }

                modPlayer.CurseLevel = 0; // Reset it to 0

                player.AddBuff(ModContent.BuffType<Invincible>(), 480, false); // 8 seconds
                player.AddBuff(ModContent.BuffType<Strength>(), 3600, false);
                Main.NewText(LaUtils.GetTextValue("Buffs.CurseBuildup.CurseText2"));

                for (int i = 0; i < 30; i++)
                {
                    var dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.VilePowder, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 200, Color.Pink, Main.rand.NextFloat(2f, 5f));
                    dust.noGravity = true;
                }
            }
        }

        public override bool ReApply(Player player, int time, int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().CurseLevel += Main.rand.Next(22, 36); // 22-35, aka 3-4 hits before curse proc (projectiles now also inflict a bit of buildup; +5 in the case of bio spit from basilisks)

            for (int i = 0; i < 10; i++)
            {
                var dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.VilePowder, 0, 0, 200, Color.Pink, Main.rand.NextFloat(2f, 3f));
                dust.noGravity = true;
            }

            return true;
        }
    }
}
