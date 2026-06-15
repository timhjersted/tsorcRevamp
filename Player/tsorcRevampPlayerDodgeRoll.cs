using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Textures;

namespace tsorcRevamp
{
    public struct Timer
    {
        private uint endTime;

        public bool Active => Main.GameUpdateCount < endTime;
        public uint Value
        {
            get => (uint)Math.Max(0, (long)endTime - Main.GameUpdateCount);
            set => endTime = Main.GameUpdateCount + value;
        }

        public void Set(uint minValue) => Value = Math.Max(minValue, Value);

        public static implicit operator Timer(uint value) => new Timer() { Value = value };
        public static implicit operator Timer(int value) => new Timer() { Value = (uint)value };
    }

    public enum PlayerFrames
    {
        Idle,
        Use1,
        Use2,
        Use3,
        Use4,
        Jump,
        Walk1,
        Walk2,
        Walk3,
        Walk4,
        Walk5,
        Walk6,
        Walk7,
        Walk8,
        Walk9,
        Walk10,
        Walk11,
        Walk12,
        Walk13,
        Walk14,
        Count
    }
    public class BearerOfTheCurseAccessorySlot : ModAccessorySlot
    {
        public override bool IsEnabled()
        {
            if (Main.gameMenu) return false;
            return Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse;
        }
    }
    public partial class tsorcRevampPlayer : ModPlayer
    {
        public static float DodgeTimeMax => 0.37f;
        public static uint DodgeDefaultCooldown => 30;
        public static int DefaultDodgeImmuneTime = 18;
        public static int DodgeImmuneTime = 18;

        public Timer dodgeCooldown;
        public sbyte dodgeDirection;
        public sbyte dodgeDirectionVisual;
        public sbyte wantedDodgerollDir;
        public float dodgeTime;
        public float dodgeStartRot;
        public float dodgeItemRotation;
        public bool isDodging;
        public float wantsDodgerollTimer;
        public bool forceDodgeroll;
        public bool noDodge;
        public float rotation;
        public float? forcedItemRotation;
        public PlayerFrames? forcedHeadFrame;
        public PlayerFrames? forcedBodyFrame;
        public PlayerFrames? forcedLegFrame;
        public int forcedDirection;
        public int itemBlockingTime;
        public float deaccelerationRate;
        public float deaccelerationFactor;
        public float airDeaccelerationRate;
        public float dodgeSpeed = 8f;
        public float beforeRollSpeed;
        public float speedMultiplier;


        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            if ((isDodging || dodgeCooldown.Value != 0 || blockVisuals > 0) && !Player.GetModPlayer<tsorcRevampPlayer>().CanUseItemsWhileDodging)
            {
                PlayerDrawLayers.HeldItem.Hide();
                return;
            }
        }

