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
            if (player.HeldItem.type == ModContent.ItemType<InterstellarVesselGauntlet>())
            {
                player.maxMinions += 1;
            }

            modPlayer.RuneterraMinionHitSoundCooldown--;
            modPlayer.InterstellarBoostCooldown--;

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

            if (player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost && player.statMana >= BoostManaCostPerTick)
            {
                player.statMana -= BoostManaCostPerTick;
                player.manaRegenDelay = MeleeEdits.ManaDelay;
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost && (player.statMana < BoostManaCostPerTick || player.HasBuff(BuffID.ManaSickness)))
            {
                player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost = false;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/InterstellarVessel/BoostDeactivation") with { Volume = InterstellarVesselGauntlet.SoundVolume });
            }
        }
    }
}