using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Projectiles.Summon.Runeterra.CirclingProjectiles;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class CenterOfTheHeat : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (player.HeldItem.type == ModContent.ItemType<ScorchingPoint>())
            {
                player.maxMinions += 1;
            }

            modPlayer.RuneterraMinionHitSoundCooldown--;
            modPlayer.InterstellarBoostCooldown--;

            // If the minions exist reset the buff time, otherwise remove the buff from the player
            if (player.ownedProjectileCounts[ModContent.ProjectileType<ScorchingPointFireball>()] > 0)
            {
                // Update projectiles
                ScorchingPoint.ReposeProjectiles(player);
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