using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors.Melee;
using tsorcRevamp.Items.Armors.Summon;

namespace tsorcRevamp.Items.VanillaItems
{
    class MeleeEdits : GlobalItem
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
            if (item.type == ItemID.FieryGreatsword)
            {
                item.useAnimation = 30;
                item.useTime = 30;
                item.damage = 40;
            }
            if(item.type == ItemID.PiercingStarlight)
            {
                item.damage = 50;
            }
        }
        public override void UpdateInventory(Item item, Player player)
        {
            if (item.type == ItemID.IceBlade | item.type == ItemID.EnchantedSword | item.type == ItemID.BeamSword | item.type == ItemID.Frostbrand | item.type == ItemID.TrueNightsEdge //| item.type == ItemID.TerraBlade
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
            if ((item.type == ItemID.IceBlade | item.type == ItemID.EnchantedSword | item.type == ItemID.BeamSword | item.type == ItemID.Frostbrand | item.type == ItemID.TrueNightsEdge //| item.type == ItemID.TerraBlade
                | item.type == ItemID.Meowmere | item.type == ItemID.StarWrath) & SwordHit <= 0)
            {
                return false;
            }
            return true;
        }
        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (item.type == ItemID.IceBlade | item.type == ItemID.EnchantedSword | item.type == ItemID.BeamSword | item.type == ItemID.Frostbrand | item.type == ItemID.TrueNightsEdge //| item.type == ItemID.TerraBlade 
                | item.type == ItemID.Meowmere | item.type == ItemID.StarWrath)
            {
                SwordHit = 2;
            }
        }
        public override void ModifyItemScale(Item item, Player player, ref float scale)
        {
            base.ModifyItemScale(item, player, ref scale);

            if (item.type == ItemID.IceBlade | item.type == ItemID.EnchantedSword | item.type == ItemID.BeamSword | item.type == ItemID.Frostbrand | item.type == ItemID.TrueNightsEdge //| item.type == ItemID.TerraBlade
                | item.type == ItemID.Meowmere | item.type == ItemID.StarWrath)
            {
                scale *= 1.25f;
            }
        }
        public override string IsArmorSet(Item head, Item body, Item legs)
        {
            if (head.type == ModContent.ItemType<Items.Armors.Melee.AncientGoldenHelmet>() && body.type == ItemID.Gi && legs.type == ModContent.ItemType<AncientGoldenGreaves>())
            {
                return "GoldenGi";
            }
            else return base.IsArmorSet(head, body, legs);
        }
        public override void UpdateArmorSet(Player player, string set)
        {
            if (set == "GoldenGi")
            {
                player.setBonus = "Increases melee damage by 3 flat";

                player.GetDamage(DamageClass.Melee).Flat += 3f;
            }
        }
    }
}
