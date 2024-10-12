using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace tsorcRevamp.Items;
public class HerosCrest : ModItem
{
    public const float Stats = 2f;
    public const float LuckMaxLifeMult = 12.5f;
    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Stats, (int)(Stats * LuckMaxLifeMult));
    public override void SetStaticDefaults()
    {
    }

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 24;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.UseSound = SoundID.Item4;
        Item.useAnimation = 60;
        Item.useTime = 60;
        Item.expert = true;
        Item.value = PriceByRarity.Yellow_8;
        Item.consumable = true;
        Item.maxStack = Item.CommonMaxStack;
    }
    public override bool CanUseItem(Player player)
    {
        return !player.GetModPlayer<HerosCrestPlayer>().HerosCrest;
    }
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            player.GetModPlayer<HerosCrestPlayer>().HerosCrest = true;
            return true;
        }
        else
        {
            return false;
        }
    }
    public override void UpdateAccessory(Player player, bool hideVisual)
    {
    }
    private class HerosCrestPlayer : ModPlayer
    {
        public bool HerosCrest = false;

        public override void SaveData(TagCompound tag)
        {
            tag.Add("HerosCrest", HerosCrest);
        }
        public override void LoadData(TagCompound tag)
        {
            HerosCrest = tag.GetBool("HerosCrest");
        }
        public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
        {
            base.ModifyMaxStats(out health, out mana);
            if (HerosCrest)
            {
                health.Base += Stats * LuckMaxLifeMult;
            }
        }
        public override void PostUpdateEquips()
        {
            if (HerosCrest)
            {
                Player.GetDamage(DamageClass.Generic) += Stats / 100f;
                Player.GetCritChance(DamageClass.Generic) += Stats;
                Player.luck += Stats * 10f / 100f;
            }
        }
    }
}