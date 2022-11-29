using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class MinorEdits : GlobalItem
    {

        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.StaffofRegrowth && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                item.createTile = -1; //block placing grass, thus allowing use
            }
            if (item.type == ItemID.DivingHelmet)
            {
                item.accessory = true;
            }

        }
        public override void GrabRange(Item item, Player player, ref int grabRange)
        {
            if (item.type == ItemID.ManaCloakStar)
            {
                if (player.manaMagnet)
                {
                    grabRange += 100;
                }
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if ((item.type == ItemID.DirtRod || item.type == ItemID.BoneWand) && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return false;
            }
            return true;
        }

        public override void HoldItem(Item item, Player player)
        {
            float scaleDelta;
            if (item.DamageType == DamageClass.SummonMeleeSpeed)
            {
                switch (item.prefix)
                {

                    case PrefixID.Large:

                        scaleDelta = 0.12f;
                        break;

                    case PrefixID.Massive:

                        scaleDelta = 0.18f;
                        break;

                    case PrefixID.Dangerous:

                        scaleDelta = 0.06f;
                        break;

                    case PrefixID.Tiny:

                        scaleDelta = -0.18f;
                        break;

                    case PrefixID.Terrible:

                        scaleDelta = -0.14f;
                        break;

                    case PrefixID.Small:

                        scaleDelta = -0.1f;
                        break;

                    case PrefixID.Unhappy:

                        scaleDelta = -0.1f;
                        break;

                    case PrefixID.Bulky:

                        scaleDelta = 0.1f;
                        break;

                    case PrefixID.Shameful:

                        scaleDelta = 0.1f;
                        break;

                    case PrefixID.Legendary:

                        scaleDelta = 0.1f;
                        break;

                    default:
                        scaleDelta = 0;
                        break;
                }
                player.whipRangeMultiplier += scaleDelta;
            }
        }
    }
}
