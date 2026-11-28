using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Mobility.CloakingSerpentScale
{
    public class CloakingSerpentScaleBuff : ModBuff
    {
        public override LocalizedText Description => base.Description.WithFormatArgs(
            CloakingSerpentScaleItem.AmbushCritBonus,
            CloakingSerpentScaleItem.AmbushDamageBonus);

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Reduced aggro and ambush crit bonus persist until the player lands a direct strike or the buff expires.
            player.aggro -= 600;
            player.GetCritChance(DamageClass.Generic) += CloakingSerpentScaleItem.AmbushCritBonus;

            if (!Main.dedServ && Main.GameUpdateCount % 4 == 0)
            {
                Vector2 dustPos = player.position + new Vector2(Main.rand.NextFloat(player.width), Main.rand.NextFloat(player.height));
                Vector2 dustVel = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.4f, 1.0f));
                Dust shimmer = Dust.NewDustPerfect(dustPos, DustID.AncientLight, dustVel, 55, new Color(170, 90, 240), Main.rand.NextFloat(0.75f, 1.0f));
                shimmer.noGravity = true;
            }
        }
    }
}
