using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Projectiles.Summon.Runeterra.CirclingProjectiles;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class InterstellarCommander : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
        public const int BoostManaCostPerTick = 2;
        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            modPlayer.RuneterraMinionHitSoundCooldown--;
            modPlayer.TurboboostControlsCooldown--;

            // If the minions exist reset the buff time, otherwise remove the buff from the player
            if (player.ownedProjectileCounts[ModContent.ProjectileType<InterstellarVesselShip>()] > 0)
            {
                InterstellarVesselGauntlet.ReposeProjectiles(player);
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