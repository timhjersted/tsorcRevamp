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
                || item.type == ItemID.BloodySpine
                || item.type == ItemID.Abeemination
                || item.type == ItemID.DeerThing
                || item.type == ItemID.QueenSlimeCrystal
                || item.type == ItemID.MechanicalEye
                || item.type == ItemID.MechanicalSkull
                || item.type == ItemID.MechanicalWorm
                || item.type == ItemID.LihzahrdPowerCell
                || item.type == ItemID.CelestialSigil
                || item.type == ItemID.PirateMap
                )
            {
                item.consumable = false;
            }

            if (ModLoader.TryGetMod("ThoriumMod", out Mod thorium))
            {
                if (item.type == thorium.Find<ModItem>("JellyfishResonator").Type
                    || item.type == thorium.Find<ModItem>("StarCaller").Type
                    || item.type == thorium.Find<ModItem>("StriderTear").Type
                    || item.type == thorium.Find<ModItem>("VoidLens").Type
                    || item.type == thorium.Find<ModItem>("AbyssalShadow2").Type
                    || item.type == thorium.Find<ModItem>("DoomSayersCoin").Type
                    )
                {
                    item.consumable = false;
                }
            }
        }
    }
}
