using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.FriendlyPuppets
{
    /// <summary>
    /// The player-owned layer on top of <see cref="MirrorPuppetNPC"/> (Soul Mimic, swap test dummy): a friendly,
    /// by default invulnerable body that player weapons pass through, the Reflective Obsidian look, the summon damage
    /// rule (best-of bonus × <see cref="DamageShare"/>), minion slots, enemy aggro (FriendlyPuppetAggro.cs), and
    /// despawning when the owner leaves. Design: tsorcDocs/MimicTear_FriendlyPuppet_Design.md.
    /// </summary>
    public abstract class FriendlyPuppetNPC : MirrorPuppetNPC
    {
        /// <summary>
        /// Fraction of the owner's own damage the puppet deals with a weapon (applied in
        /// <see cref="ModifySwapWeaponDamage"/>). ESTIMATE — first-pass value, tune from play.
        /// Stamina already caps how often it attacks, so its real output lands below this share of the player's.
        /// </summary>
        public virtual float DamageShare => 0.5f;

        public override void SetDefaults()
        {
            base.SetDefaults();

            NPC.friendly = true;
            NPC.dontTakeDamage = true;
            NPC.immortal = true;
        }

        public override bool? CanBeHitByItem(Player player, Item item)
        {
            return false;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            // Post-hit immunity, shared with contact and the decoy redirect (PostHitImmunity).
            if (PostHitImmunity > 0)
            {
                return false;
            }

            // Player weapons never hit a friendly puppet. Hostile projectiles fall through to vanilla, which hits
            // friendly NPCs (Projectile.Damage: npc.friendly && !dontTakeDamageFromHostiles) — that only matters
            // for subclasses that turn off dontTakeDamage (the Mimic Tear has HP).
            bool playerProjectile = projectile.friendly && !projectile.hostile;

            if (playerProjectile)
            {
                return false;
            }

            return null;
        }

        /// <summary>
        /// The player's post-hit immunity, one window for every way a puppet can be hurt: a hit from anything (contact,
        /// a hostile projectile, a decoyed enemy's redirected attack) blocks all of them for PostHitImmunityTicks. Vanilla
        /// alone would give three separate windows — 10 ticks per projectile, 30 for contact, plus the redirect — so a
        /// puppet holding several enemies' attention was hit far more often than its owner would be. Per machine; set in
        /// HitEffect, which every strike path runs.
        /// </summary>
        internal int PostHitImmunity;

        /// <summary>Length of <see cref="PostHitImmunity"/>: the player's 40 ticks by default.</summary>
        internal virtual int PostHitImmunityTicks => 40;

        public override bool CanBeHitByNPC(NPC attacker)
        {
            return PostHitImmunity <= 0;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            PostHitImmunity = PostHitImmunityTicks;
        }

        public override void AI()
        {
            if (PostHitImmunity > 0)
            {
                PostHitImmunity--;
            }

            // Owner left the world: despawn (server decides; vanilla syncs the deactivation).
            DespawnIfOwnerGone();
        }

        /// <summary>Minion slots this puppet occupies on its owner (reserved each tick by
        /// <see cref="MirrorPuppetOwnerPlayer"/>). 0 = free.</summary>
        public virtual float GetMinionSlotsUsed(Player owner)
        {
            return 0f;
        }

        // ── Aggro (FriendlyPuppetAggro.cs) ──────────────────────────────────────────────────────────

        /// <summary>Whether enemies may switch their attention to this puppet. Off for invulnerable puppets.</summary>
        public virtual bool DrawsAggro => false;

        /// <summary>Per-moment veto, e.g. while still forming out of its summon pool.</summary>
        public virtual bool CanDrawAggroNow => true;

        // ── Damage: the owner's damage with the better of class or summon bonus, times the share ────

        internal override void ModifySwapWeaponDamage(Item item, Player owner, ref StatModifier damage)
        {
            // Best-of bonus: a warrior's mimic scales with melee gear, a summoner's mimic swinging the same sword
            // scales with summon gear. Compared as applied totals on the weapon's base damage; the summon side only
            // wins when it is strictly higher, and then lifts the incoming modifier by that ratio (which keeps any
            // item-specific modifiers other hooks already added).
            float baseDamage = Math.Max(1, item.damage);
            float classDamage = owner.GetTotalDamage(item.DamageType).ApplyTo(baseDamage);
            float summonDamage = owner.GetTotalDamage(DamageClass.Summon).ApplyTo(baseDamage);
            float bonusRatio = 1f;

            if (classDamage > 0f && summonDamage > classDamage)
            {
                bonusRatio = summonDamage / classDamage;
            }

            damage *= bonusRatio * DamageShare;
        }

        internal override void ModifySwapWeaponCrit(Item item, Player owner, ref float crit)
        {
            // Same best-of rule for crit chance. Vanilla summon crit is usually 0, so this rarely changes anything.
            float summonCrit = owner.GetTotalCritChance(DamageClass.Summon) + item.crit;

            if (summonCrit > crit)
            {
                crit = summonCrit;
            }
        }

        // ── Look: Reflective Obsidian shell over the owner's equipment ───────────────────────────

        /// <summary>
        /// Forces every dye shader to Reflective Obsidian and greys out skin/hair/eyes, since dyes don't reach skin.
        /// Overwrites every colour/dye field the base copied from the owner.
        /// </summary>
        protected override void StyleDisplayClone(Player clone)
        {
            ApplyObsidianShell(clone);
        }

        /// <summary>The Soul Mimic's look: gunmetal body colours and Reflective Obsidian Dye on every equipment slot and
        /// the skin. Callers: the friendly clone (above) and the mirror invader's adds (PuppetNPC.MirrorShell).</summary>
        internal static void ApplyObsidianShell(Player clone)
        {
            clone.hairDye = 0;

            // Gunmetal body colours: the "liquid metal" read comes from the dye; these stop bare skin and
            // cloth breaking it up.
            Color gunmetalDark = new Color(38, 40, 46);
            Color gunmetalMid = new Color(62, 65, 74);
            clone.skinColor = gunmetalMid;
            clone.hairColor = gunmetalDark;
            clone.eyeColor = new Color(150, 155, 170);
            clone.shirtColor = gunmetalDark;
            clone.underShirtColor = gunmetalMid;
            clone.pantsColor = gunmetalDark;
            clone.shoeColor = gunmetalDark;

            int obsidianShader = GameShaders.Armor.GetShaderIdFromItemId(ItemID.ReflectiveObsidianDye);
            clone.cHead = obsidianShader;
            clone.cBody = obsidianShader;
            clone.cLegs = obsidianShader;
            clone.cHandOn = obsidianShader;
            clone.cHandOff = obsidianShader;
            clone.cBack = obsidianShader;
            clone.cFront = obsidianShader;
            clone.cShoe = obsidianShader;
            clone.cWaist = obsidianShader;
            clone.cShield = obsidianShader;
            clone.cShieldFallback = obsidianShader;
            clone.cNeck = obsidianShader;
            clone.cFace = obsidianShader;
            clone.cFaceHead = obsidianShader;
            clone.cFaceFlower = obsidianShader;
            clone.cBalloon = obsidianShader;
            clone.cBalloonFront = obsidianShader;
            clone.cBackpack = obsidianShader;
            clone.cTail = obsidianShader;
            clone.cBeard = obsidianShader;
            clone.cWings = obsidianShader;

            // Dyes only reach equipment; without this, every bit of exposed skin (hands, face, gaps in the armor)
            // draws as bare skin and the copy reads as naked under a dyed shell. skinDyePacked is vanilla's skin
            // shader slot, packed as an ARMOR shader so it's the same Obsidian material as the armor.
            clone.skinDyePacked = PlayerDrawHelper.PackShader(obsidianShader, PlayerDrawHelper.ShaderConfiguration.ArmorShader);
        }
    }
}
