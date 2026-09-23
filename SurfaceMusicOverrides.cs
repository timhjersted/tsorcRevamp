using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp
{
    // Applies the same sticky-biome principle from SurfaceBackgroundOverrides to music: vanilla's
    // Main.UpdateAudio_DecideOnNewMusic falls back to the plain Forest track the instant no biome
    // Zone flag is set nearby (e.g. flying above desert), with no memory of what was just playing.
    // This remembers the last real track and replays it during that fallback, UNLESS the background
    // system's own neutral-ground check (already reused here, not duplicated) confirms we're
    // genuinely standing in real forest - in which case the forest track plays and becomes the new
    // sticky baseline. Anything else vanilla picks (a boss, an event, a real biome track, a music
    // box override applied after this hook returns) is left untouched and simply remembered.
    //
    // The tsorcMusic sub-mod's own tsorcMusicScene (a ModSceneEffect) is active for almost every
    // non-boss frame and resolves its OWN "OverworldDay"/"Night"/"HardmodeNight" tracks the same
    // Zone-flag way vanilla does - which wins over vanilla's raw IDs in Main's own priority chain
    // before this hook ever sees them. Those resolved slot IDs are treated as generic fallback too,
    // so the sticky check still fires when tsorcMusic is installed instead of silently doing nothing.
    internal static class SurfaceMusicOverrides
    {
        // The only three newMusic IDs that mean "vanilla found no biome/event/boss, plain Forest
        // fallback" without ambiguity. 79 (bloodmoon storm) is deliberately excluded - vanilla reuses
        // it for meteor/graveyard/bloodmoon-night-forest alike, so it can't be trusted as forest-only.
        private const int ForestDayCloudyMusic = 62;
        private const int ForestDayClearMusic = 63;
        private const int ForestNightMusic = 64;
        private const int NoStickyTrack = -1;
        private const int UnresolvedTrack = -1;

        private static int stickyMusicTrack = NoStickyTrack;

        // Runtime-assigned tsorcMusic slot IDs - resolved once its content is loaded (PostSetupContent),
        // since it's an optional sub-mod that may load after this one, or not be installed at all.
        private static int tsorcMusicForestDayTrack = UnresolvedTrack;
        private static int tsorcMusicForestNightTrack = UnresolvedTrack;
        private static int tsorcMusicForestHardmodeNightTrack = UnresolvedTrack;

        internal static void RegisterHook()
        {
            MethodInfo decideOnNewMusic = typeof(Main).GetMethod("UpdateAudio_DecideOnNewMusic", BindingFlags.NonPublic | BindingFlags.Instance);
            if (decideOnNewMusic != null)
            {
                MonoModHooks.Add(decideOnNewMusic, (Action<Action<Main>, Main>)ApplyStickyBiomeMusic);
            }
        }

        internal static void ResolveTsorcMusicForestTracks()
        {
            tsorcMusicForestDayTrack = UnresolvedTrack;
            tsorcMusicForestNightTrack = UnresolvedTrack;
            tsorcMusicForestHardmodeNightTrack = UnresolvedTrack;

            if (!ModLoader.TryGetMod("tsorcMusic", out Mod musicMod))
            {
                return;
            }

            tsorcMusicForestDayTrack = ResolveTrack(musicMod, "Sounds/Music/OverworldDay");
            tsorcMusicForestNightTrack = ResolveTrack(musicMod, "Sounds/Music/Night");
            tsorcMusicForestHardmodeNightTrack = ResolveTrack(musicMod, "Sounds/Music/HardmodeNight");
        }

        internal static void ClearResolvedTracks()
        {
            tsorcMusicForestDayTrack = UnresolvedTrack;
            tsorcMusicForestNightTrack = UnresolvedTrack;
            tsorcMusicForestHardmodeNightTrack = UnresolvedTrack;
            stickyMusicTrack = NoStickyTrack;
        }

        private static int ResolveTrack(Mod mod, string path)
        {
            return MusicLoader.MusicExists(mod, path) ? MusicLoader.GetMusicSlot(mod, path) : UnresolvedTrack;
        }

        private static void ApplyStickyBiomeMusic(Action<Main> orig, Main self)
        {
            orig(self);

            if (Main.dedServ || Main.gameMenu)
            {
                stickyMusicTrack = NoStickyTrack;
                return;
            }

            bool isPlainForestTrack = Main.newMusic == ForestDayCloudyMusic
                || Main.newMusic == ForestDayClearMusic
                || Main.newMusic == ForestNightMusic
                || (tsorcMusicForestDayTrack != UnresolvedTrack && Main.newMusic == tsorcMusicForestDayTrack)
                || (tsorcMusicForestNightTrack != UnresolvedTrack && Main.newMusic == tsorcMusicForestNightTrack)
                || (tsorcMusicForestHardmodeNightTrack != UnresolvedTrack && Main.newMusic == tsorcMusicForestHardmodeNightTrack);

            if (!isPlainForestTrack)
            {
                //A real biome, boss, or event won vanilla's (or tsorcMusic's) own decision - trust it and remember it.
                stickyMusicTrack = Main.newMusic;
                return;
            }

            //Re-run the (already sticky-corrected) surface background choice to reuse its neutral-ground
            //check instead of duplicating a second tile scan here.
            int effectiveBiomeStyle = Main.GetPreferredBGStyleForPlayer();
            bool isGenericFallbackStyle = effectiveBiomeStyle == SurfaceBackgroundOverrides.DefaultSurfaceBackgroundStyle
                || effectiveBiomeStyle == SurfaceBackgroundOverrides.ForestTreeVariantStyle1
                || effectiveBiomeStyle == SurfaceBackgroundOverrides.ForestTreeVariantStyle2
                || effectiveBiomeStyle == SurfaceBackgroundOverrides.ForestTreeVariantStyle3;

            if (isGenericFallbackStyle)
            {
                //Genuinely near ground in real forest (or no sticky memory yet) - keep the forest track.
                stickyMusicTrack = Main.newMusic;
                return;
            }

            if (stickyMusicTrack != NoStickyTrack)
            {
                Main.newMusic = stickyMusicTrack;
            }
        }
    }

    public sealed class SurfaceMusicOverridesSystem : ModSystem
    {
        public override void PostSetupContent()
        {
            SurfaceMusicOverrides.ResolveTsorcMusicForestTracks();
        }

        public override void Unload()
        {
            SurfaceMusicOverrides.ClearResolvedTracks();
        }
    }
}
