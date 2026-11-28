using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Whips.UhlansSiegebow
{
    public class UhlansSiegebowItem : ModItem
    {
        public const int BaseDamage = 22;
        public const int SummonTagDamage = 5;
        public const int MaxRangeTiles = 20;
        public const int MaxLodgedBolts = 3;
        public const int LodgedDurationSeconds = 6;
        public const int PowderPopPercent = 45;
        public const int TagDurationSeconds = 4;
        public const int PowderPopCooldownTicks = 72;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            SummonTagDamage,
            MaxRangeTiles,
            MaxLodgedBolts,
            PowderPopPercent);

        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 26;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.damage = BaseDamage;
            Item.knockBack = 3.5f;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 34;
            Item.useAnimation = 34;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item98;
            Item.shoot = ModContent.ProjectileType<UhlansSiegebowBoltProj>();
            Item.shootSpeed = 11.5f;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
        }

        public override bool MeleePrefix()
        {
            return true;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-4f, 0f);
        }
    }
}
