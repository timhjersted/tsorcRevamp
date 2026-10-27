using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Bases;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Projectiles.CirclingProjectiles;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Buffs
{
    public class CenterOfTheUniverseBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<RuneterraGauntletPlayer>();

            modPlayer.RuneterraMinionHitSoundCooldown--;
            modPlayer.TurboboostControlsCooldown--;

            // If the minions exist reset the buff time, otherwise remove the buff from the player
            if (player.ownedProjectileCounts[ModContent.ProjectileType<CenterOfTheUniverseStar>()] > 0)
            {
                // update projectiles
                CenterOfTheUniverse.ReposeProjectiles(player);
                player.buffTime[buffIndex] = 18000;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}