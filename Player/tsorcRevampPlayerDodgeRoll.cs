using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

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

        public override bool PreItemCheck()
        {
            UpdateDodging();
            //UpdateSwordflip();

            //Stop umbrella and other things from working
            if (isDodging && Player.HeldItem.type == ItemID.Umbrella)
            {
                return false;
            }

            Item item = Player.HeldItem;

            #region BotC Stamina Usage

            if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (item.damage >= 1 && item.useAnimation * 0.8f > Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 && Player.itemAnimation == Player.itemAnimationMax - 1)
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2;
                }

                // Stamina drain for most (hopefully) swords and spears
                if (item.damage >= 1 && item.DamageType == DamageClass.Melee && Player.itemAnimation == Player.itemAnimationMax - 1 && item.pick == 0 && item.axe == 0 && !(item.type == ItemID.WoodenBoomerang || item.type == ItemID.EnchantedBoomerang || item.type == ItemID.FruitcakeChakram
                    || item.type == ItemID.BloodyMachete || item.type == ItemID.IceBoomerang || item.type == ItemID.ThornChakram || item.type == ItemID.Flamarang || item.type == ItemID.LightDisc
                    || item.type == ModContent.ItemType<Items.Weapons.Melee.ShatteredMoonlight>() || item.type == ItemID.FlyingKnife))
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= (item.useAnimation * Player.GetAttackSpeed(DamageClass.Melee) * .8f);
                }

                // Stamina drain for boomerangs
                if (item.damage >= 1 && item.DamageType == DamageClass.Melee && Player.itemAnimation == Player.itemAnimationMax - 1 && item.pick == 0 && (item.type == ItemID.WoodenBoomerang || item.type == ItemID.EnchantedBoomerang || item.type == ItemID.FruitcakeChakram
                    || item.type == ItemID.BloodyMachete || item.type == ItemID.IceBoomerang || item.type == ItemID.ThornChakram || item.type == ItemID.Flamarang || item.type == ItemID.LightDisc || item.type == ModContent.ItemType<Items.Weapons.Melee.ShatteredMoonlight>()))
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= (item.useAnimation * Player.GetAttackSpeed(DamageClass.Melee) * 1f);
                }

                // Stamina drain for flails and yoyos
                if (item.damage >= 1 && item.useStyle == ItemUseStyleID.Shoot && item.DamageType == DamageClass.Melee && Player.itemAnimation != 0
                    && (item.type != ItemID.Spear && item.type != ItemID.Trident && item.type != ItemID.TheRottedFork && item.type != ItemID.Swordfish && item.type != ItemID.DarkLance
                    && item.type != ItemID.CobaltNaginata && item.type != ItemID.PalladiumPike && item.type != ItemID.MythrilHalberd && item.type != ItemID.OrichalcumHalberd
                    && item.type != ItemID.AdamantiteGlaive && item.type != ItemID.TitaniumTrident && item.type != ItemID.Gungnir && item.type != ItemID.ChlorophytePartisan
                    && /*item.type != ItemID.MonkStaffT1 &&*/ item.type != ItemID.MonkStaffT2 && /*item.type != ItemID.MonkStaffT3 &&*/ item.type != ItemID.MushroomSpear
                    && item.type != ItemID.ObsidianSwordfish && item.type != ItemID.NorthPole && item.type != ModContent.ItemType<Items.Weapons.Melee.CopperSpear>()
                    && item.type != ModContent.ItemType<Items.Weapons.Melee.IronSpear>() && item.type != ModContent.ItemType<Items.Weapons.Melee.SilverSpear>()
                    && item.type != ModContent.ItemType<Items.Weapons.Melee.GoldSpear>() && item.type != ModContent.ItemType<Items.Weapons.Melee.ForgottenPearlSpear>()
                    && item.type != ModContent.ItemType<Items.Weapons.Melee.HiRyuuSpear>() && item.type != ModContent.ItemType<Items.Weapons.Melee.AncientDragonLance>()
                    && item.type != ModContent.ItemType<Items.Weapons.Melee.AncientBloodLance>() && item.type != ModContent.ItemType<Items.Weapons.Melee.AncientHolyLance>()
                    && item.type != ModContent.ItemType<Items.Weapons.Melee.CelestialLance>() && item.type != ModContent.ItemType<Items.Weapons.Melee.DragoonLance>()
                     && item.type != ModContent.ItemType<Items.Weapons.Melee.SupremeDragoonLance>()
                    && item.type != ModContent.ItemType<Items.Weapons.Melee.ForgottenImpHalberd>() && item.type != ModContent.ItemType<Items.Weapons.Melee.OldHalberd>()
                    && item.type != ModContent.ItemType<Items.Weapons.Melee.OrcishHalberd>() && item.type != ModContent.ItemType<Items.Weapons.Melee.ReforgedOldHalberd>()
                    && item.type != ModContent.ItemType<Items.Weapons.Melee.ForgottenPolearm>()))
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= 0.6f; // Drain .6 stamina/tick
                }

                // Ranged
                if (item.damage >= 1 && item.DamageType == DamageClass.Ranged && Player.itemAnimation == Player.itemAnimationMax - 1 && !(item.type == ItemID.PiranhaGun))
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= ReduceStamina(item.useAnimation);

                }

                // Magic & Throwing
                if (item.damage >= 1 && (item.CountsAsClass(DamageClass.Magic) || item.CountsAsClass(DamageClass.Throwing)) && Player.itemAnimation == Player.itemAnimationMax - 1)
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= ReduceStamina(item.useAnimation);
                }

                // Summoner
                if (item.damage >= 1 && item.CountsAsClass(DamageClass.Summon) && Player.itemAnimation == Player.itemAnimationMax - 1)
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= ReduceStamina(item.useAnimation);
                }

                // Classless? Just in case? 
                if (item.damage >= 1 && (!item.CountsAsClass(DamageClass.Magic) && !item.CountsAsClass(DamageClass.Ranged) && !item.CountsAsClass(DamageClass.Magic) && !item.CountsAsClass(DamageClass.Summon) && !item.CountsAsClass(DamageClass.Throwing)) && Player.itemAnimation == Player.itemAnimationMax - 1)
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= ReduceStamina(item.useAnimation);
                }

                if (item.type == ItemID.Harpoon && Player.itemAnimation == 1)
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= 14f;
                }

                if (Player.itemAnimation != 0 && (item.type == ItemID.PiranhaGun || item.type == ModContent.ItemType<Items.Weapons.Magic.DivineSpark>() || item.type == ModContent.ItemType<Items.Weapons.Magic.DivineBoomCannon>() || item.type == ItemID.LastPrism || item.type == ItemID.DD2PhoenixBow || item.type == ItemID.Phantasm))
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= .8f;
                }

                if (Player.itemAnimation != 0 && (item.type == ItemID.LaserMachinegun))
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= .6f;
                }

                if (Player.itemAnimation != 0 && (item.type == ModContent.ItemType<Items.Weapons.Ranged.ArtemisBow>() || item.type == ModContent.ItemType<Items.Weapons.Ranged.SagittariusBow>() || item.type == ModContent.ItemType<Items.Weapons.Ranged.CernosPrime>()))
                {
                    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= .5f;
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
        internal float ReduceStamina(int itemUseAnimation)
        {
            // y=\left(\log_{1.025}\left(x+41\right)\right)-150.392
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
                && Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 30 && !Player.GetModPlayer<tsorcRevampEstusPlayer>().isDrinking
                && !Player.HasBuff(BuffID.Frozen) && !Player.HasBuff(ModContent.BuffType<Buffs.Hold>()) && !Player.HasBuff(BuffID.Stoned))
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

                //Don't allow dodging on mounts and during item use.
                if ((Player.mount != null && Player.mount.Active) || Player.itemAnimation > 0)
                {
                    return false;
                }
            }

            wantsDodgerollTimer = 0f;
            Player.grappling[0] = -1;
            Player.grapCount = 0;
            for (int p = 0; p < 1000; p++)
            {
                if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && Main.projectile[p].aiStyle == 7)
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
            //Apply velocity
            if (dodgeTime < DodgeTimeMax * 0.5f)
            {

                float newVelX = (onGround ? 12f : 8f) * dodgeDirection;

                if (Math.Abs(Player.velocity.X) < Math.Abs(newVelX) || Math.Sign(newVelX) != Math.Sign(Player.velocity.X))
                {
                    Player.velocity.X = newVelX;
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
                Player.velocity.X *= 0.85f;
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

        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot) {
            return !isDodging;
        }

        public override bool CanBeHitByProjectile(Projectile proj) {
            return !isDodging;
        }
    }
}
