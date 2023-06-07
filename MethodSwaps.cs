using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.Personalities;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.UI;
using Terraria.Utilities;
using tsorcRevamp.Items;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.UI;
using Terraria.DataStructures;
using tsorcRevamp.Projectiles.Enemy.Marilith;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.Projectiles.Enemy.Triad;
using Terraria.Graphics;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.GameContent.Drawing;
using Terraria.WorldBuilding;
using System.ComponentModel.Design;
using tsorcRevamp.Items.Weapons.Melee.Spears;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.VanillaItems;

namespace tsorcRevamp
{
    class MethodSwaps
    {
        internal static void ApplyMethodSwaps()
        {
            Terraria.On_Player.Spawn += SpawnPatch;

            Terraria.On_WorldGen.TriggerLunarApocalypse += StopLunarApocalypse;

            Terraria.On_WorldGen.UpdateLunarApocalypse += StopMoonLord;

            Terraria.On_Player.TileInteractionsCheckLongDistance += SignTextPatch;

            Terraria.On_NPC.SpawnNPC += BossZenPatch;

            Terraria.On_Main.DrawMenu += DownloadMapButton;

            Terraria.On_Main.StartInvasion += BlockInvasions;

            Terraria.On_NPC.AI_037_Destroyer += DestroyerAIRevamp;

            On_NPCUtils.TargetClosestOldOnesInvasion += OldOnesArmyPatch;

            Terraria.On_NPC.AI_111_DD2LightningBug += LightningBugTeleport;

            Terraria.On_Player.QuickBuff += CustomQuickBuff;
            Terraria.On_Player.QuickMana += CustomQuickMana;
            Terraria.On_Player.QuickHeal += CustomQuickHeal;
            Terraria.On_Player.QuickGrapple_GetItemToUse += Player_QuickGrapple_GetItemToUse;
            Terraria.On_Player.QuickMount_GetItemToUse += Player_QuickMount_GetItemToUse; ;

            Terraria.UI.On_ChestUI.LootAll += PotionBagLootAllPatch;

            Terraria.On_Player.HasUnityPotion += HasWormholePotion;
            Terraria.On_Player.TakeUnityPotion += ConsumeWormholePotion;

            Terraria.On_Main.DrawNPCs += DrawNPCsPatch;

            Terraria.GameContent.On_ShopHelper.GetShoppingSettings += ShopHelper_GetShoppingSettings;

            Terraria.GameContent.UI.States.On_UIWorldSelect.NewWorldClick += UIWorldSelect_NewWorldClick;

            Terraria.On_Player.HandleBeingInChestRange += Player_HandleBeingInChestRange;

            Terraria.On_Wiring.DeActive += Wiring_DeActive;

            Terraria.On_WorldGen.StartHardmode += WorldGen_StartHardmode;

            Terraria.On_Projectile.FishingCheck += Projectile_FishingCheck;

            Terraria.On_Main.CraftItem += Main_CraftItem;

            Terraria.On_Main.DrawInterface_35_YouDied += Main_DrawInterface_35_YouDied;

            Terraria.On_Player.InZonePurity += Player_InZonePurity;
			//On.Terraria.GameContent.ItemDropRules.ItemDropResolver.ResolveRule += ItemDropResolver_ResolveRule;

			Terraria.On_Player.DashMovement += Player_DashMovement;

            Terraria.On_Player.DropTombstone += On_Player_DropTombstone;

            Terraria.DataStructures.On_PlayerDrawLayers.DrawPlayer_TransformDrawData += PaperMarioMode;

            Terraria.On_Main.Draw += Main_Draw;

            Terraria.On_Main.DrawProjectiles += Main_DrawProjectiles;

            Terraria.On_Main.DrawCachedProjs += Main_DrawCachedProjs;

            On_Recipe.CollectItemsToCraftWithFrom += Recipe_CollectItemsToCraftWithFrom;

            On_Player.PickupItem += On_Player_PickupItem;

            On_Player.OnHurt_Part2 += On_Player_OnHurt_Part2;

            On_Player.ItemCheck_ApplyManaRegenDelay += On_Player_ItemCheck_ApplyManaRegenDelay;

            On_Projectile.StatusNPC += On_Projectile_StatusNPC;

            On_Player.ApplyEquipFunctional += On_Player_ApplyEquipFunctional;

            On_Player.UpdateEquips += On_Player_UpdateEquips;

            On_WorldGen.KillTile_ShouldDropSeeds += On_WorldGen_KillTile_ShouldDropSeeds;
        }

        private static bool On_WorldGen_KillTile_ShouldDropSeeds(On_WorldGen.orig_KillTile_ShouldDropSeeds orig, int x, int y)
        {
            if (Main.rand.NextBool(2) && (Main.LocalPlayer.HasItem(ModContent.ItemType<ToxicShot>()) || Main.LocalPlayer.HasItem(ModContent.ItemType<AlienRifle>()) || Main.LocalPlayer.HasItem(ModContent.ItemType<OmegaSquadRifle>())))
            {
                return true;
            }
            return orig(x, y);
        }

        private static void On_Player_UpdateEquips(On_Player.orig_UpdateEquips orig, Player self, int i)
        {
            if (self.inventory[self.selectedItem].type == ModContent.ItemType<AncientDragonLance>())
            {
                self.trident = true;
            }
            orig(self, i);
        }

        public static float SummonTagStrengthBoost = 50;
        public static float SummonTagDurationBoost = 50;
        private static void On_Player_ApplyEquipFunctional(On_Player.orig_ApplyEquipFunctional orig, Player self, Item currentItem, bool hideVisual)
        {
            var modPlayer = self.GetModPlayer<tsorcRevampPlayer>();
            if(currentItem.type == ItemID.NecromanticScroll)
            {
                self.GetDamage(DamageClass.Summon) -= 0.1f;
                modPlayer.SummonTagDuration += SummonTagDurationBoost / 100f;
            }
            if(currentItem.type == ItemID.HerculesBeetle)
            {
                self.GetKnockback(DamageClass.Summon) -= 2f;
                self.GetDamage(DamageClass.Summon) -= 0.15f;
                modPlayer.SummonTagStrength += SummonTagStrengthBoost / 100f;
            }
            if (currentItem.type == ItemID.PapyrusScarab)
            {
                self.GetKnockback(DamageClass.Summon) -= 2f;
                self.GetDamage(DamageClass.Summon) -= 0.15f;
                self.GetModPlayer<tsorcRevampPlayer>().SummonTagStrength += SummonTagStrengthBoost / 100f;
                modPlayer.SummonTagDuration += SummonTagDurationBoost / 100f;
            }
            orig(self, currentItem, hideVisual);
        }

        private static void On_Projectile_StatusNPC(On_Projectile.orig_StatusNPC orig, Projectile self, int i)
        {
            if (self.type == ProjectileID.BlandWhip || self.type == ProjectileID.ThornWhip || self.type == ProjectileID.BoneWhip || self.type == ProjectileID.FireWhip || self.type == ProjectileID.CoolWhip || self.type == ProjectileID.SwordWhip || self.type == ProjectileID.MaceWhip || self.type == ProjectileID.ScytheWhip || self.type == ProjectileID.RainbowWhip)
            {
                return;
            }
            orig(self, i);
        }

        private static void On_Player_DropTombstone(On_Player.orig_DropTombstone orig, Player self, long coinsOwned, Terraria.Localization.NetworkText deathText, int hitDirection)
        {
            //thank you DarkLight66's NoMoreTombs and stinkylizard
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                orig(self, coinsOwned, deathText, hitDirection);
        }

        private static void On_Player_ItemCheck_ApplyManaRegenDelay(On_Player.orig_ItemCheck_ApplyManaRegenDelay orig, Player self, Item sItem)
        {
            if (self.GetManaCost(sItem) > 0 && self.manaRegenDelay < self.maxRegenDelay)
            {
                self.manaRegenDelay = (int)self.maxRegenDelay;
            }
            if (self.GetManaCost(sItem) > 0 && self.manaRegenDelay < self.maxRegenDelay && sItem.DamageType != DamageClass.Magic)
            {
                self.manaRegenDelay = MeleeEdits.ManaDelay;
            }
        }

        private static void On_Player_OnHurt_Part2(On_Player.orig_OnHurt_Part2 orig, Player self, Player.HurtInfo info)
        {
            if (self.magicCuffs && self.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                int ManaGain = info.SourceDamage / 7;
                self.statMana += ManaGain;
                if (self.statMana > self.statManaMax2)
                {
                    self.statMana = self.statManaMax2;
                }
                self.ManaEffect(ManaGain);
                return;
            }
            orig(self, info);
        }

        private static Item On_Player_PickupItem(On_Player.orig_PickupItem orig, Player self, int playerIndex, int worldItemArrayIndex, Item itemToPickUp)
        {
            if (itemToPickUp.type == ItemID.Star || itemToPickUp.type == ItemID.SugarPlum || itemToPickUp.type == ItemID.SoulCake || itemToPickUp.type == ItemID.ManaCloakStar && self.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                SoundEngine.PlaySound(SoundID.Item7, new Vector2((int)self.position.X, (int)self.position.Y));
                self.statMana += self.statManaMax2 / 25;
                self.ManaEffect(self.statManaMax2 / 25);
                if (self.statMana > self.statManaMax2)
                {
                    self.statMana = self.statManaMax2;
                }
                itemToPickUp.TurnToAir();
                return itemToPickUp;
            }
            return orig(self, playerIndex, worldItemArrayIndex, itemToPickUp);
        }

        private static void StopLunarApocalypse(Terraria.On_WorldGen.orig_TriggerLunarApocalypse orig)
        {
            return;
        }

