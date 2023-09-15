using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Items.Weapons.Melee;
using tsorcRevamp.Projectiles.Summon.Whips;
using tsorcRevamp.Utilities;

namespace tsorcRevamp
{
    // This class stores necessary player info for our custom resource (stamina) that governs the usage of special abilities such as dodge rolls, spins, dashes etc.
    public class tsorcRevampStaminaPlayer : ModPlayer
    {
        public static tsorcRevampStaminaPlayer ModPlayer(Player player)
        {
            return player.GetModPlayer<tsorcRevampStaminaPlayer>();
        }


        // Here we include a custom resource, similar to mana or health.
        // Creating some variables to define the current value of our Stamina resource as well as the current maximum value. We also include a temporary max value, as well as some variables to handle the natural regeneration of this resource.
        public float staminaResourceCurrent;
        public const float DefaultStaminaResourceMax = 100;
        public float staminaResourceMax;
        public float staminaResourceMax2;
        public float staminaResourceRegenRate;
        public float staminaResourceGainMult = 1;
        public float staminaResourceGain;
        internal float staminaResourceRegenTimer = 0f;
        public static readonly Color RestoreStaminaResource = new Color(20, 100, 20); // We can use this for CombatText, if you create an item that replenishes exampleResourceCurrent.

        public int minionStaminaCap;

        /*
		In order to make the Example Resource example straightforward, several things have been left out that would be needed for a fully functional resource similar to mana and health. 
		Here are additional things you might need to implement if you intend to make a custom resource:
		- Multiplayer Syncing: The current example doesn't require MP code, but pretty much any additional functionality will require this. ModPlayer.SendClientChanges and clientClone will be necessary, as well as SyncPlayer if you allow the user to increase exampleResourceMax.
		- Save/Load and increased max resource: You'll need to implement Save/Load to remember increases to your exampleResourceMax cap.
		- Resouce replenishment item: Use GlobalNPC.NPCLoot to drop the item. ModItem.OnPickup and ModItem.ItemSpace will allow it to behave like Mana Star or Heart. Use code similar to Player.HealEffect to spawn (and sync) a colored number suitable to your resource.
		*/

        public override void SaveData(TagCompound tag)
        {
            tag.Add("staminaResourceMax", staminaResourceMax);
        }

        public override void LoadData(TagCompound tag)
        {

            staminaResourceMax = tag.GetFloat("staminaResourceMax");

        }

        public override void Initialize()
        {
            staminaResourceMax = DefaultStaminaResourceMax;
            staminaResourceCurrent = staminaResourceMax;
        }

        public override void OnRespawn()
        {
            staminaResourceCurrent = staminaResourceMax; //
        }

        public override void ResetEffects()
        {
            ResetVariables();
        }

        public override void UpdateDead()
        {
            ResetVariables();
        }

        private void ResetVariables()
        {
            if (!Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                staminaResourceRegenRate = 1f;
            }

            else
            {
                staminaResourceRegenRate = 2f; // Bearer of the Curse regains stamina at 2x speed
            }

            staminaResourceGainMult = 1f;

            staminaResourceMax2 = staminaResourceMax;
        }

        public override void PostUpdateMiscEffects()
        {
            UpdateResource();
        }

        //const float BoomerangDrainPerFrame = 0.6f;
        const float HeldProjectileDrainPerFrame = 1f;
        const float SpecialHeldProjectileDrainPerFrame = 0.6f;
        const float FlailDrainPerFrame = 0.4f;
        const float YoyoDrainPerFrame = 0.4f;
        const float ChargedWhipDrainPerFrame = 0.3f;

