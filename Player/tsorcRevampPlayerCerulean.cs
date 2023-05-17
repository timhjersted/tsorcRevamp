using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace tsorcRevamp
{

    //This class stores necessary player info for Cerulean usage, which is used by those playing as Bearer of the Curse, as their main source of MP recovery

    public class tsorcRevampCeruleanPlayer : ModPlayer
    {

        public static tsorcRevampCeruleanPlayer ModPlayer(Player player)
        {
            return player.GetModPlayer<tsorcRevampCeruleanPlayer>();
        }


        public int ceruleanChargesCurrent; //Current amount of charges left
        public const int DefaultCeruleanChargesMax = 6; //How many charges the player starts with
        public int ceruleanChargesMax; //The max amount of charges the player has
        public const int DefaultCeruleanManaGain = 120; //How much 1 charge heals to begin with
        public int ceruleanManaGain; //The amount of health restored per charge
        public float ceruleanManaGainBonus; //A bonus to the health restored


        public bool isDrinking; //Whether or not the player is currently drinking estus
        public bool isCeruleanRestoring; //Whether or not the player is currently healing after drinking estus

        public float ceruleanDrinkTimerMax => 1.5f; //This is actually seconds. How long it takes to drink a charge
        public float ceruleanDrinkTimer; //How far through the animation we are
        public float ceruleanManaPerTick; //How much mana to restore per tick
        public float ceruleanRestorationTimerMax; //Timer for how long drinking the estus will restore for
        public float ceruleanRestorationTimerBonus;
        public float ceruleanRestorationTimer; //How far through the healing timer we are

        public override void SaveData(TagCompound tag) //Save current amount of charges and restore amount
        {
            tag.Add("ceruleanChargesMax", ceruleanChargesMax);
            tag.Add("ceruleanChargesCurrent", ceruleanChargesCurrent);
            tag.Add("ceruleanManaGain", ceruleanManaGain);
        }

        public override void LoadData(TagCompound tag) //Load saved data
        {
            ceruleanChargesMax = tag.GetInt("ceruleanChargesMax");
            ceruleanChargesCurrent = tag.GetInt("ceruleanChargesCurrent");
            ceruleanManaGain = tag.GetInt("ceruleanManaGain");
        }

        public override void Initialize() //On loading up the player, set max charges to default, this is then overriden by the saved quantity from Save() and Load()
        {
            ceruleanChargesMax = DefaultCeruleanChargesMax;
            ceruleanManaGain = DefaultCeruleanManaGain;
        }

        public override void OnRespawn() //When a player respawns, restore charges
        {
            ceruleanChargesCurrent = ceruleanChargesMax;
        }

        public override void PostUpdateBuffs()
        {
            if (Player.HasBuff(ModContent.BuffType<Buffs.Bonfire>()) && !Main.npc.Any(n => n?.active == true && n.boss && n != Main.npc[200])
                && ceruleanChargesCurrent != ceruleanChargesMax && Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) //When the player visits a bonfire, restore charges
            {
                ceruleanChargesCurrent = ceruleanChargesMax;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f }, Player.position);
            }
        }
        public override void PostUpdateMiscEffects()
        {
            ceruleanManaGainBonus = 1f + ((float)Player.manaRegenBonus / 200f); 
            ceruleanRestorationTimerBonus = 1f + (Player.manaRegenDelayBonus / 4f); 
            if (Player.manaRegenBuff) //so mana regen pot does something
            {
                ceruleanRestorationTimerBonus = 1.2f + (Player.manaRegenDelayBonus / 3f); 
            }
            ceruleanRestorationTimerMax = 600 * ceruleanRestorationTimerBonus;
        }

        public override bool PreItemCheck()
        {
            UpdateDrinkingCerulean();

            if (isDrinking && (Player.HeldItem.type == ItemID.Umbrella || Player.HeldItem.type == ItemID.BreathingReed))
            {
                return false;
            }

            return base.PreItemCheck();
        }
        public void UpdateDrinkingCerulean()
        {
            //Attempt to drink if the player isn't already
            if (!isDrinking /*&& !TryDrinkEstus()*/)
            {
                return;
            }

            //Progress the action
            ceruleanDrinkTimer += 1f / 60f;

            //Force player body frame to be Use3, this includes the players arm (drinking position)
            if (ceruleanDrinkTimer >= ceruleanDrinkTimerMax * 0.4f)
            {
                Player.GetModPlayer<tsorcRevampPlayer>().forcedBodyFrame = PlayerFrames.Use2;
            }

            //Slow player for whole duration of action
            Player.velocity.X *= 0.9f;
            Player.velocity.Y *= 0.9f;
            Player.eocHit = 0;

            if (ceruleanDrinkTimer >= ceruleanDrinkTimerMax) //Once finished drinking:
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.5f }, Player.position);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item3, Player.position);

                for (int i = 0; i <= 15; i++)
                {
                    int z = Dust.NewDust(Player.position, Player.width, Player.height, DustID.WaterCandle, 0f, 0f, 120, default(Color), 1f);
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
                ceruleanChargesCurrent--; //Remove a charge
                ceruleanDrinkTimer = 0; //Set the timer back to 0
                Player.ManaEffect((int)(ceruleanManaGain * ceruleanManaGainBonus * ceruleanRestorationTimerBonus)); //Show blue restoration text equal to mana gain
                isCeruleanRestoring = true; //Commence restoration process
            }
        }

        public override void PostUpdate()
        {
            if (isCeruleanRestoring) //Is the player's mana restoring from cerulean?
            {
                ceruleanRestorationTimer++; //Advance the timer

                if (ceruleanRestorationTimer <= ceruleanRestorationTimerMax && Player.statMana < Player.statManaMax2) //If the timer is less or equal to timer max and player mp is not at max
                {

                    ceruleanManaPerTick += (ceruleanManaGain * ceruleanManaGainBonus * ceruleanRestorationTimerBonus) / ceruleanRestorationTimerMax; //Heal this much each tick

                    if (ceruleanManaPerTick > (int)ceruleanManaPerTick)
                    {
                        Player.statMana += (int)ceruleanManaPerTick;
                        ceruleanManaPerTick -= (int)ceruleanManaPerTick;
                    }


                    int z = Dust.NewDust(Player.position, Player.width, Player.height, DustID.WaterCandle, 0f, 0f, 120, default(Color), 1f);
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

                if (ceruleanRestorationTimer >= ceruleanRestorationTimerMax) //Once restoration process is over
                {
                    ceruleanManaPerTick = 0;
                    ceruleanRestorationTimer = 0; //Set timer back to 0
                    isCeruleanRestoring = false; //No longer drinking
                }
            }
        }










    }
}