        public static bool fixRainbowRod = true;
        private static void Main_DrawCachedProjs(Terraria.On_Main.orig_DrawCachedProjs orig, Main self, List<int> projCache, bool startSpriteBatch)
        {
            if (Main.IsGraphicsDeviceAvailable)
            {
                if(startSpriteBatch == false)
                {
                    Main.spriteBatch.End();
                }

                //Rainbow rod & friends are super duper special and *must* be drawn in their own spritebatch before *any* additive projectiles are drawn or else they will throw a tantrum and fucking die
                if (fixRainbowRod)
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i] != null && Main.projectile[i].active)
                        {
                            if (Main.projectile[i].type == 34)
                                default(FlameLashDrawer).Draw(Main.projectile[i]);

                            if (Main.projectile[i].type == 16)
                                default(MagicMissileDrawer).Draw(Main.projectile[i]);

                            if (Main.projectile[i].type == 933)
                                default(FinalFractalHelper).Draw(Main.projectile[i]);

                            if (Main.projectile[i].type == 79)
                                default(RainbowRodDrawer).Draw(Main.projectile[i]);
                        }
                        fixRainbowRod = false;
                    }
                    Main.spriteBatch.End();
                }
                
                //Draw anything additive that needs to be drawn on a different layer in one big batch
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (projCache.Contains(i))
                    {
                        if (Main.projectile[i] != null && Main.projectile[i].active)
                        {
                            if (Main.projectile[i].ModProjectile is DynamicTrail)
                            {
                                DynamicTrail trail = (DynamicTrail)Main.projectile[i].ModProjectile;
                                trail.additiveContext = true;
                                Color color = Color.White;
                                trail.PreDraw(ref color);
                                trail.additiveContext = false;
                            }
                        }
                    }
                }
                Main.spriteBatch.End();
                if (startSpriteBatch == false)
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
                }
            }


            orig(self, projCache, startSpriteBatch);
        }

        private static void Main_DrawProjectiles(Terraria.On_Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);
            if (Main.IsGraphicsDeviceAvailable)
            {
                //Draw most additive projectiles in one big batch
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i] != null && Main.projectile[i].active && !Main.projectile[i].hide)
                    {
                        if (Main.projectile[i].ModProjectile is DynamicTrail)
                        {
                            DynamicTrail trail = (DynamicTrail)Main.projectile[i].ModProjectile;
                            trail.additiveContext = true;
                            Color color = Color.White;
                            trail.PreDraw(ref color);
                            trail.additiveContext = false;
                        }
                        if (Main.projectile[i].ModProjectile is GenericLaser)
                        {
                            GenericLaser laser = (GenericLaser)Main.projectile[i].ModProjectile;
                            laser.additiveContext = true;
                            Color color = Color.White;
                            laser.PreDraw(ref color);
                            laser.additiveContext = false;
                        }
                        if (Main.projectile[i].ModProjectile is SpazFireJet)
                        {
                            SpazFireJet jet = (SpazFireJet)Main.projectile[i].ModProjectile;
                            jet.additiveContext = true;
                            Color color = Color.White;
                            jet.PreDraw(ref color);
                            jet.additiveContext = false;
                        }
                        if (Main.projectile[i].ModProjectile is Projectiles.Spears.FetidExhaust)
                        {
                            Projectiles.Spears.FetidExhaust jet = (Projectiles.Spears.FetidExhaust)Main.projectile[i].ModProjectile;
                            jet.additiveContext = true;
                            Color color = Color.White;
                            jet.PreDraw(ref color);
                            jet.additiveContext = false;
                        }
                    }
                }
                Main.spriteBatch.End();
            }
        }

        //Changing the rendertarget destroys everything currently in it
        //So we have to do it here, before the game draws anything else
        private static void Main_Draw(Terraria.On_Main.orig_Draw orig, Main self, GameTime gameTime)
        {
            fixRainbowRod = true;
            if (Main.IsGraphicsDeviceAvailable)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i] != null && Main.projectile[i].active)
                    {
                        //I really should just make one "Lightning" class that all these inherit from
                        //But also ¯\_(ツ)_/¯
                        if (Main.projectile[i].ModProjectile is MarilithLightning)
                        {
                            MarilithLightning lightning = (MarilithLightning)Main.projectile[i].ModProjectile;
                            if (lightning.lightningTarget == null && lightning.branches != null)
                            {
                                lightning.CreateRenderTarget();
                            }
                        }
                        /*
                        if (Main.projectile[i].ModProjectile is MarilithLightning)
                        {
                            MarilithLightning lightning = (MarilithLightning)Main.projectile[i].ModProjectile;
                            if (lightning.lightningTarget == null && lightning.branches != null)
                            {
                                lightning.CreateRenderTarget();
                            }
                        }*/

                        //I really should just make one "Lightning" class that all these inherit from
                        //But also ¯\_(ツ)_/¯
                        if (Main.projectile[i].ModProjectile is JellyfishLightning)
                        {
                            JellyfishLightning lightning = (JellyfishLightning)Main.projectile[i].ModProjectile;
                            if (lightning.lightningTarget == null)
                            {
                                lightning.CreateRenderTarget();
                            }
                        }

                        if (Main.projectile[i].ModProjectile is RealityCrack)
                        {
                            RealityCrack lightning = (RealityCrack)Main.projectile[i].ModProjectile;
                            if (lightning.lightningTarget == null)
                            {
                                lightning.CreateRenderTarget();
                            }
                        }


                        /*if (Main.projectile[i].ModProjectile is JellyfishLightning)
                        {
                            JellyfishLightning lightning = (JellyfishLightning)Main.projectile[i].ModProjectile;
                            if (lightning.lightningTarget == null)
                            {
                                lightning.CreateRenderTarget();
                            }
                        }*/
                    }
                }
            }


            orig(self, gameTime);
        }
        private static Item Player_QuickMount_GetItemToUse(Terraria.On_Player.orig_QuickMount_GetItemToUse orig, Player self)
        {
            Item item = null;
            if (item == null && self.miscEquips[3].mountType != -1 && !MountID.Sets.Cart[self.miscEquips[3].mountType] && CombinedHooks.CanUseItem(self, self.miscEquips[3]))
            {
                bool restrictedHook = false;

                if(self.miscEquips[3].type == ItemID.SlimySaddle && !NPC.downedBoss2)
                {
                    restrictedHook = true;
                }
                if (self.miscEquips[3].type == ItemID.QueenSlimeMountSaddle && !NPC.downedMechBoss3)
                {
                    restrictedHook = true;
                }

                if (!restrictedHook)
                {
                    item = self.miscEquips[3];
                }
            }

            if (item == null)
            {
                for (int i = 0; i < 58; i++)
                {
                    if (self.inventory[i].mountType != -1 && !MountID.Sets.Cart[self.inventory[i].mountType] && CombinedHooks.CanUseItem(self, self.inventory[i]))
                    {
                        bool restrictedHook = false;

                        if (self.inventory[i].type == ItemID.SlimySaddle && !NPC.downedBoss2)
                        {
                            restrictedHook = true;
                        }
                        if (self.inventory[i].type == ItemID.QueenSlimeMountSaddle && !NPC.downedMechBoss3)
                        {
                            restrictedHook = true;
                        }

                        if (!restrictedHook)
                        {
                            item = self.inventory[i];
                            break;
                        }
                        
                    }
                }
            }

            return item;
        }


		private static void Player_DashMovement(Terraria.On_Player.orig_DashMovement orig, Player self)
		{
            float originalVelocity = self.velocity.X;
            bool wasInDash = false;

            if (self.dashDelay < 0) //we are mid-dash.
                wasInDash = true;
            
            orig(self);

            //fix for auto-acceleration post-dash with a high max speed
            if (wasInDash && self.dashDelay > 0 && self.eocDash <= 0)
            {
				//we are no longer mid-dash; it must've just ended this frame.
				//(we have just passed below 12 / above -12 velocity, and our speed now equals our max speed)
                
                //eocDash seems to be a SoC dash hit timer. not sure what it's used for...
                // but if it's more than 0 then we just hit something and shouldn't reset the speed.

				if ((double)self.velocity.X < 0.0) //not completely sure why the cast, but thats wha
				{
					//moving left
                    if (!self.controlLeft)
    					self.velocity.X = originalVelocity;
				}
    			else
				{
					//moving right
                    if (!self.controlRight)
    					self.velocity.X = originalVelocity;
				}
			}

			//fix for not being able to dash until stopping with a high max speed
			if (wasInDash && Math.Abs(self.velocity.X) > 17)
			{
				//our speed has gone above 16.9. the dash is not doing this - in fact the dash continuously slows you down even without friction. we should end it.
				self.dashDelay = 20; //different dashes have different cooldowns (i think Tabi & SoC have 30), but this is the most conservative one. It will never be *worse* than the regular dash.
			}

		}

		//The reason the dungeon code is getting inserted here isn't because it has anything to do with ZonePurity
		//It's just because this is a function that has the player as a parameter, and is called in UpdateBiomes *after* ZoneDungeon is set but before it is used.
		private static bool Player_InZonePurity(Terraria.On_Player.orig_InZonePurity orig, Player self)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && NPC.downedBoss3)
            {
                if (Main.SceneMetrics.DungeonTileCount >= 200 || Main.SceneMetrics.DungeonTileCount >= 50 && tsorcRevampWorld.SuperHardMode)
                {
                    int playerTileX = (int)self.Center.X / 16;
                    int playerTileY = (int)self.Center.Y / 16;
                    for (int i = -10; i < 11; i++)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            int cross = (2 * j) - 1;
                            //check in an x shape instead of checking the entire region, since checking 100 tiles every frame is a little silly
                            if (Main.wallDungeon[Main.tile[playerTileX + i, playerTileY + (i * cross)].WallType] || tsorcRevamp.CustomDungeonWalls[Main.tile[playerTileX + i, playerTileY + (i * cross)].WallType])
                            {
                                self.ZoneDungeon = true;
                            }
                        }
                    }
                }
            }

            return orig(self);
        }

        private static void Main_DrawInterface_35_YouDied(Terraria.On_Main.orig_DrawInterface_35_YouDied orig)
        {
            orig();
            if (Main.player[Main.myPlayer].dead)
            {
                string textValue2 = Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().DeathText;
                if(textValue2 == null)
                {
                    textValue2 = "";
                }
                float scale = 0.5f;
                Main.spriteBatch.DrawString(FontAssets.DeathText.Value, textValue2, new Vector2((float)(Main.screenWidth / 2) - 40 - FontAssets.MouseText.Value.MeasureString(textValue2).X / 2f, (float)(Main.screenHeight / 2) + 220), Main.player[Main.myPlayer].GetDeathAlpha(Microsoft.Xna.Framework.Color.Transparent), 0f, default(Vector2), scale, SpriteEffects.None, 0f);
            }
        }

        /*
        private static Terraria.GameContent.ItemDropRules.ItemDropAttemptResult ItemDropResolver_ResolveRule(On.Terraria.GameContent.ItemDropRules.ItemDropResolver.orig_ResolveRule orig, Terraria.GameContent.ItemDropRules.ItemDropResolver self, Terraria.GameContent.ItemDropRules.IItemDropRule rule, Terraria.GameContent.ItemDropRules.DropAttemptInfo info)
        {
            if (!rule.CanDrop(info))
            {
                ItemDropAttemptResult itemDropAttemptResult = default(ItemDropAttemptResult);
                itemDropAttemptResult.State = ItemDropAttemptResultState.DoesntFillConditions;
                ItemDropAttemptResult itemDropAttemptResult2 = itemDropAttemptResult;
                self.ResolveRuleChains(rule, info, itemDropAttemptResult2);
                return itemDropAttemptResult2;
            }

            ItemDropAttemptResult itemDropAttemptResult3 = (rule as INestedItemDropRule)?.TryDroppingItem(info, ResolveRule) ?? rule.TryDroppingItem(info);
            ResolveRuleChains(rule, info, itemDropAttemptResult3);
            return itemDropAttemptResult3;
        }*/

        private static void Main_CraftItem(Terraria.On_Main.orig_CraftItem orig, Recipe r)
        {
            orig(r);

            if (Main.mouseItem.type > 0 || r.createItem.type > 0)
            {
                tsorcRevampPlayer modPlayer = Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>();
                foreach (Item ingredient in r.requiredItem)
                {
                    if (ingredient.type == ModContent.ItemType<DarkSoul>())
                    {

                        //a recipe with souls will only be craftable if you have enough souls, even if theyre in soul slot
                        modPlayer.SoulSlot.Item.stack -= ingredient.stack;

                        //if you have exactly enough for the recipe
                        if (modPlayer.SoulSlot.Item.stack == 0)
                        {
                            modPlayer.SoulSlot.Item.TurnToAir();
                        }

                    }
                }

                //force a recipe recalculation so you cant craft things without enough souls
                Recipe.FindRecipes();
            }
        }

        private static void Projectile_FishingCheck(Terraria.On_Projectile.orig_FishingCheck orig, Projectile self)
        {
            Terraria.DataStructures.FishingAttempt fisher = default(Terraria.DataStructures.FishingAttempt);
            fisher.X = (int)(self.Center.X / 16f);
            fisher.Y = (int)(self.Center.Y / 16f);
            fisher.bobberType = self.type;

            fisher.playerFishingConditions = Main.player[self.owner].GetFishingConditions();
            if (fisher.playerFishingConditions.BaitItemType == 2673)
            {
                if (Main.player[self.owner].wet)
                {
                    return;
                }
                Main.player[self.owner].displayedFishingInfo = Terraria.Localization.Language.GetTextValue("GameUI.FishingWarning");
                if ((fisher.X < 380 || fisher.X > Main.maxTilesX - 380) && !NPC.AnyNPCs(370))
                {
                    self.ai[1] = Main.rand.Next(-180, -60) - 100;
                    self.localAI[1] = 1f;
                    self.netUpdate = true;
                }

                return;
            }
            else
            {
                orig(self);
            }
        }

        private static Item Player_QuickGrapple_GetItemToUse(Terraria.On_Player.orig_QuickGrapple_GetItemToUse orig, Player self)
        {
            Item item = null;

            bool restrictedHook = false;
            if (tsorcRevamp.RestrictedHooks.Contains(self.miscEquips[4].type))
            {
                restrictedHook = true;
            }

            if (Main.projHook[self.miscEquips[4].shoot])
            {
                if (NPC.downedBoss3 || !restrictedHook || !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                {
                    item = self.miscEquips[4];
                }
            }

            if (item == null)
            {
                for (int i = 0; i < 58; i++)
                {
                    if (Main.projHook[self.inventory[i].shoot])
                    {
                        restrictedHook = false;
                        if(self.inventory[i].type == ItemID.SlimeHook || self.inventory[i].type == ItemID.SquirrelHook)
                        {
                            restrictedHook = true;
                        }
                        if (NPC.downedBoss3 || !restrictedHook || !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                        {
                            item = self.inventory[i];
                            break;
                        }
                    }
                }
            }

            return item;
        }

        private static void WorldGen_StartHardmode(Terraria.On_WorldGen.orig_StartHardmode orig)
        {
            Main.hardMode = true;
            /* Hallow the spawn
            for (int i = 4658; i < 5238; i++)
            {
                for (int j = 630; j < 945; j++)
                {
                    WorldGen.Convert(i, j, 2, 1);
                }
            }*/
        }

        private static void Wiring_DeActive(Terraria.On_Wiring.orig_DeActive orig, int i, int j)
        {
            tsorcRevamp.ActuationBypassActive = true;
            orig(i, j);
            tsorcRevamp.ActuationBypassActive = false;
        }

        private static void Player_HandleBeingInChestRange(Terraria.On_Player.orig_HandleBeingInChestRange orig, Player self) {
            if (self.chest != -1) {
                if (self.chest != -2) {
                    self.piggyBankProjTracker.Clear();
                }
                if (self.chest != -5) {
                    self.voidLensChest.Clear();
                }
                bool flag = false;
                int projectileLocalIndex = self.piggyBankProjTracker.ProjectileLocalIndex;
                if (projectileLocalIndex >= 0) {
                    flag = true;
                    if (!Main.projectile[projectileLocalIndex].active || (Main.projectile[projectileLocalIndex].type != 525 && Main.projectile[projectileLocalIndex].type != 960)) {
                        Main.PlayInteractiveProjectileOpenCloseSound(Main.projectile[projectileLocalIndex].type, false);
                        self.chest = -1;
                        Recipe.FindRecipes(false);
                    }
                    else {
                        int num = (int)(((double)self.position.X + (double)self.width * 0.5) / 16.0);
                        int num2 = (int)(((double)self.position.Y + (double)self.height * 0.5) / 16.0);
                        Vector2 vector = Main.projectile[projectileLocalIndex].Hitbox.ClosestPointInRect(self.Center);
                        self.chestX = (int)vector.X / 16;
                        self.chestY = (int)vector.Y / 16;
                        if (num < self.chestX - Player.tileRangeX || num > self.chestX + Player.tileRangeX + 1 || num2 < self.chestY - Player.tileRangeY || num2 > self.chestY + Player.tileRangeY + 1) {
                            if (self.chest != -1) {
                                Main.PlayInteractiveProjectileOpenCloseSound(Main.projectile[projectileLocalIndex].type, false);
                            }
                            self.chest = -1;
                            Recipe.FindRecipes(false);
                        }
                    }
                }
                int projectileLocalIndex2 = self.voidLensChest.ProjectileLocalIndex;
                if (projectileLocalIndex2 >= 0) {
                    flag = true;
                    if (!Main.projectile[projectileLocalIndex2].active || Main.projectile[projectileLocalIndex2].type != 734) {
                        SoundEngine.PlaySound(SoundID.Item130, null);
                        self.chest = -1;
                        Recipe.FindRecipes(false);
                    }
                    else {
                        int num3 = (int)(((double)self.position.X + (double)self.width * 0.5) / 16.0);
                        int num4 = (int)(((double)self.position.Y + (double)self.height * 0.5) / 16.0);
                        Vector2 vector2 = Main.projectile[projectileLocalIndex2].Hitbox.ClosestPointInRect(self.Center);
                        self.chestX = (int)vector2.X / 16;
                        self.chestY = (int)vector2.Y / 16;
                        if (num3 < self.chestX - Player.tileRangeX || num3 > self.chestX + Player.tileRangeX + 1 || num4 < self.chestY - Player.tileRangeY || num4 > self.chestY + Player.tileRangeY + 1) {
                            if (self.chest != -1) {
                                SoundEngine.PlaySound(SoundID.Item130, null);
                            }
                            self.chest = -1;
                            Recipe.FindRecipes(false);
                        }
                    }
                }
                if (flag) {
                    return;
                }
                if (!self.IsInInteractionRangeToMultiTileHitbox(self.chestX, self.chestY)) {
                    if (self.chest != -1) {
                        //SoundEngine.PlaySound(11, -1, -1, 1, 1f, 0f);
                    }
                    self.chest = -1;
                    Recipe.FindRecipes(false);
                    return;
                }
                if (!Main.tile[self.chestX, self.chestY].HasTile) {
                    //SoundEngine.PlaySound(11, -1, -1, 1, 1f, 0f);
                    self.chest = -1;
                    Recipe.FindRecipes(false);
                    return;
                }
            }
            else {
                self.piggyBankProjTracker.Clear();
                self.voidLensChest.Clear();
            }
        }

        private static void UIWorldSelect_NewWorldClick(Terraria.GameContent.UI.States.On_UIWorldSelect.orig_NewWorldClick orig, Terraria.GameContent.UI.States.UIWorldSelect self, UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);

            Main.MenuUI.SetState(new CustomMapUIState());
        }

        static Type ShopHelper = null;
        static FieldInfo currentNPC = null;
        static FieldInfo currentPlayer = null;
        //Hijacks the vanilla method and just sets the NPC price to default and happiness to "", which signals the game to not draw the button
        private static ShoppingSettings ShopHelper_GetShoppingSettings(Terraria.GameContent.On_ShopHelper.orig_GetShoppingSettings orig, ShopHelper self, Player player, NPC npc)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                ShoppingSettings shoppingSettings = default;
                shoppingSettings.PriceAdjustment = 1.0;
                shoppingSettings.HappinessReport = "";

                if (ShopHelper == null)
                {
                    ShopHelper = typeof(ShopHelper);
                    currentNPC = ShopHelper.GetField("_currentNPCBeingTalkedTo", BindingFlags.NonPublic | BindingFlags.Instance);
                    currentPlayer = ShopHelper.GetField("_currentPlayerTalking", BindingFlags.NonPublic | BindingFlags.Instance);
                }

                currentNPC.SetValue(self, npc);
                currentPlayer.SetValue(self, player);

                return shoppingSettings;
            }
            else
            {
                return orig(self, player, npc);
            }
        }

       

        private static void DrawNPCsPatch(Terraria.On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
        {
            orig(self, behindTiles);
            if (NPCs.VanillaChanges.drawingDestroyer)
            {
                NPCs.VanillaChanges.drawingDestroyer = false;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
            }
        }


        private static void PotionBagLootAllPatch(Terraria.UI.On_ChestUI.orig_LootAll orig)
        {
            if (Main.LocalPlayer.HasItem(ModContent.ItemType<PotionBag>()))
            {
                Player player = Main.player[Main.myPlayer];
                Item[] inventory;
                if (player.chest > -1)
                {
                    inventory = Main.chest[player.chest].item;
                }
                else if (player.chest == -3)
                {
                    inventory = player.bank2.item;
                }

                else if (player.chest == -4)
                {
                    inventory = player.bank3.item;
                }
                else
                {
                    inventory = player.bank.item;
                }

                for (int i = 0; i < 40; i++)
                {
                    if (inventory[i].type > 0 && PotionBagUIState.IsValidPotion(inventory[i]))
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().ShiftClickSlot(inventory, ItemSlot.Context.BankItem, i);
                        if (Main.netMode == 1 && player.chest >= -1)
                        {
                            NetMessage.SendData(32, -1, -1, null, player.chest, i);
                        }
                    }
                }
            }

            orig();
        }

        private static void CustomQuickBuff(Terraria.On_Player.orig_QuickBuff orig, Player player)
        {
            if (player.noItems || player.dead)
                return;

            Item[] PotionBagItems = player.GetModPlayer<tsorcRevampPlayer>().PotionBagItems;

            for (int i = 0; i < 58; i++)
            {
                CheckUseBuffPotion(player.inventory[i], player);
            }
            for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
            {
                if (!(bool)PotionBagItems[i]?.favorited)
                {
                    CheckUseBuffPotion(PotionBagItems[i], player);
                }
            }
        }

        private static void CheckUseBuffPotion(Item item, Player player)
        {
            if (player.CountBuffs() == Player.MaxBuffs)
                return;

            if (item.stack <= 0 || item.type <= 0 || item.buffType <= 0 || item.CountsAsClass(DamageClass.Summon) || item.buffType == 90)
                return;

            if(item.type == ModContent.ItemType<Items.Potions.HealingElixir>() || item.type == ModContent.ItemType<Items.Potions.HolyWarElixir>())
            {
                return;
            }

            int buffType = item.buffType;
            bool validItem = ItemLoader.CanUseItem(item, player);

            //Block using finite or inconvienent potions on accident
            if(item.type == ItemID.ObsidianSkinPotion && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                validItem = false;
            }

            if (item.type == ModContent.ItemType<Items.Potions.HolyWarElixir>())
            {
                validItem = false;
            }

            //No using potions that are already active
            for (int j = 0; j < Player.MaxBuffs; j++)
            {
                if (buffType == BuffID.FairyBlue && (player.buffType[j] == BuffID.FairyBlue || player.buffType[j] == BuffID.FairyRed || player.buffType[j] == BuffID.FairyGreen))
                {
                    validItem = false;
                    break;
                }
                if (player.buffType[j] == buffType)
                {
                    validItem = false;
                    break;
                }
                if (Main.meleeBuff[buffType] && Main.meleeBuff[player.buffType[j]])
                {
                    validItem = false;
                    break;
                }
                if (player.buffImmune[buffType]) {
                    validItem = false;
                    break;
                }
            }

            if (Main.lightPet[item.buffType] || Main.vanityPet[item.buffType])
            {
                for (int k = 0; k < Player.MaxBuffs; k++)
                {
                    if (Main.lightPet[player.buffType[k]] && Main.lightPet[item.buffType])
                        validItem = false;

                    if (Main.vanityPet[player.buffType[k]] && Main.vanityPet[item.buffType])
                        validItem = false;
                }
            }

            if (player.whoAmI == Main.myPlayer && item.type == ItemID.Carrot && !Main.runningCollectorsEdition)
                validItem = false;

            if (buffType == BuffID.FairyBlue)
            {
                buffType = Main.rand.Next(3);
                if (buffType == 0)
                    buffType = BuffID.FairyBlue;

                if (buffType == 1)
                    buffType = BuffID.FairyRed;

                if (buffType == 2)
                    buffType = BuffID.FairyGreen;
            }

            if (validItem)
            {
                UsePotion(item, player);
            }
        }

        private static void CustomQuickMana(Terraria.On_Player.orig_QuickMana orig, Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            tsorcRevampCeruleanPlayer ceruleanPlayer = player.GetModPlayer<tsorcRevampCeruleanPlayer>();
            if (modPlayer.BearerOfTheCurse && player.statMana < player.statManaMax2 && ceruleanPlayer.ceruleanChargesCurrent > 0 && !ceruleanPlayer.isCeruleanRestoring)
            {
                if (player == Main.LocalPlayer && !player.mouseInterface && ceruleanPlayer.ceruleanChargesCurrent > 0 && player.itemAnimation == 0
                && !modPlayer.isDodging && !ceruleanPlayer.isDrinking && !player.CCed)
                {
                    ceruleanPlayer.isDrinking = true;
                    ceruleanPlayer.ceruleanDrinkTimer = 0;
                    player.AddBuff(ModContent.BuffType<Crippled>(), 60 + 30);
                    player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 60 + 30);
                }
                return;
            }
            if (player.noItems || player.statMana == player.statManaMax2)
            {
                return;
            }
            Item selectedItem = player.QuickMana_GetItemToUse();
            if (player.HasItem(ModContent.ItemType<Items.Potions.SupremeManaPotion>()))
            {                
                for(int i = 0; i < player.inventory.Length; i++)
                {
                    if (player.inventory[i] != null && player.inventory[i].type == ModContent.ItemType<Items.Potions.SupremeManaPotion>())
                    {
                        selectedItem = player.inventory[i];
                    }
                }
            }
            if (selectedItem == null)
            {
                selectedItem = new Item();
                selectedItem.SetDefaults(0);
            }

            Item[] PotionBagItems = player.GetModPlayer<tsorcRevampPlayer>().PotionBagItems;

            if (PotionBagItems != null)
            {
                for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
                {
                    if (PotionBagItems[i] != null && PotionBagItems[i].type != 0 && (player.potionDelay == 0 || !PotionBagItems[i].potion) && ItemLoader.CanUseItem(PotionBagItems[i], player) && (player.GetHealMana(PotionBagItems[i], true) > player.GetHealMana(selectedItem, true)) || PotionBagItems[i].type == ModContent.ItemType<Items.Potions.SupremeManaPotion>())
                    {
                        selectedItem = PotionBagItems[i];
                    }
                }
            }

            UsePotion(selectedItem, player);
        }

        private static void CustomQuickHeal(Terraria.On_Player.orig_QuickHeal orig, Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            tsorcRevampEstusPlayer estusPlayer = player.GetModPlayer<tsorcRevampEstusPlayer>();
            tsorcRevampCeruleanPlayer ceruleanPlayer = player.GetModPlayer<tsorcRevampCeruleanPlayer>();

            if (modPlayer.BearerOfTheCurse && player.statLife < player.statLifeMax2)
            {
                if (player == Main.LocalPlayer && !player.mouseInterface && estusPlayer.estusChargesCurrent > 0 && player.itemAnimation == 0
                && !modPlayer.isDodging && !estusPlayer.isDrinking && !player.CCed)
                {
                    estusPlayer.isDrinking = true;
                    estusPlayer.estusDrinkTimer = 0;
                    player.AddBuff(ModContent.BuffType<Crippled>(), 2 * 60);
                    player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 2 * 60);
                }
                return;
            }

            if (player.noItems || player.statLife == player.statLifeMax2 || player.potionDelay > 0)
                return;
            Item selectedItem = player.QuickHeal_GetItemToUse();
            if (selectedItem == null)
            {
                selectedItem = new Item();
                selectedItem.SetDefaults(0);
            }
            Item[] PotionBagItems = modPlayer.PotionBagItems;
            if (!player.HasBuff(BuffID.PotionSickness))
            {
                for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
                {
                    if (PotionBagItems[i] != null && PotionBagItems[i].potion && ItemLoader.CanUseItem(PotionBagItems[i], player) && (selectedItem == null || player.GetHealLife(PotionBagItems[i], true) > player.GetHealLife(selectedItem, true)))
                    {
                        selectedItem = PotionBagItems[i];
                    }
                }

                UsePotion(selectedItem, player);
            }
        }

        //Generic "use item" code. Since items can be any or all of the 3 categories at once, this handles all of it.
        private static void UsePotion(Item item, Player player)
        {
            if(item.UseSound != null)
            {
                SoundEngine.PlaySound((SoundStyle)item.UseSound, player.position);
            }
            if (item.potion)
            {
                if (item.type == 227)
                {
                    player.potionDelay = player.restorationDelayTime;
                    player.AddBuff(BuffID.PotionSickness, player.potionDelay);
                }
                else
                {
                    player.potionDelay = player.potionDelayTime;
                    player.AddBuff(BuffID.PotionSickness, player.potionDelay);
                }
            }

            ItemLoader.UseItem(item, player);

            int healLife = player.GetHealLife(item, true);
            int healMana = player.GetHealMana(item, true);
            player.statLife += healLife;
            player.statMana += healMana;
            if (player.statLife > player.statLifeMax2)
                player.statLife = player.statLifeMax2;

            if (player.statMana > player.statManaMax2)
                player.statMana = player.statManaMax2;

            if (healLife > 0 && Main.myPlayer == player.whoAmI)
                player.HealEffect(healLife, true);

            if (healMana > 0)
            {
                player.AddBuff(BuffID.ManaSickness, 300);
                if (Main.myPlayer == player.whoAmI)
                    player.ManaEffect(healMana);
            }
            if(item.type == ModContent.ItemType<Items.Potions.SupremeManaPotion>())
            {
                player.AddBuff(BuffID.ManaSickness, 180);
                if (Main.myPlayer == player.whoAmI)
                    player.ManaEffect(400);
            }

            if (item.buffType > 0)
            {
                int buffTime = item.buffTime;
                if (buffTime == 0)
                {
                    buffTime = 3600;
                }
                player.AddBuff(item.buffType, buffTime);
            }


            if (ItemLoader.ConsumeItem(item, player))
                item.stack--;

            if (item.stack <= 0)
                item.TurnToAir();

            Recipe.FindRecipes();
        }

        private static void OldOnesArmyPatch(On_NPCUtils.orig_TargetClosestOldOnesInvasion orig, NPC searcher, bool faceTarget, Vector2? checkPosition)
        {
            if (!Terraria.GameContent.Events.DD2Event.Ongoing)
            {
                searcher.TargetClosest(faceTarget);
            }
            else
            {
                orig(searcher, faceTarget, checkPosition);
            }
        }

        private static void LightningBugTeleport(Terraria.On_NPC.orig_AI_111_DD2LightningBug orig, NPC self)
        {
            //Put extra things you want it to do before it runs its normal ai code here
            orig(self); //Run its normal ai
            //Put extra things you want it to do once it is done with its normal ai here. Before vs after usually doesn't matter unless you're trying to manipulate its ai somehow.
        }


        //allow spawns to be set outside a valid house (for bonfires)
        internal static void SpawnPatch(Terraria.On_Player.orig_Spawn orig, Player self, PlayerSpawnContext context)
        {
            Main.LocalPlayer.creativeInterface = false;
            self._funkytownAchievementCheckCooldown = 100;
            bool flag = false;
            if (context == PlayerSpawnContext.SpawningIntoWorld)
            {
                if (Main.netMode == 0 && self.unlockedBiomeTorches)
                {
                    NPC nPC = new NPC();
                    nPC.SetDefaults(664);
                    Main.BestiaryTracker.Kills.RegisterKill(nPC);
                }
                if (self.dead)
                {
                    //Player.AdjustRespawnTimerForWorldJoining(self);
                    {
                        if (Main.myPlayer != self.whoAmI || !self.dead) {
                            return;
                        }
                        long num = DateTime.UtcNow.ToBinary() - self.lastTimePlayerWasSaved;
                        if (num > 0) {
                            int num2 = Utils.Clamp((int)(Utils.Clamp(new TimeSpan(num).TotalSeconds, 0.0, 1000.0) * 60.0), 0, self.respawnTimer);
                            self.respawnTimer -= num2;
                            if (self.respawnTimer == 0) {
                                self.dead = false;
                            }
                        }
                    }
                    if (self.dead)
                    {
                        flag = true;
                    }
                }
            }
            self.StopVanityActions();
            if (self.whoAmI == Main.myPlayer)
            {
                Main.NotifyOfEvent(GameNotificationType.SpawnOrDeath);
            }
            if (self.whoAmI == Main.myPlayer)
            {
                if (Main.mapTime < 5)
                {
                    Main.mapTime = 5;
                }
                Main.instantBGTransitionCounter = 10;
                self.FindSpawn();
                if (!SpawnCheck(self))
                {
                    self.SpawnX = -1;
                    self.SpawnY = -1;
                }
                Main.maxQ = true;
                NPC.ResetNetOffsets();
            }
            if (Main.netMode == 1 && self.whoAmI == Main.myPlayer)
            {
                NetMessage.SendData(12, -1, -1, null, Main.myPlayer, (int)(byte)context);
            }
            if (self.whoAmI == Main.myPlayer && context == PlayerSpawnContext.SpawningIntoWorld)
            {
                self.SetPlayerDataToOutOfClassFields();
                Main.ReleaseHostAndPlayProcess();
            }
            self.headPosition = Vector2.Zero;
            self.bodyPosition = Vector2.Zero;
            self.legPosition = Vector2.Zero;
            self.headRotation = 0f;
            self.bodyRotation = 0f;
            self.legRotation = 0f;
            self.rabbitOrderFrame.Reset();
            self.lavaTime = self.lavaMax;
            if (!flag)
            {
                if (self.statLife <= 0)
                {
                    int num = self.statLifeMax2 / 2;
                    self.statLife = 100;
                    if (num > self.statLife)
                    {
                        self.statLife = num;
                    }
                    self.breath = self.breathMax;
                    self.statLife = self.statLifeMax2;
                    self.statMana = self.statManaMax2;
                }
                self.immune = true;
                if (self.dead)
                {
                    PlayerLoader.OnRespawn(self);
                }
                self.dead = false;
                self.immuneTime = 0;
            }
            self.active = true;
            Vector2 position = self.position;
            if (self.SpawnX >= 0 && self.SpawnY >= 0)
            {
                _ = self.SpawnX;
                _ = self.SpawnY;
                //self.Spawn_SetPosition(self.SpawnX, self.SpawnY);
                self.position.X = self.SpawnX * 16 + 8 - self.width / 2;
                self.position.Y = self.SpawnY * 16 - self.height;
            }
            else {
                int spawnTileX = Main.spawnTileX;
                int spawnTileY = Main.spawnTileY;
                if (!IsAreaAValidWorldSpawn(spawnTileX, spawnTileY)) {
                    bool test = false;
                    if (!test) {
                        for (int i = 0; i < 30; i++) {
                            if (IsAreaAValidWorldSpawn(spawnTileX, spawnTileY - i)) {
                                spawnTileY -= i;
                                test = true;
                                break;
                            }
                        }
                    }
                    if (!test) {
                        for (int j = 0; j < 30; j++) {
                            if (IsAreaAValidWorldSpawn(spawnTileX, spawnTileY - j)) {
                                spawnTileY -= j;
                                test = true;
                                break;
                            }
                        }
                    }
                    if (test) {
                        //self.Spawn_SetPosition(spawnTileX, spawnTileY);
                        self.position.X = spawnTileX * 16 + 8 - self.width / 2;
                        self.position.Y = spawnTileY * 16 - self.height;
                        return;
                    }
                    //self.Spawn_SetPosition(Main.spawnTileX, Main.spawnTileY);
                    self.position.X = Main.spawnTileX * 16 + 8 - self.width / 2;
                    self.position.Y = Main.spawnTileY * 16 - self.height;
                    if (!IsAreaAValidWorldSpawn(Main.spawnTileX, Main.spawnTileY)) {
                        ForceClearArea(Main.spawnTileX, Main.spawnTileY);
                    }
                }
                else {
                    //spawnTileY = Player.Spawn_DescendFromDefaultSpace(spawnTileX, spawnTileY);
                    {
                        int x = spawnTileX;
                        int y = spawnTileY;
                        for (int i = 0; i < 50; i++) {
                            bool test = false;
                            for (int j = -1; j <= 1; j++) {
                                Tile tile = Main.tile[x + j, y + i];
                                if (tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || !Main.tileSolidTop[tile.TileType])) {
                                    test = true;
                                    break;
                                }
                            }
                            if (test) {
                                y += i;
                                break;
                            }
                        }
                        spawnTileY = y;
                    }
                    //self.Spawn_SetPosition(spawnTileX, spawnTileY);
                    self.position.X = spawnTileX * 16 + 8 - self.width / 2;
                    self.position.Y = spawnTileY * 16 - self.height;
                    if (!IsAreaAValidWorldSpawn(spawnTileX, spawnTileY)) {
                        ForceClearArea(spawnTileX, spawnTileY);
                    }
                }
            }
            self.wet = false;
            self.wetCount = 0;
            self.lavaWet = false;
            self.fallStart = (int)(self.position.Y / 16f);
            self.fallStart2 = self.fallStart;
            self.velocity.X = 0f;
            self.velocity.Y = 0f;
            self.ResetAdvancedShadows();
            for (int i = 0; i < 3; i++)
            {
                self.UpdateSocialShadow();
            }
            self.oldPosition = self.position + self.BlehOldPositionFixer;
            self.SetTalkNPC(-1);
            if (self.whoAmI == Main.myPlayer)
            {
                Main.npcChatCornerItem = 0;
            }
            if (!flag)
            {
                if (self.pvpDeath)
                {
                    self.pvpDeath = false;
                    self.immuneTime = 300;
                    self.statLife = self.statLifeMax;
                }
                else
                {
                    self.immuneTime = 60;
                }
                if (self.immuneTime > 0 && !self.hostile)
                {
                    self.immuneNoBlink = true;
                }
            }
            if (self.whoAmI == Main.myPlayer)
            {
                if (context == PlayerSpawnContext.SpawningIntoWorld)
                {
                    Main.LocalGolfState.SetScoreTime();
                }
                float num2 = Vector2.Distance(position, self.position);
                Vector2 val = new Vector2((float)Main.screenWidth, (float)Main.screenHeight);
                bool flag2 = num2 < val.Length() / 2f + 100f;
                if (flag2)
                {
                    Main.SetCameraLerp(0.1f, 0);
                    flag2 = true;
                }
                else
                {
                    Main.BlackFadeIn = 255;
                    Lighting.Clear();
                    Main.screenLastPosition = Main.screenPosition;
                    Main.instantBGTransitionCounter = 10;
                }
                if (!flag2)
                {
                    Main.renderNow = true;
                }
                if (Main.netMode == 1)
                {
                    Netplay.AddCurrentServerToRecentList();
                }
                if (!flag2)
                {
                    Main.screenPosition.X = self.position.X + (float)(self.width / 2) - (float)(Main.screenWidth / 2);
                    Main.screenPosition.Y = self.position.Y + (float)(self.height / 2) - (float)(Main.screenHeight / 2);
                    self.ForceUpdateBiomes();
                }
            }
            if (flag)
            {
                self.immuneAlpha = 255;
            }
            //self.UpdateGraveyard(now: true);
            //im reflecting this. i dont even care any more.
            //if you dont want it to lag, stop dying.
            //im exhausted okay? im tired. im so tired. i never stop being tired.
            Assembly tml = self.GetType().Assembly;
            Type playerclass = null;
            foreach (Type T in tml.GetTypes()) {
                if (T.Name == "Player") {
                    playerclass = T;
                    break;
                }
            }
            MethodInfo[] methods = playerclass.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < methods.Length - 1; i++) {
                if (methods[i].Name == "UpdateGraveyard") {
                    methods[i].Invoke(self, new object[] { true });
                }
            }

            if (self.whoAmI == Main.myPlayer && context == PlayerSpawnContext.ReviveFromDeath && self.difficulty == 3)
            {
                self.AutoFinchStaff();
            }
            if (self.whoAmI == Main.myPlayer && context == PlayerSpawnContext.SpawningIntoWorld)
            {
                Main.ReleaseHostAndPlayProcess();
                self.RefreshItems(true);
                self.SetPlayerDataToOutOfClassFields();
                Main.LocalGolfState.SetScoreTime();
                Main.ActivePlayerFileData.StartPlayTimer();
                Player.Hooks.EnterWorld(self.whoAmI);
            }
        }
        
        internal static bool IsAreaAValidWorldSpawn(int floorX, int floorY) {
            for (int i = floorX - 1; i < floorX + 2; i++) {
                for (int j = floorY - 3; j < floorY; j++) {
                    if (Main.tile[i, j] != null) {
                        if (Main.tile[i, j].HasUnactuatedTile && Main.tileSolid[Main.tile[i, j].TileType] && !Main.tileSolidTop[Main.tile[i, j].TileType]) {
                            return false;
                        }
                        if (Main.tile[i, j].LiquidAmount > 0) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        internal static void ForceClearArea(int floorX, int floorY) {
            for (int i = floorX - 1; i < floorX + 2; i++) {
                for (int j = floorY - 3; j < floorY; j++) {
                    if (!(Main.tile[i, j] != null)) {
                        continue;
                    }
                    Tile tile = Main.tile[i, j];
                    if (tile.HasUnactuatedTile) {
                        bool[] tileSolid = Main.tileSolid;
                        tile = Main.tile[i, j];
                        if (tileSolid[tile.TileType]) {
                            bool[] tileSolidTop = Main.tileSolidTop;
                            tile = Main.tile[i, j];
                            if (!tileSolidTop[tile.TileType]) {
                                WorldGen.KillTile(i, j);
                            }
                        }
                    }
                    tile = Main.tile[i, j];
                    if (tile.LiquidAmount > 0) {
                        tile = Main.tile[i, j];
                        tile.LiquidType = 0;
                        tile = Main.tile[i, j];
                        tile.LiquidAmount = 0;
                        WorldGen.SquareTileFrame(i, j);
                    }
                }
            }
        }

        //Checks if a bed or bonfire is within 10 blocks of a player
        internal static bool SpawnCheck(Player self)
        {

            for (int i = -10; i < 10; i++)
            {
                for (int j = -10; j < 10; j++)
                {
                    if (i + self.SpawnX > 0 && j + self.SpawnY > 0)
                    {
                        if (Main.tile.Width > self.SpawnX && Main.tile.Height > self.SpawnY)
                        {
                            if (Main.tile[i + self.SpawnX, j + self.SpawnY] != null)
                            {
                                Tile thisTile = Main.tile[i + self.SpawnX, j + self.SpawnY];
                                if (thisTile.HasTile)
                                {
                                    if (thisTile.TileType == TileID.Beds || thisTile.TileType == ModContent.TileType<Tiles.BonfireCheckpoint>())
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        //stop moon lord from spawning after pillars are killed (adventure mode only)
        internal static void StopMoonLord(Terraria.On_WorldGen.orig_UpdateLunarApocalypse orig)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                if (!NPC.LunarApocalypseIsUp)
                {
                    return;
                }
                //bool flag = false; this bool was used to check for moon lord's core
                bool flag2 = false;
                bool flag3 = false;
                bool flag4 = false;
                bool flag5 = false;
                for (int i = 0; i < 200; i++)
                {
                    if (Main.npc[i].active)
                    {
                        switch (Main.npc[i].type)
                        {
                            /*
                            case 398:
                                flag = true;
                                break;
                            */
                            case 517:
                                flag2 = true;
                                break;
                            case 422:
                                flag3 = true;
                                break;
                            case 507:
                                flag4 = true;
                                break;
                            case 493:
                                flag5 = true;
                                break;
                        }
                    }
                }
                if (!flag2)
                {
                    NPC.TowerActiveSolar = false;
                }
                if (!flag3)
                {
                    NPC.TowerActiveVortex = false;
                }
                if (!flag4)
                {
                    NPC.TowerActiveNebula = false;
                }
                if (!flag5)
                {
                    NPC.TowerActiveStardust = false;
                }
                if (!NPC.TowerActiveSolar && !NPC.TowerActiveVortex && !NPC.TowerActiveNebula && !NPC.TowerActiveStardust/* && !flag*/)
                {
                    //WorldGen.StartImpendingDoom();
                    //recreate the effects of StartImpendingDoom, minus the part about spawning moon lord
                    NPC.LunarApocalypseIsUp = false;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        WorldGen.GetRidOfCultists();
                    }
                }

            }
            else
            {
                orig();
            }
        }

        //stop sign text from drawing when the player is too far away / does not have line of sight to the sign
        internal static void SignTextPatch(Terraria.On_Player.orig_TileInteractionsCheckLongDistance orig, Player self, int myX, int myY)
        {
            if(myX >= Main.tile.Width || myY >= Main.tile.Height || myX < 0 || myY < 0)
            {
                return;
            }
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && Main.tileSign[Main.tile[myX, myY].TileType])
            {
                if (Main.tile[myX, myY] == null)
                {
                    Main.tile[myX, myY].ClearTile();
                }
                if (!Main.tile[myX, myY].HasTile)
                {
                    return;
                }
                if (Main.tile[myX, myY].TileType == 21)
                {
                    orig(self, myX, myY);
                }
                if (Main.tile[myX, myY].TileType == 88)
                {
                    orig(self, myX, myY);
                }
                if (Main.tileSign[Main.tile[myX, myY].TileType])
                {
                    Vector2 signPos = new Vector2(myX * 16, myY * 16);
                    Vector2 toSign = signPos - self.position;
                    if (Collision.CanHitLine(self.position, 0, 0, signPos, 0, 0) && toSign.Length() < 240)
                    {
                        self.noThrow = 2;
                        int num3 = Main.tile[myX, myY].TileFrameX / 18;
                        int num4 = Main.tile[myX, myY].TileFrameY / 18;
                        num3 %= 2;
                        int num7 = myX - num3;
                        int num5 = myY - num4;
                        Main.signBubble = true;
                        Main.signX = num7 * 16 + 16;
                        Main.signY = num5 * 16;
                        int num6 = Sign.ReadSign(num7, num5);
                        if (num6 != -1)
                        {
                            Main.signHover = num6;
                            self.cursorItemIconEnabled = false;
                            self.cursorItemIconID = -1;
                        }
                    }
                }
                TileLoader.MouseOverFar(myX, myY);
            }
            else
            {
                orig(self, myX, myY);
            }
        }

        //boss zen actually zens
        internal static void BossZenPatch(Terraria.On_NPC.orig_SpawnNPC orig)
        {
            bool BossZen = false;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (!Main.player[i].active || Main.player[i].dead)
                { continue; }
                if (Main.player[i].HasBuff(ModContent.BuffType<Buffs.BossZenBuff>()))
                {
                    BossZen = true;
                    break;
                }
            }

            if (BossZen)
            {
                return; 
            }
            else
            {
                orig();
            }
        }

        internal static void DownloadMapButton(Terraria.On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
        {
            orig(self, gameTime);
            Mod mod = ModContent.GetInstance<tsorcRevamp>();
            tsorcRevamp thisMod = (tsorcRevamp)mod;

            if (Main.mouseLeftRelease)
            {
                thisMod.UICooldown = false;
            }

            if (Main.menuMode == 0)
            {
                string musicModDir = Main.SavePath + "\\Mods\\tsorcMusic.tmod";
                //This goes here so that it runs *after* the first reload has finished and the game has transitioned back to the main menu.
                //Do *not* want to initiate a second reload in the middle of the first.
                if (tsorcRevamp.ReloadNeeded)
                {
                    tsorcRevamp.EnableMusicAndReload();
                }
                if (tsorcRevamp.SpecialReloadNeeded)
                {
                    tsorcRevamp.SpecialReloadNeeded = false;
                    object[] modParam = new object[1] { "tsorcMusic" };
                    typeof(ModLoader).GetMethod("DisableMod", BindingFlags.NonPublic | BindingFlags.Static).Invoke(default, modParam);
                    typeof(ModLoader).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Static).Invoke(default, new object[] { });
                }

                //Only display this if necessary
                else if (!File.Exists(musicModDir) || tsorcRevamp.MusicNeedsUpdate)
                {
                    String musicText = "";
                    if (tsorcRevamp.DownloadingMusic)
                    {
                        musicText = "Updating Music Mod: " + (int)tsorcRevamp.MusicDownloadProgress + "%";
                    }                    
                    else if (!File.Exists(musicModDir))
                    {
                        musicText = "Click here to get the Story of Red Cloud Music Mod!";
                    }
                    else if (tsorcRevamp.MusicNeedsUpdate)
                    {
                        musicText = "Music mod update available, click here to download! (Will restart the game!)";
                    }
                    if (tsorcRevamp.musicModDownloadFailures > 0 && !tsorcRevamp.DownloadingMusic)
                    {
                        musicText = "Music Mod Download Failed, click to retry...";
                    }

                    float musicTextScale = 2;
                    Vector2 musicTextOrigin = FontAssets.MouseText.Value.MeasureString(musicText);
                    Vector2 musicTextPosition = new Vector2((Main.screenWidth / 2f), 610f);
                    Vector2 unscaledMusicTextPosition = musicTextPosition;
                    musicTextPosition *= Main.UIScale;

                    musicTextPosition.X -= musicTextOrigin.X;
                    Color musicTextColor = Main.DiscoColor;

                    if ((Main.mouseX > unscaledMusicTextPosition.X - musicTextOrigin.X && Main.mouseX < unscaledMusicTextPosition.X + (musicTextOrigin.X * musicTextScale * 0.5f)) && !tsorcRevamp.DownloadingMusic)
                    {
                        if (Main.mouseY > unscaledMusicTextPosition.Y && Main.mouseY < unscaledMusicTextPosition.Y + (musicTextOrigin.Y * musicTextScale))
                        {
                            musicTextColor = Color.Yellow;

                            if (Main.mouseLeft)
                            {
                                tsorcRevamp.MusicDownload();
                            }
                        }
                    }

                    Main.spriteBatch.Begin();
                    DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, musicText, new Vector2(musicTextPosition.X + 2, musicTextPosition.Y + 2), Color.Black, 0, Vector2.Zero, musicTextScale, SpriteEffects.None, 0);
                    DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, musicText, musicTextPosition, musicTextColor, 0, Vector2.Zero, musicTextScale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
                    Main.spriteBatch.End();
                }


                if(tsorcRevamp.MapDownloadTotalBytes > 0)
                {
                    string mapText = "Adventure Map Download In Progress: " + (int)(((float)tsorcRevamp.MapDownloadProgress / (float)tsorcRevamp.MapDownloadTotalBytes) * 100) + "%";// (" + tsorcRevamp.MapDownloadProgress + " / " + tsorcRevamp.MapDownloadTotalBytes + " bytes)";

                    if(tsorcRevamp.MapDownloadProgress == tsorcRevamp.MapDownloadTotalBytes)
                    {
                        mapText = "Adventure Map Update Complete!";
                    }

                    float musicTextScale = 2;
                    Vector2 musicTextOrigin = FontAssets.MouseText.Value.MeasureString(mapText);
                    Vector2 musicTextPosition = new Vector2((Main.screenWidth / 2f), 650f);
                    Vector2 unscaledMusicTextPosition = musicTextPosition;
                    musicTextPosition *= Main.UIScale;

                    musicTextPosition.X -= musicTextOrigin.X;
                    Color musicTextColor = Main.DiscoColor;
                   

                    Main.spriteBatch.Begin();
                    DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, mapText, new Vector2(musicTextPosition.X + 2, musicTextPosition.Y + 2), Color.Black, 0, Vector2.Zero, musicTextScale, SpriteEffects.None, 0);
                    DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, mapText, musicTextPosition, musicTextColor, 0, Vector2.Zero, musicTextScale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
                    Main.spriteBatch.End();
                }
            }

            else
            {
                thisMod.worldButtonClicked = false;
            }
        }

        internal static void BlockInvasions(Terraria.On_Main.orig_StartInvasion orig, int type)
        {
            //The game sets time to 0 at the start of the day *right* before it checks to naturally spawn invasions.
            //Only applies to adventure mode
            if (Main.time == 0 && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return;
            }
            else
            {
                orig(type);
            }
        }


        private static void DestroyerAIRevamp(Terraria.On_NPC.orig_AI_037_Destroyer orig, NPC npc)
        {
            if (npc.ai[3] > 0f)
                npc.realLife = (int)npc.ai[3];

            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead)
                npc.TargetClosest();

            if (npc.type >= 134 && npc.type <= 136)
            {
                if (npc.type == 134 || (npc.type != 134 && Main.npc[(int)npc.ai[1]].alpha < 128))
                {
                    if (npc.alpha != 0)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            int num = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 182, 0f, 0f, 100, default(Color), 2f);
                            Main.dust[num].noGravity = true;
                            Main.dust[num].noLight = true;
                        }
                    }

                    npc.alpha -= 42;
                    if (npc.alpha < 0)
                        npc.alpha = 0;
                }
            }

            if (npc.type > 134)
            {
                bool flag = false;
                if (npc.ai[1] <= 0f)
                    flag = true;
                else if (Main.npc[(int)npc.ai[1]].life <= 0)
                    flag = true;

                if (flag)
                {
                    //Slightly more dramatic death
                    for (int j = 0; j < 2; j++)
                    {
                        Vector2 dustVel = npc.velocity + Main.rand.NextVector2Circular(14, 14);

                        Dust.NewDustPerfect(npc.Center, DustID.Torch, dustVel, Scale: 6).noGravity = true;
                        Dust thisDust = Dust.NewDustPerfect(npc.Center, 130, dustVel * 2f, Scale: 3.5f);
                        thisDust.shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
                    }

                    npc.life = 0;
                    npc.HitEffect();
                    npc.checkDead();
                }
            }


            //Spawn segments
            if (Main.netMode != 1)
            {
                if (npc.ai[0] == 0f && npc.type == 134)
                {
                    npc.ai[3] = npc.whoAmI;
                    npc.realLife = npc.whoAmI;
                    int num2 = 0;
                    int num3 = npc.whoAmI;
                    int num4 = 80;
                    for (int j = 0; j <= num4; j++)
                    {
                        int num5 = 135;
                        if (j == num4)
                            num5 = 136;

                        //Spawn probes
                        num2 = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.position.X + (float)(npc.width / 2)), (int)(npc.position.Y + (float)npc.height), num5, npc.whoAmI);
                        Main.npc[num2].ai[3] = npc.whoAmI;
                        Main.npc[num2].realLife = npc.whoAmI;
                        Main.npc[num2].ai[1] = num3;
                        Main.npc[num3].ai[0] = num2;
                        NetMessage.SendData(23, -1, -1, null, num2);
                        num3 = num2;
                    }
                }
            }

            float num17 = 16f;
            bool flag2 = false;
            if (Main.dayTime || Main.player[npc.target].dead)
            {
                flag2 = false;
                npc.velocity.Y += 1f;
                if ((double)npc.position.Y > Main.worldSurface * 16.0)
                {
                    npc.velocity.Y += 1f;
                    num17 = 32f;
                }

                if ((double)npc.position.Y > Main.rockLayer * 16.0)
                {
                    for (int n = 0; n < 200; n++)
                    {
                        if (Main.npc[n].aiStyle == npc.aiStyle)
                            Main.npc[n].active = false;
                    }
                }
            }


            int num12 = (int)(npc.position.X / 16f) - 1;
            int num13 = (int)((npc.position.X + (float)npc.width) / 16f) + 2;
            int num14 = (int)(npc.position.Y / 16f) - 1;
            int num15 = (int)((npc.position.Y + (float)npc.height) / 16f) + 2;
            if (num12 < 0)
                num12 = 0;

            if (num13 > Main.maxTilesX)
                num13 = Main.maxTilesX;

            if (num14 < 0)
                num14 = 0;

            if (num15 > Main.maxTilesY)
                num15 = Main.maxTilesY;

            if (!flag2)
            {
                Vector2 vector2 = default(Vector2);
                for (int k = num12; k < num13; k++)
                {
                    for (int l = num14; l < num15; l++)
                    {
                        if (Main.tile[k, l] != null && ((Main.tile[k, l].HasUnactuatedTile && (Main.tileSolid[Main.tile[k, l].TileType] || (Main.tileSolidTop[Main.tile[k, l].TileType] && Main.tile[k, l].TileFrameY == 0))) || Main.tile[k, l].LiquidAmount > 64))
                        {
                            vector2.X = k * 16;
                            vector2.Y = l * 16;
                            if (npc.position.X + (float)npc.width > vector2.X && npc.position.X < vector2.X + 16f && npc.position.Y + (float)npc.height > vector2.Y && npc.position.Y < vector2.Y + 16f)
                            {
                                flag2 = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (!flag2)
            {
                if (npc.type != 135 || npc.ai[2] != 1f)
                    Lighting.AddLight((int)((npc.position.X + (float)(npc.width / 2)) / 16f), (int)((npc.position.Y + (float)(npc.height / 2)) / 16f), 0.3f, 0.1f, 0.05f);

                npc.localAI[1] = 1f;
                if (npc.type == 134)
                {
                    Rectangle rectangle = new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height);
                    int num16 = 1000;
                    bool flag3 = true;
                    if (npc.position.Y > Main.player[npc.target].position.Y)
                    {
                        for (int m = 0; m < 255; m++)
                        {
                            if (Main.player[m].active)
                            {
                                Rectangle rectangle2 = new Rectangle((int)Main.player[m].position.X - num16, (int)Main.player[m].position.Y - num16, num16 * 2, num16 * 2);
                                if (rectangle.Intersects(rectangle2))
                                {
                                    flag3 = false;
                                    break;
                                }
                            }
                        }

                        if (flag3)
                            flag2 = true;
                    }
                }
            }
            else
            {
                npc.localAI[1] = 0f;
            }


            float num18 = 0.1f;
            float num19 = 0.15f;
            Vector2 vector3 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
            float num20 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2);
            float num21 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2);
            num20 = (int)(num20 / 16f) * 16;
            num21 = (int)(num21 / 16f) * 16;
            vector3.X = (int)(vector3.X / 16f) * 16;
            vector3.Y = (int)(vector3.Y / 16f) * 16;
            num20 -= vector3.X;
            num21 -= vector3.Y;
            float num22 = (float)Math.Sqrt(num20 * num20 + num21 * num21);
            if (npc.ai[1] > 0f && npc.ai[1] < (float)Main.npc.Length)
            {
                try
                {
                    vector3 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                    num20 = Main.npc[(int)npc.ai[1]].position.X + (float)(Main.npc[(int)npc.ai[1]].width / 2) - vector3.X;
                    num21 = Main.npc[(int)npc.ai[1]].position.Y + (float)(Main.npc[(int)npc.ai[1]].height / 2) - vector3.Y;
                }
                catch
                {
                }

                npc.rotation = (float)Math.Atan2(num21, num20) + 1.57f;
                num22 = (float)Math.Sqrt(num20 * num20 + num21 * num21);
                int num23 = (int)(44f * npc.scale);
                num22 = (num22 - (float)num23) / num22;
                num20 *= num22;
                num21 *= num22;
                npc.velocity = Vector2.Zero;
                npc.position.X += num20;
                npc.position.Y += num21;
                return;
            }

            if (!flag2)
            {
                npc.TargetClosest();
                npc.velocity.Y += 0.15f;
                if (npc.velocity.Y > num17)
                    npc.velocity.Y = num17;

                if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)num17 * 0.4)
                {
                    if (npc.velocity.X < 0f)
                        npc.velocity.X -= num18 * 1.1f;
                    else
                        npc.velocity.X += num18 * 1.1f;
                }
                else if (npc.velocity.Y == num17)
                {
                    if (npc.velocity.X < num20)
                        npc.velocity.X += num18;
                    else if (npc.velocity.X > num20)
                        npc.velocity.X -= num18;
                }
                else if (npc.velocity.Y > 4f)
                {
                    if (npc.velocity.X < 0f)
                        npc.velocity.X += num18 * 0.9f;
                    else
                        npc.velocity.X -= num18 * 0.9f;
                }
            }
            else
            {
                if (npc.soundDelay == 0)
                {
                    float num24 = num22 / 40f;
                    if (num24 < 10f)
                        num24 = 10f;

                    if (num24 > 20f)
                        num24 = 20f;

                    npc.soundDelay = (int)num24;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, npc.position);
                }

                num22 = (float)Math.Sqrt(num20 * num20 + num21 * num21);
                float num25 = Math.Abs(num20);
                float num26 = Math.Abs(num21);
                float num27 = num17 / num22;
                num20 *= num27;
                num21 *= num27;
                if (((npc.velocity.X > 0f && num20 > 0f) || (npc.velocity.X < 0f && num20 < 0f)) && ((npc.velocity.Y > 0f && num21 > 0f) || (npc.velocity.Y < 0f && num21 < 0f)))
                {
                    if (npc.velocity.X < num20)
                        npc.velocity.X += num19;
                    else if (npc.velocity.X > num20)
                        npc.velocity.X -= num19;

                    if (npc.velocity.Y < num21)
                        npc.velocity.Y += num19;
                    else if (npc.velocity.Y > num21)
                        npc.velocity.Y -= num19;
                }

                if ((npc.velocity.X > 0f && num20 > 0f) || (npc.velocity.X < 0f && num20 < 0f) || (npc.velocity.Y > 0f && num21 > 0f) || (npc.velocity.Y < 0f && num21 < 0f))
                {
                    if (npc.velocity.X < num20)
                        npc.velocity.X += num18;
                    else if (npc.velocity.X > num20)
                        npc.velocity.X -= num18;

                    if (npc.velocity.Y < num21)
                        npc.velocity.Y += num18;
                    else if (npc.velocity.Y > num21)
                        npc.velocity.Y -= num18;

                    if ((double)Math.Abs(num21) < (double)num17 * 0.2 && ((npc.velocity.X > 0f && num20 < 0f) || (npc.velocity.X < 0f && num20 > 0f)))
                    {
                        if (npc.velocity.Y > 0f)
                            npc.velocity.Y += num18 * 2f;
                        else
                            npc.velocity.Y -= num18 * 2f;
                    }

                    if ((double)Math.Abs(num20) < (double)num17 * 0.2 && ((npc.velocity.Y > 0f && num21 < 0f) || (npc.velocity.Y < 0f && num21 > 0f)))
                    {
                        if (npc.velocity.X > 0f)
                            npc.velocity.X += num18 * 2f;
                        else
                            npc.velocity.X -= num18 * 2f;
                    }
                }
                else if (num25 > num26)
                {
                    if (npc.velocity.X < num20)
                        npc.velocity.X += num18 * 1.1f;
                    else if (npc.velocity.X > num20)
                        npc.velocity.X -= num18 * 1.1f;

                    if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)num17 * 0.5)
                    {
                        if (npc.velocity.Y > 0f)
                            npc.velocity.Y += num18;
                        else
                            npc.velocity.Y -= num18;
                    }
                }
                else
                {
                    if (npc.velocity.Y < num21)
                        npc.velocity.Y += num18 * 1.1f;
                    else if (npc.velocity.Y > num21)
                        npc.velocity.Y -= num18 * 1.1f;

                    if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)num17 * 0.5)
                    {
                        if (npc.velocity.X > 0f)
                            npc.velocity.X += num18;
                        else
                            npc.velocity.X -= num18;
                    }
                }
            }

            npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
            if (npc.type != 134)
                return;

            if (flag2)
            {
                if (npc.localAI[0] != 1f)
                    npc.netUpdate = true;

                npc.localAI[0] = 1f;
            }
            else
            {
                if (npc.localAI[0] != 0f)
                    npc.netUpdate = true;

                npc.localAI[0] = 0f;
            }

            if (((npc.velocity.X > 0f && npc.oldVelocity.X < 0f) || (npc.velocity.X < 0f && npc.oldVelocity.X > 0f) || (npc.velocity.Y > 0f && npc.oldVelocity.Y < 0f) || (npc.velocity.Y < 0f && npc.oldVelocity.Y > 0f)) && !npc.justHit)
                npc.netUpdate = true;





        }


        internal static bool HasWormholePotion(Terraria.On_Player.orig_HasUnityPotion orig, Player self)
        {
            bool hasWormhole = false;
            for (int i = 0; i < 58; i++)
            {
                if (self.inventory[i].type == ItemID.WormholePotion && self.inventory[i].stack > 0)
                {
                    hasWormhole = true;
                    break;
                }
            }

            if (!hasWormhole)
            {
                for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
                {
                    if (self.GetModPlayer<tsorcRevampPlayer>().PotionBagItems[i].type == ItemID.WormholePotion)
                    {
                        hasWormhole = true;
                        break;
                    }
                }
            }

            return hasWormhole;
        }

        internal static void ConsumeWormholePotion(Terraria.On_Player.orig_TakeUnityPotion orig, Player self)
        {
            int wormholeSlot = 0;
            bool potionBag = false;

            for (int i = 0; i < 58; i++)
            {
                if (self.inventory[i].type == ItemID.WormholePotion && self.inventory[i].stack > 0)
                {
                    wormholeSlot = i;
                    break;
                }
            }

            if (wormholeSlot == 0)
            {
                for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
                {
                    if (self.GetModPlayer<tsorcRevampPlayer>().PotionBagItems[i].type == ItemID.WormholePotion)
                    {
                        wormholeSlot = i;
                        potionBag = true;
                        break;
                    }
                }
            }

            Item wormholePotion;

            if (potionBag)
            {
                wormholePotion = self.GetModPlayer<tsorcRevampPlayer>().PotionBagItems[wormholeSlot];
            }
            else
            {
                wormholePotion = self.inventory[wormholeSlot];
            }

            if (ItemLoader.ConsumeItem(wormholePotion, self))
            {
                wormholePotion.stack--;
            }
            if (wormholePotion.stack <= 0)
            {
                wormholePotion.TurnToAir();
            }

        }

        
        private static void PaperMarioMode(Terraria.DataStructures.On_PlayerDrawLayers.orig_DrawPlayer_TransformDrawData orig, ref Terraria.DataStructures.PlayerDrawSet drawinfo) {
            orig(ref drawinfo);
            for (int k = 0; k < drawinfo.DrawDataCache.Count; k++) {
                drawinfo.DrawDataCache[k] = ManipulateDrawInfo(drawinfo.DrawDataCache[k], drawinfo.drawPlayer);
            }
        }        
        //allow the soul slot to be included in recipes
        private static void Recipe_CollectItemsToCraftWithFrom(On_Recipe.orig_CollectItemsToCraftWithFrom orig, Player player)
        {
            orig(player);
            Item souls = player.GetModPlayer<tsorcRevampPlayer>().SoulSlot.Item;
            if (souls != null && souls.stack >= 1)
            {
                Item[] asArray = { souls };

                Assembly tml = player.GetType().Assembly;
                Type recipeClass = null;
                foreach (Type T in tml.GetTypes())
                {
                    if (T.Name == "Recipe")
                    {
                        recipeClass = T;
                        break;
                    }
                }

                MethodInfo mi = recipeClass.GetMethod("CollectItems", BindingFlags.NonPublic | BindingFlags.Static, new Type[] { typeof(Item[]), typeof(int) });
                mi.Invoke(default, new object[] { asArray, 1 });
            }
        }
        private static DrawData ManipulateDrawInfo(DrawData input, Player Player) {
            float rotation = Player.GetModPlayer<tsorcRevampPlayer>().rotation3d;

            if (rotation != 0) 
            {
                float sin = (float)Math.Sin(rotation + 1.57f * Player.direction);
                int offset = Math.Abs((int)((input.useDestinationRectangle ? input.destinationRectangle.Width : input.sourceRect?.Width ?? input.texture.Width) * sin));

                SpriteEffects effect = sin > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                if (input.effect == SpriteEffects.FlipHorizontally) effect = effect == SpriteEffects.FlipHorizontally ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                DrawData newData = new(input.texture, new Rectangle((int)input.position.X, (int)input.position.Y, offset, input.useDestinationRectangle ? input.destinationRectangle.Height : input.sourceRect?.Height ?? input.texture.Height),
                    input.sourceRect, input.color, input.rotation, input.origin, effect, 0) {
                    shader = input.shader
                };

                return newData;
            }

            return input;
        }

    }
}
