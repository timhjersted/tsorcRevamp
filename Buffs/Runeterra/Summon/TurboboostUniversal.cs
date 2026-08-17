using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Projectiles.Summon.Runeterra.CirclingProjectiles;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class TurboboostUniversal : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
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
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            int baseCooldown = RuneterraGauntlets.BoostCooldown * 60;
            int baseDuration = RuneterraGauntlets.BoostDuration * 60;
            int remainder = baseDuration - player.buffTime[buffIndex];
            float buffTimeCooldownRatio = (float)baseCooldown / (float)baseDuration;
            int reducedCooldown = (int)((float)remainder * buffTimeCooldownRatio);
            
            modPlayer.Turboboost = false;
            player.AddBuff(ModContent.BuffType<TurboboostUniversalCooldown>(), reducedCooldown);
            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/CenterOfTheUniverse/BoostDeactivation") with { Volume = CenterOfTheUniverse.SoundVolume });
        }
    }
}