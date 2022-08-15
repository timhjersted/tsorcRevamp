using Mono.Cecil.Cil;
using static Mono.Cecil.Cil.OpCodes;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp
{
    class ILEdits
    {
        internal static void ApplyILs()
        {
            
            IL.Terraria.Player.Update += Player_Update;
            //IL.Terraria.Player.Update += Chest_Patch;
            IL.Terraria.Recipe.FindRecipes += SoulSlotRecipesPatch;

            //Disable drawing of wires when in adventure mode
            //editing a get accessor of a property and built in hooks don't have any of those
            HookEndpointManager.Modify(typeof(Terraria.GameContent.UI.WiresUI.Settings).GetProperty("DrawWires").GetGetMethod(), new ILContext.Manipulator(DrawWires_Patch));

            /*
            if (ModContent.GetInstance<tsorcRevampConfig>().GravityFix)
            {
                IL.Terraria.Main.DoDraw += Gravity_Screenflip_Patch;
                IL.Terraria.Main.DoDraw += Gravity_Rasterizer_Patch;
                IL.Terraria.Main.DoDraw += Gravity_CombatText_Patch;
                IL.Terraria.Main.DrawNPCChatBubble += Gravity_Generic_Patch;
                IL.Terraria.Main.DrawNPCHousesInWorld += Gravity_Generic_Patch;
                IL.Terraria.Main.DrawInterface_14_EntityHealthBars += Gravity_Generic_Patch;
                IL.Terraria.Main.DrawMouseOver += Gravity_Generic_Patch;
                IL.Terraria.Main.DrawInterface_19_SignTileBubble += Gravity_Generic_Patch;
                IL.Terraria.Main.DrawHealthBar += Gravity_Generic_Patch;
                IL.Terraria.Player.QuickGrapple += Gravity_Generic_Patch;
                IL.Terraria.GameContent.UI.EmoteBubble.Draw += Gravity_Generic_Patch;
                IL.Terraria.Player.Update += Gravity_TileAim_Patch;
                IL.Terraria.CombatText.NewText_Rectangle_Color_string_bool_bool += Gravity_Generic_Patch;
                IL.Terraria.Player.ItemCheck += Gravity_Aim_Patch;

                //Screw it, i'll make my own hook
                HookEndpointManager.Modify(typeof(Main).GetProperty("MouseWorld").GetGetMethod(), new ILContext.Manipulator(Gravity_Generic_Patch));
            }*/

            //IL.Terraria.Main.DrawPlayer_DrawAllLayers += Rotate_Patch;
            
        }



        internal static void UnloadILs()
        {

        }
           
        internal static void DrawWires_Patch(ILContext il)
		{
            var c = new ILCursor(il);
            var label = il.DefineLabel();

            //get if we're in adventure mode and push it onto the stack
            c.EmitDelegate<Func<bool>>(() => ModContent.GetInstance<tsorcRevampConfig>().AdventureMode);
            //if that's false, maybe it should be true, so jump to label (the original function)
            c.Emit(OpCodes.Brfalse, label);
            //else, push 0 (false) and return
            c.Emit(OpCodes.Ldc_I4_0);
            c.Emit(OpCodes.Ret);

            c.MarkLabel(label);

		}

		internal static void Player_Update(ILContext il)
        {
            Mod mod = ModContent.GetInstance<tsorcRevamp>();

            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.Before,
                                    i => i.MatchLdfld("Terraria.Player", "statManaMax2"),
                                    i => i.MatchLdcI4(400)))
            {
                mod.Logger.Fatal("Could not find instruction to patch (Player_Update)");
                return;
            }

            cursor.Next.Next.Operand = int.MaxValue;
        }

        internal static void Chest_Patch(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!c.TryGotoNext(instr => instr.MatchLdcR4(1f) && instr.Next.Next.Next.Next.Next.Next.MatchStfld(typeof(Player).GetField("chest"))))
            {
                throw new Exception("Could not find instruction to patch (Chest_Patch)");
            }

            c.FindNext(out ILCursor[] cursors, instr => instr.MatchLdcR4(1f));
            c = cursors[0];

            c.Index++;
            c.EmitDelegate<Func<float, float>>((volume) =>
            {
                if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().chestBankOpen
                || Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().chestPiggyOpen)
                {
                    // Return 0 volume if one is open so the sound is silent
                    return 0f;
                }

                return volume;
            });
        }


        internal static void Gravity_Screenflip_Patch(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (!c.TryGotoNext(instr => instr.MatchLdcI4(2) && instr.Next.MatchCallvirt<Terraria.Graphics.SpriteViewMatrix>("set_Effects")))
            {
                throw new Exception("Could not find instruction to patch (Gravity_Screenflip_Patch)");
            }
            c.Index++;
            c.EmitDelegate<Func<int, int>>(Gravity_Screenflip_Delegate);
        }

        //This takes an int to ensure that it eats the old value and removes it from the stack, letting us push 0 (no screenflip) to it instead
        internal static int Gravity_Screenflip_Delegate(int oldValue)
        {
            return 0;
        }

        internal static void Gravity_Rasterizer_Patch(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (!c.TryGotoNext(instr => instr.MatchLdsfld<Microsoft.Xna.Framework.Graphics.RasterizerState>("CullClockwise")))
            {
                throw new Exception("Could not find instruction to patch (Gravity_Rasterizer_Patch)");
            }
            c.Index++;
            c.EmitDelegate<Func<Microsoft.Xna.Framework.Graphics.RasterizerState, Microsoft.Xna.Framework.Graphics.RasterizerState>>(Gravity_Rasterizer_Delegate);
        }

        //Again, this exists to eat the old state and push the desired one
        internal static Microsoft.Xna.Framework.Graphics.RasterizerState Gravity_Rasterizer_Delegate(Microsoft.Xna.Framework.Graphics.RasterizerState oldState)
        {
            return Microsoft.Xna.Framework.Graphics.RasterizerState.CullCounterClockwise;
        }

        internal static void Gravity_TileAim_Patch(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (!c.TryGotoNext(instr => instr.MatchLdcR4(16) && instr.Next.MatchDiv() && instr.Next.Next.MatchConvI4() && instr.Next.Next.Next.MatchStsfld<Player>("tileTargetY") && instr.Next.Next.Next.Next.MatchLdarg(0) && instr.Next.Next.Next.Next.Next.MatchLdfld<Player>("gravDir")))
            {
                throw new Exception("Could not find instruction to patch (Gravity_TileAim_Patch)");
            }
            c.Index += 6;
            c.EmitDelegate<Func<float, float>>(GravDir_Replace_Delegate);
        }

        internal static void Gravity_CombatText_Patch(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (!c.TryGotoNext(instr => instr.MatchLdfld<Player>("gravDir") && instr.Next.MatchLdcR4(-1) && instr.Next.Next.Next.MatchLdsfld<Main>("combatText") && instr.Next.Next.Next.Next.Next.MatchLdelemRef()))
            {
                throw new Exception("Could not find instruction to patch (Gravity_CombatText_Patch)");
            }
            c.Index += 1;
            c.EmitDelegate<Func<float, float>>(GravDir_Replace_Delegate);
        }

        internal static void Gravity_Aim_Patch(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            //There are like 50 sequences just like this in the player file, so this one was annoying to pin down the location of
            if (!c.TryGotoNext(instr => instr.MatchDup() && instr.Next.MatchLdfld<Microsoft.Xna.Framework.Vector2>("X") && instr.Next.Next.Next.MatchLdfld<Microsoft.Xna.Framework.Vector2>("Y") && instr.Next.Next.Next.Next.Next.MatchLdarg(0) && instr.Next.Next.Next.Next.Next.Next.MatchLdfld<Player>("gravDir") && instr.Next.Next.Next.Next.Next.Next.Next.MatchLdcR4(-1)))
            {
                throw new Exception("Could not find instruction to patch (Gravity_Aim_Patch)");
            }
            c.Index += 7;
            c.EmitDelegate<Func<float, float>>(GravDir_Replace_Delegate);
        }

        //Can be used to patch any function where gravDir is used only once.
        internal static void Gravity_Generic_Patch(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (!c.TryGotoNext(instr => instr.MatchLdfld<Player>("gravDir")))
            {
                throw new Exception("Could not find instruction to patch (Gravity_Generic_Patch)");
            }
            c.Index += 1;
            c.EmitDelegate<Func<float, float>>(GravDir_Replace_Delegate);
        }

        //Goes right after the current gravDir is loaded onto the stack. Eats that value, then places "1" on the stack. Useful to make code run as if the gravDir is 1.
        internal static float GravDir_Replace_Delegate(float oldValue)
        {
            return 1;
        }

        //Stick this into a section of code you are trying to *avoid* running to let you know for sure if it still is (if so you messed up skipping it, if not you edited the wrong section)
        internal static void DebugDelegate()
        {
            UsefulFunctions.BroadcastText("Hello! I am running!!");
        }

        /*
        private static void Rotate_Patch(ILContext il) {
            throw new NotImplementedException();
        }
        */
        //allow soul slot to be included in recipes
        internal static void SoulSlotRecipesPatch(ILContext il) {
            ILCursor c = new ILCursor(il);
            //grab the dictionary right after it's made, before it gets stored in a local
            if (!c.TryGotoNext(MoveType.After, instr => instr.MatchNewobj<Dictionary<int, int>>())) {
                throw new Exception("Could not find instruction to patch (SoulSlotRecipesPatch)");
            }
            c.EmitDelegate<Func<Dictionary<int, int>, Dictionary<int, int>>>(AddSoulSlotRecipes);
        }

        internal static Dictionary<int, int> AddSoulSlotRecipes(Dictionary<int, int> dictionary) {
            Item item = Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().SoulSlot.Item;
            if (item.stack > 0) {
                if (dictionary.ContainsKey(item.netID)) {
                    dictionary[item.netID] += item.stack;
                }
                else {
                    dictionary[item.netID] = item.stack;
                }
            }
            //so we dont just eat it off the stack
            return dictionary;
        }
    }
}
