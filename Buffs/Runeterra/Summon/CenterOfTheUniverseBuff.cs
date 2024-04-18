using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Projectiles.Summon.Runeterra.CirclingProjectiles;

namespace tsorcRevamp.Buffs.Runeterra.Summon
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
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (player.HeldItem.type == ModContent.ItemType<CenterOfTheUniverse>())
            {
                player.maxMinions += 1;
            }

            modPlayer.RuneterraMinionHitSoundCooldown--;
            modPlayer.InterstellarBoostCooldown--;

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
            if (player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost && player.statMana >= InterstellarCommander.BoostManaCostPerTick)
            {
                player.statMana -= InterstellarCommander.BoostManaCostPerTick;
                player.manaRegenDelay = MeleeEdits.ManaDelay;
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost && (player.statMana < InterstellarCommander.BoostManaCostPerTick || player.HasBuff(BuffID.ManaSickness)))
            {
                player.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost = false;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/CenterOfTheUniverse/BoostDeactivation") with { Volume = CenterOfTheUniverse.SoundVolume });
            }
        }
    }
}