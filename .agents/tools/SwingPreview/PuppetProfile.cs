using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using tsorcRevamp.NPCs.Puppets;

namespace SwingPreview
{
    /// <summary>
    /// The per-puppet switches that decide what a combo actually looks like in game, read out of the
    /// compiled mod rather than retyped. Every one of these is a protected/private member of
    /// PuppetNPC or a boss subclass, which is why the preview used to guess them - and guessed wrong
    /// (it swung Gwyn Linear while the game swung him Smooth, and carried the blade at 0.78 rad when
    /// PuppetNPC.HoldRotation is -0.30).
    ///
    /// Read from an UNINITIALIZED instance (no constructor, no SetDefaults), so a getter that touches
    /// NPC/Main state throws; each read falls back to the PuppetNPC base default and records a note.
    /// Every getter used today is a constant or a pure expression over other getters.
    /// </summary>
    internal sealed class PuppetProfile
    {
        public string Name = "PuppetNPC base";

        // Gate 1: UseAuthoredComboSwingClock. Off -> every step uses UseSwingEasing (Smooth/Linear)
        // and ignores its authored Ease. Also sizes arc steps to AttackTicks/SwingSpeedMult.
        public bool AuthoredClock;
        public bool UseSwingEasing;
        public bool AimSwingActive;
        public bool LogicalTelegraphs;
        public bool LandingTimedLeapSlam;

        public float OverheadWindupOvershoot;
        public float ComboTelegraphMultiplier = 1.35f;
        public int MinComboTelegraphTicks = 30;
        public int InterStepLingerTicks;
        public int RecoveryLingerTicks;
        public int DefaultRecoveryTicks = 25;

        /// <summary>PuppetNPC.LogicalWindupSettleFraction for the bound combo.</summary>
        public float WindupSettleFraction = 0.25f;

        /// <summary>Landing-timed LeapSlam poses for the bound combo (PuppetNPC.LeapSlamCarryRotation /
        /// LeapSlamImpactRotation). The base getters need FrontHandWeapon, which is live state, so
        /// they fall back to the formula with a zero weapon RotationOffset.</summary>
        public float LeapCarryRotation;
        public float LeapImpactRotation;

        /// <summary>Ticks a leap stays aloft on flat ground: 2 * LeapAttackUpSpeed / gravity 0.3, as
        /// PuppetNPC.BeginLeapAttack's far branch solves it.</summary>
        public int LeapAirtimeTicks = 63;

        /// <summary>PuppetNPC.MeleeHandleNorm - the normalised grip point in the weapon texture that
        /// DrawWeaponToLayer pins to the hand - and MeleeWeaponDrawScale, the sprite's scale relative
        /// to the body (DrawPlayer's PuppetDrawScale enlarges both together, so it cancels).</summary>
        public float HandleNormX = 0.10f;
        public float HandleNormY = 0.85f;
        public float WeaponDrawScale = 1f;

        public float HoldRotation = -0.30f;
        public int DefaultWeaponAnimMax = 22;

        /// <summary>item.useAnimation of the puppet's melee weapon. In game this, NOT the step's
        /// AttackTicks, is the swing clock for every non-arc motion (JoustDash, Leap*, Spin...) and
        /// for every motion at all when the authored clock is off.</summary>
        public int WeaponUseAnimation = 22;

        public MeleeCombo[] Pool;
        public readonly List<string> Notes = new List<string>();

        private object _instance;
        private MethodInfo _modifyEndpoints;
        private MethodInfo _customizeCombo;

