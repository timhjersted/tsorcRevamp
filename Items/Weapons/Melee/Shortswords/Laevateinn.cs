using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Melee;
using tsorcRevamp.Projectiles.Melee.Shortswords;

namespace tsorcRevamp.Items.Weapons.Melee.Shortswords
{
    class Laevateinn : ModItem
    {
        public static int InvincibilityDuration = 2;
        public static int InvincibilityCooldown = 10;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(InvincibilityCooldown, InvincibilityDuration);
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 60;
            Item.useStyle = ItemUseStyleID.Rapier;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<LaevateinnProjectile>(); // The projectile is what makes a shortsword work
            Item.shootSpeed = 2.1f; // This value bleeds into the behavior of the projectile as velocity, keep that in mind when tweaking values
            Item.useAnimation = 8; //holy shit that is some d(ee)ps
            Item.useTime = 8;
            Item.damage = 55;
            Item.knockBack = 3.8f;
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.Pink_5;
            Item.DamageType = DamageClass.Melee;
        }
        public override bool MeleePrefix()
        {
            return true;
        }
        public override bool AltFunctionUse(Player player)
        {
            if (!player.HasBuff(ModContent.BuffType<LaevateinnInvincibleCooldown>()))
            {
                player.AddBuff(ModContent.BuffType<LaevateinnInvincible>(), InvincibilityDuration * 60);
                player.AddBuff(ModContent.BuffType<LaevateinnInvincibleCooldown>(), InvincibilityCooldown * 60);
                return true;
            }
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
    }
}
