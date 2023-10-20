using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Axes
{
    public class ForgottenPoisonAxe : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("The blade has been dipped in poison.");
        }
        public override void SetDefaults()
        {

            Item.rare = ItemRarityID.Pink;
            Item.damage = 76;
            Item.width = 63;
            Item.height = 58;
            Item.knockBack = 5;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1.2f;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 15;
            Item.value = PriceByRarity.Pink_5;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.YellowGreen;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 6 * 60, false);
        }
    }
}
