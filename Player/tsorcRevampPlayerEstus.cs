using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace tsorcRevamp
{

    //This class stores necessary player info for Estus usage, which is used by those playing as Bearer of the Curse, as their main source of HP recovery

    public class tsorcRevampEstusPlayer : ModPlayer
    {

        public static tsorcRevampEstusPlayer ModPlayer(Player player)
        {
            return player.GetModPlayer<tsorcRevampEstusPlayer>();
        }


        public int estusChargesCurrent; //Current amount of charges left
        public const int DefaultEstusChargesMax = 3; //How many charges the player starts with
        public int estusChargesMax; //The max amount of charges the player has
                                    //public int estusChargesMax2; //The temporary amount of charges left
        public const int DefaultEstusHealthGain = 60; //How much 1 charge heals to begin with
        public int estusHealthGain; //The amount of health restored per charge


        public bool isDrinking; //Whether or not the player is currently drinking estus
        public bool isEstusHealing; //Whether or not the player is currently healing after drinking estus

        public float estusDrinkTimerMax => 2f; //This is actually seconds. How long it takes to drink a charge
        public float estusDrinkTimer; //How far through the animation we are
        public float estusHealthPerTick; //How much health to restore per tick
        public float estusHealingTimerMax = 90; //Timer for how long drinking the estus will heal for
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

        public override void PostUpdateBuffs()
        {
            if (Player.HasBuff(ModContent.BuffType<Buffs.Bonfire>()) && !Main.npc.Any(n => n?.active == true && n.boss && n != Main.npc[200])
                && estusChargesCurrent != estusChargesMax && Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) //When the player visits a bonfire, restore charges
            {
                estusChargesCurrent = estusChargesMax;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f }, Player.position);

                for (int i = 0; i <= 15; i++)
                {
                    int z = Dust.NewDust(Player.position, Player.width, Player.height, 270, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 2.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(80, 95) * 0.043f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 25f;
                    Main.dust[z].position = Player.Center - vectorother;
                }
            }
        }
        public override void PostUpdateMiscEffects()
        {
            UpdateResource();
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
            //estusHealthPerTick += estusHealthGain / estusHealingTimerMax; //Heal this much each tick
            //Attempt to drink if the player isn't already
            if (!isDrinking /*&& !TryDrinkEstus()*/)
            {
                return;
            }

            //Progress the action
            estusDrinkTimer += 1f / 60f;

            //Force player body frame to be Use3, this includes the players arm (drinking position)
            if (estusDrinkTimer >= estusDrinkTimerMax * 0.4f)
            {
                Player.GetModPlayer<tsorcRevampPlayer>().forcedBodyFrame = PlayerFrames.Use2;
            }

            //Slow player for whole duration of action
            Player.velocity.X *= 0.9f;
            Player.eocHit = 0;

            if (estusDrinkTimer >= estusDrinkTimerMax) //Once finished drinking:
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f }, Player.position);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item3, Player.position);

                for (int i = 0; i <= 15; i++)
                {
                    int z = Dust.NewDust(Player.position, Player.width, Player.height, 270, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 2.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(80, 95) * 0.043f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 25f;
                    Main.dust[z].position = Player.Center - vectorother;
                }

                isDrinking = false; //No longer drinking
                estusChargesCurrent--; //Remove a charge
                estusDrinkTimer = 0; //Set the timer back to 0
                Player.HealEffect(estusHealthGain); //Show green heal text equal to health gain
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

                    estusHealthPerTick += estusHealthGain / estusHealingTimerMax; //Heal this much each tick

                    if (estusHealthPerTick > (int)estusHealthPerTick)
                    {
                        Player.statLife += (int)estusHealthPerTick;
                        estusHealthPerTick -= (int)estusHealthPerTick;
                    }


                    int z = Dust.NewDust(Player.position, Player.width, Player.height, 270, 0f, 0f, 120, default(Color), 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 2.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(80, 95) * 0.043f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 25f;
                    Main.dust[z].position = Player.Center - vectorother;

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
