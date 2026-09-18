using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>
    /// Held-prop item for the Grand Occultist's Destined Death Flask throw. The projectile art is
    /// deliberately reused so the flask in the hand is the exact object released by the attack.
    /// </summary>
    public class EnemyDestinedDeathFlask : ModItem
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/DestinedDeathFlask";

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.White;
            Item.width = 20;
            Item.height = 44;
            Item.damage = 25;
            Item.knockBack = 3f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<global::tsorcRevamp.Projectiles.Enemy.OolacileSorcerer.OccultistDestinedDeathFlask>();
            Item.shootSpeed = 13f;
            Item.DamageType = DamageClass.Magic;
            Item.value = 0;
        }
    }
}
