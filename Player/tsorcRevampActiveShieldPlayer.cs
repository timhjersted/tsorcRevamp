using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp
{
    /// <summary>
    /// Active Shields Revamp — held-RMB Dark-Souls-style blocking.
    /// Hold right mouse (with an eligible shield equipped and a weapon that has no right-click function,
    /// while Unkindled or Bearer of the Curse and the config toggle is on) to raise the shield and fully
    /// negate frontal hits at the cost of stamina (or mana and stamina, for magic wards). Running out of the resource
    /// on a hit is a "guard break": the hit is still blocked, but you suffer Ichor, Slow, reduced regen,
    /// and a brief lockout.
    /// </summary>
    public class tsorcRevampActiveShieldPlayer : ModPlayer
    {
        /// <summary>True while the player is actively holding up a shield this frame (local-authoritative).</summary>
        public bool isBlocking;
        /// <summary>The item type of the shield currently being used to block, or -1.</summary>
        public int activeShieldType = -1;
        /// <summary>Frames remaining during which the player cannot raise a shield (set on guard break).</summary>
        public int blockLockTimer;

        // Last values pushed to the network, so we only send a packet when the block state actually changes.
        private bool lastSyncedBlocking;
        private int lastSyncedShield = -1;

        // True while we've forced Player.shield to draw a 2nd-slot shield's sprite (so we can clear it after).
        private bool shieldDrawForcedBySlot;

        // True from the start of a Shield of Cthulhu dash until it ends (hit an enemy / slowed down) — keeps the
        // shield drawn for the whole dash instead of vanishing the instant the brief eocDash timer expires.
        private bool dashShield;
        private int dashShieldFrames;
        /// <summary>True while a Shield of Cthulhu dash is in progress (used by the draw layer to render the
        /// dash shield for a 2nd-slot EoC, which vanilla only draws for an accessory-slot one).</summary>
        public bool DashShieldActive => dashShield;

        // Block hold duration + which shield was held, for release-triggered effects (mana ward pulse / bolts).
        private int blockHoldFrames;
        private int heldShieldType = -1;

        // Dragon Crest sustained fire-breath state (local player only; see UpdateFireBreath).
        private bool fireBreathActive;      // burns until the guard drops or mana runs out — no duration cap
        private int fireBreathFlameTimer;   // frames until the next flame spawns
        private float fireBreathManaAccum;  // fractional mana carried between frames

        // Right-Click (2nd) slot use: while active, the slot item is temporarily the held item and is driven by RMB.
        public bool usingSecondSlotItem;
        private int swappedSlotIndex = -1;     // the hotbar index we swapped the slot item into
        private Item secondSlotStoredItem;     // the hotbar item that was there before the swap

        public static bool RevampActive => ModContent.GetInstance<tsorcRevampConfig>().ActiveShieldsRevamp;

        /// <summary>The item in this player's Right-Click (2nd) slot, or null.</summary>
        private Item RightClickItem => Player.GetModPlayer<tsorcRevampPlayer>().RightClickSlot?.Item;
        private bool SecondSlotControlHeld => tsorcRevamp.SecondSlotKey?.Current ?? Player.controlUseTile;
        private bool RightClickConflictActive => Player.controlUseTile;

        /// <summary>True when the Active Shields Revamp governs this player's shields (config on + SoulsMode).
        /// Shields read this to decide whether to apply their classic passive bonuses or defer to active blocking.</summary>
        public static bool ActiveFor(Player player) => RevampActive && player.GetModPlayer<tsorcRevampPlayer>().SoulsMode;

        public override void PreUpdate()
        {
            if (blockLockTimer > 0)
            {
                blockLockTimer--;
            }
        }

        // ProcessTriggers is only called for the local player and runs once inputs are gathered,
        // so the dedicated 2nd Slot keybind is valid here.
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            // Resolve the Right-Click (2nd) slot use first; it gates blocking below.
            UpdateSecondSlotUse();

            isBlocking = ComputeBlocking();
            if (!isBlocking)
            {
                activeShieldType = -1;
            }

            // Track hold duration; fire release effects (mana-ward pulse / Celestriad bolts) when the guard drops.
            if (isBlocking)
            {
                blockHoldFrames++;
                heldShieldType = activeShieldType;
                // One-time white twinkle the instant a Celestriad ward reaches its 2s charge, signalling the
                // seeking-bolt volley is now armed (it fires on release, see OnBlockReleased).
                if (blockHoldFrames == CelestriadChargeFrames
                    && activeShieldType == ModContent.ItemType<Items.Accessories.Defensive.Celestriad>()
                    && Player.whoAmI == Main.myPlayer)
                {
                    SpawnChargeTwinkle();
                }
            }
            else
            {
                if (blockHoldFrames > 0 && heldShieldType != -1)
                {
                    OnBlockReleased(heldShieldType, blockHoldFrames);
                }
                blockHoldFrames = 0;
                heldShieldType = -1;
            }
            // NOTE: the "no stamina regen while blocking" effect is applied directly in
            // tsorcRevampStaminaPlayer.UpdateResource (reading isBlocking), which is the only place
            // robust to hook ordering — setting staminaResourceGainMult from a ModPlayer hook here
            // raced the stamina system's regen calc and never took effect.

            // In multiplayer, tell everyone else when our block state changes so they can draw the raised
            // shield and the server can run the body-block authoritatively.
            if (Main.netMode != NetmodeID.SinglePlayer
                && (isBlocking != lastSyncedBlocking || activeShieldType != lastSyncedShield))
            {
                SendBlockState();
                lastSyncedBlocking = isBlocking;
                lastSyncedShield = activeShieldType;
            }
        }

        private void SendBlockState()
        {
            ModPacket packet = ModContent.GetInstance<tsorcRevamp>().GetPacket();
            packet.Write(tsorcPacketID.SyncActiveShield);
            packet.Write((byte)Player.whoAmI);
            packet.Write(isBlocking);
            packet.Write(activeShieldType);
            packet.Send();
        }

        private bool ComputeBlocking()
        {
            if (!RevampActive)
            {
                return false;
            }
            if (!Player.GetModPlayer<tsorcRevampPlayer>().SoulsMode)
            {
                return false;
            }
            if (blockLockTimer > 0 || Player.dead || Player.CCed)
            {
                return false;
            }
            // Can't raise a shield while guard-broken (the debuff lasts longer than the brief blockLockTimer).
            if (Player.HasBuff(ModContent.BuffType<ShieldGuardBreak>()))
            {
                return false;
            }
            // Can't raise a guard you can't pay for. Without this, blocking while empty or in stamina debt
            // would guard-break on every single hit, which compounds into a death spiral — and "the shield
            // won't come up" reads far more clearly than "the shield breaks instantly, repeatedly".
            if (Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent <= 0f)
            {
                return false;
            }
            // Using a (non-shield) Right-Click slot item takes RMB; can't also block.
            if (usingSecondSlotItem)
            {
                return false;
            }

            // 2nd Slot key held...
            if (!SecondSlotControlHeld)
            {
                return false;
            }
            // ...but only if the held weapon has no right-click (alt) function of its own (weapon alt-fire wins).
            if (RightClickConflictActive && Player.HeldItem != null && !Player.HeldItem.IsAir && ItemLoader.AltFunctionUse(Player.HeldItem, Player))
            {
                return false;
            }

            // Priority for which shield raises: a shield in the Right-Click slot beats an accessory-slot shield.
            // A non-shield item in the slot means RMB is a slot-use (handled above), so don't block here.
            // Note: I kept this logic in place in case the following changes but currently only 1 shield can be equipped at a time  
            ActiveShieldData data;
            int shield;
            Item slotItem = RightClickItem;
            if (slotItem != null && !slotItem.IsAir)
            {
                if (tsorcRevamp.ActiveShieldRegistry.TryGetValue(slotItem.type, out data))
                {
                    shield = slotItem.type;
                }
                else
                {
                    return false; // slot holds a non-shield usable item → slot-use owns RMB
                }
            }
            else
            {
                shield = FindBestEquippedShield(out data);
            }
            if (shield == -1)
            {
                return false;
            }

            // Need at least some of the relevant resource to raise the shield at all.
            if (data.Resource == ShieldResource.Stamina)
            {
                if (Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent <= 0)
                {
                    return false;
                }
            }
            else if (Player.statMana <= 0)
            {
                return false;
            }

            activeShieldType = shield;
            return true;
        }

        /// <summary>Finds the equipped shield with the lowest base cost (the "best" one). Returns -1 if none.</summary>
        private int FindBestEquippedShield(out ActiveShieldData bestData)
        {
            bestData = default;
            int best = -1;
            float bestBase = float.MaxValue;
            int slots = 8 + Player.extraAccessorySlots;
            for (int i = 3; i < slots; i++)
            {
                Item acc = Player.armor[i];
                if (acc == null || acc.IsAir)
                {
                    continue;
                }
                if (tsorcRevamp.ActiveShieldRegistry.TryGetValue(acc.type, out ActiveShieldData data) && data.BaseCost < bestBase)
                {
                    best = acc.type;
                    bestBase = data.BaseCost;
                    bestData = data;
                }
            }
            return best;
        }

        /// <summary>Finds an equipped (accessory slot or Right-Click slot) item of the given type and returns its
        /// shield draw slot, or -1 if it isn't equipped / has no shield sprite. Falls back to the item's content
        /// sample when a particular instance reports no slot (e.g. a vanilla shield whose instance shieldSlot is
        /// left at -1 outside of an equip slot).</summary>
        private int GetEquippedShieldSlot(int itemType)
        {
            int slots = 8 + Player.extraAccessorySlots;
            for (int i = 3; i < slots; i++)
            {
                Item acc = Player.armor[i];
                if (acc != null && !acc.IsAir && acc.type == itemType)
                {
                    return ResolveShieldSlot(acc);
                }
            }
            Item rc = RightClickItem;
            if (rc != null && !rc.IsAir && rc.type == itemType)
            {
                return ResolveShieldSlot(rc);
            }
            return -1;
        }

        private static int ResolveShieldSlot(Item item)
        {
            if (item.shieldSlot >= 0)
            {
                return item.shieldSlot;
            }
            if (item.type > 0 && ContentSamples.ItemsByType.ContainsKey(item.type))
            {
                return ContentSamples.ItemsByType[item.type].shieldSlot;
            }
            return -1;
        }

        // --- Right-Click (2nd) slot item use ---
        // A non-shield item in the 2nd slot is "used" on right click by temporarily swapping it into the hand and
        // driving it with RMB (reusing vanilla's whole use pipeline: animation, ammo, mana, stamina, autoswing).
        // Shields in the slot are handled by the block system above, not here.

        private void UpdateSecondSlotUse()
        {
            if (Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            if (usingSecondSlotItem)
            {
                // Abort if the situation changes out from under us.
                if (!RevampActive || !Player.GetModPlayer<tsorcRevampPlayer>().SoulsMode
                    || Main.playerInventory || Player.dead || Player.selectedItem != swappedSlotIndex
                    || (RightClickConflictActive && IsVanillaContextRightClickAvailable()))
                {
                    EndSecondSlotUse();
                    return;
                }

                // The 2nd Slot key is the sole driver, so a stray left click can't also fire the item.
                Player.controlUseItem = SecondSlotControlHeld;

                // End once the button is released and any in-progress swing/throw finishes.
                if (!SecondSlotControlHeld && Player.itemAnimation == 0)
                {
                    EndSecondSlotUse();
                }
                return;
            }

            // --- conditions to begin a use ---
            if (!RevampActive || !Player.GetModPlayer<tsorcRevampPlayer>().SoulsMode)
            {
                return;
            }
            if (Main.playerInventory || Player.dead || Player.CCed || blockLockTimer > 0)
            {
                return;
            }
            if (!SecondSlotControlHeld)
            {
                return; // 2nd Slot control not held
            }
            if (Player.itemAnimation > 0)
            {
                return; // can't right-click while a left-click action is already in progress
            }
            // Vanilla contextual right-clicks (talking to NPCs, opening nearby interactible projectiles like the
            // Void Bag, tile interactions, etc.) take priority over the 2nd slot.
            if (RightClickConflictActive && IsVanillaContextRightClickAvailable())
            {
                return;
            }
            // Held-weapon alt-fire takes priority over the slot.
            if (RightClickConflictActive && Player.HeldItem != null && !Player.HeldItem.IsAir && ItemLoader.AltFunctionUse(Player.HeldItem, Player))
            {
                return;
            }
            Item slotItem = RightClickItem;
            if (slotItem == null || slotItem.IsAir)
            {
                return;
            }
            // Shields are raised/blocked via the block system, not a one-shot use.
            if (tsorcRevamp.ActiveShieldRegistry != null && tsorcRevamp.ActiveShieldRegistry.ContainsKey(slotItem.type))
            {
                return;
            }
            // Must be an actually-usable item.
            if (slotItem.useStyle == ItemUseStyleID.None && slotItem.damage <= 0 && !slotItem.consumable)
            {
                return;
            }

            BeginSecondSlotUse();
        }

        private bool IsVanillaContextRightClickAvailable()
        {
            if (IsHoveringTalkableNPC() || IsHoveringInteractibleProjectile())
            {
                return true;
            }

            Player.SmartInteractLookup();

            if (!Main.SmartInteractShowingGenuine)
            {
                return false;
            }

            return Main.SmartInteractNPC >= 0
                || Main.SmartInteractProj >= 0
                || Main.SmartInteractPotionOfReturn
                || Main.HasInteractibleObjectThatIsNotATile
                || (Main.SmartInteractTileCoordsSelected != null && Main.SmartInteractTileCoordsSelected.Count > 0);
        }

        private bool IsHoveringTalkableNPC()
        {
            const float TalkRange = 400f;
            Point mouseWorld = Main.MouseWorld.ToPoint();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc == null || !npc.active || !npc.CanBeTalkedTo)
                {
                    continue;
                }
                if (!npc.Hitbox.Contains(mouseWorld))
                {
                    continue;
                }
                if (Player.Distance(npc.Center) <= TalkRange)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsHoveringInteractibleProjectile()
        {
            Point mouseWorld = Main.MouseWorld.ToPoint();
            Vector2 playerCenter = Player.Center;

            Player.UpdateNearbyInteractibleProjectilesList();
            foreach (int projectileIndex in Player.GetListOfProjectilesToInteractWithHack())
            {
                if (projectileIndex < 0 || projectileIndex >= Main.maxProjectiles)
                {
                    continue;
                }

                Projectile projectile = Main.projectile[projectileIndex];
                if (projectile == null || !projectile.active || !projectile.IsInteractible())
                {
                    continue;
                }
                Rectangle hoverHitbox = projectile.Hitbox;
                hoverHitbox.Inflate(16, 16);
                if (!hoverHitbox.Contains(mouseWorld))
                {
                    continue;
                }
                if (Player.IsProjectileInteractibleAndInInteractionRange(projectile, ref playerCenter))
                {
                    return true;
                }
            }

            return false;
        }

        private void BeginSecondSlotUse()
        {
            tsorcRevampPlayer modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
            swappedSlotIndex = Player.selectedItem;
            secondSlotStoredItem = Player.inventory[swappedSlotIndex];
            // The slot item becomes the held item. The slot keeps referencing the same Item object, so saves/sync
            // see it logically in the slot; EndSecondSlotUse reads it back afterwards.
            Player.inventory[swappedSlotIndex] = modPlayer.RightClickSlot.Item;
            usingSecondSlotItem = true;
            Player.controlUseItem = SecondSlotControlHeld; // fire on this frame too
        }

        public void EndSecondSlotUse()
        {
            if (!usingSecondSlotItem)
            {
                return;
            }
            if (swappedSlotIndex >= 0 && swappedSlotIndex < Player.inventory.Length)
            {
                tsorcRevampPlayer modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
                // The (possibly used/decremented) item returns to the slot; the original hotbar item is restored.
                modPlayer.RightClickSlot.Item = Player.inventory[swappedSlotIndex];
                Player.inventory[swappedSlotIndex] = secondSlotStoredItem ?? new Item();
            }
            usingSecondSlotItem = false;
            swappedSlotIndex = -1;
            secondSlotStoredItem = null;
        }

        // Undo the swap before the character is saved so a mid-use save can never persist the swapped/duplicated state.
        public override void PreSavePlayer()
        {
            EndSecondSlotUse();
        }

        // A shield/ward sitting in the Right-Click (2nd) slot grants its passive effects too (no need to raise it),
        // by running its equip logic. Mod shields run ModItem.UpdateEquip (utility immunities + small active
        // defense; %DR/penalties gated off inside each shield in active mode). Vanilla shields have no ModItem
        // hook, so they're run through Player.ApplyEquipFunctional to grant their full native passive (immunities,
        // defense) — so any shield in the 2nd slot behaves like it would in an accessory slot.
        public override void PostUpdateEquips()
        {
            if (!RevampActive || !Player.GetModPlayer<tsorcRevampPlayer>().SoulsMode || usingSecondSlotItem)
            {
                return;
            }
            Item rc = RightClickItem;
            if (rc == null || rc.IsAir
                || tsorcRevamp.ActiveShieldRegistry == null || !tsorcRevamp.ActiveShieldRegistry.TryGetValue(rc.type, out ActiveShieldData data))
            {
                return;
            }

            // Vanilla grants the Shield of Cthulhu dash only from an accessory slot; replicate it for the 2nd slot.
            if (rc.type == ItemID.EoCShield && Player.dashType == 0)
            {
                Player.dashType = 1; // 1 = Shield of Cthulhu dash
            }

            if (rc.ModItem != null)
            {
                rc.ModItem.UpdateEquip(Player);            // mod shields: utility immunities (DR/penalties gated off in active mode)
                Player.statDefense += data.ActiveDefense;  // small active-mode defense (no auto-defense for a non-accessory slot)
            }
            else
            {
                // Vanilla shields have no ModItem hook, so run their full accessory passive the same way an
                // accessory slot would (debuff/fire/knockback immunities, native defense, etc.). This makes a
                // vanilla shield in the 2nd slot behave like one worn in an accessory slot — block + native
                // passive — instead of block-only.
                Player.ApplyEquipFunctional(rc, true);
            }

            // Greatshield equip-load tax, applied for BOTH branches. Neither path reaches
            // ActiveShieldGlobalItem.UpdateEquip (which handles the accessory slots): the mod branch calls
            // ModItem.UpdateEquip directly, and ApplyEquipFunctional only invokes ItemLoader.UpdateAccessory,
            // not UpdateEquip. So this is the sole application point for a 2nd-slot shield — no double-dip.
            if (data.PassiveRegenPenalty > 0f)
            {
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult -= data.PassiveRegenPenalty;
            }
        }

        public override bool FreeDodge(Player.HurtInfo info)
        {
            if (!isBlocking || activeShieldType == -1)
            {
                return false;
            }
            if (!tsorcRevamp.ActiveShieldRegistry.TryGetValue(activeShieldType, out ActiveShieldData data))
            {
                return false;
            }

            // Physical shields cover the front + top + bottom (everything but the rear): we can't aim the shield
            // vertically, so a hit coming more from above/below than the side is always blocked, while a side-
            // dominant hit must come from the direction the player faces. Mana wards are 360° and skip this.
            if (data.Resource != ShieldResource.Mana)
            {
                if (TryGetAttackerCenter(info, out Vector2 atkCenter))
                {
                    float dx = atkCenter.X - Player.Center.X;
                    float dy = atkCenter.Y - Player.Center.Y;
                    if (Math.Abs(dx) > Math.Abs(dy) && Math.Sign(dx) != Player.direction)
                    {
                        return false; // side-dominant hit from behind
                    }
                }
                else if (info.HitDirection != 0 && Player.direction != -info.HitDirection)
                {
                    return false; // fallback when the attacker position can't be resolved
                }
            }

            // Only block hits from a real attacker (NPC contact, hostile projectile, or another player).
            // Environmental damage (lava, drowning, fall) and DoT debuffs are not blockable.
            Terraria.DataStructures.PlayerDeathReason src = info.DamageSource;
            bool blockable = src.SourceNPCIndex >= 0
                             || src.SourceProjectileLocalIndex >= 0
                             || src.SourceProjectileType > 0
                             || info.PvP
                             || src.SourcePlayerIndex >= 0;
            if (!blockable)
            {
                return false;
            }

            // Perfect parry: if the shield was raised only a handful of frames before this hit landed, the block
            // is "free" — no resource spent and no chance of a guard break. We can't see damage coming in advance,
            // but blockHoldFrames (set in ProcessTriggers earlier this frame) tells us how long the guard has been
            // up, so a hit that arrives right after you raise rewards the timing. Applies to every shield.
            bool perfectParry = blockHoldFrames <= PerfectParryWindow;
            bool guardBroke = false;

            if (perfectParry)
            {
                // A perfectly-timed raise still spends resources, just far less (see PerfectParryCostMult) — so it's
                // rewarding but not a free, spammable block. Bright chime + gold sparks to read it as a clean parry.
                SoundEngine.PlaySound(SoundID.Item37, Player.Center);
                SpawnParryDust();
            }

            // Scale on the raw incoming damage (pre-defense), since the shield negates the whole hit — using the
            // post-defense info.Damage would make the scaled portion shrink to near-nothing on a defended player.
            float blockedDamage = info.SourceDamage > 0 ? info.SourceDamage : info.Damage;
            float cost = (float)Math.Ceiling(data.BaseCost + blockedDamage * data.DamageFactor);
            if (perfectParry)
            {
                cost = (float)Math.Ceiling(cost * PerfectParryCostMult);
            }

            tsorcRevampStaminaPlayer staminaPlayer = Player.GetModPlayer<tsorcRevampStaminaPlayer>();

            // Stability break: a hit heavy enough relative to your MAXIMUM stamina overwhelms the guard even when
            // you can comfortably afford it. This is what makes shield progression mean something beyond arithmetic —
            // a big telegraphed attack simply cannot be held with a starter shield and must be dodged, while a late
            // shield eats the same blow. Scaled off max (not current) so it's a property of the shield-vs-attack
            // matchup, not of how tired you happen to be. A perfect parry is always exempt — that's the reward.
            bool stabilityBreak = !perfectParry && cost > staminaPlayer.staminaResourceMax2 * StabilityBreakFraction;

            if (data.Resource == ShieldResource.Stamina)
            {
                tsorcRevampStaminaPlayer stamina = staminaPlayer;
                if (stamina.staminaResourceCurrent >= cost)
                {
                    stamina.staminaResourceCurrent -= cost;
                }
                else
                {
                    stamina.staminaResourceCurrent = 0;
                    TriggerGuardBreak(manaWard: false);
                    guardBroke = true;
                }
            }
            else
            {
                // Mana wards spend mana (scaled) AND a flat stamina sip (~1/3 of the base cost), so a mage
                // who never otherwise touches stamina still has a real resource limit on the 360° block.
                tsorcRevampStaminaPlayer stamina = staminaPlayer;
                int stamSip = (int)Math.Ceiling(data.BaseCost / 3f);
                if (perfectParry)
                {
                    stamSip = (int)Math.Ceiling(stamSip * PerfectParryCostMult);
                }
                tsorcRevampPlayer modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
                if (Player.statMana >= cost && stamina.staminaResourceCurrent >= stamSip)
                {
                    modPlayer.SpendManaOnHit((int)cost); // routes through the Unkindled mana-delay path
                    stamina.staminaResourceCurrent -= stamSip;
                }
                else
                {
                    modPlayer.SpendManaOnHit((int)cost);
                    stamina.staminaResourceCurrent = Math.Max(0f, stamina.staminaResourceCurrent - stamSip);
                    TriggerGuardBreak(manaWard: true);
                    guardBroke = true;
                }
            }

            // A stability break still costs the resource (paid above) — the guard just fails anyway.
            if (stabilityBreak && !guardBroke)
            {
                TriggerGuardBreak(data.Resource == ShieldResource.Mana);
                guardBroke = true;
            }

            // Post-block regen pause. An ordinary block doubles the class's normal spend delay (60 Unkindled /
            // 30 BotC); a perfect parry keeps the ordinary one (30 / 15). That gap is the point: the shield-down
            // window between a kiter's spaced shots was long enough to refill what the block cost, so a
            // block→shoot loop never lost ground. Doubling it makes sloppy blocking bleed while clean timing
            // still recovers at the normal rate. Set explicitly rather than leaning on UpdateResource's generic
            // "you spent stamina" detector, so a future change to the parry cost can't silently drop the pause.
            // Mana wards included — they spend a stamina sip per block, and a block is a block.
            //
            // BlockRegenDelayMult is the greatshield/buckler axis: a no-chip greatshield stretches this pause
            // instead of leaking HP, a buckler shortens it. Perfect parries ignore the multiplier entirely, so
            // clean timing recovers at the same rate no matter which shield you're holding.
            staminaPlayer.PauseStaminaRegen(perfectParry
                ? staminaPlayer.SpendRegenDelay
                : (int)Math.Round(staminaPlayer.BlockRegenDelay * data.BlockRegenDelayMult));

            // Chip: an ordinary block leaks a slice of the raw hit as unblockable HP loss. Perfect parry negates fully.
            if (!perfectParry)
            {
                ApplyChipDamage(data, info);
            }

            ApplyOnBlockEffects(activeShieldType, info, perfectParry);

            // Grant a brief immunity window after a block. FreeDodge by itself grants no i-frames, so an enemy
            // pressed against the shield would otherwise re-trigger a block every single frame and instantly
            // drain stamina. This gates the drain to a sane rate (~3 blocks/sec while pressed).
            Player.immune = true;
            Player.immuneTime = BlockImmuneFrames;
            Player.immuneNoBlink = true;

            // Block feedback (metallic clang) — skip it on a perfect parry (its own brighter chime) and on a
            // guard break (TriggerGuardBreak plays its distinct pitched-down tink), so those sounds aren't
            // masked by the normal full-pitch clang.
            //
            // Pitch encodes shield quality, so you can HEAR what you're holding: a stable endgame shield rings
            // high and crisp, a leaky starter thuds low and dull. Pairs with the chip number — the sound tells you
            // roughly how much leaked before you read the damage.
            if (!perfectParry && !guardBroke)
            {
                float pitch = MathHelper.Lerp(BlockPitchBest, BlockPitchWorst, data.QualityT);
                SoundEngine.PlaySound(SoundID.NPCHit4 with { Pitch = pitch }, Player.Center);
            }
            return true;
        }

        /// <summary>Light the Dragon Crest fire-breath, if the player can pay the ignition cost.
        /// The flat cost is what stops "ignite, drop the guard, re-ignite" from farming near-free flames.</summary>
        private void TryIgniteFireBreath()
        {
            // Player.manaCost is the global "spells cost more/less" stat; honouring it means Smough's armor
            // (+50% mana cost, -50% attack speed) pays extra for the very trick its attack-speed penalty
            // makes attractive in the first place.
            int ignitionCost = (int)Math.Ceiling(DragonCrestFireIgnitionMana * Math.Max(0.1f, Player.manaCost));
            if (Player.statMana < ignitionCost)
            {
                return; // not enough mana to light it — the parry still blocked, it just doesn't breathe
            }
            Player.GetModPlayer<tsorcRevampPlayer>().SpendManaOnHit(ignitionCost);
            fireBreathActive = true;
            fireBreathFlameTimer = 0;
            fireBreathManaAccum = 0f;
        }

        /// <summary>
        /// The player's current damage-per-second, as the yardstick the fire breath is priced against.
        ///
        /// Reads the stamina system's measured output EMA directly — that baseline is already per-second, so no
        /// conversion happens here. It used to be a per-USE figure that this method divided by the held weapon's
        /// use animation; when the stamina system switched its baseline to per-second, leaving that conversion in
        /// would have divided by swing speed a second time and made the breath weaker the slower your weapon.
        ///
        /// Self-calibrating either way: it tracks gear, class, buffs and progression with no table to maintain.
        /// Falls back to the coarse tier table only until the EMA has data.
        /// </summary>
        private float EstimatePlayerDps()
        {
            float measuredDps = Player.GetModPlayer<tsorcRevampStaminaPlayer>().PlayerDamagePerSecondEma;
            return measuredDps > 0f ? measuredDps : FireBreathTierReferenceDps[ProgressionTier()];
        }

        /// <summary>
        /// Per-frame driver for the Dragon Crest fire-breath. Ends early when the guard drops or mana runs out,
        /// so it costs tempo (no attacking, no stamina regen while it burns) rather than being fire-and-forget.
        /// </summary>
        private void UpdateFireBreath()
        {
            if (!fireBreathActive)
            {
                return;
            }
            if (Player.whoAmI != Main.myPlayer)
            {
                fireBreathActive = false;
                return;
            }
            // Lowering the shield (or a guard break, which forces isBlocking false) snuffs it out.
            if (!isBlocking || activeShieldType != ModContent.ItemType<Items.Accessories.Defensive.Shields.DragonCrestShield>())
            {
                fireBreathActive = false;
                return;
            }

            // Sustain cost, accumulated fractionally so 10/sec doesn't round to 0 every frame.
            fireBreathManaAccum += DragonCrestFireManaPerSecond / 60f * Math.Max(0.1f, Player.manaCost);
            if (fireBreathManaAccum >= 1f)
            {
                int tickCost = (int)fireBreathManaAccum;
                fireBreathManaAccum -= tickCost;
                if (Player.statMana < tickCost)
                {
                    fireBreathActive = false; // out of mana — the breath dies
                    return;
                }
                Player.GetModPlayer<tsorcRevampPlayer>().SpendManaOnHit(tickCost);
            }

            if (fireBreathFlameTimer > 0)
            {
                fireBreathFlameTimer--;
                return;
            }
            fireBreathFlameTimer = DragonCrestFireFlameInterval;

            // Per-flame damage = (tier reference DPS x share) spread over the flame rate, then flavoured by defense.
            // Because it's derived from a DPS target, holding the breath longer never inflates the rate — it just
            // trades more mana and more shield-up time for proportionally more damage, which is a fair trade.
            float flamesPerSecond = 60f / DragonCrestFireFlameInterval;
            float defenseMult = Math.Clamp(FireBreathDefenseBase + Player.statDefense / FireBreathDefensePerPoint,
                                           FireBreathDefenseMultMin, FireBreathDefenseMultMax);
            int fireDamage = Math.Max(1, (int)Math.Round(EstimatePlayerDps() * FireBreathDpsShare / flamesPerSecond * defenseMult));
            // Aimed at the cursor rather than the original attacker: the breath outlives any single hit, and the
            // player already faces the cursor while blocking, so this keeps it steerable.
            Vector2 dir = (Main.MouseWorld - Player.Center).SafeNormalize(new Vector2(Player.direction, 0f));
            Projectile.NewProjectile(Player.GetSource_Misc("ActiveShieldBlock"), Player.Center,
                dir.RotatedBy(Main.rand.NextFloat(-0.12f, 0.12f)) * 7f,
                ProjectileID.Flames, fireDamage, 1f, Player.whoAmI);
        }

        /// <summary>
        /// Unblockable HP leak on an ordinary (non-perfect) block. Scales on the RAW incoming damage rather than
        /// post-defense, because the whole point is that stacking defense shouldn't buy back free turtling.
        ///
        /// Applied as a direct statLife write rather than through Player.Hurt — routing it through Hurt would
        /// re-enter FreeDodge and spend a second block on the chip itself. Same pattern as BasiliskLeechTongue.
        /// Chip CAN kill: turtling at low HP is supposed to be the wrong answer.
        /// </summary>
        private void ApplyChipDamage(ActiveShieldData data, Player.HurtInfo info)
        {
            // Player HP is owned by the client that owns the player; the server must not write it for a remote.
            if (Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            float frac = data.ResolvedChipFraction;
            if (frac <= 0f)
            {
                return; // greatshield: takes no chip, pays in tempo instead (BlockRegenDelayMult)
            }

            // Melee/contact blocks chip less than projectile blocks. The behaviour this whole system exists to
            // tax is the ranged block-shoot loop — camping a distance, eating spaced telegraphed shots, firing
            // back — so projectile blocks carry the full rate. A contact block already costs you far more: the
            // enemy is on top of you, your flanks are exposed (rear/side hits don't block at all), and the
            // stability break threatens the heavy swings. It doesn't need the full chip rate on top.
            bool fromProjectile = info.DamageSource.SourceProjectileLocalIndex >= 0
                                  || info.DamageSource.SourceProjectileType > 0;
            if (!fromProjectile)
            {
                frac *= MeleeChipMult;
            }

            // Mana wards pay a SECOND resource (mana) for every block and grant zero ActiveDefense, and they're
            // carried by the squishiest builds. Without this they'd sit at the worst chip rate on the curve while
            // also blocking mostly projectiles (full rate) — three penalties stacked on the thinnest HP pool.
            if (data.Resource == ShieldResource.Mana)
            {
                frac *= ManaWardChipMult;
            }

            float raw = info.SourceDamage > 0 ? info.SourceDamage : info.Damage;
            int chip = (int)Math.Round(raw * frac);
            // Floor of 1 so every ordinary block costs something; cap as a share of max HP so one enormous boss
            // hit can't delete a full bar through a raised shield.
            int cap = Math.Max(1, (int)(Player.statLifeMax2 * MaxChipFractionOfMaxLife));
            chip = Math.Clamp(chip, 1, cap);

            Player.statLife -= chip;
            CombatText.NewText(Player.Hitbox, ChipDamageColor, chip);

            if (Player.statLife <= 0)
            {
                // Reuse the incoming hit's death reason so the death message names the real attacker.
                Player.KillMe(info.DamageSource, chip, info.HitDirection);
            }
        }

        /// <summary>Per-shield effects that fire when a hit is successfully blocked (Phase 5).
        /// <paramref name="perfectParry"/> is true when the hit landed within the perfect-parry window of raising.</summary>
        private void ApplyOnBlockEffects(int shieldType, Player.HurtInfo info, bool perfectParry)
        {
            // Resolve the attacking NPC (contact hits and NPC-owned projectiles).
            NPC attacker = null;
            int npcIndex = info.DamageSource.SourceNPCIndex;
            if (npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active)
            {
                attacker = Main.npc[npcIndex];
            }
            else
            {
                int projIndex = info.DamageSource.SourceProjectileLocalIndex;
                if (projIndex >= 0 && projIndex < Main.maxProjectiles && Main.projectile[projIndex].active)
                {
                    Projectile proj = Main.projectile[projIndex];
                    if (proj.owner >= 0 && proj.owner < Main.maxNPCs && proj.hostile && Main.npc[proj.owner].active)
                    {
                        attacker = Main.npc[proj.owner];
                    }
                }
            }
            if (attacker == null || attacker.friendly)
            {
                return;
            }

            // A blocked hit shoves the attacker back: per-shield strength scaled by knockback resistance, but with
            // a floor (even 0-resist enemies bump ~3 tiles so the slowed player can back off) and a cap (the
            // strongest shield only nudges a few tiles — melee still wants them in range). Bosses aren't flung.
            if (!attacker.boss)
            {
                float kb = tsorcRevamp.ActiveShieldRegistry != null
                    && tsorcRevamp.ActiveShieldRegistry.TryGetValue(shieldType, out ActiveShieldData kbData) ? kbData.Knockback : 5f;
                float push = MathHelper.Clamp(kb * attacker.knockBackResist, MinBlockKnockback, MaxBlockKnockback);
                float pushDir = attacker.Center.X >= Player.Center.X ? 1f : -1f;
                attacker.velocity.X = pushDir * push;
                attacker.velocity.Y -= 1.2f;
                attacker.netUpdate = true;
            }

            // The knockback shove above happens on every block, but each shield's *signature* ability now only
            // triggers on a perfect parry (reward for timing the raise) rather than on every blocked hit.
            if (!perfectParry)
            {
                return;
            }

            // RIPOSTE: a clean parry staggers. Until now every parry reward was the *absence* of a penalty (half
            // cost, no chip, normal regen delay) — nothing good actually happened. Poise damage scales with shield
            // quality, so a starter shield needs ~4 clean parries to break an enemy and the best needs ~2. Uses
            // ApplyParryPoise, which bypasses hyper-armor: a parry lands mid-swing by definition, and the ordinary
            // poise path would discard it for exactly that reason.
            if (tsorcRevamp.ActiveShieldRegistry != null
                && tsorcRevamp.ActiveShieldRegistry.TryGetValue(shieldType, out ActiveShieldData parryData))
            {
                float poiseFrac = MathHelper.Lerp(RipostePoiseBest, RipostePoiseWorst, parryData.QualityT);
                attacker.GetGlobalNPC<NPCs.tsorcRevampGlobalNPC>().ApplyParryPoise(attacker, poiseFrac, Player.Center);
            }

            // Thorns shields reflect the blocked damage back at the attacker.
            if (shieldType == ModContent.ItemType<Items.Accessories.Defensive.Shields.SpikedIronShield>()
                || shieldType == ModContent.ItemType<Items.Accessories.Defensive.Shields.AncientDemonShield>())
            {
                attacker.SimpleStrikeNPC(info.SourceDamage > 0 ? info.SourceDamage : info.Damage, -info.HitDirection);
            }

            // Mythril Bulwark applies its Vulnerability debuff to the attacker on block...
            if (shieldType == ModContent.ItemType<Items.Accessories.Damage.MythrilBulwark>())
            {
                attacker.AddBuff(ModContent.BuffType<MythrilRamDebuff>(), Items.Accessories.Damage.MythrilBulwark.VulnerabilityDuration * 60);
                // ...and deflects a blocked hostile projectile straight back at its source.
                int projIdx = info.DamageSource.SourceProjectileLocalIndex;
                if (projIdx >= 0 && projIdx < Main.maxProjectiles && Main.projectile[projIdx].active && Main.projectile[projIdx].hostile)
                {
                    Projectile p = Main.projectile[projIdx];
                    p.hostile = false;
                    p.friendly = true;
                    p.owner = Player.whoAmI;
                    p.velocity = -p.velocity;
                    p.netUpdate = true;
                }
            }

            // Eye line — the shield's gaze unsettles attackers, escalating across the tier.
            if (shieldType == ModContent.ItemType<Items.Accessories.Melee.BeholderShield>())
            {
                if (Main.rand.NextFloat() < 0.4f) attacker.AddBuff(BuffID.Confused, 120);
            }
            else if (shieldType == ModContent.ItemType<Items.Accessories.Melee.BeholderShield2>())
            {
                if (Main.rand.NextFloat() < 0.5f) attacker.AddBuff(BuffID.Confused, 150);
                if (Main.rand.NextFloat() < 0.25f) attacker.AddBuff(BuffID.Frozen, 60); // brief "petrify"
            }
            else if (shieldType == ModContent.ItemType<Items.Accessories.Melee.EnchantedBeholderShield2>())
            {
                if (Main.rand.NextFloat() < 0.6f) attacker.AddBuff(BuffID.Confused, 180);
                if (Main.rand.NextFloat() < 0.35f) attacker.AddBuff(BuffID.Frozen, 90);
                // Retaliatory eye-beam (reuses the vanilla Eye laser projectile).
                if (Player.whoAmI == Main.myPlayer)
                {
                    Vector2 dir = (attacker.Center - Player.Center).SafeNormalize(new Vector2(Player.direction, 0f));
                    Projectile.NewProjectile(Player.GetSource_Misc("ActiveShieldBlock"), Player.Center, dir * 11f,
                        ProjectileID.EyeLaser, 60, 2f, Player.whoAmI);
                }
            }

            // Dragon Crest — ignite the sustained fire-breath (perfect-parry gated like the others, above).
            // The breath itself is driven per-frame by UpdateFireBreath; this only lights it.
            if (shieldType == ModContent.ItemType<Items.Accessories.Defensive.Shields.DragonCrestShield>()
                && Player.whoAmI == Main.myPlayer)
            {
                TryIgniteFireBreath();
            }

            // Icebound Mythril Aegis — a small frost nova on block.
            if (shieldType == ModContent.ItemType<Items.Accessories.Defensive.Shields.IceboundMythrilAegis>())
            {
                attacker.AddBuff(BuffID.Frostburn, 180);
                if (Main.rand.NextFloat() < 0.25f) attacker.AddBuff(BuffID.Frozen, 60);
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.active && !n.friendly && !n.townNPC && n.Distance(Player.Center) <= 140f)
                    {
                        n.AddBuff(BuffID.Frostburn, 120);
                    }
                }
            }
        }

        // Mana wards release a 360° feedback pulse when lowered, stronger the longer they were held.
        // Celestriad additionally looses 3 seeking bolts if it was held at least ~2s.
        private void OnBlockReleased(int shieldType, int holdFrames)
        {
            if (Player.whoAmI != Main.myPlayer
                || tsorcRevamp.ActiveShieldRegistry == null
                || !tsorcRevamp.ActiveShieldRegistry.TryGetValue(shieldType, out ActiveShieldData data)
                || data.Resource != ShieldResource.Mana)
            {
                return;
            }

            // Charge ramps over ~1.5s to a capped max.
            float charge = MathHelper.Clamp(holdFrames / 90f, 0.35f, 1f);
            float pushSpeed = 16f * charge;          // much stronger shove than before (was 7) so it actually lands enemies a few tiles away
            const float pulseRange = 340f;           // wider reach (was 220)
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active || n.friendly || n.townNPC || n.boss || n.knockBackResist <= 0f || n.Distance(Player.Center) > pulseRange)
                {
                    continue;
                }
                Vector2 away = (n.Center - Player.Center).SafeNormalize(-Vector2.UnitY);
                // Floor the resist scaling so heavy enemies still get noticeably bumped (the old raw *resist made it
                // imperceptible on anything sturdy).
                n.velocity = away * pushSpeed * MathHelper.Clamp(n.knockBackResist, 0.6f, 1f);
                n.netUpdate = true;
            }
            if (shieldType == ModContent.ItemType<Items.Accessories.Defensive.Celestriad>()
                && holdFrames >= CelestriadChargeFrames && Player.statMana >= CelestriadBoltManaCost)
            {
                Player.GetModPlayer<tsorcRevampPlayer>().SpendManaOnHit(CelestriadBoltManaCost); // routes through the Unkindled mana-delay path
                FireWardBolts();
            }
        }

        private const int CelestriadChargeFrames = 120;   // ~2s hold to arm the Celestriad seeking-bolt volley
        private const int CelestriadBoltManaCost = 50;     // mana spent when the volley fires on release

        // Looses three Soul Arrow bolts outward — up-left, up-right, and straight up — rather than beelining at a
        // target. Soul Arrow is lightly homing, so each bolt then curves onto a nearby enemy on its own.
        private void FireWardBolts()
        {
            if (Player.whoAmI != Main.myPlayer)
            {
                return;
            }
            Vector2[] dirs =
            {
                new Vector2(-1f, -0.35f),
                new Vector2(1f, -0.35f),
                new Vector2(0f, -1f),
            };
            foreach (Vector2 d in dirs)
            {
                Vector2 vel = d.SafeNormalize(-Vector2.UnitY) * 10f;
                Projectile.NewProjectile(Player.GetSource_Misc("ActiveShieldBlock"), Player.Center, vel,
                    ModContent.ProjectileType<Projectiles.SoulArrow>(), 300, 1f, Player.whoAmI);
            }
        }

        /// <summary>Bright gold/white spark burst on a perfect parry, in front of the player.</summary>
        private void SpawnParryDust()
        {
            Vector2 origin = Player.Center + new Vector2(Player.direction * 14f, 0f);
            for (int i = 0; i < 18; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / 18f).ToRotationVector2() * Main.rand.NextFloat(2.5f, 5f);
                int d = Dust.NewDust(origin, 4, 4, DustID.GoldFlame, vel.X, vel.Y, 100, Color.White, 1.4f);
                Main.dust[d].noGravity = true;
            }
        }

        /// <summary>One-time white twinkle around the player marking a Celestriad ward reaching full charge.</summary>
        private void SpawnChargeTwinkle()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(20f, 26f);
                int d = Dust.NewDust(Player.Center + offset, 4, 4, DustID.TreasureSparkle, 0f, 0f, 100, Color.White, 1.2f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.2f;
            }
            SoundEngine.PlaySound(SoundID.Item25 with { Pitch = 0.5f }, Player.Center); // soft chime
        }

        /// <summary>Best-effort world position of whatever dealt this hit (NPC, projectile, or player).</summary>
        private static bool TryGetAttackerCenter(Player.HurtInfo info, out Vector2 center)
        {
            Terraria.DataStructures.PlayerDeathReason src = info.DamageSource;
            if (src.SourceNPCIndex >= 0 && src.SourceNPCIndex < Main.maxNPCs && Main.npc[src.SourceNPCIndex].active)
            {
                center = Main.npc[src.SourceNPCIndex].Center;
                return true;
            }
            if (src.SourceProjectileLocalIndex >= 0 && src.SourceProjectileLocalIndex < Main.maxProjectiles
                && Main.projectile[src.SourceProjectileLocalIndex].active)
            {
                center = Main.projectile[src.SourceProjectileLocalIndex].Center;
                return true;
            }
            if (src.SourcePlayerIndex >= 0 && src.SourcePlayerIndex < Main.maxPlayers && Main.player[src.SourcePlayerIndex].active)
            {
                center = Main.player[src.SourcePlayerIndex].Center;
                return true;
            }
            center = default;
            return false;
        }

        private void TriggerGuardBreak(bool manaWard)
        {
            Player.AddBuff(BuffID.Ichor, 120);   // 2s
            Player.AddBuff(BuffID.Slow, 60);     // 1s
            Player.AddBuff(ModContent.BuffType<ShieldGuardBreak>(), 120); // 2s reduced regen
            if (manaWard)
            {
                Player.AddBuff(BuffID.ManaSickness, 120);
            }
            blockLockTimer = 30; // brief lock; the ShieldGuardBreak debuff keeps the shield down for its full duration
            isBlocking = false;
            activeShieldType = -1;
            // Guard-break feedback: the shield-hit "tink", pitched down so it reads as a heavy, failed block.
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Pitch = -0.6f }, Player.Center);
        }

        public override void PostUpdateRunSpeeds()
        {
            if (!isBlocking)
            {
                return;
            }

            // Better shields slow you less while raised (per-shield MoveSpeedMult).
            float slow = 0.8f;
            if (tsorcRevamp.ActiveShieldRegistry != null
                && tsorcRevamp.ActiveShieldRegistry.TryGetValue(activeShieldType, out ActiveShieldData data))
            {
                slow = data.MoveSpeedMult;
            }
            // Chloranthy Rings ease the shield-hold slow: Ring I by 2%, Ring II by 4% (they don't stack — II replaces I).
            tsorcRevampPlayer modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.ChloranthyRing2)
            {
                slow = Math.Min(1f, slow + Items.Accessories.Mobility.ChloranthyRing2.ShieldSlowReduction / 100f);
            }
            else if (modPlayer.ChloranthyRing1)
            {
                slow = Math.Min(1f, slow + Items.Accessories.Mobility.ChloranthyRing.ShieldSlowReduction / 100f);
            }
            Player.maxRunSpeed *= slow;
            Player.accRunSpeed *= slow;
            Player.runAcceleration *= slow;
            // NOTE: the stamina-regen reduction is applied in ProcessTriggers, NOT here — PostUpdateRunSpeeds
            // runs AFTER the stamina system's PostUpdateMiscEffects, so setting the mult here would be too late.
        }

        public override bool CanUseItem(Item item)
        {
            // Can't attack while a shield is raised.
            if (isBlocking && item.damage > 0 && item.useStyle != ItemUseStyleID.None)
            {
                return false;
            }
            return true;
        }

        // Set true to also apply a small one-time outward shove (Cthulhu-style) when an enemy first
        // contacts the shield wall, instead of a pure solid wall. Off by default for the "hold the line" feel.
        private const bool BodyBlockKnockback = false;
        private const float BodyBlockKnockbackSpeed = 4f;
        private const int ShieldWallReach = 12; // px the wall extends past the player's front edge
        private const int BlockImmuneFrames = 36; // i-frames granted after a block, to gate how often a pressed enemy re-triggers a block (~0.6s)
        private const float MinBlockKnockback = 3.5f; // floor so even knockback-immune (0-resist) non-boss enemies bump ~3 tiles
        private const float MaxBlockKnockback = 8f;    // cap so the strongest shield only shoves a few tiles (melee keeps them in range)
        public const float BlockStaminaRegenMult = 0f; // stamina regen retained while a shield is raised (0 = no regen, Dark-Souls style)

        // --- Chip damage (ordinary blocks only; perfect parry always fully negates) ---
        private const float MeleeChipMult = 0.75f;            // contact/melee blocks chip less than projectile blocks
        private const float ManaWardChipMult = 0.7f;          // mana wards already pay mana + have 0 ActiveDefense
        private const float MaxChipFractionOfMaxLife = 0.10f; // backstop: one block can never leak >10% of max HP
        private static readonly Color ChipDamageColor = new Color(205, 95, 45); // rust/ember — distinct from vanilla red

        // --- Stability break: a hit costing more than this share of MAX stamina breaks the guard even if affordable ---
        private const float StabilityBreakFraction = 0.60f;

        // --- Riposte: perfect parry poise damage, as a fraction of the enemy's current poise bar ---
        private const float RipostePoiseBest = 0.45f;  // best shield in the registry
        private const float RipostePoiseWorst = 0.25f; // worst — ~a third of the bar at the centre, so 2-4 parries stagger

        // --- Block sound pitch by shield quality (crisp ring → dull thud) ---
        private const float BlockPitchBest = 0.25f;
        private const float BlockPitchWorst = -0.35f;
        private const int PerfectParryWindow = 12; // frames after raising a shield within which a blocked hit counts as a perfect parry (~0.2s)
        private const float PerfectParryCostMult = 0.5f; // a perfect parry costs this fraction of the normal block cost (50% off)
        // --- Dragon Crest sustained fire-breath (perfect parry only) ---
        //
        // Was an instant 3-flame burst dealing statDefense * 2 each, uncapped. That scaled off the exact stat a
        // shield/turtle build stacks, so the payoff for a parry grew past the payoff for actually attacking and
        // the optimal play became "stop swinging, only parry" (a player killed Queen Bee — 3400 HP, 8 defense —
        // in ~6 parries in Smough's, whose -50% attack speed makes that trade even better). Note ProjectileID.Flames
        // carries ArmorPenetration 15, so against most early/mid enemies the listed damage is the delivered damage.
        //
        // Now: flatter and CAPPED scaling, delivered as a sustained breath (dodgeable, and it costs you tempo since
        // you must keep the guard up) on two independent limiters — the frame cap bounds a single parry's payoff,
        // and mana bounds how many breaths you get per fight. Mana is the right resource here because SoulsMode
        // mana regen is pinned off while a boss is alive, so a 200 pool is a hard per-fight budget, not a faucet.
        // Scaling is denominated in DPS, not in burst. The original bug wasn't "6 parries killed Queen Bee" — it was
        // that those 6 parries were 6 INSTANTS, delivering a 15-second kill in about a second of real time (~15x the
        // player's weapon DPS). Six parries spread over 18 seconds of sustained breath is just a normal kill. So the
        // number that has to stay honest is damage-per-second while breathing; once that's right, a long hold is fine
        // and burst caps stop being load-bearing.
        //
        // Base comes from a per-tier reference DPS (progression), and defense becomes a bounded MODIFIER rather than
        // the base — so a tanky build is rewarded without the runaway feedback loop that caused the original problem.
        private const float FireBreathDpsShare = 0.8f;          // MASTER DIAL: breath output as a share of tier reference DPS
        private const float FireBreathDefenseBase = 0.6f;       // defense multiplier at 0 defense
        private const float FireBreathDefensePerPoint = 150f;   // divisor: +1.0x per this many defense
        private const float FireBreathDefenseMultMin = 0.6f;
        private const float FireBreathDefenseMultMax = 1.5f;    // bounded — defense can flavour it, never run away with it

        // No duration cap: the breath runs as long as you hold the guard and can pay for it. Safe because the
        // output is denominated in DPS — a longer hold buys proportionally more damage for proportionally more
        // mana and shield-up time, which is a fair trade, not an exploit. Mana is now the sole uptime limiter.
        private const int DragonCrestFireFlameInterval = 10;    // one flame per 10 frames => 6/sec
        private const int DragonCrestFireIgnitionMana = 15;     // flat cost to light it (also blocks tap-ignite abuse)
        // 25/sec puts it in line with what an actual magic weapon burns to sustain its DPS (a ~20 useTime staff at
        // ~10 mana/cast is ~30 mana/sec). At the old 10/sec the breath was roughly 3x more mana-efficient than the
        // spells it's competing with, which is the wrong answer for something billed as a mana weapon.
        private const float DragonCrestFireManaPerSecond = 25f;

        /// <summary>
        /// BOOTSTRAP ONLY: reference DPS per progression tier, used until the player's measured output EMA exists
        /// (fresh character who hasn't swung anything, or a non-weapon in hand). Once the EMA is live it wins —
        /// it's the same number but personalised and self-calibrating. Rough estimates; the Balance Telemetry log
        /// (Utilities/Balance/) is the right source if these ever need to be accurate.
        /// </summary>
        private static readonly float[] FireBreathTierReferenceDps =
        {
            60f,    // 0: pre-boss
            120f,   // 1: post EoC / EoW / BoC
            220f,   // 2: post Skeletron / Queen Bee
            550f,   // 3: hardmode
            900f,   // 4: post-mech
            1600f,  // 5: post-Plantera
            2800f,  // 6: post-Golem / Cultist
            4500f,  // 7: post-Moon Lord
            9000f   // 8: SuperHardMode
        };

        /// <summary>Coarse world progression tier (0-8), indexing FireBreathTierReferenceDps.
        /// Local to the fire breath for now; worth promoting to UsefulFunctions if anything else needs it.</summary>
        private static int ProgressionTier()
        {
            if (tsorcRevampWorld.SuperHardMode) return 8;
            if (NPC.downedMoonlord) return 7;
            if (NPC.downedGolemBoss || NPC.downedAncientCultist) return 6;
            if (NPC.downedPlantBoss) return 5;
            if (NPC.downedMechBossAny) return 4;
            if (Main.hardMode) return 3;
            if (NPC.downedBoss3 || NPC.downedQueenBee) return 2;
            if (NPC.downedBoss1 || NPC.downedBoss2) return 1;
            return 0;
        }
        private const int MaxDashShieldFrames = 75;            // safety cap (~1.25s) on how long the EoC dash window can be re-armed

        public override void PostUpdate()
        {
            if (!RevampActive)
            {
                return;
            }

            // Dragon Crest sustained fire-breath (lit by a perfect parry; self-terminates on guard-down / no mana).
            UpdateFireBreath();

            // EoC dash window. The mod shows the Shield of Cthulhu for the whole dash (until you slow back to
            // walking speed), but vanilla's ram window (eocDash) — the part that lets the shield HIT enemies and
            // shrug off their contact damage — is much briefer, so the shield was visually "out" long after it
            // stopped doing anything (you'd take damage despite the shield showing). Keep eocDash alive for the
            // whole dash so the FUNCTION matches the VISUAL: ram + contact protection last as long as the shield is
            // shown. (Vanilla also draws the accessory-slot shield while eocDash > 0, so its sprite stays synced too.)
            // Ends when you slow to walking speed (incl. the knockback after ramming an enemy) or a safety cap.
            if (Player.eocDash > 0 && !dashShield)
            {
                dashShield = true;
                dashShieldFrames = 0;
            }
            if (dashShield)
            {
                dashShieldFrames++;
                if (Player.velocity.Length() < 4f || dashShieldFrames > MaxDashShieldFrames)
                {
                    dashShield = false;
                    dashShieldFrames = 0;
                }
                else if (Player.eocDash <= 0)
                {
                    Player.eocDash = 2; // re-arm vanilla's ram window so it persists for the whole visible dash
                }
            }

            // Face the cursor while blocking (like a weapon swing) so you can hold the shield toward a threat
            // while walking backwards. Done in PostUpdate (not PostUpdateRunSpeeds) because vanilla movement
            // sets direction earlier in the update and would otherwise override it. Local player only;
            // remote players' facing arrives through vanilla's normal direction sync.
            if (isBlocking && Player.whoAmI == Main.myPlayer && !Main.gameMenu)
            {
                Player.direction = Main.MouseWorld.X >= Player.Center.X ? 1 : -1;
            }

            // Visuals run for every player instance on this client (local computes isBlocking; remotes get it
            // via SyncActiveShield), so raised shields are visible in multiplayer.
            // Player.shield is the equipped shield's draw slot, set during UpdateEquips each frame on every client.
            // A shield in the Right-Click slot isn't "equipped", so vanilla never sets Player.shield (the worn-shield
            // draw slot) for it. We point it at the slotted shield's equip slot while blocking and clear it after.
            // Item.shieldSlot is set for both vanilla shields and [AutoloadEquip(EquipType.Shield)] ones.
            Item slotShield = RightClickItem;
            bool slotShieldActive = isBlocking && slotShield != null && !slotShield.IsAir
                                    && slotShield.type == activeShieldType && slotShield.shieldSlot >= 0;

            if (isBlocking)
            {
                Player.shieldRaised = true; // ask vanilla to draw the shield in its raised pose
                if (slotShieldActive)
                {
                    Player.shield = slotShield.shieldSlot;
                    shieldDrawForcedBySlot = true;
                }
            }
            else if (dashShield)
            {
                // Shield of Cthulhu dash: force the EoC shield's sprite to stay drawn for the whole dash. Vanilla
                // draws it from Player.shield, which we otherwise hide while not blocking — and never set at all for
                // a shield sitting in the Right-Click (2nd) slot. Forcing the slot here covers both cases so the
                // shield shows from dash start until it ends (hit an enemy / slowed back to walking speed).
                int eocSlot = GetEquippedShieldSlot(ItemID.EoCShield);
                if (eocSlot >= 0)
                {
                    Player.shield = eocSlot;
                    shieldDrawForcedBySlot = true;
                }
            }
            else
            {
                // Clear a draw-slot we forced for a 2nd-slot shield or the dash (vanilla won't reset it for us)...
                if (shieldDrawForcedBySlot)
                {
                    Player.shield = -1;
                    shieldDrawForcedBySlot = false;
                }
                // ...and hide the passive back-shield of a registered accessory shield while not blocking.
                else if (ActiveFor(Player) && Player.shield >= 0 && FindBestEquippedShield(out _) != -1)
                {
                    Player.shield = -1;
                }
            }

            // Body-block runs where NPC movement is authoritative: the server for every blocking player,
            // or the local player in single-player. Clients skip it to avoid fighting server-corrected NPC positions.
            if (isBlocking && (Main.netMode == NetmodeID.Server
                || (Main.netMode == NetmodeID.SinglePlayer && Player.whoAmI == Main.myPlayer)))
            {
                UpdateBodyBlock();
            }
        }

        /// <summary>
        /// While the shield is raised, prevent knockback-able, non-boss enemies in front from walking
        /// through the player — a "hold the line" wall. Bosses and immovable enemies push through.
        /// </summary>
        private void UpdateBodyBlock()
        {
            int dir = Player.direction;
            float centerX = Player.Center.X;
            Rectangle reachBox = Player.Hitbox;
            reachBox.Inflate(ShieldWallReach, 0);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.townNPC)
                {
                    continue;
                }
                // Elite/mini-boss types in BodyBlockableNPCs are always stopped; otherwise stop only
                // knockback-able, non-boss, contact-damaging enemies (true bosses push through).
                bool forced = tsorcRevamp.BodyBlockableNPCs != null && tsorcRevamp.BodyBlockableNPCs.Contains(npc.type);
                if (!forced && (npc.boss || npc.damage <= 0 || npc.knockBackResist <= 0f))
                {
                    continue;
                }

                // Only the front side, and only enemies near the player.
                bool inFront = dir == 1 ? npc.Center.X > centerX : npc.Center.X < centerX;
                if (!inFront || !npc.Hitbox.Intersects(reachBox))
                {
                    continue;
                }

                // Clamp the enemy's near edge to the player's center: it can press into the front half of the
                // player (so its hitbox overlaps and contact damage still fires, which the shield then blocks),
                // but it can't push its body through to the player's far side. Bosses/immovables ignored above.
                if (dir == 1)
                {
                    if (npc.position.X < centerX)
                    {
                        npc.position.X = centerX;
                        if (npc.velocity.X < 0f)
                        {
                            npc.velocity.X = BodyBlockKnockback ? BodyBlockKnockbackSpeed : 0.5f;
                        }
                        npc.netUpdate = true;
                    }
                }
                else
                {
                    if (npc.position.X + npc.width > centerX)
                    {
                        npc.position.X = centerX - npc.width;
                        if (npc.velocity.X > 0f)
                        {
                            npc.velocity.X = BodyBlockKnockback ? -BodyBlockKnockbackSpeed : -0.5f;
                        }
                        npc.netUpdate = true;
                    }
                }
            }
        }
    }
}
