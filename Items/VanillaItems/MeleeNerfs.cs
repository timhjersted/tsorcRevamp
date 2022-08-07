using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class MeleeNerfs : GlobalItem
    {
        public static int SwordHit = 0;
        public override void SetDefaults(Item item)
        {

            //Lunar items
            if (item.type == ItemID.DayBreak)
            {
                item.useAnimation = 32;
                item.useTime = 32;
                item.damage = 125;
            }
            if (item.type == ItemID.Terrarian)
            {
                item.damage = 130;
            }
        }
        public override void UpdateInventory(Item item, Player player)
        {
            if (item.type == ItemID.IceBlade | item.type == ItemID.EnchantedSword | item.type == ItemID.BeamSword | item.type == ItemID.Frostbrand
                | item.type == ItemID.TrueExcalibur | item.type == ItemID.TrueNightsEdge | item.type == ItemID.TerraBlade
                | item.type == ItemID.Meowmere | item.type == ItemID.StarWrath)
            {
                if (Main.GameUpdateCount % 20 == 0)
                {
                    SwordHit--;
                }
            }
        }
        public override bool CanShoot(Item item, Player player)
        {
            if ((item.type == ItemID.IceBlade | item.type == ItemID.EnchantedSword | item.type == ItemID.BeamSword | item.type == ItemID.Frostbrand 
                | item.type == ItemID.TrueExcalibur | item.type == ItemID.TrueNightsEdge | item.type == ItemID.TerraBlade
                | item.type == ItemID.Meowmere | item.type == ItemID.StarWrath) & SwordHit <= 0)
            {
                return false;
            }
            return true;
        }
        public override void OnHitNPC(Item item, Player player, NPC target, int damage, float knockBack, bool crit)
        {
            if (item.type == ItemID.IceBlade | item.type == ItemID.EnchantedSword | item.type == ItemID.BeamSword | item.type == ItemID.Frostbrand 
                | item.type == ItemID.TrueExcalibur | item.type == ItemID.TrueNightsEdge | item.type == ItemID.TerraBlade 
                | item.type == ItemID.Meowmere | item.type == ItemID.StarWrath)
            {
                SwordHit = 2;
            }
        }
    }
}