        int oldItemAnimation = 0;
        bool wasJustRolling = false;
        int blockVisuals; //Block the remaining itemAnimation visuals after a roll, to prevent visual jank
        public override bool PreItemCheck()
        {
            UpdateDodging();
            //UpdateSwordflip();

            //Make held item vfx code still run while rolling
            Item item = Player.HeldItem;
            if (item != null && item.ModItem != null)
            {
                Player.HeldItem.ModItem.HoldItem(Player);
            }

            //Stop players from using items while rolling, and fix their offset
            if ((isDodging || dodgeCooldown.Value != 0) && !Player.GetModPlayer<tsorcRevampPlayer>().CanUseItemsWhileDodging)
            {
                Player.itemLocation = Player.Center + new Vector2(-32, -16); //Stops it from being as disjointed when the player comes out of a roll
                wasJustRolling = true;
                blockVisuals = Player.itemAnimation;
                return false;
            }

            if (blockVisuals > 0)
            {
                blockVisuals--;
            }



            #region Souls Mode Stamina Usage

            // Bearer of the Curse pays full weapon stamina; Unkindled pays 75% (mult = 0.75).
            if (Player.GetModPlayer<tsorcRevampPlayer>().UsesWeaponStamina)
            {

                tsorcRevampStaminaPlayer modPlayer = Player.GetModPlayer<tsorcRevampStaminaPlayer>();
                float mult = Player.GetModPlayer<tsorcRevampPlayer>().WeaponStaminaMult;
                int scaledUseAnimation = (int)(item.useAnimation / Player.GetAttackSpeed(item.DamageType));

                bool startedAnimation = (Player.itemAnimation > oldItemAnimation && Player.itemAnimationMax > 0);
                oldItemAnimation = Player.itemAnimation;

                if (!startedAnimation && item.type != ItemID.Harpoon && item.type != ModContent.ItemType<Items.Weapons.Ranged.Flamethrowers.Freezethrower>()
                     && item.type != ModContent.ItemType<Items.Weapons.Ranged.Flamethrowers.Meltdown>()
                      && item.type != ModContent.ItemType<Items.Weapons.Magic.DivineBoomCannon>()
                       && item.type != ModContent.ItemType<Items.Weapons.Magic.DivineSpark>())
                {
                    return true;
                }
                /*if (item.DamageType == DamageClass.Magic)
                {
                    scaledUseAnimation *= 8;
                    scaledUseAnimation /= 10;
                }*/

                if (item.type == ItemID.CoinGun) //coin gun has a damage stat of zero but can still do damage!
                {
                    modPlayer.staminaResourceCurrent -= ReduceStamina(scaledUseAnimation) * mult;
                }

                else if (item.pick != 0 || item.axe != 0 || item.hammer != 0 || item.damage <= 1 || item.type == ModContent.ItemType<Items.Weapons.Ranged.Specialist.GlaiveBeam>() || item.type == ModContent.ItemType<Items.Weapons.Magic.ArcaneLightrifle>() || item.DamageType == DamageClass.Summon)
                {
                    return true;
                }

                if (item.useAnimation * 0.8f > modPlayer.staminaResourceMax2)
                {
                    modPlayer.staminaResourceCurrent -= modPlayer.staminaResourceMax2 * mult;
                }

                //Note: This is where EVERY other weapon aside from these exceptions applies its stamina usage
                else if (item.type != ItemID.PiranhaGun && item.type != ItemID.Harpoon && item.type != ModContent.ItemType<Items.Weapons.Ranged.Flamethrowers.Meltdown>() && item.type != ModContent.ItemType<Items.Weapons.Ranged.Flamethrowers.Freezethrower>()
                    && !(item.type == ModContent.ItemType<Items.Weapons.Magic.DivineSpark>() || item.type == ModContent.ItemType<Items.Weapons.Magic.DivineBoomCannon>()))
                {
                    modPlayer.staminaResourceCurrent -= ReduceStamina(scaledUseAnimation) * mult;
                }

                //i have no clue how they made this item behave the way it does, but it is deeply cursed
                else if (item.type == ItemID.Harpoon && Player.itemAnimation == 4)
                {
                    modPlayer.staminaResourceCurrent -= 14 * mult;
                }

                if (Player.itemAnimation != 0 && (item.type == ModContent.ItemType<Items.Weapons.Ranged.Flamethrowers.Meltdown>() || item.type == ModContent.ItemType<Items.Weapons.Ranged.Flamethrowers.Freezethrower>()))
                {
                    modPlayer.staminaResourceCurrent -= 0.7f * mult;
                }

                if (Player.itemAnimation != 0 && (item.type == ModContent.ItemType<Items.Weapons.Magic.DivineSpark>() || item.type == ModContent.ItemType<Items.Weapons.Magic.DivineBoomCannon>()))
                {
                    modPlayer.staminaResourceCurrent -= 1.2f * mult;
                }
            }

            #endregion

            return true;
        }

        public void QueueDodgeroll(float wantTime, sbyte direction, bool force = false)
        {
            wantsDodgerollTimer = wantTime;
            wantedDodgerollDir = direction;

            if (force)
            {
                dodgeCooldown = 0;
            }
        }

        //request that the compiler inlines this method, as opposed to making method calls which are slightly slower
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float ReduceStamina(int itemUseAnimation)
        {
            // y=\left(\log_{1.025}\left(x+41\right)\right)-150.392
            //float foo = (float)((Math.Log(itemUseAnimation + 41, 1.025)) - 150.392);
            //Main.NewText(foo);
            return (float)((Math.Log(itemUseAnimation + 41, 1.025)) - 150.392);
        }

        public int KeyDirection(Player player) => player.controlLeft ? -1 : player.controlRight ? 1 : 0;
        public static bool OnGround(Player player) => player.velocity.Y == 0f;
        public static bool WasOnGround(Player player) => player.oldVelocity.Y == 0f;
        public static float StepTowards(float value, float goal, float step)
        {
            if (goal > value)
            {
                value += step;

                if (value > goal)
                {
                    return goal;
                }
            }
            else if (goal < value)
            {
                value -= step;

                if (value < goal)
                {
                    return goal;
                }
            }

            return value;
        }

