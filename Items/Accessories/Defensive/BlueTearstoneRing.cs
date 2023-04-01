using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class BlueTearstoneRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("The rare gem called tearstone has the uncanny ability to sense imminent death." +
                                "\nThis blue tearstone from Catarina boosts the defense of its wearer by 12 and reduces damage taken by 8%" +
                                "\n when life falls below 33%. This also reduces melee damage by 200%.");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 8;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }


        public override void UpdateEquip(Player player)
        {
            if (player.statLife <= (player.statLifeMax / 3))
            {
                player.statDefense += 12;
                player.endurance = 0.08f;
                player.GetDamage(DamageClass.Melee) -= 2f;
            }
        }

    }
}
