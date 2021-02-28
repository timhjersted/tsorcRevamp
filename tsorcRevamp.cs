using Terraria.ModLoader;

namespace tsorcRevamp {
    public class tsorcRevamp : Mod {
        public static ModHotKey toggleDragoonBoots;

        public override void Load() {
            toggleDragoonBoots = RegisterHotKey("Dragoon Boots", "Z");
        }
        public override void Unload() {
            toggleDragoonBoots = null;
        }
    }
}