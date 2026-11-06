using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor;
using tsorcRevamp.Content.Projectiles.Melee.Broadswords;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Broadswords
{
    public class SwordOfLordGwyn : Greatsword
    {
        public const int DashManaCost = 28;
        public const int SpearManaCost = 22;
        public const int NovaManaCost = 45;

        // Share of a swing's damage the finisher's lightning bolt deals.
        public const float SlamLightningDamageScale = 0.6f;

        // The 96px sprite: tip at (92, 4) is 115px from the grip at (11, 86).
        protected internal override Vector2 Grip => new(11f, 86f);
        protected internal override Vector2 HiltTop => new(18f, 78f);
        protected internal override Vector2 BladeHeel => new(34f, 62f);
        protected internal override Vector2 CuttingEdge => new(60f, 36f);
        protected internal override Vector2 BladeTip => new(92f, 4f);

        public override int BaseSwingTicks => 32;

        public override bool UsesSlashTrail => true;

        // Quicker and sharper than the Gravelord Sword: a short accel into a hard-falling speed curve with a long
        // settle, and a heavier, slower-starting slam. Gwyn's shorter blade needs a higher slam finish to meet the floor.
        private static readonly GreatswordStep[] GwynSteps =
        {
            new GreatswordStep
            {
                LoadedAngle = -2.2f,
                FinishAngle = 0.25f,
                WindupFraction = 0.26f,
                AccelFraction = 0.10f,
                StartSpeed = 0.3f,
                DecelPower = 3f,
                DurationScale = 1f,
                DamageMultiplier = 1f
            },
            new GreatswordStep
            {
                LoadedAngle = 0.45f,
                FinishAngle = -2.2f,
                WindupFraction = 0.18f,
                AccelFraction = 0.10f,
                StartSpeed = 0.3f,
                DecelPower = 3f,
                DurationScale = 1f,
                DamageMultiplier = 1f
            },
            new GreatswordStep
            {
                LoadedAngle = -2.95f,
                FinishAngle = 0.38f,
                WindupFraction = 0.38f,
                AccelFraction = 0.32f,
                StartSpeed = 0.04f,
                DecelPower = 2.2f,
                DurationScale = 1.2f,
                DamageMultiplier = 2f,
                Slam = true
            }
        };

        public override GreatswordStep[] ComboSteps => GwynSteps;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 315;
            Item.width = 96;
            Item.height = 96;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.channel = true;
            Item.knockBack = 9f;
            Item.crit = 8;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.Purple_11;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<SwordOfLordGwynController>();

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = new Color(255, 160, 20); // orange-gold, between the old yellow and the first-flame orange-red
            // Center the slash band at ~86px so its outer edge reaches the blade tip 115px from the fist.
            instancedGlobal.slashVisualScale = 0.635f;
        }

        // A tap may arrive mid-swing: the controller then queues the next combo step instead of slashing at once.
        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<SwordOfLordGwynController>()] == 0;
        }

        public override bool AltFunctionUse(Player player) => FourAttackWeaponControls.UsesAltFunction;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int mode = FourAttackWeaponControls.GetAttackMode(player);
            Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback,
                player.whoAmI, mode, player.altFunctionUse == 2 ? 1f : 0f);
            return false;
        }

        public override void SwingVisuals(Projectile swing, Player player, Vector2 hand, Vector2 tip, bool live)
        {
            // Embers along the blade the whole swing, denser while it can hit.
            int count = 2;
            if (live)
            {
                count = 5;
            }

            for (int i = 0; i < count; i++)
            {
                Vector2 position = Vector2.Lerp(hand, tip, Main.rand.NextFloat(0.2f, 1f));
                int type = DustID.Torch;
                if (Main.rand.NextBool())
                {
                    type = DustID.GoldFlame;
                }

                Dust ember = Dust.NewDustDirect(position - new Vector2(3f), 6, 6, type, player.velocity.X * 0.1f, player.velocity.Y * 0.1f, 60, default, 1.35f);
                ember.noGravity = true;
            }

            Lighting.AddLight(Vector2.Lerp(hand, tip, 0.6f), 0.8f, 0.42f, 0.12f);
        }

        public override void OnSwingHitNPC(Projectile swing, NPC target, int step)
        {
            target.AddBuff(BuffID.OnFire3, 5 * 60);
        }

        public override void OnSlamImpact(Projectile swing, Player player, Vector2 impact, bool hitGround)
        {
            bool empowered = player.GetModPlayer<LordGwynSetPlayer>().AboveHalfMana;
            int damage = (int)(swing.damage * SlamLightningDamageScale);
            float empoweredFlag = 0f;
            if (empowered)
            {
                empoweredFlag = 1f;
            }

            Projectile.NewProjectile(swing.GetSource_FromThis(), impact, Vector2.Zero,
                ModContent.ProjectileType<SwordOfLordGwynSlamLightning>(), damage, swing.knockBack * 0.5f, player.whoAmI, empoweredFlag);
        }
    }
}
