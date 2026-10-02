using System.Collections.Generic;
using System.Security.Cryptography;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Content.Items.Weapons;

namespace tsorcRevamp.Utilities.Balance
{
    /// <summary>
    /// Remembers which item spawned a projectile, so damage from bullets, minions, whips and
    /// spell projectiles can be attributed back to the weapon the player actually chose.
    /// Without this, every ranged/magic/summon weapon in the log would read as zero DPS.
    /// </summary>
    internal sealed class BalanceSourceProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        internal int SourceItemType = -1;
        internal int SourceAmmoType = -1;

        /// <summary>
        /// Which of a multi-attack weapon's attacks produced this projectile, captured at spawn because
        /// it is unrecoverable later. Many weapons here map several attacks to one item via
        /// <see cref="FourAttackWeaponControls"/> (Sword of Lord Gwyn has four), and those
        /// attacks can differ in damage by a wide margin — averaging them together measures nothing.
        /// </summary>
        internal int SourceAttackMode;
        internal int SourceAltFunction;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            // Checked before the plain ItemUse case below, since this is a subclass of it.
            if (source is EntitySource_ItemUse_WithAmmo withAmmo && withAmmo.Item != null && !withAmmo.Item.IsAir)
            {
                SourceItemType = withAmmo.Item.type;
                SourceAmmoType = withAmmo.AmmoItemIdUsed;
                CaptureAttackMode(withAmmo.Player);
                return;
            }

            if (source is EntitySource_ItemUse itemUse && itemUse.Item != null && !itemUse.Item.IsAir)
            {
                SourceItemType = itemUse.Item.type;
                CaptureAttackMode(itemUse.Player);
                return;
            }

            if (source is EntitySource_Parent { Entity: Projectile parent }
                && parent.TryGetGlobalProjectile<BalanceSourceProjectile>(out var inherited))
            {
                SourceItemType = inherited.SourceItemType;
                SourceAmmoType = inherited.SourceAmmoType;
                SourceAttackMode = inherited.SourceAttackMode;
                SourceAltFunction = inherited.SourceAltFunction;
                return;
            }