        // Lets do all our logic for the custom resource here, such as limiting it, increasing it and so on.
        private void UpdateResource()
        {
            
            //Main.NewText("Stamina: " + staminaResourceCurrent + "/" + staminaResourceMax2);
            //Main.NewText("Stamina regen rate: " + staminaResourceRegenRate);
            //Main.NewText("Stamina regen gain mult: " + staminaResourceGainMult);
            //Main.NewText("Timer: " + staminaResourceRegenTimer);

            for (int p = 0; p < 1000; p++) //To-do add a check before this making this loop only run if there are actually any projectiles in the array
            {
                if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                {
                    //if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && Main.projectile[p].aiStyle == ProjAIStyleID.Boomerang) //find boomerangs, if so, cut regen by 2/3
                    //{
                    //    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= .333333f;
                    //    break; //break to prevent it nuking the regen rate when multiple boomerangs are present
                    //}

                    if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && (Main.projectile[p].type == ProjectileID.ChainGuillotine
                        || Main.projectile[p].type == ProjectileID.MechanicalPiranha))
                    {
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= SpecialHeldProjectileDrainPerFrame;
                        if (staminaResourceCurrent < 1)
                        {
                            Main.projectile[p].Kill();
                        }
                        break;
                    }

                    //why did shortswords have a special per-frame drain in addition to their normal drain?

                    /*if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && Main.projectile[p].aiStyle == ProjAIStyleID.Boomerang) //Can't have boomerangs just not use any stamina, especially weapons like Bananarangs and Possessed Hatchet(their individual throws just do not use stamina currently)
                    {
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 0.34f;
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= BoomerangDrainPerFrame;
                        break;
                    }*/

                    if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && (Main.projectile[p].type == ProjectileID.VortexBeater 
                        || Main.projectile[p].type == ProjectileID.Celeb2Weapon || Main.projectile[p].type == ProjectileID.FlyingKnife 
                        || Main.projectile[p].type == ProjectileID.ShadowFlameKnife))
                    {
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= HeldProjectileDrainPerFrame;
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 0f;
                        if (staminaResourceCurrent < 1)
                        {
                            Main.projectile[p].Kill();
                        }
                    }

                    if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && (Main.projectile[p].type == ProjectileID.Terragrim || Main.projectile[p].type == ProjectileID.Arkhalis
                        || Main.projectile[p].type == ProjectileID.ChargedBlasterCannon || Main.projectile[p].type == ProjectileID.MedusaHead
                        || Main.projectile[p].type == ProjectileID.ChainKnife || Main.projectile[p].type == ProjectileID.LastPrism
                        || Main.projectile[p].type == ProjectileID.LaserMachinegun
                        || Main.projectile[p].type == ProjectileID.DD2PhoenixBow || Main.projectile[p].type == ProjectileID.Phantasm))
                    {
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= HeldProjectileDrainPerFrame;
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 0f;
                        if (staminaResourceCurrent < 1)
                        {
                            Main.projectile[p].Kill();
                        }
                    }

                    if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && (Main.projectile[p].aiStyle == ProjAIStyleID.Flail
                        || Main.projectile[p].type == ProjectileID.Anchor))

                    {
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= FlailDrainPerFrame;
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 0f;
                        if (staminaResourceCurrent < 1)
                        {
                            Main.projectile[p].Kill();
                        }
                    }


