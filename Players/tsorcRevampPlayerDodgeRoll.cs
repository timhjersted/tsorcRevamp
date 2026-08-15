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
using tsorcRevamp.Systems;
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
    /// <summary>
    /// The Souls-mode bonus accessory slot. Available to both Unkindled and Bearer of the Curse.
    ///
    /// The class name is now a misnomer, but renaming it would change the slot's identity — accessory
    /// slot contents are saved keyed by slot name, so a rename would silently empty this slot for every
    /// existing Bearer of the Curse save. Not worth it for a cosmetic fix.
    /// </summary>
    public class BearerOfTheCurseAccessorySlot : ModAccessorySlot
    {
        public override bool IsEnabled()
        {
            if (Main.gameMenu) return false;
            return Player.GetModPlayer<tsorcRevampPlayer>().SoulsMode;
        }
    }
    public partial class tsorcRevampPlayer : ModPlayer
    {
        /// <summary>Seconds from the start of a roll within which an incoming attack counts as a perfect
        /// dodge. 12 frames, matching the shield's PerfectParryWindow.</summary>
        public const float PerfectDodgeWindow = 12f / 60f;
        /// <summary>Fraction of the roll's stamina cost refunded on a perfect dodge (matches PerfectParryCostMult).</summary>
        public const float PerfectDodgeRefundMult = 0.5f;

        private float lastRollStaminaCost;
        private bool perfectDodgeTriggered;

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
            // Swallowed by the Vessel of Souls: hide the whole player.
            if (SwallowHidden)
            {
                foreach (Terraria.ModLoader.PlayerDrawLayer layer in Terraria.ModLoader.PlayerDrawLayerLoader.Layers)
                    layer.Hide();
                return;
            }

            // This has to run before the dodge-roll early return below. Otherwise the roll hides
            // the held item and exits before Capture the Gem's large-gem icon can be suppressed.
            PlayerDrawLayers.CaptureTheGem.Hide();

            if ((isDodging || dodgeCooldown.Value != 0 || blockVisuals > 0) && !Player.GetModPlayer<tsorcRevampPlayer>().CanUseItemsWhileDodging)
            {
                PlayerDrawLayers.HeldItem.Hide();
                return;
            }

            // Suppress the balloon sprite for all vanilla balloon accessories.
            // Jump height is already baked in by UpdateEquips before draw runs.
            PlayerDrawLayers.BalloonAcc.Hide();

        }

        int oldItemAnimation = 0;
        bool wasJustRolling = false;
        int blockVisuals; //Block the remaining itemAnimation visuals after a roll, to prevent visual jank
        public override bool PreItemCheck()
        {
            if (Player.GetModPlayer<tsorcRevampPlayer>().ImpaleFreezeTimer > 0)
                return false;

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

            // Apply the selected class multiplier and, when enabled, the output-based weapon cost experiment.
            if (Player.GetModPlayer<tsorcRevampPlayer>().UsesWeaponStamina)
            {

                tsorcRevampStaminaPlayer modPlayer = Player.GetModPlayer<tsorcRevampStaminaPlayer>();
                var arcanePlayer = Player.GetModPlayer<ArcaneSorceryPlayer>();
                
                
                float mult = Player.GetModPlayer<tsorcRevampPlayer>().WeaponStaminaMult;
                int scaledUseAnimation = (int)(item.useAnimation / Player.GetAttackSpeed(item.DamageType));

                bool startedAnimation = (Player.itemAnimation > oldItemAnimation && Player.itemAnimationMax > 0);
                oldItemAnimation = Player.itemAnimation;

                if (Player.HeldItem.DamageType == DamageClass.Magic && arcanePlayer.Enabled &&
                    modPlayer.staminaResourceCurrent <= modPlayer.staminaResourceMax2 * arcanePlayer.ManaBurnStaminaThreshold / 100f)
                {
                    return true;
                }

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

                if (tsorcRevampStaminaPlayer.UsesOutputBasedWeaponCost(item))
                {
                    float staminaCost = modPlayer.BeginOutputBasedWeaponUse(item, scaledUseAnimation);
                    modPlayer.staminaResourceCurrent -= Math.Min(staminaCost, modPlayer.staminaResourceMax2);
                    return true;
                }

                if (item.type == ItemID.CoinGun) //coin gun has a damage stat of zero but can still do damage!
                {
                    modPlayer.staminaResourceCurrent -= ReduceStamina(scaledUseAnimation) * mult;
                }

                // Weapon-classified tools (tsorcRevamp.WeaponClassifiedTools — most of the mod's axes and
                // hammers, which deal real combat damage despite also having chop/dig power) are excluded
                // here so they fall through to the plain legacy weapon cost below instead, same as any
                // sword. Only genuine utility tools (Diamond Pickaxe, Heaven Piercer, vanilla picks/hamaxes)
                // still hit this branch.
                else if ((item.pick != 0 || item.axe != 0 || item.hammer != 0)
                    && (tsorcRevamp.WeaponClassifiedTools == null || !tsorcRevamp.WeaponClassifiedTools.Contains(item.type)))
                {
                    // Paid on every swing now (hit, miss, tile, or air) instead of only on a landed NPC
                    // hit — see ToolSwingStaminaMult for why this is its own knob and not WeaponStaminaMult
                    // alone. The old on-hit-only charge (tsorcGlobalItem.OnHitNPC) let whiffing skip the
                    // cost entirely and made a single connecting swing pay for every miss that led up to it.
                    //
                    // MUST return here: falling through hits the second if/else-if chain below (the
                    // "useAnimation * 0.8 > max" block), which is not pick/axe/hammer-excluded and would
                    // charge the full, un-scaled ReduceStamina cost a second time on top of this one.
                    modPlayer.staminaResourceCurrent -= ReduceStamina(scaledUseAnimation) * mult * tsorcRevampStaminaPlayer.ToolSwingStaminaMult;
                    return true;
                }

                else if (item.damage <= 1 || item.type == ModContent.ItemType<Items.Weapons.Ranged.Specialist.GlaiveBeam>() || item.type == ModContent.ItemType<Items.Weapons.Magic.ArcaneLightrifle>() || item.DamageType == DamageClass.Summon)
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

        /// <summary>
        /// Perfect dodge: rolling into an attack that was about to connect refunds half the roll's stamina and
        /// grants a short stamina-regeneration and critical-strike buff.
        ///
        /// Detected by hitbox overlap rather than by hooking the hit, because a successful dodge produces
        /// no hit to hook — the roll sets <c>Player.immune</c>, and damage sources check that themselves
        /// and skip out before <c>Player.Hurt</c> (and therefore any ModPlayer dodge hook) is ever reached.
        /// From the game's perspective nothing happened, so "what would have hit me" has to be inferred.
        ///
        /// That makes this approximate where the parry is exact. It can over-trigger on a projectile that
        /// overlapped but would have expired or been on its own immunity cooldown, and it misses attacks
        /// that deal damage through custom checks instead of hitbox overlap.
        /// </summary>
        private void CheckPerfectDodge()
        {
            if (perfectDodgeTriggered
                || !Player.GetModPlayer<tsorcRevampPlayer>().SoulsMode
                || Player.whoAmI != Main.myPlayer
                || dodgeTime > PerfectDodgeWindow)
            {
                return;
            }

            Rectangle playerHitbox = Player.Hitbox;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                // timeLeft > 1 skips projectiles dying this frame, which would not have landed anyway.
                if (projectile != null && projectile.active && projectile.hostile && projectile.damage > 0
                    && projectile.timeLeft > 1 && projectile.Hitbox.Intersects(playerHitbox))
                {
                    AwardPerfectDodge();
                    return;
                }
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc != null && npc.active && !npc.friendly && !npc.dontTakeDamage
                    && npc.damage > 0 && npc.Hitbox.Intersects(playerHitbox))
                {
                    AwardPerfectDodge();
                    return;
                }
            }
        }

        private void AwardPerfectDodge()
        {
            perfectDodgeTriggered = true;

            tsorcRevampStaminaPlayer stamina = Player.GetModPlayer<tsorcRevampStaminaPlayer>();
            stamina.staminaResourceCurrent = Math.Min(
                stamina.staminaResourceCurrent + lastRollStaminaCost * PerfectDodgeRefundMult,
                stamina.staminaResourceMax2);
            Player.AddBuff(ModContent.BuffType<Buffs.PerfectDodge>(), Buffs.PerfectDodge.DurationTicks);

            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.35f, PitchVariance = 0.2f }, Player.position);

            Vector2 origin = Player.Center;
            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * Main.rand.NextFloat(2.2f, 4.4f);
                Dust dust = Dust.NewDustPerfect(origin, DustID.SilverFlame, velocity, 90, Color.White, 1.15f);
                dust.noGravity = true;
            }
            for (int i = 0; i < 5; i++)
            {
                Dust twinkle = Dust.NewDustPerfect(origin + Main.rand.NextVector2Circular(12f, 18f),
                    DustID.TreasureSparkle, Main.rand.NextVector2Circular(0.8f, 0.8f), 70, Color.White, 0.95f);
                twinkle.noGravity = true;
            }
            Lighting.AddLight(origin, new Vector3(0.45f, 0.55f, 0.75f));
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
                // > 0, not > 30: the roll now follows the same rule as every other action — you may act with any
                // stamina at all, and overdrawing banks the shortfall as debt you must repay before acting again.
                // The old hard gate was the odd one out, and it made the roll silently unresponsive rather than
                // letting the player make the risky call themselves.
                && Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 0 && !Player.GetModPlayer<tsorcRevampEstusPlayer>().isDrinking && !Player.GetModPlayer<CeruleanFlaskPlayer>().IsDrinking
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
            float rollStaminaCost = 30 * Player.GetModPlayer<tsorcRevampPlayer>().TiredStaminaMult * (1f - Player.GetModPlayer<tsorcRevampPlayer>().ArtoriasAbysswalkerDodgeStaminaCostReduction / 100f);
            Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= rollStaminaCost;

            // Remembered so a perfect dodge can refund half of what this specific roll actually cost —
            // the cost is charged up front, before anything is known about the roll's timing, so the
            // discount has to arrive later as a refund.
            lastRollStaminaCost = rollStaminaCost;
            perfectDodgeTriggered = false;
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
            // Chloranthy Rings no longer modify deaccelerationRate � their speed boost during the roll
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

            if (Player.CCed)
            {
                noDodge = true;
            }

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

            TryGrantArtoriasAbysswalkerPoise();
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

                // Chloranthy Ring dodge-speed boost � intended to grant only "a little bit more dodge length"
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
                // again here when assigning velocity), producing ~1.96� the intended velocity on the
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

            CheckPerfectDodge();

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
