using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Buffs;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Projectiles;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Sounds.Nightbringer;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Sounds.PlasmaWhirlwind;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Bases;

public class RuneterraKatanaPlayer : ModPlayer
{
    public int SteelTempestStacks;
    public Vector2 MouseHitboxSize = new Vector2(125, 125);
    public int SweepingBladeTimer;
    public Vector2 SweepingBladeVelocity;
    public override void PreUpdateMovement()
    {
        if (SweepingBladeTimer > 0)
        {
            Player.velocity = SweepingBladeVelocity;
            SweepingBladeTimer--;
            Player.RefreshMovementAbilities();
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (tsorcRevamp.specialAbility.JustReleased)
        {
            DoSweepingBlade();

            DoFirewall();
        }
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        SteelTempestStacks = 0;
    }

    // Picked each tick by RuneterraKatanaItem.HoldItem (owner only); null when nothing valid is under the cursor.
    public NPC SweepingBladeTarget;

    public override void ResetEffects()
    {
        // Runs after ProcessTriggers and before HoldItem, so the dash key always sees last tick's pick and
        // never a reference left over from when the katana was last held (that NPC slot may be reused by now).
        SweepingBladeTarget = null;
    }

        public void DoSweepingBlade()
        {
            NPC target = SweepingBladeTarget;
            if (target == null || !target.active || target.dontTakeDamage)
            {
                return;
            }

            int heldItem = Player.HeldItem.type;
            int dashBuffID;
            int dashHitboxID;
            string dashSoundPath;

            if (heldItem == ModContent.ItemType<PlasmaWhirlwind>())
            {
                dashBuffID = ModContent.BuffType<PlasmaWhirlwindDash>();
                dashHitboxID = ModContent.ProjectileType<PlasmaWhirlwindDashHitbox>();
                dashSoundPath = UsefulFunctions.RefactorableFilepath(typeof(PlasmaWhirlwindSound));
            }
            else if (heldItem == ModContent.ItemType<Nightbringer>())
            {
                dashBuffID = ModContent.BuffType<NightbringerDash>();
                dashHitboxID = ModContent.ProjectileType<NightbringerDashHitbox>();
                dashSoundPath = UsefulFunctions.RefactorableFilepath(typeof(NightbringerSound));
            }
            else
            {
                return;
            }

            // Dash toward the target at 17 px/tick. DirectionTo turns a zero vector into NaN when the player's center
            // sits exactly on the target's, and vanilla never repairs a NaN velocity, so it would poison the player's
            // position. SafeNormalize falls back to dashing the way the player faces.
            Vector2 toTarget = target.Center - Player.Center;
            Vector2 dashDirection = toTarget.SafeNormalize(Vector2.UnitX * Player.direction);
            SweepingBladeVelocity = dashDirection * 17;

            Player.immune = true;
            Player.SetImmuneTimeForAllTypes((int)(PlasmaWhirlwind.DashDuration * 60f * 5));
            Player.AddBuff(dashBuffID, (int)(PlasmaWhirlwind.DashDuration * 60f * 2));
            SoundEngine.PlaySound(new SoundStyle(dashSoundPath + "_Dash") with { Volume = 1f });

            //cooldown is added to the target by the dash hitbox's AI (ai[0] = target index)
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Player.Center, Vector2.Zero, dashHitboxID, Player.HeldItem.damage, 0, Player.whoAmI, target.whoAmI);
        }

        public void DoFirewall()
        {
            bool hasNightbringer = Player.HeldItem.type == ModContent.ItemType<Nightbringer>() &&
                                   !Player.HasBuff(ModContent.BuffType<NightbringerFirewallCooldown>());
            if ((Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) && hasNightbringer)
            {
                Vector2 unitVectorTowardsMouse = Player.Center.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * Player.direction);
                Projectile Firewall = Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), Player.Center, unitVectorTowardsMouse * 5f,
                    ModContent.ProjectileType<NightbringerFirewall>(), Player.HeldItem.damage, 0, Main.myPlayer);

                Player.AddBuff(ModContent.BuffType<NightbringerFirewallCooldown>(), Nightbringer.WindwallCooldown * 60);
            }
        }
}