        /// <summary>Builds the profile for a named PuppetNPC subclass, or the base defaults when
        /// <paramref name="puppetName"/> matches no class (--archetype previews with no boss).</summary>
        public static PuppetProfile For(string puppetName)
        {
            var profile = new PuppetProfile();
            profile.ReadBaseConstants();

            Type puppetType = PuppetTypeNamed(puppetName);
            if (puppetType == null || puppetType.IsAbstract)
            {
                profile.Notes.Add("no puppet class matched: PuppetNPC base defaults (no authored clock, Linear, 0 linger)");
                return profile;
            }

            profile.Name = puppetType.Name;
            profile._instance = RuntimeHelpers.GetUninitializedObject(puppetType);

            profile.UseSwingEasing = profile.Read("UseSwingEasing", false);
            profile.AimSwingActive = profile.Read("AimSwingActive", false);
            profile.AuthoredClock = profile.Read("UseAuthoredComboSwingClock", profile.AimSwingActive);
            profile.LogicalTelegraphs = profile.Read("UseLogicalMeleeTelegraphs", false);
            profile.LandingTimedLeapSlam = profile.Read("UseLandingTimedLeapSlam", false);
            profile.OverheadWindupOvershoot = profile.Read("OverheadWindupOvershoot", 0f);
            profile.ComboTelegraphMultiplier = profile.Read("ComboTelegraphMultiplier", 1.35f);
            profile.MinComboTelegraphTicks = profile.Read("MinComboTelegraphTicks", 30);
            profile.InterStepLingerTicks = profile.Read("MeleeComboInterStepLingerTicks", 0);
            profile.RecoveryLingerTicks = profile.Read("MeleeRecoveryLingerTicks", 0);
            profile.DefaultRecoveryTicks = profile.Read("MeleeRecoveryTicks", 25);
            float leapUpSpeed = profile.Read("LeapAttackUpSpeed", 9.5f);
            profile.LeapAirtimeTicks = (int)Math.Round(2f * leapUpSpeed / 0.3f);

            Microsoft.Xna.Framework.Vector2 handleNorm = profile.Read("MeleeHandleNorm", new Microsoft.Xna.Framework.Vector2(0.10f, 0.85f));
            profile.HandleNormX = handleNorm.X;
            profile.HandleNormY = handleNorm.Y;
            profile.WeaponDrawScale = profile.Read("MeleeWeaponDrawScale", 1f);

            BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            profile._modifyEndpoints = puppetType.GetMethod("ModifyMeleeArcEndpoints", instanceFlags);
            profile._customizeCombo = puppetType.GetMethod("CustomizeMeleeCombo", instanceFlags);

            // Same resolution as PuppetNPC.EnsureMeleeComboPool: the bespoke override, else the
            // archetype table.
            profile.Pool = profile.Read<MeleeCombo[]>("MeleeComboPoolOverride", null);
            if (profile.Pool == null)
            {
                WeaponArchetype archetype = profile.Read("MeleeArchetype", WeaponArchetype.Broadsword);
                profile.Pool = WeaponArchetypeTables.GetMeleeCombos(archetype);
            }

            profile.WeaponUseAnimation = profile.ReadWeaponUseAnimation(puppetType);
            return profile;
        }

