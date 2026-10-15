using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.FriendlyPuppets
{
    /// <summary>
    /// The hostile layer on top of <see cref="MirrorPuppetNPC"/>: the mirror invader's damage path (design §12.3–12.4).
    /// The attack is still 100% the player's weapon run through the swap; this layer only redirects who it hurts.
    ///
    ///   Projectiles — MirrorPuppetProjectile flips everything this puppet's swap spawns to hostile (synced through its
    ///                 extra AI), so vanilla tests every client's own player against the real projectile.
    ///   Melee       — the swing's hitbox, kept only for Strike-segment runs of the eased clock (MirrorPuppetNPC
    ///                 .StrikeHitbox), is tested against the host's own player here and streamed to the other clients
    ///                 in the owner-state extras, where each tests ITS own player (victim-authoritative).
    ///   Vanilla     — the borrowed ItemCheck lands no NPC or PvP hits (<see cref="SwapHitsAsOwner"/> = false).
    ///   Damage      — base weapon damage (item damage incl. prefix, none of the host's bonuses), reduced by the victim's
    ///                 defense, then capped at 15–25% of the victim's max HP by weapon weight (HostileMirrorPuppetPlayer).
    /// </summary>
    public abstract class HostileMirrorPuppetNPC : MirrorPuppetNPC
    {
        /// <summary>Per-hit cap (§12.4): 15% of the victim's max HP for a light weapon, up to 25% for a heavy one.</summary>
        internal const float HitCapLightFraction = 0.15f;
        internal const float HitCapHeavyExtraFraction = 0.10f;

        /// <summary>Weight from the weapon's native use time: 10 ticks (dagger) = 0, 40 ticks (greatsword) = 1.</summary>
        internal const float WeightLightUseTime = 10f;
        internal const float WeightHeavyUseTime = 40f;

        /// <summary>How long the last strike hitbox stays readable for debug drawing (test rig), in ticks.</summary>
        internal const int StrikeDebugShowTicks = 6;

        public override void SetDefaults()
        {
            base.SetDefaults();

            NPC.friendly = false;
        }

        // Hostile puppets swing on the eased clock (§12.6) so every melee strike is rollable.
        protected override bool UsesEasedMeleeClock => true;

        internal override bool SwapHitsAsOwner => false;

        internal override void ModifySwapWeaponDamage(Item item, Player owner, ref StatModifier damage)
        {
            // Base weapon damage: drop the host's class/armor bonuses so a host change doesn't change how hard it hits.
            // Hooks that run after this one (later GlobalItems) can still add item-specific modifiers.
            damage = StatModifier.Default;
        }

        /// <summary>Cap fraction for hits from <paramref name="weapon"/>, from item stats only (useAnimation, not the
        /// host's attack speed). Callers: projectile tagging (MirrorPuppetProjectile) and the melee strike below.</summary>
        internal static float GetHitCapFraction(Item weapon)
        {
            if (weapon == null || weapon.IsAir)
            {
                return HitCapLightFraction;
            }

            float weight = MathHelper.Clamp((weapon.useAnimation - WeightLightUseTime) / (WeightHeavyUseTime - WeightLightUseTime), 0f, 1f);
            return HitCapLightFraction + HitCapHeavyExtraFraction * weight;
        }

        // ── Melee strike: host computes it, every client tests its own player ────────────────────────

        // This tick's strike (host) or the last streamed one (other clients). The hitbox is kept relative to
        // NPC.position so on remote screens it stays attached to the body they see.
        private bool _strikeActive;
        private Rectangle _strikeHitboxOffset;
        private int _strikeDamage;

        /// <summary>Cap of the current strike's weapon; read by HostileMirrorPuppetPlayer.ModifyHurt on the victim.</summary>
        internal float StrikeCapFraction;

        /// <summary>Last strike hitbox in world coords and how many more ticks to show it (debug drawing).</summary>
        internal Rectangle LastStrikeHitboxWorld;
        internal int LastStrikeShowTicks;

        public override void AI()
        {
            if (LastStrikeShowTicks > 0)
            {
                LastStrikeShowTicks--;
            }
        }

        protected override void AfterWeaponUse(Player owner, Item weapon)
        {
            _strikeActive = HasStrikeHitbox && weapon != null && !weapon.IsAir;

            if (!_strikeActive)
            {
                return;
            }

            Rectangle strikeWorld = StrikeHitbox;
            _strikeHitboxOffset = new Rectangle(strikeWorld.X - (int)NPC.position.X, strikeWorld.Y - (int)NPC.position.Y, strikeWorld.Width, strikeWorld.Height);
            _strikeDamage = weapon.damage;
            StrikeCapFraction = GetHitCapFraction(weapon);

            // The host's own player is a victim like any other. The swap has restored the host's real position by now.
            TryStrikeLocalPlayer(strikeWorld);
        }

        /// <summary>Subclasses that add their own extras must call base first (and in ReadOwnerExtras).</summary>
        protected override void WriteOwnerExtras(BinaryWriter writer)
        {
            writer.Write(_strikeActive);

            if (!_strikeActive)
            {
                return;
            }

            writer.Write((short)_strikeHitboxOffset.X);
            writer.Write((short)_strikeHitboxOffset.Y);
            writer.Write((short)_strikeHitboxOffset.Width);
            writer.Write((short)_strikeHitboxOffset.Height);
            writer.Write(_strikeDamage);
            writer.Write((byte)Math.Round(StrikeCapFraction * 100f));
        }

        protected override void ReadOwnerExtras(BinaryReader reader)
        {
            _strikeActive = reader.ReadBoolean();

            if (!_strikeActive)
            {
                return;
            }

            int offsetX = reader.ReadInt16();
            int offsetY = reader.ReadInt16();
            int width = reader.ReadInt16();
            int height = reader.ReadInt16();
            _strikeHitboxOffset = new Rectangle(offsetX, offsetY, width, height);
            _strikeDamage = reader.ReadInt32();
            StrikeCapFraction = reader.ReadByte() / 100f;

            // The server only forwards; each client tests its own player against the box on its own copy of the body.
            if (Main.dedServ)
            {
                return;
            }

            Rectangle strikeWorld = new Rectangle((int)NPC.position.X + offsetX, (int)NPC.position.Y + offsetY, width, height);
            TryStrikeLocalPlayer(strikeWorld);
        }

        /// <summary>Victim-authoritative melee hit: this machine's own player against one strike hitbox. Player.Hurt
        /// respects i-frames (rolls, post-hit immunity), so a strike lands at most once per 40 ticks; the cap and the
        /// death message are applied in HostileMirrorPuppetPlayer.ModifyHurt (source = this NPC).</summary>
        private void TryStrikeLocalPlayer(Rectangle strikeWorld)
        {
            LastStrikeHitboxWorld = strikeWorld;
            LastStrikeShowTicks = StrikeDebugShowTicks;

            Player victim = Main.LocalPlayer;

            if (!victim.active || victim.dead || victim.immune)
            {
                return;
            }

            if (!strikeWorld.Intersects(victim.Hitbox))
            {
                return;
            }

            int hitDirection = victim.Center.X < NPC.Center.X ? -1 : 1;
            victim.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), _strikeDamage, hitDirection);
        }
    }

    /// <summary>
    /// Victim side of every hostile mirror hit (melee strike or flipped projectile): undo vanilla's ×2 on hostile
    /// projectiles, cap the hit at the weapon's fraction of max HP AFTER defense, and name the invader as the killer.
    /// </summary>
    public class HostileMirrorPuppetPlayer : ModPlayer
    {
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            PlayerDeathReason source = modifiers.DamageSource;

            if (source == null)
            {
                return;
            }

            float capFraction = 0f;
            int puppetIndex = -1;
            bool fromProjectile = false;
            int projectileIndex = source.SourceProjectileLocalIndex;
            int npcIndex = source.SourceNPCIndex;
            bool projectileIndexValid = projectileIndex >= 0 && projectileIndex < Main.maxProjectiles;
            bool npcIndexValid = npcIndex >= 0 && npcIndex < Main.maxNPCs;

            if (projectileIndexValid && Main.projectile[projectileIndex].active)
            {
                MirrorPuppetProjectile tag = Main.projectile[projectileIndex].GetGlobalProjectile<MirrorPuppetProjectile>();

                if (tag.HostileToPlayers)
                {
                    capFraction = tag.HitCapFraction;
                    puppetIndex = tag.PuppetNpcIndex;
                    fromProjectile = true;
                }
            }
            else if (npcIndexValid && Main.npc[npcIndex].active && Main.npc[npcIndex].ModNPC is HostileMirrorPuppetNPC hostilePuppet)
            {
                capFraction = hostilePuppet.StrikeCapFraction;
                puppetIndex = npcIndex;
            }

            if (capFraction <= 0f)
            {
                return;
            }

            // Projectile.Damage hurts players for DamageVar(damage) * 2 on hostile projectiles (vanilla's convention
            // for NPC shots); the invader's shots should deal base weapon damage like its blade.
            if (fromProjectile)
            {
                modifiers.SourceDamage *= 0.5f;
            }

            int maxDamage = Math.Max(1, (int)(capFraction * Player.statLifeMax2));
            modifiers.SetMaxDamage(maxDamage);

            // Killer = the invader, not "<host>'s <projectile>". CustomReason is synced with the death packet.
            bool puppetValid = puppetIndex >= 0 && puppetIndex < Main.maxNPCs && Main.npc[puppetIndex].active;

            if (puppetValid)
            {
                NetworkText killerName = Main.npc[puppetIndex].GetTypeNetName();
                source.CustomReason = NetworkText.FromKey("Mods.tsorcRevamp.NPCs.HostileMirrorPuppetNPC.DeathMessage", Player.name, killerName);
            }
        }
    }
}
