using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class Pulsar : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Pulsar");
            Tooltip.SetDefault("Keep your enemies close, but not too close!"
                                + "\nEnlarged projectile does x1.5 damage"
                                + "\nElectrocutes enemies"
                                + "\nPowerful, but hard to master");

        }

        public override void SetDefaults()
        {
            item.damage = 34;
            item.ranged = true;
            item.crit = 0;
            item.width = 38;
            item.height = 28;
            item.useTime = 26;
            item.useAnimation = 26;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.knockBack = 2f;
            item.value = 20000;
            item.scale = 0.8f;
            item.rare = ItemRarityID.Green;
            item.shoot = mod.ProjectileType("PulsarShot");
            item.shootSpeed = 5f;
        }


        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-5, 4);
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarShot").WithVolume(.6f).WithPitchVariance(.3f), player.Center);
            }

            if (player.wet)
            {
                player.AddBuff(BuffID.Electrified, 90);
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
