using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class WitchkingsSword : ModItem
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Witchking's Sword");
            // Tooltip.SetDefault("May inflict multiple debuffs");
        }
        public override void SetDefaults()
        {
            Item.expert = true;
            Item.damage = 337;
            Item.width = 100;
            Item.height = 100;
            Item.autoReuse = true;
            Item.knockBack = 8;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 15;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 15;
            Item.value = PriceByRarity.Red_10;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.OnFire3, 5 * 60, false);
            }
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.ShadowFlame, 5 * 60, false);
            }
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.CursedInferno, 5 * 60, false);
            }
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.BetsysCurse, 5 * 60, false);
            }
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.Ichor, 5 * 60, false);
            }
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(ModContent.BuffType<CrimsonBurn>(), 5 * 60, false);
            }
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(ModContent.BuffType<DarkInferno>(), 5 * 60, false);
            }
        }
        public override void MeleeEffects(Player player, Rectangle rectangle)
        {
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }
    }
}
