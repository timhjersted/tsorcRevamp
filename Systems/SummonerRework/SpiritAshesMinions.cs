using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.SummonerRework
{
    /// <summary>
    /// Makes an Unkindled or Bearer of the Curse player's minions and sentries mortal, and lets them
    /// physically intercept attacks aimed at their owner ("spirit ashes").
    ///
    /// Keys off vanilla's own Projectile.minion / Projectile.sentry flags rather than a registry, so
    /// every existing and future summon participates with no per-item changes and no exception list.
    ///
    /// Everything here is owner-authoritative: only the client that owns a minion tracks its health,
    /// applies damage to it, or kills it. A minion's health is never meaningful to another client -
    /// it is never drawn for them - so there is no state to network. The kill itself goes through
    /// Projectile.Kill(), which vanilla already replicates.
    /// </summary>
    public class SpiritAshesMinions : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        /// <summary>Current health. Negative means "not a tracked spirit ash" - either not a minion, or
        /// it spawned while the rework was off. IsTrackedAsh is the check everything else uses.</summary>
        public float MinionHealth = -1f;

        public float MinionMaxHealth = -1f;

        /// <summary>
        /// Buff that directly spawned this ash, if any. Some temporary whip summons recreate their
        /// projectile whenever it disappears, so lethal damage must remove that buff before killing the
        /// projectile or the ash simply returns on the next update.
        /// </summary>
        private int SpawningBuffType = -1;

        /// <summary>Ticks before this ash can block another hostile projectile. Set on spawn too, as
        /// spawn protection.</summary>
        public int ProjectileBlockCooldown;

        /// <summary>Ticks before body contact can hurt this ash again. Deliberately independent of the
        /// projectile timer - see SummonerRework.MinionProjectileBlockCooldownTicks.</summary>
        public int ContactCooldown;

        /// <summary>Ticks since this minion last took damage, used to fade the health bar out once it
        /// stops being hit. Starts high so a freshly summoned minion draws no bar.</summary>
        public int TicksSinceLastHit = HealthBarHiddenTicks;

        /// <summary>Parked value for TicksSinceLastHit meaning "not recently hit". Large enough to be
        /// past any linger window, small enough that incrementing it can never overflow.</summary>
        private const int HealthBarHiddenTicks = 100000;

        public bool IsTrackedAsh => MinionHealth >= 0f && MinionMaxHealth > 0f;

        /// <summary>The tick this slot's 90-second recovery warranty matures - stamped once at TRUE
        /// spawn (see OnSpawn), inherited (not refreshed) across repositions. Dying before this passes
        /// blocks resummon until it does; dying after costs nothing, the warranty already matured.</summary>
        public uint RecoveryDeadline;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (!projectile.minion && !projectile.sentry)
            {
                return;
            }

            // Invisible spawner projectiles (Abigail's Flower's AbigailCounter, etc.) create the real
            // minion as a child rather than being one themselves - see SummonerRework.IsUntrackedSpawner.
            // Giving one its own health/mortality would create a bogus, invisible "ash" that never takes
            // combat damage the way the real minion does.
            if (SummonerRework.IsUntrackedSpawner(projectile.type))
            {
                return;
            }

            // Only the owning client tracks health, so only it needs to initialise any of this.
            if (projectile.owner != Main.myPlayer)
            {
                return;
            }

            Player owner = Main.player[projectile.owner];

            if (!SummonerRework.Active(owner))
            {
                return;
            }

            if (source is EntitySource_Buff buffSource)
            {
                SpawningBuffType = buffSource.BuffId;
            }

            MinionMaxHealth = owner.statLifeMax2 * SummonerRework.MinionHealthFraction;

            // Recasting while an instance of this exact type is already alive is a reposition, not a
            // fresh summon: vanilla spawns a replacement projectile and the old one silently kills itself
            // once it notices, on its own next tick - it never touches TakeDamage, so this never fires
            // for a genuine combat death. Inherit the old instance's current health here (clamped to this
            // new max, in case max HP changed) rather than resetting to full, or repositioning would
            // double as a free heal that bypasses both Estus and the recovery cooldown.
            //
            // Single-instance types (Abigail) always inherit when a sibling is found - see
            // SummonerRework.SingleInstanceAshTypes for why that "reposition or genuine 2nd copy?"
            // ambiguity does not exist for them. Every other type only inherits when there was NO free
            // minion slot for a new instance at cast time: with room to spare, a second cast is
            // presumptively adding a coexisting instance, not replacing the first - this is what let a
            // 2nd Enchanted Sword silently inherit the 1st's current health instead of spawning at full.
            // HasProvenMultipleInstances is kept as a second, independent guard on top - once a type HAS
            // shown 2+ at once, never inherit again regardless of slot bookkeeping, since a live sibling
            // at that point could just as easily be an unrelated army member as the one being replaced.
            SpiritAshesMinions replaced = null;
            SpiritAshesRecoveryPlayer recoveryPlayerForSpawn = owner.GetModPlayer<SpiritAshesRecoveryPlayer>();
            bool isSingleInstanceType = SummonerRework.IsSingleInstanceAshType(projectile.type);
            bool hadFreeSlot = owner.maxMinions - owner.slotsMinions >= projectile.minionSlots;
            bool shouldAttemptReposition = isSingleInstanceType
                || (!hadFreeSlot && !recoveryPlayerForSpawn.HasProvenMultipleInstances(projectile.type));

            if (shouldAttemptReposition)
            {
                replaced = FindReplacedInstance(projectile);
            }

            bool inheritedFromPredecessor = false;

            if (replaced != null)
            {
                MinionHealth = Math.Min(replaced.MinionHealth, MinionMaxHealth);
                TicksSinceLastHit = replaced.TicksSinceLastHit;

                // Inherit the warranty too, not just health - repositioning must not refresh it, or a
                // player could keep an ash permanently "freshly warrantied" just by recasting it.
                RecoveryDeadline = replaced.RecoveryDeadline;
                inheritedFromPredecessor = true;
            }
            else if (shouldAttemptReposition && recoveryPlayerForSpawn.TryGetAliveState(
                projectile.type, out float lastHealth, out int lastTicksSinceLastHit, out uint lastRecoveryDeadline)
                && Main.GameUpdateCount < lastRecoveryDeadline)
            {
                // No LIVE sibling was found, but this exact type was alive a moment ago with less than
                // full health and never actually died - see SpiritAshesRecoveryPlayer.RecordAliveState.
                // Without this, right-clicking a damaged ash's buff icon to cancel it and immediately
                // resummoning it was a free, instant, unlimited full heal that bypassed Estus entirely.
                //
                // Gated on the snapshot's OWN warranty still being unexpired: a dismissed (not dead) ash
                // is a free, unlimited reposition right up until that original 90-second warranty would
                // have mattered anyway, exactly like a real death - once it is past, resummoning starts a
                // clean slate (full health, fresh warranty) rather than letting a player stash a damaged
                // ash in cancel-limbo indefinitely as a way to dodge ever actually risking it in combat.
                MinionHealth = Math.Min(lastHealth, MinionMaxHealth);
                TicksSinceLastHit = lastTicksSinceLastHit;
                RecoveryDeadline = lastRecoveryDeadline;
                inheritedFromPredecessor = true;
            }

            if (!inheritedFromPredecessor)
            {
                MinionHealth = MinionMaxHealth;

                // A TRUE new slot (not a reposition) starts its own 90-second warranty right now, and
                // that is recorded immediately - not deferred to whenever this slot eventually dies. That
                // is what makes the countdown visible from the moment of summon instead of only appearing
                // after a death, and what makes a death that happens AFTER the warranty already matured
                // cost nothing: there is nothing left to record at that point.
                RecoveryDeadline = Main.GameUpdateCount + SpiritAshesRecoveryPlayer.RecoveryDelayTicks;
                recoveryPlayerForSpawn.RecordNewSlot(projectile.type, RecoveryDeadline);
            }

            // Spawn protection, so resummoning into an attack that is already on screen is not an
            // instant re-death.
            ProjectileBlockCooldown = SummonerRework.MinionProjectileBlockCooldownTicks;
            ContactCooldown = SummonerRework.MinionContactCooldownTicks;

            // Record this type's high-water mark the moment a new instance is confirmed alive, not only
            // when the player happens to cast again. A player who summons once and lets it fight never
            // presses the button a second time until it dies - if peak only updated inside CanSummon (cast
            // time), it would still be sitting at 0 when that death happens, "nothing missing" would look
            // true, and the recovery cooldown would silently never engage.
            int aliveForPeakTracking = CountAlive(owner, projectile.type);

            if (replaced != null)
            {
                // The predecessor we just inherited from is STILL active in the projectile array right
                // now - Abigail's counter-driven respawn in particular can leave the old and new minion
                // coexisting for a tick before vanilla retires the old one on its own. That overlap is the
                // same logical slot being renewed, not a second one, so it must not count toward peak: a
                // peak that reads 2 here permanently disables reposition-inheritance for this type (see
                // HasProvenMultipleInstances above), which was resetting Abigail's health and stacking a
                // fresh 90-second recovery entry on every single recast instead of zero.
                aliveForPeakTracking--;
            }

            recoveryPlayerForSpawn.NotifyAlive(projectile.type, aliveForPeakTracking);
        }

        public override void AI(Projectile projectile)
        {
            if (projectile.owner != Main.myPlayer || !IsTrackedAsh)
            {
                return;
            }

            // Refreshed every tick this instance is alive, so SpiritAshesRecoveryPlayer always has a
            // near-current snapshot to fall back on if this instance vanishes WITHOUT dying (buff-cancel,
            // vanilla slot enforcement) - see RecordAliveState for why that fallback exists.
            Main.player[projectile.owner].GetModPlayer<SpiritAshesRecoveryPlayer>()
                .RecordAliveState(projectile.type, MinionHealth, TicksSinceLastHit, RecoveryDeadline);

            if (ProjectileBlockCooldown > 0)
            {
                ProjectileBlockCooldown--;
            }

            if (ContactCooldown > 0)
            {
                ContactCooldown--;
            }

            if (TicksSinceLastHit < HealthBarHiddenTicks)
            {
                TicksSinceLastHit++;
            }

            // The two defensive jobs run on independent timers, so being in contact with an enemy never
            // costs an ash the chance to body-block a shot.
            if (ProjectileBlockCooldown <= 0)
            {
                TryAbsorbHostileProjectile(projectile);
            }

            HandleEnemyContact(projectile);
        }

        /// <summary>
        /// Everything that happens while an ash is standing inside an enemy: it marks that enemy with
        /// Drawn Ire, and it takes a little damage for being there.
        ///
        /// The two run on different clocks on purpose. MARKING happens every frame of contact, so the
        /// protection never flickers while a minion is holding an enemy. DAMAGE is gated by
        /// ContactCooldown, so contact costs about one bite per second rather than sixty.
        ///
        /// The damage is heavily reduced (SummonerRework.MinionContactDamageMult) because body contact is the
        /// *working state* for melee summons, not a positioning error - they cannot deal damage without
        /// being there. Charging the normal rate would single out exactly the summons that have to take
        /// risks to function.
        /// </summary>
        private void HandleEnemyContact(Projectile minion)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC enemy = Main.npc[i];

                // ⚠ Do NOT gate this on `enemy.damage > 0`. The mod's puppet/invader enemies (DreadWraith,
                // Hero of Lumelia, Gwyn, Witchking and the rest of NPCs/Puppets) set NPC.damage = 0 by
                // design and deal everything through weapon hitboxes and projectiles - so a damage-based
                // filter would make both Drawn Ire and contact damage inert against the hardest fights in
                // the game.
                if (!UsefulFunctions.IsHostileThreat(enemy))
                {
                    continue;
                }

                if (!enemy.Hitbox.Intersects(minion.Hitbox))
                {
                    continue;
                }

                // Marking is independent of contact damage, so an ash holding a puppet enemy still earns
                // the protection even though that enemy has no contact damage to give. MELEE tier - see
                // OnHitNPC below for the separate, weaker RANGED tier.
                enemy.GetGlobalNPC<DrawnIre>().MeleeMarkTicks = SummonerRework.DrawnIreDurationTicks;

                if (ContactCooldown <= 0 && enemy.damage > 0)
                {
                    TakeDamage(minion, enemy.damage, SummonerRework.MinionContactDamageMult);
                    ContactCooldown = SummonerRework.MinionContactCooldownTicks;
                }

                return;
            }
        }

        /// <summary>
        /// The RANGED tier of Drawn Ire (see DrawnIre.cs): marks an enemy when it is hit by a genuinely
        /// ranged attack from one of the player's ashes - a projectile dealing summon-classed damage that
        /// is NOT itself a tracked minion/sentry body (that case is the MELEE tier, set by
        /// HandleEnemyContact above instead, so a melee-contact ash's own attack never also triggers
        /// this) and is not a whip (the player's own weapon, not a minion's attack).
        ///
        /// Fires for every projectile's hits in the game, not just ones this mod cares about - the
        /// checks below are what scope it down, rather than needing a registry of which weapons matter.
        /// </summary>
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner != Main.myPlayer || projectile.minion || projectile.sentry)
            {
                return;
            }

            Player owner = Main.player[projectile.owner];

            if (!SummonerRework.Active(owner) || !UsefulFunctions.IsHostileThreat(target))
            {
                return;
            }

            if (!projectile.CountsAsClass(DamageClass.Summon) || ProjectileID.Sets.IsAWhip[projectile.type])
            {
                return;
            }

            target.GetGlobalNPC<DrawnIre>().RangedMarkTicks = SummonerRework.DrawnIreDurationTicks;
        }

        /// <summary>
        /// Looks for a hostile projectile overlapping this ash and, if it finds one, eats the hit that
        /// would otherwise have carried on to the player.
        ///
        /// Scanning from the minion's side rather than the hostile projectile's is deliberate: minions
        /// are capped at a handful while hostile projectiles are not, so this bounds the per-frame work
        /// at (minions x projectile array) instead of (hostile projectiles x projectile array), which
        /// matters in this mod's denser boss fights.
        ///
        /// Single-hit projectiles (penetrate == 1) are destroyed on contact - a blocked arrow. Piercing
        /// and persistent ones (beams, lingering hazard fields) deal their damage but survive, so a
        /// minion can soak a tick of a boss hazard without deleting the hazard outright.
        /// </summary>
        private void TryAbsorbHostileProjectile(Projectile minion)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile hostile = Main.projectile[i];

                if (!hostile.active || !hostile.hostile || hostile.damage <= 0)
                {
                    continue;
                }

                if (!hostile.Hitbox.Intersects(minion.Hitbox))
                {
                    continue;
                }

                TakeDamage(minion, hostile.damage);
                ProjectileBlockCooldown = SummonerRework.MinionProjectileBlockCooldownTicks;

                if (hostile.penetrate == 1)
                {
                    hostile.Kill();
                }

                return;
            }
        }

        /// <summary>
        /// Applies a hit to this ash and kills it if that empties its health. Callers own their own
        /// cooldown: they check it before calling and set it after, so the projectile and contact timers
        /// stay independent.
        ///
        /// damageMult defaults to the ordinary reduction for real attacks; body contact passes its own,
        /// much lower value instead (not on top).
        /// </summary>
        public void TakeDamage(Projectile projectile, int rawDamage, float damageMult = SummonerRework.MinionDamageTakenMult)
        {
            if (!IsTrackedAsh)
            {
                return;
            }

            // Ashes are armoured by their summoner: they use the player's own defense, at the same
            // effectiveness the player gets (0.5 Classic / 0.75 Expert / 1.0 Master, plus any modifiers).
            // Defensive gear therefore keeps the army alive as well as its owner, which is the whole
            // reason a summoner would ever wear it.
            //
            // Applied to the RAW hit before the source multiplier, mirroring how the player's own defense
            // works - the multiplier then represents the ash's own resilience on top.
            Player owner = Main.player[projectile.owner];
            float afterDefense = rawDamage - (owner.statDefense * owner.DefenseEffectiveness.Value);

            if (afterDefense < 1f)
            {
                afterDefense = 1f;
            }

            int damage = (int)(afterDefense * damageMult);

            if (damage < 1)
            {
                damage = 1;
            }

            MinionHealth -= damage;
            TicksSinceLastHit = 0;

            CombatText.NewText(projectile.Hitbox, new Color(190, 120, 255), damage);

            if (MinionHealth > 0f)
            {
                return;
            }

            MinionHealth = 0f;

            SpawnDeathBurst(projectile);

            // No recovery bookkeeping here - RecoveryDeadline was already stamped (or inherited) back in
            // OnSpawn. Dying just means whatever warranty was already running now decides the outcome: if
            // it has not matured yet, the still-pending entry recorded at spawn blocks resummon until it
            // does; if it already matured, there is nothing left to block anything with.

            // A GENUINE death, unlike a buff-cancel or slot-eviction, really did reach 0 HP - drop the
            // alive-state snapshot so the next spawn correctly defaults to full health instead of
            // inheriting a dead instance's 0. See SpiritAshesRecoveryPlayer.RecordAliveState.
            owner.GetModPlayer<SpiritAshesRecoveryPlayer>().ClearAliveState(projectile.type);

            if (SpawningBuffType > 0)
            {
                owner.ClearBuff(SpawningBuffType);
            }

            // Some spawner projectiles (AbigailCounter) persist and silently respawn their real child
            // on their own - see SummonerRework.TryGetSpawnerType. Killing the spawner alongside a
            // genuine combat death is what stops that respawn loop; without this, the death above is
            // recorded correctly but the spawner just recreates the minion on its own next tick,
            // bypassing the cooldown entirely.
            if (SummonerRework.TryGetSpawnerType(projectile.type, out int spawnerType))
            {
                KillActiveInstancesOfType(owner, spawnerType);
            }

            projectile.Kill();
        }

        /// <summary>Kills every active instance of `projectileType` owned by `owner` - see the
        /// spawner-cleanup call in TakeDamage above.</summary>
        private static void KillActiveInstancesOfType(Player owner, int projectileType)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile candidate = Main.projectile[i];

                if (candidate.active && candidate.owner == owner.whoAmI && candidate.type == projectileType)
                {
                    candidate.Kill();
                }
            }
        }

        /// <summary>
        /// How many living instances of `projectileType` this player currently owns. Used to detect
        /// whether a summon-weapon cast would be climbing back toward a previously-proven army size
        /// (see SpiritAshesRecoveryPlayer) rather than every hit needing its own tracking.
        /// </summary>
        public static int CountAlive(Player owner, int projectileType)
        {
            int count = 0;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile candidate = Main.projectile[i];

                if (candidate.active && candidate.owner == owner.whoAmI && candidate.type == projectileType)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Finds another living tracked ash of the exact same type and owner as `newInstance` - the
        /// signature of a reposition recast replacing it, rather than a fresh addition to the army or a
        /// resurrection after a real death. Returns null if none exists.
        /// </summary>
        private static SpiritAshesMinions FindReplacedInstance(Projectile newInstance)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile candidate = Main.projectile[i];

                if (candidate.whoAmI == newInstance.whoAmI || !candidate.active
                    || candidate.owner != newInstance.owner || candidate.type != newInstance.type)
                {
                    continue;
                }

                SpiritAshesMinions ash = candidate.GetGlobalProjectile<SpiritAshesMinions>();

                if (ash.IsTrackedAsh)
                {
                    return ash;
                }
            }

            return null;
        }

        /// <summary>
        /// Splits the amount an Estus draught just restored across every wounded spirit ash the player
        /// owns, so one charge repairs the army by that total rather than by that much EACH. A large
        /// army therefore heals broadly but shallowly, a single hurt ash gets the lot.
        ///
        /// Full-health ashes are left out of the division rather than counted and wasted - otherwise a
        /// charge drunk with one wounded minion among four healthy ones would deliver a quarter of its
        /// value and look broken.
        ///
        /// Called when a drink FINISHES rather than from the per-tick healing loop, because that loop
        /// stops entirely once the player is at full health - a summoner topped off but with a battered
        /// army would otherwise get nothing for the charge.
        /// </summary>
        public static void HealOwnedMinions(Player owner, int amount)
        {
            if (amount <= 0 || owner.whoAmI != Main.myPlayer || !SummonerRework.Active(owner))
            {
                return;
            }

            int woundedCount = 0;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile candidate = Main.projectile[i];

                if (!candidate.active || candidate.owner != owner.whoAmI)
                {
                    continue;
                }

                SpiritAshesMinions counted = candidate.GetGlobalProjectile<SpiritAshesMinions>();

                if (counted.IsTrackedAsh && counted.MinionHealth < counted.MinionMaxHealth)
                {
                    woundedCount++;
                }
            }

            if (woundedCount == 0)
            {
                return;
            }

            float share = amount / (float)woundedCount;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile candidate = Main.projectile[i];

                if (!candidate.active || candidate.owner != owner.whoAmI)
                {
                    continue;
                }

                SpiritAshesMinions ash = candidate.GetGlobalProjectile<SpiritAshesMinions>();

                if (!ash.IsTrackedAsh || ash.MinionHealth >= ash.MinionMaxHealth)
                {
                    continue;
                }

                float healed = Math.Min(share, ash.MinionMaxHealth - ash.MinionHealth);
                ash.MinionHealth += healed;

                // Show the bar so the player can see the army top up, and mark the heal on the minion
                // itself rather than only on the player who drank.
                ash.TicksSinceLastHit = 0;
                CombatText.NewText(candidate.Hitbox, new Color(120, 255, 150), (int)healed);

                for (int dust = 0; dust < 8; dust++)
                {
                    Dust spark = Dust.NewDustDirect(candidate.position, candidate.width, candidate.height,
                        DustID.HealingPlus, 0f, -1.5f, 100, default, 1.2f);
                    spark.noGravity = true;
                }
            }
        }

        /// <summary>
        /// The death effect, identical for every minion and sentry regardless of type. One shared burst
        /// rather than per-summon flourishes: a player needs to recognise "an ash just died" instantly and
        /// from the corner of their eye, which only works if the cue never changes.
        ///
        /// 86 particles - white, cast outward at twice the original speed, and slightly slowed (0.94
        /// damping) so it reads as a spirit dissipating across a wide area rather than as an explosion.
        /// noGravity keeps it hanging where the minion was instead of raining down.
        /// </summary>
        private static void SpawnDeathBurst(Projectile projectile)
        {
            const int dustCount = 86;

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 outward = Main.rand.NextVector2CircularEdge(6.8f, 6.8f) * Main.rand.NextFloat(0.4f, 1.15f);

                Dust dust = Dust.NewDustDirect(
                    projectile.position,
                    projectile.width,
                    projectile.height,
                    DustID.Cloud,
                    outward.X,
                    outward.Y,
                    Alpha: 90,
                    newColor: Color.White,
                    Scale: Main.rand.NextFloat(1.1f, 1.8f));

                dust.noGravity = true;
                dust.velocity *= 0.94f;
            }
        }

        /// <summary>
        /// Shows the owning player an enemy-style name and health label when the cursor is over one of
        /// their tracked ashes. This runs after vanilla checks items, players, and NPCs, so their hover
        /// text keeps priority when hitboxes overlap.
        /// </summary>
        public static void ShowHoverText(Rectangle mouseRectangle)
        {
            if (Main.mouseText || Main.LocalPlayer.mouseInterface)
            {
                return;
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];

                if (!projectile.active || projectile.owner != Main.myPlayer || !mouseRectangle.Intersects(projectile.Hitbox))
                {
                    continue;
                }

                SpiritAshesMinions ash = projectile.GetGlobalProjectile<SpiritAshesMinions>();

                if (!ash.IsTrackedAsh)
                {
                    continue;
                }

                Main.LocalPlayer.cursorItemIconEnabled = false;

                int health = Math.Max(0, (int)Math.Ceiling(ash.MinionHealth));
                int maxHealth = Math.Max(1, (int)Math.Ceiling(ash.MinionMaxHealth));
                string name = Lang.GetProjectileName(projectile.type).Value;

                Main.instance.MouseTextHackZoom($"{name}: {health}/{maxHealth}");
                Main.mouseText = true;
                return;
            }
        }

        public override void PostDraw(Projectile projectile, Color lightColor)
        {
            if (projectile.owner != Main.myPlayer || !IsTrackedAsh)
            {
                return;
            }

            // Only show the bar while this ash is actively taking fire, matching how a vanilla enemy
            // health bar appears on damage and fades once it stops.
            if (TicksSinceLastHit > SummonerRework.MinionHealthBarLingerTicks)
            {
                return;
            }

            const int barWidth = 28;
            const int barHeight = 4;
            const float barVerticalOffset = 12f;

            float healthFraction = MathHelper.Clamp(MinionHealth / MinionMaxHealth, 0f, 1f);

            Vector2 barPosition = new Vector2(
                projectile.Center.X - (barWidth / 2f),
                projectile.position.Y - barVerticalOffset) - Main.screenPosition;

            Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;

            Rectangle background = new Rectangle((int)barPosition.X, (int)barPosition.Y, barWidth, barHeight);
            Main.spriteBatch.Draw(pixel, background, new Color(20, 10, 30, 180));

            Rectangle foreground = new Rectangle((int)barPosition.X, (int)barPosition.Y, (int)(barWidth * healthFraction), barHeight);
            Main.spriteBatch.Draw(pixel, foreground, new Color(170, 100, 240));
        }
    }
}
