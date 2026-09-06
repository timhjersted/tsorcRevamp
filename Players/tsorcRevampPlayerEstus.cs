using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Accessories.Defensive.Rings;

namespace tsorcRevamp
{

    //This class stores necessary player info for Estus usage, which is used by those playing as Bearer of the Curse, as their main source of HP recovery

    public class tsorcRevampEstusPlayer : ModPlayer
    {

        public static tsorcRevampEstusPlayer ModPlayer(Player player)
        {
            return player.GetModPlayer<tsorcRevampEstusPlayer>();
        }


        public int EstusChargesCurrent = 3; //Current amount of charges left
        public const int DefaultEstusChargesMax = 3; //How many charges the player starts with
        public int EstusChargesMax; //The max amount of charges the player has
                                    //public int estusChargesMax2; //The temporary amount of charges left
        public const int DefaultEstusHealthGain = 60; //How much 1 charge heals to begin with
        public int EstusHealthGain; //The amount of health restored per charge
        public int EstusHealthGainBonus; //A bonus to the health restored
        public int EstusHealthGainMaxHealthBonus; //A bonus to the health restored


        public bool IsDrinking; //Whether or not the player is currently drinking estus
        public bool IsEstusHealing; //Whether or not the player is currently healing after drinking estus
        public bool EstusRing; //Whether or not the player is currently wearing an estus ring

        public const float EstusDrinkTimerMaxBase = 2f;
        public const float EstusPStoneDrinkTimeReduction = 25f;
        public const float EstusDrinkTimeReductionPStone = EstusDrinkTimerMaxBase * (EstusPStoneDrinkTimeReduction / 100f);
        public const float EstusDrinkTimeReductionRing = EstusDrinkTimerMaxBase * (Items.Accessories.Defensive.Rings.EstusRing.DrinkTimeReduction / 100f);
        public float EstusDrinkTimeReduction = 0;
        public float EstusDrinkTimerMax = EstusDrinkTimerMaxBase; //This is actually seconds. How long it takes to drink a charge
        public const float UnkindledEstusDrinkTimerReduction = 25f; //final drink time *= 1f - this / 100f in Unkindled
        public float EstusDrinkTimer; //How far through the animation we are
        public float EstusHealthPerTick; //How much health to restore per tick
        public const float DefaultEstusHealingTimerMax = 90; //Ticks the heal is spread over
        public const float UnkindledEstusHealingTimerMax = 60; //Unkindled resolves its heal faster
        public float EstusHealingTimerMax = DefaultEstusHealingTimerMax; //Timer for how long drinking the estus will heal for
        public float EstusHealingTimer; //How far through the healing timer we are

        public override void SaveData(TagCompound tag) //Save max amount of charges, current amount of charges and also health gained for next time the player enters the world
        {
            tag.Add("estusChargesMax", EstusChargesMax);
            tag.Add("estusChargesCurrent", EstusChargesCurrent);
            tag.Add("estusHealthGain", EstusHealthGain);
        }

        public override void LoadData(TagCompound tag) //Load saved data
        {
            EstusChargesMax = tag.GetInt("estusChargesMax");
            EstusChargesCurrent = tag.GetInt("estusChargesCurrent");
            EstusHealthGain = tag.GetInt("estusHealthGain");
        }

        public override void Initialize() //On loading up the player, set max charges to default, this is then overriden by the saved quantity from Save() and Load()
        {
            EstusChargesMax = DefaultEstusChargesMax;
            EstusHealthGain = DefaultEstusHealthGain;
            //estusChargesCurrent = estusChargesMax;
        }

        public override void OnRespawn() //When a player respawns, restore charges
        {
            EstusChargesCurrent = EstusChargesMax;
        }

        public override void ResetEffects()
        {
            EstusRing = false;
        }

        public override void PostUpdateBuffs()
        {
            if (Player.HasBuff(ModContent.BuffType<Buffs.Bonfire>()) && !Main.npc.Any(n => n?.active == true && n.boss && n != Main.npc[200])
                && EstusChargesCurrent != EstusChargesMax && Player.GetModPlayer<tsorcRevampPlayer>().SoulsMode) //When the player visits a bonfire, restore charges
            {
                EstusChargesCurrent = EstusChargesMax;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f }, Player.position);

