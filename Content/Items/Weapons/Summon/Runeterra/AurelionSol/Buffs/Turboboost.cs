using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Bases;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Sounds.InterstellarVessel;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Buffs
{
    public class Turboboost : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<RuneterraGauntletPlayer>();
            modPlayer.Turboboost = true;
            if (player.buffTime[buffIndex] == 0)
            {
                OnRemoval(buffIndex, player);
            }
        }
        public override bool RightClick(int buffIndex)
        {
            Player player = Main.LocalPlayer;
            OnRemoval(buffIndex, player);
            return base.RightClick(buffIndex);
        }

        public static void OnRemoval(int buffIndex, Player player)
        {
            var modPlayer = player.GetModPlayer<RuneterraGauntletPlayer>();
            int baseCooldown = RuneterraGauntlets.BoostCooldown * 60;
            int baseDuration = RuneterraGauntlets.BoostDuration * 60;
            int remainder = baseDuration - player.buffTime[buffIndex];
            float buffTimeCooldownRatio = (float)baseCooldown / (float)baseDuration;
            int reducedCooldown = (int)((float)remainder * buffTimeCooldownRatio);
            
            modPlayer.Turboboost = false;
            player.AddBuff(ModContent.BuffType<TurboboostCooldown>(), reducedCooldown);
            SoundEngine.PlaySound(new SoundStyle( UsefulFunctions.RefactorableFilepath(typeof(InterstellarVesselSound)) + "_BoostDeactivation") with { Volume = InterstellarVesselGauntlet.SoundVolume });
        }
    }
}