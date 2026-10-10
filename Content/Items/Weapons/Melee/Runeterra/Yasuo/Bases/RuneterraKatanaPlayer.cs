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

    public NPC SweepingBladeTarget = Main.npc.Last();
        public void DoSweepingBlade()
        {
            /*List<NPC> validTargets = new List<NPC>();
            SweepingBladeTarget = Main.npc.Last();
            bool plasma = true;
            foreach (var other in Main.ActiveNPCs)
            {
                bool hasPlasma = Player.HeldItem.type == ModContent.ItemType<PlasmaWhirlwind>() &&
                                 !Player.HasBuff(ModContent.BuffType<PlasmaWhirlwindDash>());
                bool hasNightbringer = Player.HeldItem.type == ModContent.ItemType<Nightbringer>() &&
                                       !Player.HasBuff(ModContent.BuffType<NightbringerDash>());
                bool targetable = !tsorcRevamp.UntargetableNPCs.Contains(other.type) && !other.friendly 
                    && other.Hitbox.Intersects(Utils.CenteredRectangle(Main.MouseWorld, MouseHitboxSize)) 
                    && other.Distance(Player.Center) <= 400 && !other.HasBuff(ModContent.BuffType<PlasmaWhirlwindDashCooldown>())
                    && !other.HasBuff(ModContent.BuffType<NightbringerDashCooldown>());
                if (targetable && hasPlasma)
                {
                    validTargets.Add(other);
                    plasma = true;
                } //cooldown is added by On-Hit in the dash projectile hitbox
                if (!(Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) && targetable  && hasNightbringer)
                {
                    validTargets.Add(other);
                    plasma = false;
                } //cooldown is added by On-Hit in the dash projectile hitbox

                foreach (NPC target in validTargets)
                {
                    if (target.Center.Distance(Main.MouseWorld) < SweepingBladeTarget.Center.Distance(Main.MouseWorld))
                    {
                        SweepingBladeTarget = target; //so it actually dashes to the npc nearest to your cursor, not any random npc that is in the cursors range
                    }
                }
            }*/

            if (SweepingBladeTarget != Main.npc.Last())
            {
                int heldItem = Player.HeldItem.type;
                if (heldItem == ModContent.ItemType<PlasmaWhirlwind>())
                {
                    Player.immune = true;
                    Player.SetImmuneTimeForAllTypes((int)(PlasmaWhirlwind.DashDuration * 60f * 5));
                    SweepingBladeVelocity = Player.DirectionTo(SweepingBladeTarget.Center) * 17;
                    Player.AddBuff(ModContent.BuffType<PlasmaWhirlwindDash>(), (int)(PlasmaWhirlwind.DashDuration * 60f * 2));
                    SoundEngine.PlaySound(new SoundStyle( UsefulFunctions.RefactorableFilepath(typeof(PlasmaWhirlwindSound)) + "_Dash") with { Volume = 1f });
                    Projectile DashHitbox = Projectile.NewProjectileDirect(Projectile.GetSource_None(), Player.Center, Vector2.Zero, 
                        ModContent.ProjectileType<PlasmaWhirlwindDashHitbox>(), Player.HeldItem.damage, 0, Player.whoAmI, SweepingBladeTarget.whoAmI);
                }
                else if (heldItem == ModContent.ItemType<Nightbringer>())
                {
                    Player.immune = true;
                    SweepingBladeVelocity = Player.DirectionTo(SweepingBladeTarget.Center) * 17;
                    Player.SetImmuneTimeForAllTypes((int)(PlasmaWhirlwind.DashDuration * 60f * 5));
                    Player.AddBuff(ModContent.BuffType<NightbringerDash>(), (int)(PlasmaWhirlwind.DashDuration * 60f * 2));
                    SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(NightbringerSound)) + "_Dash") with { Volume = 1f });                    
                    Projectile DashHitbox = Projectile.NewProjectileDirect(Projectile.GetSource_None(), Player.Center, Vector2.Zero, 
                        ModContent.ProjectileType<NightbringerDashHitbox>(), Player.HeldItem.damage, 0, Player.whoAmI, SweepingBladeTarget.whoAmI);

                }
            }
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