                // Bonfire-refill green dust burst removed — it was firing the frame after a drink
                // (charges drop → this block immediately refills + spawned dust at feet), which read
                // as "the Estus drink produced dust". Sound effect alone is enough feedback.
            }
        }
        public override void PostUpdateMiscEffects()
        {
            var tsorcPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
            EstusHealthGainBonus = 0;
            EstusHealthGainMaxHealthBonus = 0;

            // Unkindled's heal lands over 60 ticks instead of 90. The total restored is unchanged —
            // per-tick healing is (gain + bonus) / estusHealingTimerMax, so the window only controls
            // how long you stay committed and vulnerable, which is the point.
            EstusHealingTimerMax = tsorcPlayer.Unkindled
                ? UnkindledEstusHealingTimerMax
                : DefaultEstusHealingTimerMax;
            if (Player.pStone)
            {
                EstusDrinkTimeReduction += EstusDrinkTimeReductionPStone;
            }
            if (EstusRing)
            {
                EstusHealthGainMaxHealthBonus += Items.Accessories.Defensive.Rings.EstusRing.PercentHealIncrease;
                EstusHealthGainBonus += Items.Accessories.Defensive.Rings.EstusRing.HealIncrease;
                EstusDrinkTimeReduction += EstusDrinkTimeReductionRing;
            }
            
            EstusDrinkTimerMax = EstusDrinkTimerMaxBase - EstusDrinkTimeReduction;
            EstusDrinkTimeReduction = 0; //resetting it back to default state after it was used to calc so it doesn't add up infinitely

            if (tsorcPlayer.Unkindled)
            {
                EstusDrinkTimerMax *= 1f - UnkindledEstusDrinkTimerReduction / 100f; //multiplier to final so it respects pStone and ring properly, better than messing with base again
            }
            
            EstusHealthGainBonus += (int)(EstusHealthGainMaxHealthBonus * Player.statLifeMax2 / 100f);
        }

        private void UpdateResource()
        {
            /*Main.NewText("estusChargesCurrent: " + estusChargesCurrent);
			Main.NewText("estusChargesMax: " + estusChargesMax);
			Main.NewText("estusHealthGain: " + estusHealthGain);*/


            // Limit estusChargesCurrent from going over the limit imposed by estusChargesMax
            //estusChargesCurrent = Utils.Clamp(estusChargesCurrent, 0, estusChargesMax);

        }

        public override bool PreItemCheck()
        {
            UpdateDrinkingEstus();

            if (IsDrinking && (Player.HeldItem.type == ItemID.Umbrella || Player.HeldItem.type == ItemID.BreathingReed))
            {
                return false;
            }

            return base.PreItemCheck();
        }

        /*public bool TryDrinkEstus()
		{
			bool isLocal = player.whoAmI == Main.myPlayer;

			if (isLocal && tsorcRevamp.DrinkEstusKey.JustPressed && !player.mouseInterface && estusChargesCurrent > 0 && player.itemAnimation == 0 
				&& player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.GetModPlayer<tsorcRevampPlayer>().ReceivedGift 
				&& !player.GetModPlayer<tsorcRevampPlayer>().isDodging && player.statLife != player.statLifeMax2)
			{
				isDrinking = true;
				estusDrinkTimer = 0;
				return true;
			}
			return false;
		}*/

        public void UpdateDrinkingEstus()
        {
            tsorcRevampPlayer modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
            //estusHealthPerTick += estusHealthGain / estusHealingTimerMax; //Heal this much each tick
            //Attempt to drink if the player isn't already
            if (!IsDrinking /*&& !TryDrinkEstus()*/)
            {
                return;
            }

            //Slow player for whole duration of action
            Player.velocity.X *= 0.9f;
            Player.eocHit = 0;
            
            // Chloranthy Ring (I or II): trade the standard drink slowdown for temporary
            // vulnerability. Without the ring, the Crippled debuff blocks extra jumps, wings,
            // rocket boots, and reduces moveSpeed by 10% for the drink duration (ground-bound
            // and slowed). With the ring, those mobility losses are swapped for Ichor
            // (-15 defense + glow) — full mobility but more damage taken if you get hit.
            // Completely removing the challenge behind finding moments to drink Estus in a single accessory that also grants other very powerful effects? NOPE
            /*if ((modPlayer.ChloranthyRing1 || modPlayer.ChloranthyRing2) && EstusDrinkTimer == 0)
            {
                Player.AddBuff(BuffID.Ichor, (int)(EstusDrinkTimerMax * 60f));
            }
            else */
            if (EstusDrinkTimer == 0)
            {
                Player.AddBuff(ModContent.BuffType<Crippled>(), (int)(EstusDrinkTimerMax * 60f));
                Player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), (int)(EstusDrinkTimerMax * 60f));
            }

            //Progress the action
            EstusDrinkTimer += 1f / 60f;

            //Force player body frame to be Use3, this includes the players arm (drinking position)
            // Threshold dropped from 0.4 → 0.05 so the drinking pose appears almost immediately on key
            // press instead of after an anticipation gap that read as input lag.
            if (EstusDrinkTimer >= EstusDrinkTimerMax * 0.05f)
            {
                Player.GetModPlayer<tsorcRevampPlayer>().forcedBodyFrame = PlayerFrames.Use2;
            }

            if (EstusDrinkTimer >= EstusDrinkTimerMax) //Once finished drinking:
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f }, Player.position);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item3, Player.position);

                // On-finish green dust burst removed — drink completion is already signalled
                // by the two sound effects and the HealEffect text below.

                IsDrinking = false; //No longer drinking
                EstusChargesCurrent--; //Remove a charge
                EstusDrinkTimer = 0; //Set the timer back to 0
                Player.HealEffect(EstusHealthGain + EstusHealthGainBonus);
                Player.GetModPlayer<tsorcRevampPlayer>().ActivateSporePowderEffect();
                Player.GetModPlayer<tsorcRevampPlayer>().ActivateVenomPowderEffect();
                IsEstusHealing = true; //Commence healing process
                                       //kplayer.eocDash = 0;
            }
        }

        public override void PostUpdate()
        {
            if (IsEstusHealing) //Is the player healing from estus?
            {
                EstusHealingTimer++; //Advance the timer

                //Main.NewText(estusHealthPerTick);

                if (EstusHealingTimer <= EstusHealingTimerMax && Player.statLife < Player.statLifeMax2) //If the timer is less or equal to timer max and player hp is not at max
                {
                    EstusHealthPerTick += (EstusHealthGain + EstusHealthGainBonus) / EstusHealingTimerMax;

                    if (EstusHealthPerTick >= (int)EstusHealthPerTick)
                    {
                        Player.statLife += (int)EstusHealthPerTick;
                        EstusHealthPerTick -= (int)EstusHealthPerTick;
                    }

                    // Per-tick green dust at the player's feet was removed — it ran every frame
                    // during the multi-second healing window, producing a constant green cloud.
                }

                if (EstusHealingTimer >= EstusHealingTimerMax) //Once healing process is over
                {
                    EstusHealthPerTick = 0;
                    EstusHealingTimer = 0; //Set timer back to 0
                    IsEstusHealing = false; //No longer drinking
                }
            }
        }










    }
}
