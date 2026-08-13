using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Accessories.Magic;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Systems
{

    //This class stores necessary player info for Cerulean usage, which is used by those playing as Bearer of the Curse, as their main source of MP recovery

    public class CeruleanFlaskPlayer : ModPlayer
    {

        public static CeruleanFlaskPlayer ModPlayer(Player player)
        {
            return player.GetModPlayer<CeruleanFlaskPlayer>();
        }
        public const float ManaRegenPotRestorationTimerBonus = 20f;

        public int CeruleanChargesCurrent = 9; //Current amount of charges left
        public const int DefaultCeruleanChargesMax = 9; //How many charges the player starts with
        public int CeruleanChargesMax; //The max amount of charges the player has
        public const int DefaultCeruleanManaGain = 120; //How much 1 charge heals to begin with
        public int CeruleanManaGain; //The amount of mana restored per charge
        public float CeruleanManaGainMaxManaBonus; //A bonus to the mana restored
        public float CeruleanManaGainManaRegenBonus; //A bonus to the mana restored
        public float BaseMaxManaGain = 25f;


        public bool IsDrinking; //Whether the player is currently drinking cerulean
        public bool IsCeruleanRestoring; //Whether the player is currently healing after drinking cerulean

        public const float CeruleanRestorationTicksBase = 300f; //5 seconds for a charge to fully land
        public const float CeruleanRestorationTicksUnkindled = 150f; //Unkindled resolves in 2.5s
        public const float CeruleanDrinkTimerMaxBase = 1.25f; //This is actually seconds. How long it takes to drink a charge
        public const float CeruleanManaFlowerStrength = 33.4f;
        public float CeruleanDrinkTimerReductionManaFlower = CeruleanDrinkTimerMaxBase * (CeruleanManaFlowerStrength / 100f);
        public float CeruleanDrinkTimerMax = CeruleanDrinkTimerMaxBase;
        public float CeruleanDrinkTimer; //How far through the animation we are
        public float CeruleanManaPerTick; //How much mana to restore per tick
        public float CeruleanRestorationTimerMax; //Timer for how long drinking the cerulean will restore for
        public float CeruleanRestorationTimerBonus;
        public float CeruleanRestorationTimer; //How far through the healing timer we are

        public override void SaveData(TagCompound tag) //Save current amount of charges and restore amount
        {
            tag.Add("ceruleanChargesMax", CeruleanChargesMax);
            tag.Add("ceruleanChargesCurrent", CeruleanChargesCurrent);
            tag.Add("ceruleanManaGain", CeruleanManaGain);
        }

        public override void LoadData(TagCompound tag) //Load saved data
        {
            CeruleanChargesMax = tag.GetInt("ceruleanChargesMax");
            CeruleanChargesCurrent = tag.GetInt("ceruleanChargesCurrent");
            CeruleanManaGain = tag.GetInt("ceruleanManaGain");
        }

        public override void Initialize() //On loading up the player, set max charges to default, this is then overriden by the saved quantity from Save() and Load()
        {
            CeruleanChargesMax = DefaultCeruleanChargesMax;
            CeruleanManaGain = DefaultCeruleanManaGain;
        }

        public override void OnRespawn() //When a player respawns, restore charges
        {
            CeruleanChargesCurrent = CeruleanChargesMax;
        }

        public override void PostUpdateBuffs()
        {
            if (Player.HasBuff(ModContent.BuffType<Buffs.Bonfire>()) && !Main.npc.Any(n => n?.active == true && n.boss && n != Main.npc[200])
                && CeruleanChargesCurrent != CeruleanChargesMax && Player.GetModPlayer<tsorcRevampPlayer>().SoulsMode) //When the player visits a bonfire, restore charges
            {
                CeruleanChargesCurrent = CeruleanChargesMax;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f }, Player.position);
            }
        }
        public const float ManaRegenBonusDivisor = 200f; //lower is better
        public const float ManaRegenDelayBonusDivisor = 3.8f; //lower is better
        public override void PostUpdateMiscEffects()
        {
            var arcaneSorceryPlayer = Player.GetModPlayer<ArcaneSorceryPlayer>();
            if (Player.manaFlower)
            {
                CeruleanDrinkTimerMax = CeruleanDrinkTimerMaxBase - CeruleanDrinkTimerReductionManaFlower;
            }
            else
            {
                CeruleanDrinkTimerMax = CeruleanDrinkTimerMaxBase;
            }
            CeruleanManaGainMaxManaBonus = Player.statManaMax2 * (BaseMaxManaGain / 100f);
            
            CeruleanManaGainMaxManaBonus *= arcaneSorceryPlayer.CeruleanFlaskMaxManaScalingMult;
            
            CeruleanManaGainManaRegenBonus = 1f + ((float)Player.manaRegenBonus / ManaRegenBonusDivisor); //manaRegenBonus is usually in the double digits so this is good scaling 
            CeruleanRestorationTimerBonus = 1f + (Player.manaRegenDelayBonus / ManaRegenDelayBonusDivisor);  //manaRegenDelayBonus is given out at 1 or 0.5 by 2 sources in vanilla so this is also very good scaling
            
            if (Player.manaRegenBuff) //so mana regen pot does something
            {
                CeruleanRestorationTimerBonus = 1f + (ManaRegenPotRestorationTimerBonus / 100f) + (Player.manaRegenDelayBonus / (ManaRegenDelayBonusDivisor - 0.4f));
            }
            // Unkindled's charge lands in 2.5s instead of 5s. Total mana restored is unchanged — per-tick
            // restoration divides by this same value — so this only shortens how long you are committed
            // and mana-starved after drinking. Bearer of the Curse and Classic keep the full 5s.
            float restorationTicks = Player.GetModPlayer<tsorcRevampPlayer>().Unkindled
                ? CeruleanRestorationTicksUnkindled
                : CeruleanRestorationTicksBase;
            CeruleanRestorationTimerMax = restorationTicks * CeruleanRestorationTimerBonus;
            if (ModContent.GetInstance<tsorcRevampConfig>().DisableAutomaticQuickMana)
            {
                Player.manaFlower = false;
            }
        }

        public override bool PreItemCheck()
        {
            UpdateDrinkingCerulean();

            if (IsDrinking && (Player.HeldItem.type == ItemID.Umbrella || Player.HeldItem.type == ItemID.BreathingReed))
            {
                return false;
            }

            return base.PreItemCheck();
        }
        public void UpdateDrinkingCerulean()
        {
            tsorcRevampPlayer modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
            var arcaneSorceryPlayer = Player.GetModPlayer<ArcaneSorceryPlayer>();
            //Attempt to drink if the player isn't already
            if (!IsDrinking /*&& !TryDrinkEstus()*/)
            {
                return;
            }
            
            if (arcaneSorceryPlayer.Enabled)
            {
                //Slow player for whole duration of action
                Player.velocity.X *= 0.9f;
                Player.eocHit = 0;
                
                if (CeruleanDrinkTimer == 0)
                {
                    // Chloranthy Ring (I or II): trade the standard drink slowdown for temporary
                    // vulnerability. Without the ring, the Crippled debuff blocks extra jumps, wings,
                    // rocket boots, and reduces moveSpeed by 10% for the drink duration (ground-bound
                    // and slowed). With the ring, those mobility losses are swapped for Ichor
                    // (-15 defense + glow) — full mobility but more damage taken if you get hit.
                    if (modPlayer.ChloranthyRing1 || modPlayer.ChloranthyRing2)
                    {
                        Player.AddBuff(BuffID.Ichor, (int)(CeruleanDrinkTimerMax * 60f));
                    }
                    else
                    {
                        Player.AddBuff(ModContent.BuffType<Crippled>(), (int)(CeruleanDrinkTimerMax * 60f));
                        Player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), (int)(CeruleanDrinkTimerMax * 60f));
                    }
                }
            }

            //Progress the action
            CeruleanDrinkTimer += 1f / 60f;

            //Force player body frame to be Use3, this includes the players arm (drinking position).
            //Threshold dropped from 0.4 → 0.05 so the drinking pose appears almost immediately on key
            //press instead of after a ~0.5s anticipation gap that read as input lag.
            if (CeruleanDrinkTimer >= CeruleanDrinkTimerMax * 0.05f)
            {
                Player.GetModPlayer<tsorcRevampPlayer>().forcedBodyFrame = PlayerFrames.Use2;
            }

            if (CeruleanDrinkTimer >= CeruleanDrinkTimerMax) //Once finished drinking:
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.5f }, Player.position);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item3, Player.position);
                if (Player.HasBuff(BuffID.ManaSickness))
                {
                    Player.DelBuff(Player.FindBuffIndex(BuffID.ManaSickness));
                }
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent = Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2;

                // On-finish WaterCandle dust burst removed — drink completion is already signalled
                // by the two sound effects and the ManaEffect text below.

                IsDrinking = false; //No longer drinking
                CeruleanChargesCurrent--; //Remove a charge
                CeruleanDrinkTimer = 0; //Set the timer back to 0
                Player.ManaEffect((int)(((CeruleanManaGain * arcaneSorceryPlayer.CeruleanFlatManaGainMult) + CeruleanManaGainMaxManaBonus) * CeruleanManaGainManaRegenBonus * CeruleanRestorationTimerBonus)); //Show blue restoration text equal to mana gain
                IsCeruleanRestoring = true; //Commence restoration process
            }
        }

        public override void PostUpdate()
        {
            var arcaneSorceryPlayer = Player.GetModPlayer<ArcaneSorceryPlayer>();
            if (IsCeruleanRestoring) //Is the player's mana restoring from cerulean?
            {
                CeruleanRestorationTimer++; //Advance the timer

                if (CeruleanRestorationTimer <= CeruleanRestorationTimerMax && Player.statMana < Player.statManaMax2) //If the timer is less or equal to timer max and player mp is not at max
                {

                    CeruleanManaPerTick += (((CeruleanManaGain * arcaneSorceryPlayer.CeruleanFlatManaGainMult) + CeruleanManaGainMaxManaBonus) * CeruleanManaGainManaRegenBonus * CeruleanRestorationTimerBonus) / CeruleanRestorationTimerMax; //Heal this much each tick

                    if (CeruleanManaPerTick >= (int)CeruleanManaPerTick)
                    {
                        Player.statMana += (int)CeruleanManaPerTick;
                        CeruleanManaPerTick -= (int)CeruleanManaPerTick;
                    }

                    // Per-tick WaterCandle dust at the player's feet was removed — it ran every frame
                    // for the entire 5-second restoration window, producing a constant greenish cloud.
                    // The one-shot burst when the drink finishes (in UpdateDrinkingCerulean above) is enough feedback.
                }

                if (CeruleanRestorationTimer >= CeruleanRestorationTimerMax) //Once restoration process is over
                {
                    CeruleanManaPerTick = 0;
                    CeruleanRestorationTimer = 0; //Set timer back to 0
                    IsCeruleanRestoring = false; //No longer drinking
                }
            }
        }
    }

    public class CeruleanFlaskItems : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
            if (modPlayer.SoulsMode && player.whoAmI == Main.myPlayer && (item.type == ItemID.ManaFlower || item.type == ItemID.ArcaneFlower || item.type == ItemID.MagnetFlower || item.type == ItemID.ManaCloak || item.type == ModContent.ItemType<CelestialCloak>()))
            {
                int add = item.type == ModContent.ItemType<CelestialCloak>() ? 10 : 2;
                tooltips.Insert(ttindex + add, new TooltipLine(Mod, "DrinkTime", LangUtils.GetTextValue("CommonItemTooltip.CeruleanManaFlower", CeruleanFlaskPlayer.CeruleanManaFlowerStrength)));
            }
            if (modPlayer.SoulsMode && player.whoAmI == Main.myPlayer && item.type == ItemID.ManaRegenerationPotion && ttindex != -1)
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "RestorationTime", LangUtils.GetTextValue("Items.VanillaItems.CeruleanManaRegenerationPotion", CeruleanFlaskPlayer.ManaRegenPotRestorationTimerBonus)));
            }
        }
    }
}
