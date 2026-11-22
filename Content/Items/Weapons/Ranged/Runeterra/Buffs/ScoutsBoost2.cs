using Terraria;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.ToxicShot;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs
{
    public class ScoutsBoost2 : ModBuff
    {
        public override LocalizedText Description => base.Description.WithFormatArgs(ToxicShot.ScoutsBoostMoveSpeedMult * 2, ToxicShot.ScoutsBoostStaminaRegenMult * 2);
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.buffTime[buffIndex] == ToxicShot.ScoutsBoost2Duration * 60 - 1)
            {
                SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(ToxicShotSound)) + "_SuperBuffCast") with { Volume = 1f }, player.Center);
            }
            player.GetModPlayer<RuneterraDartsPlayer>().ScoutsBoostActive = true;
            if (player.buffTime[buffIndex] == 1)
            {
                SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(ToxicShotSound)) + "_SuperBuffEnd") with { Volume = 1f }, player.Center);
            }
        }
    }
}