        private bool TryStartDodgeroll()
        {
            bool isLocal = Player.whoAmI == Main.myPlayer;

            if (isLocal && wantsDodgerollTimer <= 0f && tsorcRevamp.DodgerollKey.JustPressed && !Player.mouseInterface
                && Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 30 && !Player.GetModPlayer<tsorcRevampEstusPlayer>().isDrinking && !Player.GetModPlayer<tsorcRevampCeruleanPlayer>().isDrinking
                && !Player.HasBuff(BuffID.Frozen) && !Player.HasBuff(ModContent.BuffType<Hold>()) && !Player.HasBuff(BuffID.Stoned) && !Player.HasBuff(ModContent.BuffType<Stiff>()))
            {
                QueueDodgeroll(0.25f, (sbyte)KeyDirection(Player));
            }

            if (!forceDodgeroll)
            {
                //Only initiate dodgerolls locally.
                if (!isLocal)
                {
                    return false;
                }

                //Input & cooldown check. The cooldown can be enforced by other actions.
                if (wantsDodgerollTimer <= 0f || dodgeCooldown.Active)
                {
                    return false;
                }

                //Disabled as an experiment
                bool itemBlocking = Player.itemAnimation > 0;
                if (Player.GetModPlayer<tsorcRevampPlayer>().ReflectionShiftEnabled)
                {
                    itemBlocking = false;
                }

                //Don't allow dodging on mounts
                if (Player.mount != null && Player.mount.Active)
                {
                    return false;
                }
            }

            wantsDodgerollTimer = 0f;
            Player.grappling[0] = -1;
            Player.grapCount = 0;
            for (int p = 0; p < 1000; p++)
            {
                if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && (Main.projectile[p].aiStyle == ProjAIStyleID.Hook || Main.projectile[p].aiStyle == ProjAIStyleID.Flail || Main.projectile[p].GetGlobalProjectile<tsorcGlobalProjectile>().ModdedFlail))
                {
                    Main.projectile[p].Kill();
                }
            }

            //Player.eocHit = 1;

            isDodging = true;

            //play a dodge roll sound, choose between these 
            if (isDodging == true)
            {

                int choice = Main.rand.Next(2);
                //switch expressions are the only good thing to come out of the 1.4 change PepeLaugh
                string soundFile = choice switch
                {
                    0 => "tsorcRevamp/Sounds/DarkSouls/roll1",
                    1 => "tsorcRevamp/Sounds/DarkSouls/roll2",
                    _ => "tsorcRevamp/Sounds/DarkSouls/roll1",
                };
                SoundStyle rollSound = new SoundStyle(soundFile)
                {
                    Volume = 0.1f,
                    PitchVariance = 0.4f
                };
                SoundEngine.PlaySound(rollSound, Player.position);


            }
            //only subtract stamina on a successful roll
            Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= 30;
            Player.immune = true;
            DodgeImmuneTime = DefaultDodgeImmuneTime;
            Player.immuneTime = DodgeImmuneTime;
            dodgeStartRot = Player.GetModPlayer<tsorcRevampPlayer>().rotation;
            dodgeItemRotation = Player.itemRotation;
            dodgeTime = 0f;
            dodgeDirectionVisual = (sbyte)Player.direction;
            dodgeDirection = wantedDodgerollDir != 0 ? wantedDodgerollDir : (sbyte)Player.direction;
            dodgeCooldown = DodgeDefaultCooldown;
            beforeRollSpeed = Math.Abs(Player.velocity.X);


            if (!Player.GetModPlayer<tsorcRevampPlayer>().CanUseItemsWhileDodging)
            {
                Player.channel = false;
                Player.TryInterruptingItemUsage();
            }

            if (!isLocal)
            {
                forceDodgeroll = false;
            }
            else if (Main.netMode != NetmodeID.SinglePlayer)
            {
                ModPacket rollPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                rollPacket.Write((byte)tsorcPacketID.SyncPlayerDodgeroll);
                rollPacket.Write(false);
                rollPacket.Write((byte)Player.whoAmI);
                rollPacket.Write(wantedDodgerollDir);
                rollPacket.WriteVector2(Player.velocity);
                rollPacket.Send();
            }