        /// <summary>
        /// Makes <paramref name="combo"/> the instance's live combo, in MeleeComboAttack, and re-reads
        /// the getters that can depend on it. Bosses key per-combo tuning on ActiveMeleeComboName
        /// plus Phase (Gwyn's Wrath Flurry arcs and step linger), and on an uninitialized instance
        /// both read as "no combo". Call before simulating each combo.
        /// </summary>
        public void Bind(MeleeCombo combo)
        {
            if (_instance == null)
            {
                return;
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            FieldInfo comboField = typeof(PuppetNPC).GetField("_activeMeleeCombo", flags);
            FieldInfo phaseField = typeof(PuppetNPC).GetField("Phase", flags);
            if (comboField == null || phaseField == null)
            {
                AddNoteOnce("cannot bind the previewed combo (_activeMeleeCombo/Phase renamed?): per-combo overrides not applied");
                return;
            }

            comboField.SetValue(_instance, combo);
            phaseField.SetValue(_instance, Enum.Parse(phaseField.FieldType, "MeleeComboAttack"));

            InterStepLingerTicks = Read("MeleeComboInterStepLingerTicks", 0);
            RecoveryLingerTicks = Read("MeleeRecoveryLingerTicks", 0);
            WindupSettleFraction = Read("LogicalWindupSettleFraction", 0.25f);
            LandingTimedLeapSlam = Read("UseLandingTimedLeapSlam", false);   // Gwyn keys it on the combo

            // Base formula (PuppetNPC.LeapSlamCarryRotation/ImpactRotation) with RotationOffset 0,
            // used only when the puppet's getter needs live state.
            float restDegrees = Read("MeleeNaturalRestAngleDeg", 45f);
            float carryFallback = (float)((-105f + restDegrees) * Math.PI / 180.0);
            float impactFallback = (float)((75f + restDegrees) * Math.PI / 180.0);
            LeapCarryRotation = ReadQuietly("LeapSlamCarryRotation", carryFallback);
            LeapImpactRotation = ReadQuietly("LeapSlamImpactRotation", impactFallback);
        }

        /// <summary>PuppetNPC.WeaponSheathed for the bound combo at a given phase and PhaseTimer - bosses
        /// key it on how far into a recovery they are (Gwyn puts the sword away after the landing
        /// beat of Wrath Flurry's recovery). Leaves Phase/PhaseTimer set; Bind resets Phase.</summary>
        public bool IsSheathed(string phaseName, int phaseTimer)
        {
            if (_instance == null)
            {
                return false;
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            FieldInfo phaseField = typeof(PuppetNPC).GetField("Phase", flags);
            FieldInfo timerField = typeof(PuppetNPC).GetField("PhaseTimer", flags);
            if (phaseField == null || timerField == null)
            {
                return false;
            }

            phaseField.SetValue(_instance, Enum.Parse(phaseField.FieldType, phaseName));
            timerField.SetValue(_instance, phaseTimer);
            return ReadQuietlyBool("WeaponSheathed");
        }

        private bool ReadQuietlyBool(string property)
        {
            PropertyInfo info = FindProperty(_instance.GetType(), property);
            if (info == null)
            {
                return false;
            }
            try
            {
                return (bool)info.GetValue(_instance);
            }
            catch (TargetInvocationException)
            {
                return false;
            }
        }

        /// <summary>Read without noting a fallback - for getters that are EXPECTED to need live state
        /// on most puppets, where a note on every run would be noise.</summary>
        private float ReadQuietly(string property, float fallback)
        {
            PropertyInfo info = FindProperty(_instance.GetType(), property);
            if (info == null)
            {
                return fallback;
            }
            try
            {
                return (float)info.GetValue(_instance);
            }
            catch (TargetInvocationException)
            {
                return fallback;
            }
        }

        /// <summary>PuppetNPC.ModifyMeleeArcEndpoints on this puppet (Artorias widens his arcs).</summary>
        public void ModifyEndpoints(ComboMotion motion, ref float start, ref float end)
        {
            if (_modifyEndpoints == null || _instance == null)
            {
                return;
            }

            object[] args = { motion, start, end };
            try
            {
                _modifyEndpoints.Invoke(_instance, args);
                start = (float)args[1];
                end = (float)args[2];
            }
            catch (TargetInvocationException)
            {
                AddNoteOnce("ModifyMeleeArcEndpoints threw headless: base arc endpoints used");
            }
        }

        /// <summary>The isolated copy the game runs: Steps cloned, then CustomizeMeleeCombo at full
        /// health (Artorias raises every PostStepPause to 30 and retargets Ground Pound here).</summary>
        public MeleeCombo Customize(MeleeCombo combo)
        {
            if (combo.Steps != null)
            {
                combo.Steps = (MeleeComboStep[])combo.Steps.Clone();
            }

            if (_customizeCombo == null || _instance == null)
            {
                return combo;
            }

            object[] args = { combo, 1f };
            try
            {
                _customizeCombo.Invoke(_instance, args);
                return (MeleeCombo)args[0];
            }
            catch (TargetInvocationException)
            {
                AddNoteOnce("CustomizeMeleeCombo threw headless: combos shown as authored in the table");
                return combo;
            }
        }

        private void ReadBaseConstants()
        {
            HoldRotation = ReadConstant("HoldRotation", HoldRotation);
            DefaultWeaponAnimMax = ReadConstant("DefaultWeaponAnimMax", DefaultWeaponAnimMax);
            WeaponUseAnimation = DefaultWeaponAnimMax;
        }

        private static T ReadConstant<T>(string name, T fallback)
        {
            FieldInfo field = typeof(PuppetNPC).GetField(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null)
            {
                return fallback;
            }
            return (T)field.GetValue(null);
        }

        private T Read<T>(string property, T fallback)
        {
            PropertyInfo info = FindProperty(_instance.GetType(), property);
            if (info == null)
            {
                AddNoteOnce($"{property} not found: base default used");
                return fallback;
            }

            try
            {
                return (T)info.GetValue(_instance);
            }
            catch (TargetInvocationException)
            {
                AddNoteOnce($"{property} needs live NPC state: base default used");
                return fallback;
            }
        }

        // GetProperty on the concrete type misses a PRIVATE property declared on a base class
        // (AimSwingActive), so walk the chain. Invoking the base PropertyInfo still dispatches to
        // the override for virtual getters.
        private static PropertyInfo FindProperty(Type type, string name)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;
            for (Type current = type; current != null; current = current.BaseType)
            {
                PropertyInfo found = current.GetProperty(name, flags);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds the melee weapon's useAnimation without a running game: the MeleeWeaponItemType
        /// getter compiles to <c>call ModContent.ItemType&lt;T&gt;()</c>, and T.SetDefaults compiles
        /// <c>Item.useAnimation = N</c> to <c>ldc.i4 N; stfld Item::useAnimation</c>. Both are found by
        /// scanning the IL for those two tokens. Unknown shapes fall back to DefaultWeaponAnimMax.
        /// </summary>
        private int ReadWeaponUseAnimation(Type puppetType)
        {
            PropertyInfo weaponProperty = FindProperty(puppetType, "MeleeWeaponItemType");
            MethodInfo getter = weaponProperty?.GetGetMethod(nonPublic: true);
            byte[] getterIl = getter?.GetMethodBody()?.GetILAsByteArray();

            Type itemType = null;
            if (getterIl != null)
            {
                for (int i = 0; i + 4 < getterIl.Length && itemType == null; i++)
                {
                    if (getterIl[i] != 0x28)   // call
                    {
                        continue;
                    }
                    try
                    {
                        MethodBase called = getter.Module.ResolveMethod(BitConverter.ToInt32(getterIl, i + 1),
                            getter.DeclaringType.GetGenericArguments(), null);
                        if (called.Name == "ItemType" && called.IsGenericMethod)
                        {
                            itemType = called.GetGenericArguments()[0];
                        }
                    }
                    catch (Exception)
                    {
                        // A 0x28 byte inside another instruction's operand; keep scanning.
                    }
                }
            }

            if (itemType == null)
            {
                Notes.Add($"weapon useAnimation unresolved: {DefaultWeaponAnimMax} (DefaultWeaponAnimMax) used, pass --useanim");
                return DefaultWeaponAnimMax;
            }

            MethodInfo setDefaults = itemType.GetMethod("SetDefaults", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            byte[] il = setDefaults?.GetMethodBody()?.GetILAsByteArray();
            if (il != null)
            {
                for (int i = 1; i + 4 < il.Length; i++)
                {
                    if (il[i] != 0x7D)   // stfld
                    {
                        continue;
                    }

                    string fieldName = null;
                    try
                    {
                        fieldName = setDefaults.Module.ResolveField(BitConverter.ToInt32(il, i + 1)).Name;
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    if (fieldName != "useAnimation")
                    {
                        continue;
                    }

                    int? value = null;
                    if (i >= 2 && il[i - 2] == 0x1F)   // ldc.i4.s
                    {
                        value = (sbyte)il[i - 1];
                    }
                    else if (i >= 5 && il[i - 5] == 0x20)   // ldc.i4
                    {
                        value = BitConverter.ToInt32(il, i - 4);
                    }
                    else if (il[i - 1] >= 0x16 && il[i - 1] <= 0x1E)   // ldc.i4.0 .. ldc.i4.8
                    {
                        value = il[i - 1] - 0x16;
                    }

                    if (value.HasValue && value.Value > 0)
                    {
                        Notes.Add($"weapon {itemType.Name}: useAnimation {value.Value}");
                        return value.Value;
                    }
                }
            }

            Notes.Add($"weapon {itemType.Name}: useAnimation not found in SetDefaults, {DefaultWeaponAnimMax} used, pass --useanim");
            return DefaultWeaponAnimMax;
        }

        private void AddNoteOnce(string note)
        {
            if (!Notes.Contains(note))
            {
                Notes.Add(note);
            }
        }

        /// Finds a PuppetNPC subclass by bare class name. GetTypes() throws on a partially-loadable
        /// assembly (the mod references plenty we do not resolve headlessly), and the surviving
        /// types in the exception are still usable, which is the whole point of catching it.
        public static Type PuppetTypeNamed(string puppet)
        {
            if (string.IsNullOrWhiteSpace(puppet))
            {
                return null;
            }

            Type[] types;
            try
            {
                types = typeof(PuppetNPC).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(type => type != null).ToArray();
            }

            return types.FirstOrDefault(type =>
                typeof(PuppetNPC).IsAssignableFrom(type)
                && type.Name.Equals(puppet, StringComparison.OrdinalIgnoreCase));
        }
    }
}
