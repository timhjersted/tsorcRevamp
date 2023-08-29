using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic;

class LampTome : ModItem
{

    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("A lost tome known to cure blindness.");
    }
    public override void SetDefaults()
    {
        Item.height = 10;
        Item.knockBack = 4;
        Item.rare = ItemRarityID.Orange; //yes, even though it's hardmode
        Item.DamageType = DamageClass.Magic;
        Item.noMelee = true;
        Item.mana = 5;
        Item.UseSound = SoundID.Item21;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = 10;
        Item.useAnimation = 10;
        Item.value = PriceByRarity.Orange_3;
        Item.width = 34;
    }

    public override bool? UseItem(Player player)
    {
        int buffIndex = 0;

        foreach (int buffType in player.buffType)
        {

            if (buffType == BuffID.Darkness || buffType == BuffID.Blackout || buffType == BuffID.Obstructed)
            {
                player.DelBuff(buffIndex);
            }
            buffIndex++;
        }
        return true;
    }
}
