using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class NonConsumableItem : GlobalItem
    {

        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.SuspiciousLookingEye
                || item.type == ItemID.WormFood
                || item.type == ItemID.DeerThing
                || item.type == ItemID.QueenSlimeCrystal
                || item.type == ItemID.MechanicalEye
                || item.type == ItemID.MechanicalSkull
                || item.type == ItemID.MechanicalWorm
                || item.type == ItemID.LihzahrdPowerCell
                || item.type == ItemID.CelestialSigil
                )
            {
                item.consumable = false;
            }
        }
    }
}
