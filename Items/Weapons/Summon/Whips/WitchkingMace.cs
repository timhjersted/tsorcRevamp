using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    public class WitchkingMace : ModItem
    {
        public const int BaseDamage = 235;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
        }

        public override void SetDefaults()
        {
            Item.height = 54;
            Item.width = 48;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.damage = BaseDamage;
            Item.knockBack = 5.5f;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.value = Item.buyPrice(0, 70, 0, 0);
            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.WitchkingMaceProjectile>();
            Item.shootSpeed = 4;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 50;
            Item.useAnimation = 50;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override bool MeleePrefix()
        {
            return true;
        }
    }
}