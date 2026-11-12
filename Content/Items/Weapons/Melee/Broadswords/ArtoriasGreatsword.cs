using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Melee.Broadswords;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Broadswords
{
    class ArtoriasGreatsword : Greatsword
    {
        // Each cut throws Artorias's abyss crescent toward the cursor for mana; it deals magic damage.
        public const int SlashManaCost = 30;
        public const float SlashDamageScale = 0.5f;
        public const int SlashRangeTiles = 20;

        // Holding the button through the windup trades the swing for a forward-flip leap to the cursor.
        public const int LeapHoldTicks = 12;
        public const int LeapRangeTiles = 20;
        public const int SmokeWallTiles = 12;

        protected internal override Vector2 Grip => new(6f, 90f);
        protected internal override Vector2 HiltTop => new(16f, 80f);
        protected internal override Vector2 BladeHeel => new(40f, 54f);
        protected internal override Vector2 CuttingEdge => new(66f, 29f);
        protected internal override Vector2 BladeTip => new(95f, 1f);

        // Full ~183 deg cuts like a broadsword (up-behind to down-forward and back), and a slam that finishes with
        // the 126px blade's point at floor level (sin(0.17) x 126 = 21px below the hand). Every windup is at least
        // LeapHoldTicks long at base speed (36 x 0.34 = 12.2) so the hold-to-leap decision is made before any cut lands.
        const float CutHigh = -2.45f;
        const float CutLow = 0.75f;

        private static readonly GreatswordStep[] ArtoriasSteps =
        {
            new GreatswordStep
            {
                LoadedAngle = CutHigh,
                FinishAngle = CutLow,
                WindupFraction = 0.34f,
                AccelFraction = 0.12f,
                StartSpeed = 0.25f,
                DecelPower = 2.6f,
                DurationScale = 1f,
                DamageMultiplier = 1f
            },
            new GreatswordStep
            {
                LoadedAngle = CutLow + 0.15f,
                FinishAngle = CutHigh + 0.15f,
                WindupFraction = 0.34f,
                AccelFraction = 0.12f,
                StartSpeed = 0.25f,
                DecelPower = 2.6f,
                DurationScale = 1f,
                DamageMultiplier = 1f
            },
            new GreatswordStep
            {
                LoadedAngle = -2.95f,
                FinishAngle = 0.17f,
                WindupFraction = 0.38f,
                AccelFraction = 0.30f,
                StartSpeed = 0.05f,
                DecelPower = 2f,
                DurationScale = 1.1f,
                DamageMultiplier = 2f,
                Slam = true
            }
        };

        public override GreatswordStep[] ComboSteps => ArtoriasSteps;

        public override int HoldActionTicks => LeapHoldTicks;

        public override bool UsesBladeSlash => true;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            SlashManaCost, (int)(SlashDamageScale * 100), SlashRangeTiles, ArtoriasSteps[2].DamageMultiplier, LeapRangeTiles, SmokeWallTiles,
            ArtoriasGreatswordLeap.SlamDamageMultiplier);

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.damage = 330;
            Item.width = 96;
            Item.height = 96;
            Item.knockBack = 7f;
            Item.scale = 1f;
            Item.useAnimation = 36;
            Item.useTime = 36;
            Item.autoReuse = false;
            Item.value = PriceByRarity.Purple_11;

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Color.DarkViolet;
        }

        public override bool CanUseItem(Player player)
        {
            bool leaping = player.ownedProjectileCounts[ModContent.ProjectileType<ArtoriasGreatswordLeap>()] > 0;
            return base.CanUseItem(player) && !leaping;
        }

        public override void OnStrikeStart(Projectile swing, Player player, int position)
        {
            // No mana, no crescent: the swing itself still lands.
            int manaCost = (int)(SlashManaCost * player.manaCost);
            if (!player.CheckMana(manaCost, true))
            {
                return;
            }

            player.manaRegenDelay = (int)player.maxRegenDelay;

            Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            float baseDamage = Item.damage * SlashDamageScale;
            int damage = (int)player.GetTotalDamage(DamageClass.Magic).ApplyTo(baseDamage);
            Projectile.NewProjectile(swing.GetSource_FromThis(), player.MountedCenter, aim * ArtoriasGreatswordSlash.Speed,
                ModContent.ProjectileType<ArtoriasGreatswordSlash>(), damage, 3f, player.whoAmI);
        }

        public override void StartHoldAction(Projectile swing, Player player)
        {
            // Leap target: the cursor, pulled in to LeapRangeTiles, dropped onto the floor beneath it if there is one.
            Vector2 start = player.Bottom;
            Vector2 toCursor = Main.MouseWorld - start;
            float maxRange = LeapRangeTiles * 16f;
            if (toCursor.Length() > maxRange)
            {
                toCursor = toCursor.SafeNormalize(Vector2.UnitX) * maxRange;
            }

            Vector2 target = ArtoriasGreatswordLeap.FloorBelow(start + toCursor);
            int facing = player.direction;
            if (target.X != start.X)
            {
                facing = target.X > start.X ? 1 : -1;
            }

            // Flight time grows with distance; it rides in velocity.Y so the spawn packet carries it to every client.
            float distanceTiles = Vector2.Distance(start, target) / 16f;
            int flightTicks = ArtoriasGreatswordLeap.BaseFlightTicks + (int)(distanceTiles * ArtoriasGreatswordLeap.FlightTicksPerTile);

            int damage = player.GetWeaponDamage(Item);
            float knockback = player.GetWeaponKnockback(Item);
            Projectile.NewProjectile(swing.GetSource_FromThis(), player.MountedCenter, new Vector2(facing, flightTicks),
                ModContent.ProjectileType<ArtoriasGreatswordLeap>(), damage, knockback, player.whoAmI, target.X, target.Y);
        }
    }
}
