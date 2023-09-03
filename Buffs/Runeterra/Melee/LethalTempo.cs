using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Melee
{
    public class LethalTempo : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip = base.Description.WithFormatArgs(Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().FracturingArmor).Value;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.BotCLethalTempoStacks == 0)
            {
                modPlayer.BotCLethalTempoStacks = 1;
            }
            Main.NewText(modPlayer.BotCLethalTempoStacks);
            if (player.buffTime[buffIndex] == 1)
            {
                if (modPlayer.BotCLethalTempoStacks > 1)
                {
                    modPlayer.BotCLethalTempoStacks--;
                    player.buffTime[buffIndex] = (int)(((float)modPlayer.BotCLethalTempoDuration / 6f) * 60f);
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoFallOff") with { Volume = modPlayer.BotCClassMechanicsVolume * 0.1f }, player.Center);
                } else 
                {
                    modPlayer.BotCLethalTempoStacks = 0;
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoFallOff") with { Volume = modPlayer.BotCClassMechanicsVolume * 0.4f }, player.Center);
                }
            }
        }

        public override bool ReApply(Player player, int time, int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.BotCLethalTempoStacks < modPlayer.BotCLethalTempoMaxStacks)
            {
                modPlayer.BotCLethalTempoStacks++;
            }

            return false;
        }
    }
}
