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


        public int estusChargesCurrent = 3; //Current amount of charges left
        public const int DefaultEstusChargesMax = 3; //How many charges the player starts with
        public int estusChargesMax; //The max amount of charges the player has
                                    //public int estusChargesMax2; //The temporary amount of charges left
        public const int DefaultEstusHealthGain = 60; //How much 1 charge heals to begin with
        public int estusHealthGain; //The amount of health restored per charge
        public int estusHealthGainBonus; //A bonus to the health restored
        public int estusHealthGainMaxHealthBonus; //A bonus to the health restored


        public bool isDrinking; //Whether or not the player is currently drinking estus
        public bool isEstusHealing; //Whether or not the player is currently healing after drinking estus
        public bool estusRing; //Whether or not the player is currently wearing an estus ring

        public const float estusDrinkTimerMaxBase = 1.05f;
        public const float estusPStoneStrength = 25f;
        public float estusDrinkTimerReductionPStone = estusDrinkTimerMaxBase * (estusPStoneStrength / 100f);
        public float estusDrinkTimerMax = estusDrinkTimerMaxBase; //This is actually seconds. How long it takes to drink a charge
        public float estusDrinkTimer; //How far through the animation we are
        public float estusHealthPerTick; //How much health to restore per tick
        public const float DefaultEstusHealingTimerMax = 90; //Ticks the heal is spread over
        public const float UnkindledEstusHealingTimerMax = 60; //Unkindled resolves its heal faster
        public float estusHealingTimerMax = DefaultEstusHealingTimerMax; //Timer for how long drinking the estus will heal for
        public float estusHealingTimer; //How far through the healing timer we are

        public override void SaveData(TagCompound tag) //Save max amount of charges, current amount of charges and also health gained for next time the player enters the world
        {
            tag.Add("estusChargesMax", estusChargesMax);
            tag.Add("estusChargesCurrent", estusChargesCurrent);
            tag.Add("estusHealthGain", estusHealthGain);
        }

        public override void LoadData(TagCompound tag) //Load saved data
        {
            estusChargesMax = tag.GetInt("estusChargesMax");
            estusChargesCurrent = tag.GetInt("estusChargesCurrent");
            estusHealthGain = tag.GetInt("estusHealthGain");
        }

        public override void Initialize() //On loading up the player, set max charges to default, this is then overriden by the saved quantity from Save() and Load()
        {
            estusChargesMax = DefaultEstusChargesMax;
            estusHealthGain = DefaultEstusHealthGain;
            //estusChargesCurrent = estusChargesMax;
        }

        public override void OnRespawn() //When a player respawns, restore charges
        {
            estusChargesCurrent = estusChargesMax;
        }

        public override void ResetEffects()
        {
            estusRing = false;
        }

        public override void PostUpdateBuffs()
        {
            if (Player.HasBuff(ModContent.BuffType<Buffs.Bonfire>()) && !Main.npc.Any(n => n?.active == true && n.boss && n != Main.npc[200])
                && estusChargesCurrent != estusChargesMax && Player.GetModPlayer<tsorcRevampPlayer>().SoulsMode) //When the player visits a bonfire, restore charges
            {
                estusChargesCurrent = estusChargesMax;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f }, Player.position);

                // Bonfire-refill green dust burst removed — it was firing the frame after a drink
                // (charges drop → this block immediately refills + spawned dust at feet), which read
                // as "the Estus drink produced dust". Sound effect alone is enough feedback.
            }
        }
        public override void PostUpdateMiscEffects()
        {
            estusHealthGainBonus = 0;
            estusHealthGainMaxHealthBonus = 0;

            // Unkindled's heal lands over 60 ticks instead of 90. The total restored is unchanged —
            // per-tick healing is (gain + bonus) / estusHealingTimerMax, so the window only controls
            // how long you stay committed and vulnerable, which is the point.
            estusHealingTimerMax = Player.GetModPlayer<tsorcRevampPlayer>().Unkindled
                ? UnkindledEstusHealingTimerMax
                : DefaultEstusHealingTimerMax;
            if (Player.pStone)
            {
                estusDrinkTimerMax = estusDrinkTimerMaxBase - estusDrinkTimerReductionPStone;
            }
            else
            {
                estusDrinkTimerMax = estusDrinkTimerMaxBase;
            }
            if (estusRing)
            {
                estusHealthGainMaxHealthBonus += EstusRing.PercentHealIncrease;
                estusHealthGainBonus += EstusRing.HealIncrease;
            }
            estusHealthGainBonus += (int)(estusHealthGainMaxHealthBonus * Player.statLifeMax2 / 100f);
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

            if (isDrinking && (Player.HeldItem.type == ItemID.Umbrella || Player.HeldItem.type == ItemID.BreathingReed))
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
            if (!isDrinking /*&& !TryDrinkEstus()*/)
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
            if ((modPlayer.ChloranthyRing1 || modPlayer.ChloranthyRing2) && estusDrinkTimer == 0)
            {
                Player.AddBuff(BuffID.Ichor, (int)(estusDrinkTimerMax * 60f));
            }
            else if (estusDrinkTimer == 0)
            {
                Player.AddBuff(ModContent.BuffType<Crippled>(), (int)(estusDrinkTimerMax * 60f));
                Player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), (int)(estusDrinkTimerMax * 60f));
            }

            //Progress the action
            estusDrinkTimer += 1f / 60f;

            //Force player body frame to be Use3, this includes the players arm (drinking position)
            // Threshold dropped from 0.4 → 0.05 so the drinking pose appears almost immediately on key
            // press instead of after an anticipation gap that read as input lag.
            if (estusDrinkTimer >= estusDrinkTimerMax * 0.05f)
            {
                Player.GetModPlayer<tsorcRevampPlayer>().forcedBodyFrame = PlayerFrames.Use2;
            }

            if (estusDrinkTimer >= estusDrinkTimerMax) //Once finished drinking:
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f }, Player.position);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item3, Player.position);

                // On-finish green dust burst removed — drink completion is already signalled
                // by the two sound effects and the HealEffect text below.

                isDrinking = false; //No longer drinking
                estusChargesCurrent--; //Remove a charge
                estusDrinkTimer = 0; //Set the timer back to 0
                Player.HealEffect(estusHealthGain + estusHealthGainBonus);
                Player.GetModPlayer<tsorcRevampPlayer>().ActivateSporePowderEffect();
                Player.GetModPlayer<tsorcRevampPlayer>().ActivateVenomPowderEffect();
                isEstusHealing = true; //Commence healing process
                                       //kplayer.eocDash = 0;
            }
        }

        public override void PostUpdate()
        {
            if (isEstusHealing) //Is the player healing from estus?
            {
                estusHealingTimer++; //Advance the timer

                //Main.NewText(estusHealthPerTick);

                if (estusHealingTimer <= estusHealingTimerMax && Player.statLife < Player.statLifeMax2) //If the timer is less or equal to timer max and player hp is not at max
                {
                    estusHealthPerTick += (estusHealthGain + estusHealthGainBonus) / estusHealingTimerMax;

                    if (estusHealthPerTick >= (int)estusHealthPerTick)
                    {
                        Player.statLife += (int)estusHealthPerTick;
                        estusHealthPerTick -= (int)estusHealthPerTick;
                    }

                    // Per-tick green dust at the player's feet was removed — it ran every frame
                    // during the multi-second healing window, producing a constant green cloud.
                }

                if (estusHealingTimer >= estusHealingTimerMax) //Once healing process is over
                {
                    estusHealthPerTick = 0;
                    estusHealingTimer = 0; //Set timer back to 0
                    isEstusHealing = false; //No longer drinking
                }
            }
        }










    }
}
