using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

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
        /// <see cref="Items.Weapons.FourAttackWeaponControls"/> (Sword of Lord Gwyn has four), and those
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
                WeaponBench.NotifyProjectileSpawned(SourceItemType, SourceAttackMode, SourceAltFunction);
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

            BalanceLog.RecordHit(npc, owner, itemType, attribution.SourceAmmoType, damageDone, hit.Crit);
            WeaponBench.RecordHit(npc, owner, itemType, attribution.SourceAmmoType, damageDone, hit.Crit,
                attribution.SourceAttackMode, attribution.SourceAltFunction,
                attribution.RegisterHit(npc.whoAmI));
        }

        public override void OnKill(NPC npc)
        {
            BalanceLog.NotifyKilled(npc);
        }
    }

    internal sealed class BalanceLogPlayer : ModPlayer
    {
        public override void OnHurt(Player.HurtInfo info)
        {
            if (Player.whoAmI == Main.myPlayer)
                BalanceLog.RecordDamageTaken(info.Damage);
        }
    }

    internal sealed class BalanceLogSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            BalanceLog.Update();
            WeaponBench.Update();
        }

        public override void OnWorldUnload()
        {
            // A fight in progress when the world closes is still worth keeping — it's a real
            // "player gave up on this boss" data point.
            BalanceLog.AbortForWorldChange();

            // A benchmark run is not: it has no meaningful duration once interrupted.
            WeaponBench.Abort();
        }
    }
}