            deaccelerationRate = 0.9f;
            // Deacceleration Factor: Denotes how many times less deacceleration occurs midair.
            // Inches the speed multiplier due to deacceleration in midair closer to but never reaching 1 based on the factor given.
            deaccelerationFactor = 3f;
            airDeaccelerationRate = deaccelerationRate + (1 - 1f / deaccelerationFactor) * (1f - deaccelerationRate);

            DodgeImmuneTime = 18;
            dodgeCooldown = 30;

            bool onGround = OnGround(Player);

            // Define custom roll parameters when acessories conflict.
            // Chloranthy Rings no longer modify deaccelerationRate — their speed boost during the roll
            // already extends travel distance; adding a per-frame deceleration bonus on top produced a
            // persistent "slippery ice" glide after the roll that made precise stopping impossible.
            if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing2 && Player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis)
            {
                DodgeImmuneTime += 2;
                dodgeCooldown = 10;
            }
            // ChloranthyRing1 cancels out completely with the IceboundMythrilAegis
            else if (!(Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing1 && Player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis))
            {
                // To make sure player does not benefit from stacking ring 1 and 2
                if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing2)
                {
                    DodgeImmuneTime += 6;
                    dodgeCooldown = 0;
                }
                else if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing1)
                {
                    DodgeImmuneTime += 3;
                    dodgeCooldown = 10;
                }

                if (Player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis)
                {
                    deaccelerationRate -= 0.13f;
                    DodgeImmuneTime -= 2;
                    dodgeCooldown = 35;
                }
            }

            if (Player.GetModPlayer<tsorcRevampPlayer>().BurdenOfSmough)
            {
                deaccelerationRate -= 0.25f;
                DodgeImmuneTime -= 4;
                dodgeCooldown = dodgeCooldown.Value + 10;
            }

            if (Player.GetModPlayer<tsorcRevampPlayer>().HollowSoldierAgility)
            {
                DodgeImmuneTime += 3;
                dodgeCooldown = dodgeCooldown.Value > 20 ? dodgeCooldown.Value - 20 : 0;

                if (onGround)
                {
                    deaccelerationRate += 0.05f;
                    DodgeImmuneTime += 3;
                    dodgeCooldown = dodgeCooldown.Value > 2 ? dodgeCooldown.Value - 2 : 0;
                }
            }

            return true;
        }
        private void UpdateDodging()
        {

            wantsDodgerollTimer = StepTowards(wantsDodgerollTimer, 0f, (float)1 / 60);

            noDodge |= Player.mount.Active;

            if (noDodge)
            {
                isDodging = false;
                noDodge = false;

                return;
            }

            bool onGround = OnGround(Player);
            ref float rotation = ref Player.GetModPlayer<tsorcRevampPlayer>().rotation;

            //Attempt to initiate a dodgeroll if the player isn't doing one already.
            if (!isDodging && !TryStartDodgeroll())
            {
                return;
            }
            /*
            bool chloranthyRing = false;
            for (int i = 3; i <= (8 + Player.extraAccessorySlots); i++) {
                if (Player.armor[i].type == ModContent.ItemType<Items.Accessories.Expert.ChloranthyRing>()) {
                    chloranthyRing = true;
                    break;
                }
            }
            
            bool chloranthyRing2 = false;
            for (int i = 3; i <= (8 + Player.extraAccessorySlots); i++) {
                if (Player.armor[i].type == ModContent.ItemType<Items.Accessories.Expert.ChloranthyRing2>()) {
                    chloranthyRing2 = true;
                    break;
                }
            }*/

            //Apply velocity
            if (dodgeTime < DodgeTimeMax * 0.5f)
            {
                DodgeImmuneTime = DefaultDodgeImmuneTime;

                dodgeSpeed = 8f;

                // Increase the base roll speed if the player is moving faster than the default
                if (beforeRollSpeed > dodgeSpeed)
                    dodgeSpeed = beforeRollSpeed;

                // Chloranthy Ring dodge-speed boost — intended to grant only "a little bit more dodge length"
                // (a subtle distance increase, not a flat-out velocity multiplier). Values were previously
                // +3 / +6 which combined with the ground multiplier and momentum carry produced an extreme
                // total. Dialed back to +1 / +2 to restore the original tuning intent.
                if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing2 && Player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis)
                {
                    dodgeSpeed += 1f;
                }
                else if (!(Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing1 && Player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis))
                {
                    if (Player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis)
                        dodgeSpeed -= 1f;

                    if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing2)
                    {
                        dodgeSpeed += 2f;
                    }
                    else if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing1)
                    {
                        dodgeSpeed += 1f;
                    }
                }

                if (Player.GetModPlayer<tsorcRevampPlayer>().HollowSoldierAgility)
                    dodgeSpeed += 2f;

                if (Player.GetModPlayer<tsorcRevampPlayer>().BurdenOfSmough)
                    dodgeSpeed -= 2.5f;

                // Faster roll speed if on the ground
                float speedMultiplier = onGround ? 1.4f : 1.1f;

                dodgeSpeed *= speedMultiplier;
                dodgeSpeed *= dodgeDirection;

                // Bug fix: speedMultiplier was being applied twice (once into dodgeSpeed above, then
                // again here when assigning velocity), producing ~1.96× the intended velocity on the
                // ground and amplifying every other boost (Chloranthy rings, momentum carry, etc.).
                Player.velocity.X = dodgeSpeed;
            }

            Player.pulley = false;

            //Apply rotations & direction
            forcedItemRotation = dodgeItemRotation;
            forcedLegFrame = PlayerFrames.Jump;
            forcedDirection = dodgeDirectionVisual;

            rotation = dodgeDirection == 1
                ? Math.Min(MathHelper.Pi * 2f, MathHelper.Lerp(dodgeStartRot, MathHelper.TwoPi, dodgeTime / (DodgeTimeMax * 1f)))
                : Math.Max(-MathHelper.Pi * 2f, MathHelper.Lerp(dodgeStartRot, -MathHelper.TwoPi, dodgeTime / (DodgeTimeMax * 1f)));
            //Progress the dodgeroll
            dodgeTime += 1f / 60f;
            Player.immune = true;

            if (dodgeTime >= DodgeTimeMax * 0.6f)
            {
                if (isDodging && Player.GetModPlayer<tsorcRevampPlayer>().MythrilBulwark)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC other = Main.npc[i];

                        if (!other.friendly & other.Hitbox.Intersects(Utils.CenteredRectangle(Player.Center, new Vector2(200, 200))))
                        {
                            other.AddBuff(ModContent.BuffType<MythrilRamDebuff>(), Items.Accessories.Damage.MythrilBulwark.VulnerabilityDuration * 60);
                        }
                    }
                }
                if (isDodging && Player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC other = Main.npc[i];

                        if (!other.friendly & other.Hitbox.Intersects(Utils.CenteredRectangle(Player.Center, new Vector2(200, 200))))
                        {
                            other.AddBuff(ModContent.BuffType<MythrilRamDebuff>(), Items.Accessories.Damage.MythrilBulwark.VulnerabilityDuration * 60);
                            other.AddBuff(BuffID.Frostburn2, Items.Accessories.Damage.MythrilBulwark.VulnerabilityDuration * 60);

                            if (Main.rand.NextBool(3))
                            {
                                other.AddBuff(BuffID.Confused, Items.Accessories.Damage.MythrilBulwark.VulnerabilityDuration * 60);
                            }
                            if (Main.rand.NextBool(3))
                            {
                                other.AddBuff(BuffID.Bleeding, Items.Accessories.Damage.MythrilBulwark.VulnerabilityDuration * 60);
                            }
                            if (Main.rand.NextBool(3))
                            {
                                other.AddBuff(BuffID.Poisoned, Items.Accessories.Damage.MythrilBulwark.VulnerabilityDuration * 60);
                            }
                        }
                    }
                }

                // If the player is actively running in the roll direction, use a softer decel so
                // a roll that flows into movement doesn't feel like it hits a wall. When standing
                // still (or pressing opposite), use the full deceleration for a snappy stop.
                float groundDecel = KeyDirection(Player) == dodgeDirection
                    ? MathHelper.Lerp(deaccelerationRate, 1f, 0.35f)
                    : deaccelerationRate;
                Player.velocity.X *= onGround ? groundDecel : airDeaccelerationRate;
            }

            if (dodgeTime >= DodgeTimeMax)
            {
                isDodging = false;
                //Player.eocDash = 0;
                //forceSyncControls = true;
            }
            else
            {
                Player.runAcceleration = 0f;
            }
        }

        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            return !isDodging;
        }

        public override bool CanBeHitByProjectile(Projectile proj)
        {
            return !isDodging;
        }
    }
}
