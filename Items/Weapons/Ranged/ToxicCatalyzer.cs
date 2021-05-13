using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class ToxicCatalyzer : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Toxic Catalyzer");
            Tooltip.SetDefault("Left-click shots tag and poison enemies"
                                + "\nRight-click shots detonate sticky tags");
        }

        public override void SetDefaults()
        {
            item.damage = 16;
            item.ranged = true;
            item.crit = 0;
            item.width = 38;
            item.height = 28;
            item.useTime = 18;
            item.useAnimation = 18;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.knockBack = 2.5f;
            item.value = 150000;
            item.scale = 0.8f;
            item.rare = ItemRarityID.Green;
            item.shoot = mod.ProjectileType("ToxicCatShot");
            item.shootSpeed = 5f;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-5, 4);
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                item.useTime = 26;
                item.useAnimation = 26;
                item.shoot = ModContent.ProjectileType<Projectiles.ToxicCatDetonator>();
            }
            else
            {
                item.useTime = 18;
                item.useAnimation = 18;
                item.shoot = ModContent.ProjectileType<Projectiles.ToxicCatShot>();
            }
            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarShot").WithVolume(.6f).WithPitchVariance(.3f), player.Center);
            }

            {
                Vector2 muzzleOffset = Vector2.Normalize(new Vector2(speedX, speedY)) * 1f;
                if (Collision.CanHit(position, 0, 0, position + muzzleOffset, 0, 0))
                {
                    position += muzzleOffset;
                    position.Y += 3;
                }
            }
            return true;
        }
    }
}
