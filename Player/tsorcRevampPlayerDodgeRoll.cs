using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp
{
    public struct Timer
    {
        private uint endTime;

        public bool Active => Main.GameUpdateCount < endTime;
        public uint Value
        {
            get => (uint)Math.Max(0, (long)endTime - Main.GameUpdateCount);
            set => endTime = Main.GameUpdateCount + Math.Max(0, value);
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



            #region BotC Stamina Usage

            if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {

                tsorcRevampStaminaPlayer modPlayer = Player.GetModPlayer<tsorcRevampStaminaPlayer>();
                int scaledUseAnimation = (int)(item.useAnimation / Player.GetAttackSpeed(item.DamageType));

                bool startedAnimation = (Player.itemAnimation > oldItemAnimation && Player.itemAnimationMax > 0);
                oldItemAnimation = Player.itemAnimation;

                if (!startedAnimation && item.type != ItemID.Harpoon) return true;

                /*if (item.DamageType == DamageClass.Magic)
                {
                    scaledUseAnimation *= 8;
                    scaledUseAnimation /= 10;
                }*/

                if (item.type == ItemID.CoinGun) //coin gun has a damage stat of zero but can still do damage!
                {
                    modPlayer.staminaResourceCurrent -= ReduceStamina(scaledUseAnimation);
                }
                else if (item.pick != 0 || item.axe != 0 || item.hammer != 0 || item.damage <= 1 || item.type == ModContent.ItemType<Items.Weapons.Ranged.GlaiveBeam>() || item.type == ModContent.ItemType<Items.Weapons.Magic.ArcaneLightrifle>() || item.DamageType == DamageClass.Summon) return true;


                if (item.useAnimation * 0.8f > modPlayer.staminaResourceMax2)
                {
                    modPlayer.staminaResourceCurrent -= modPlayer.staminaResourceMax2;
                }

                //piranha gun works differently enough to warrant a special case
                else if (item.type != ItemID.PiranhaGun && item.type != ItemID.Harpoon)
                {
                    modPlayer.staminaResourceCurrent -= ReduceStamina(scaledUseAnimation);

                }
                //i have no clue how they made this item behave the way it does, but it is deeply cursed
                else if (item.type == ItemID.Harpoon && Player.itemAnimation == 4)
                {
                    modPlayer.staminaResourceCurrent -= 14;
                }
                if (Player.itemAnimation != 0 && (item.type == ModContent.ItemType<Items.Weapons.Magic.DivineSpark>() || item.type == ModContent.ItemType<Items.Weapons.Magic.DivineBoomCannon>()))
                {
                    modPlayer.staminaResourceCurrent -= .8f;
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
            Player.immuneTime = DodgeImmuneTime;
            dodgeStartRot = Player.GetModPlayer<tsorcRevampPlayer>().rotation;
            dodgeItemRotation = Player.itemRotation;
            dodgeTime = 0f;
            dodgeDirectionVisual = (sbyte)Player.direction;
            dodgeDirection = wantedDodgerollDir != 0 ? wantedDodgerollDir : (sbyte)Player.direction;
            dodgeCooldown = DodgeDefaultCooldown;

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
                float dodgeSpeed = 8f;

                if (onGround)
                    dodgeSpeed = 12f;

                if (Player.GetModPlayer<tsorcRevampPlayer>().HollowSoldierAgility)
                {
                    dodgeSpeed = 10f;

                    if (onGround)
                        dodgeSpeed = 15f;
                }

                if (Player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis)
                {
                    dodgeSpeed = 7f;

                    if (onGround)
                        dodgeSpeed = 10f;
                }
                if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing1)
                {
                    dodgeSpeed = 11f;

                    if (onGround)
                        dodgeSpeed = 13f;
                }

                if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing2)
                {
                    dodgeSpeed = 14f;

                    if (onGround)
                        dodgeSpeed = 14f;
                }
                if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing1 && Player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis)
                {
                    dodgeSpeed = 8f;

                    if (onGround)
                        dodgeSpeed = 12f;
                }
                if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing2 && Player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis)
                {
                    dodgeSpeed = 11f;

                    if (onGround)
                        dodgeSpeed = 13f;
                }

                if (Player.GetModPlayer<tsorcRevampPlayer>().BurdenOfSmough)
                {
                    dodgeSpeed = 5.5f;

                    if (onGround)
                        dodgeSpeed = 8f;
                }




                dodgeSpeed *= dodgeDirection;

                if (Math.Abs(Player.velocity.X) < Math.Abs(dodgeSpeed) || Math.Sign(dodgeSpeed) != Math.Sign(Player.velocity.X))
                {
                    Player.velocity.X = dodgeSpeed;
                }

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
                float decelerationRate = 0.85f;
                if (Player.GetModPlayer<tsorcRevampPlayer>().HollowSoldierAgility)
                {
                    decelerationRate = 0.85f;
                    DodgeImmuneTime = 20;
                    dodgeCooldown = 12;
                    if (onGround)
                    {
                        decelerationRate = 0.9f;
                        DodgeImmuneTime = 23;
                        dodgeCooldown = 8;
                    }
                    DodgeImmuneTime = 21;
                    dodgeCooldown = 10;
                }

                if (Player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis)
                {
                    decelerationRate = 0.72f;
                    DodgeImmuneTime = 16;
                    dodgeCooldown = 35;
                }
                if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing1)
                {
                    decelerationRate = 0.88f;
                    DodgeImmuneTime = 21;
                    dodgeCooldown = 10;
                }

                //chloranthy ring II effect
                if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing2)
                {
                    decelerationRate = 0.91f;
                    DodgeImmuneTime = 24;
                    dodgeCooldown = 0;
                }

                if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing1 && Player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis)
                {
                    decelerationRate = 0.85f;
                    DodgeImmuneTime = 18;
                    dodgeCooldown = 30;
                }

                if (Player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing2 && Player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis)
                {
                    decelerationRate = 0.88f;
                    DodgeImmuneTime = 21;
                    dodgeCooldown = 10;
                }

                if (Player.GetModPlayer<tsorcRevampPlayer>().BurdenOfSmough)
                {
                    decelerationRate = 0.6f;
                    DodgeImmuneTime = 14;
                    dodgeCooldown = 40;
                }


                if (isDodging && Player.GetModPlayer<tsorcRevampPlayer>().MythrilBulwark)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC other = Main.npc[i];

                        if (!other.friendly & other.Hitbox.Intersects(Utils.CenteredRectangle(Player.Center, new Vector2(200, 200))))
                        {
                            other.AddBuff(ModContent.BuffType<MythrilRamDebuff>(), Items.Accessories.Expert.MythrilBulwark.VulnerabilityDuration * 60);
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
                            other.AddBuff(ModContent.BuffType<MythrilRamDebuff>(), Items.Accessories.Expert.MythrilBulwark.VulnerabilityDuration * 60);
                            other.AddBuff(BuffID.Frostburn2, Items.Accessories.Expert.MythrilBulwark.VulnerabilityDuration * 60);

                            if (Main.rand.NextBool(3))
                            {
                                other.AddBuff(BuffID.Confused, Items.Accessories.Expert.MythrilBulwark.VulnerabilityDuration * 60);
                            }
                            if (Main.rand.NextBool(3))
                            {
                                other.AddBuff(BuffID.Bleeding, Items.Accessories.Expert.MythrilBulwark.VulnerabilityDuration * 60);
                            }
                            if (Main.rand.NextBool(3))
                            {
                                other.AddBuff(BuffID.Poisoned, Items.Accessories.Expert.MythrilBulwark.VulnerabilityDuration * 60);
                            }
                        }
                    }
                }


                //normal effect
                Player.velocity.X *= decelerationRate;
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
