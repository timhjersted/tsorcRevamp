using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp {
    class ILEdits {
        internal static void ApplyILs() {
            IL.Terraria.Player.Update += Player_Update;
            IL.Terraria.Player.Update += Chest_Patch;
            IL.Terraria.Main.UpdateAudio += Music_Patch;
            //IL.Terraria.Main.DrawPlayer_DrawAllLayers += Rotate_Patch;
        }



        internal static void UnloadILs() {

        }
        internal static void Player_Update(ILContext il) {
            Mod mod = ModContent.GetInstance<tsorcRevamp>();

            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.Before,
                                    i => i.MatchLdfld("Terraria.Player", "statManaMax2"),
                                    i => i.MatchLdcI4(400))) {
                mod.Logger.Fatal("Could not find instruction to patch (Player_Update)");
                return;
            }

            cursor.Next.Next.Operand = int.MaxValue;
        }

        internal static void Chest_Patch(ILContext il) {
            ILCursor c = new ILCursor(il);

            if (!c.TryGotoNext(instr => instr.MatchLdcR4(1f) && instr.Next.Next.Next.Next.Next.Next.MatchStfld(typeof(Player).GetField("chest")))) {
                throw new Exception("Could not find instruction to patch (Chest_Patch)");
            }

            c.FindNext(out ILCursor[] cursors, instr => instr.MatchLdcR4(1f));
            c = cursors[0];

            c.Index++;
            c.EmitDelegate<Func<float, float>>((volume) => {
                if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().chestBankOpen
                || Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().chestPiggyOpen) {
                    // Return 0 volume if one is open so the sound is silent
                    return 0f;
                }

                return volume;
            });
        }

        internal static void Music_Patch(ILContext il) {
            ILCursor c = new ILCursor(il);
            if (!c.TryGotoNext(instr => instr.MatchLdcI4(6) && instr.Next.MatchStfld(typeof(Main).GetField("newMusic")))) {
                throw new Exception("Could not find instruction to patch (Music_Patch)");
            }
            c.Index++;
            c.EmitDelegate<Func<int, int>>(MusicDelegate);
        }

        internal static int MusicDelegate(int defaultMusic) {
            Mod musicMod = ModLoader.GetMod("tsorcMusic");
            if (musicMod != null) {
                /*if (ModContent.GetInstance<tsorcRevampConfig>().LegacyMusic)
                {
                    return musicMod.GetSoundSlot((Terraria.ModLoader.SoundType)51, "Sounds/Music/OldTitle");
                }
                else*/
                {
                    return musicMod.GetSoundSlot((Terraria.ModLoader.SoundType)51, "Sounds/Music/Night");
                }
            }
            else {
                return defaultMusic;
            }
        }
        /*
        private static void Rotate_Patch(ILContext il) {
            throw new NotImplementedException();
        }
        */
    }
}