                    if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && (Main.projectile[p].aiStyle == ProjAIStyleID.Yoyo))

                    {
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= YoyoDrainPerFrame;
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 0f;
                        if (staminaResourceCurrent < 1)
                        {
                            Main.projectile[p].Kill();
                        }
                    }

                    if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && (Main.projectile[p].type == ModContent.ProjectileType<SearingLashProjectile>() || Main.projectile[p].type == ModContent.ProjectileType<NightsCrackerProjectile>() || Main.projectile[p].type == ModContent.ProjectileType<TerraFallProjectile>()))

                    {
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= ChargedWhipDrainPerFrame;
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 0f;
                        if (staminaResourceCurrent < 1)
                        {
                            Main.projectile[p].Kill();
                        }
                    }
                }
            }

            //Stamina capping for summoners - First minion costs 18, second one 16, third one 14, etc. Once the cost hits 2, at 9 minions, it keeps costing 2 for subsequent minions
            if (Player.numMinions > 0) {
                minionStaminaCap = (int)(staminaResourceMax2 * 1f); 
            }
            else { minionStaminaCap = (int)staminaResourceMax2; }


            staminaResourceGain = staminaResourceGainMult * staminaResourceRegenRate; //Apply our multiplier to our base regen rate

            // For our resource lets make it regen slowly over time to keep it simple, let's use exampleResourceRegenTimer to count up to whatever value we want, then increase currentResource.
            staminaResourceRegenTimer++; //Increase it by 60 per second, or 1 per tick.

            // A simple timer that goes up to 3, increases the exampleResourceCurrent by 1 and then resets back to 0.
            if (Player.whoAmI == Main.myPlayer)
            {
                //no stamina regen during a roll/swordspin/using an item, for balance? Yes
                if (!Player.GetModPlayer<tsorcRevampPlayer>().isDodging && !Player.GetModPlayer<tsorcRevampPlayer>().isSwordflipping)
                {
                    if (!Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                    {
                        if (staminaResourceRegenTimer > 3)
                        {
                            staminaResourceCurrent += staminaResourceGain;
                            staminaResourceRegenTimer = 0;
                        }
                    }
                    else if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && Player.itemAnimation == 0 && staminaResourceCurrent <= minionStaminaCap) //Bearer of the Curse doesn't regen stamina while using items
                    {
                        if (staminaResourceRegenTimer > 3)
                        {
                            staminaResourceCurrent += staminaResourceGain;

                            if (staminaResourceCurrent > minionStaminaCap) { staminaResourceCurrent = minionStaminaCap; }

                            staminaResourceRegenTimer = 0;
                        }
                    }
                }
            }


            // Limit exampleResourceCurrent from going over the limit imposed by exampleResourceMax.
            staminaResourceCurrent = Utils.Clamp(staminaResourceCurrent, 0, staminaResourceMax2);
        }

        static readonly List<int> HeldProjectileWeapons = new() 
        {
            ItemID.Terragrim,
            ItemID.Arkhalis,
            ItemID.ChargedBlasterCannon,
            ItemID.MedusaHead,
            ItemID.ChainKnife,
            ItemID.LastPrism,
            ItemID.LaserMachinegun,
            ItemID.DD2PhoenixBow,
            ItemID.Phantasm,
            ItemID.VortexBeater,
            ItemID.Celeb2,
            ItemID.FlyingKnife,
            ItemID.ShadowFlameKnife
        };

        static readonly List<int> SpecialHeldProjectileWeapons = new() 
        {
            ItemID.ChainGuillotines,
            ItemID.PiranhaGun
        };
        private class tsrStaminaGlobalItem : GlobalItem 
        {
            public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) 
            {
                if (!Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) return;
                if (!ModContent.GetInstance<tsorcRevampConfig>().ShowStaminaTooltip) return;
                if (item.pick != 0 || item.axe != 0 || item.hammer != 0 || item.DamageType == DamageClass.Summon) return;
                if (item.damage <= 0 && item.type != ItemID.CoinGun) return;
                if (item.ammo != AmmoID.None) return; //ammo does not consume stamina
                if (item.type == ItemID.EoCShield) return;
                StringBuilder tipToAdd = new();
                tipToAdd.Append(LangUtils.GetTextValue("UI.StaminaUse"));

                int preModificationLength = tipToAdd.Length;

                #region drain per frame AIStyle-based cases
                Projectile shot = new();
                shot.SetDefaults(item.shoot);

                float drainPerFrame = 0f;
                bool preventsRegen = false;
                bool inhibitsRegen = false;
                switch (shot.aiStyle)
                {
                    /*case ProjAIStyleID.Boomerang:
                        {
                            drainPerFrame = BoomerangDrainPerFrame;
                            inhibitsRegen = true;
                            break;
                        }*/
                    case ProjAIStyleID.Flail:
                        {
                            drainPerFrame = FlailDrainPerFrame;
                            preventsRegen = true;
                            break;
                        }
                    case ProjAIStyleID.Yoyo:
                        {
                            drainPerFrame = YoyoDrainPerFrame;
                            preventsRegen = true;
                            break;
                        }
                    default: break;

                }
                #endregion

                #region unique cases
                //it's just harpoon. seriously, what IS this weapon? i dont understand.
                if (item.type == ItemID.Harpoon) {
                    tipToAdd.Append(LangUtils.GetTextValue("UI.14"));
                }
                #endregion

                if (tipToAdd.Length == preModificationLength) {
                    int staminaUse = (int)(item.useAnimation / Main.LocalPlayer.GetAttackSpeed(item.DamageType));
                    staminaUse = (int)tsorcRevampPlayer.ReduceStamina(staminaUse);
                    /*if (item.DamageType == DamageClass.Magic)
                    {
                        staminaUse *= 8;
                        staminaUse /= 10;
                    }*/
                    tipToAdd.Append($"{staminaUse}"); 
                }

                #region special drain per frame
                if (HeldProjectileWeapons.Contains(item.type)) {
                    drainPerFrame = HeldProjectileDrainPerFrame;
                    preventsRegen = true;
                }

                else if (SpecialHeldProjectileWeapons.Contains(item.type)) {
                    drainPerFrame = SpecialHeldProjectileDrainPerFrame;
                    //these dont prevent regen
                }
                #endregion

                #region drain per frame tooltips
                if (drainPerFrame != 0f) {
                    tipToAdd.Append($" + {drainPerFrame * 60} " + LangUtils.GetTextValue("UI.PerSecond"));
                }

                if (inhibitsRegen) {
                    tipToAdd.Append(LangUtils.GetTextValue("UI.StaminaRegenReduction"));
                }
                if (preventsRegen) {
                    tipToAdd.Append(LangUtils.GetTextValue("UI.StaminaRegenNullification"));
                }
                #endregion
                int ttindex = tooltips.FindLastIndex(t => t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
                if (ttindex != -1) {// if we find one
                                    //insert the extra tooltip line
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "",
                    tipToAdd.ToString()));
                }
            }
        }
    }
}