            if (source is EntitySource_Parent { Entity: Player owner }
                && owner.HeldItem != null && !owner.HeldItem.IsAir)
            {
                SourceItemType = owner.HeldItem.type;
                CaptureAttackMode(owner);
            }
        }

        private void CaptureAttackMode(Player player)
        {
            if (player == null)
                return;
            SourceAttackMode = WeaponBench.AttackModeOf(player);
            SourceAltFunction = player.altFunctionUse == 2 ? 1 : 0;
        }

        /// <summary>
        /// Distinct NPCs this one projectile has struck over its whole lifetime. A piercing shot
        /// crossing a line of targets hits them on successive ticks, not the same tick, so timing
        /// windows can't detect piercing — tracking it per projectile can.
        /// </summary>
        private HashSet<int> _hitNpcs;

        internal int RegisterHit(int npcId)
        {
            _hitNpcs ??= new HashSet<int>();
            _hitNpcs.Add(npcId);
            return _hitNpcs.Count;
        }

        public override void PostAI(Projectile projectile)
        {
            // Counted once, on the projectile's first update rather than in OnSpawn, so that
            // SourceItemType is guaranteed resolved (including inherited chains) before we credit it.
            if (!_counted && projectile.owner == Main.myPlayer)
            {
                _counted = true;
                WeaponBench.NotifyProjectileSpawned(SourceItemType, SourceAttackMode, SourceAltFunction, projectile.type);
            }
        }

        private bool _counted;
    }

    internal sealed class BalanceLogNPC : GlobalNPC
    {
        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            int itemType = item?.type ?? -1;
            // Direct melee has no projectile to carry the attack mode, so it's read live at hit time —
            // the swing that is landing right now is the one still in progress.
            int mode = WeaponBench.AttackModeOf(player);
            int alt = player.altFunctionUse == 2 ? 1 : 0;

            BalanceLog.RecordHit(npc, player, itemType, -1, damageDone, hit.Crit);
            EnemyKillLog.RecordHit(npc, player, itemType, damageDone);
            WeaponBench.RecordHit(npc, player, itemType, -1, damageDone, hit.Crit, mode, alt,
                WeaponBench.MeleeAttackIndex(itemType, mode, alt, npc.whoAmI));
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile == null || projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;

            BalanceSourceProjectile attribution = projectile.GetGlobalProjectile<BalanceSourceProjectile>();
            int itemType = attribution.SourceItemType;
            Player owner = Main.player[projectile.owner];
            if (itemType <= 0 && owner?.HeldItem != null && !owner.HeldItem.IsAir)
                itemType = owner.HeldItem.type;

            BalanceLog.RecordHit(npc, owner, itemType, attribution.SourceAmmoType, damageDone, hit.Crit, projectile.type);
            EnemyKillLog.RecordHit(npc, owner, itemType, damageDone);
            WeaponBench.RecordHit(npc, owner, itemType, attribution.SourceAmmoType, damageDone, hit.Crit,
                attribution.SourceAttackMode, attribution.SourceAltFunction,
                attribution.RegisterHit(npc.whoAmI), projectile.type);
        }

        public override void OnKill(NPC npc)
        {
            BalanceLog.NotifyKilled(npc);
            EnemyKillLog.NotifyKilled(npc);
        }
    }

    /// <summary>
    /// Item hooks for the encounter log: a boss bag spawning from loot is the one unambiguous "this boss was
    /// killed" signal (it covers bosses that hand off to a final form and never fire OnKill on the anchor), and
    /// a consumed healing potion is how survival from healing is told apart from survival from defense.
    /// </summary>
    internal sealed class BalanceLogItem : GlobalItem
    {
        public override void OnSpawn(Item item, IEntitySource source)
        {
            if (ItemID.Sets.BossBag[item.type] && source is EntitySource_Loot)
            {
                BalanceLog.NotifyBossBag();
            }
        }

        public override void OnConsumeItem(Item item, Player player)
        {
            if (player.whoAmI != Main.myPlayer || item.healLife <= 0)
            {
                return;
            }

            BalanceLog.RecordHealing(item.ModItem?.Name ?? item.Name, item.healLife);
        }
    }

    internal sealed class BalanceLogPlayer : ModPlayer
    {
        // Crockford base32: no I, L, O or U, so a tag read aloud or typed from a screenshot is not misread.
        private const string TagAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        /// <summary>
        /// This character's 4-character log tag (about a million possible values, so uploads from many players rarely
        /// collide). Random, saved with the character, and never derived from the character's name, so renaming or
        /// copying the character keeps its file and an uploaded file does not name anyone.
        /// </summary>
        public string Tag = "";

        // Runs for every new player instance; LoadData replaces the value when the character already has a saved tag.
        public override void Initialize()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(4);
            char[] letters = new char[4];

            for (int i = 0; i < letters.Length; i++)
            {
                letters[i] = TagAlphabet[randomBytes[i] & 31];
            }

            Tag = new string(letters);
        }

        public override void SaveData(TagCompound tag)
        {
            tag["balanceTag"] = Tag;
        }

        public override void LoadData(TagCompound tag)
        {
            string saved = tag.GetString("balanceTag");

            if (!string.IsNullOrEmpty(saved))
            {
                Tag = saved;
            }
        }

        /// <summary>Set in OnHurt, consumed one tick later in PostUpdate - see PostUpdate for why the
        /// read can't happen directly in OnHurt. A plain bool rather than a counter: two hits landing in
        /// the exact same tick would under-count by one, which is an acceptable rare-case loss for
        /// telemetry, not a correctness requirement.</summary>
        private bool _pendingImmuneTimeCapture;

        public override void OnHurt(Player.HurtInfo info)
        {
            if (Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            BalanceLog.RecordDamageTaken(info);
            _pendingImmuneTimeCapture = true;
        }

        /// <summary>Feeds actual mana consumption to the encounter log and the dummy bench. Net-sampling statMana
        /// misses it entirely once regeneration per tick exceeds a weapon's cost - Ultima Tome (18 mana a use)
        /// benched as 0 mana per second across 20 samples.</summary>
        public override void OnConsumeMana(Item item, int manaConsumed)
        {
            if (Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            BalanceLog.RecordManaSpent(item.type, manaConsumed);
            WeaponBench.RecordManaSpent(item.type, manaConsumed);
        }

        /// <summary>
        /// Captures the iframe duration THIS hit actually granted, one tick late on purpose: vanilla's
        /// Player.Hurt calls PlayerLoader.OnHurt (which reaches ModPlayer.OnHurt above) BEFORE it writes
        /// the new Player.immuneTime - reading it inside OnHurt would return the PREVIOUS hit's leftover
        /// value, not this one's. By the next tick vanilla has already written the fresh grant, which
        /// already reflects iframe-extending effects like Cross Necklace's longInvince.
        /// </summary>
        public override void PostUpdate()
        {
            if (!_pendingImmuneTimeCapture)
            {
                return;
            }

            _pendingImmuneTimeCapture = false;
            BalanceLog.RecordHurtImmuneTime(Player.immuneTime);
        }
    }

    internal sealed class BalanceLogSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            BalanceLog.Update();
            EnemyKillLog.Update();
            WeaponBench.Update();
        }

        public override void OnWorldUnload()
        {
            // A fight in progress when the world closes is still worth keeping — it's a real
            // "player gave up on this boss" data point.
            BalanceLog.AbortForWorldChange();
            EnemyKillLog.Reset();

            // A benchmark run is not: it has no meaningful duration once interrupted.
            WeaponBench.Abort();
        }
    }
}
