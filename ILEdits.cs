using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp
{
    class ILEdits
    {
        internal static void ApplyILs()
        {

            //Fix lihzahrd power cell being consumed
            Terraria.IL_Player.TileInteractionsUse += PowerCell_Patch;

            Terraria.IL_Player.Update += Player_Update;
            //IL.Terraria.Player.Update += Chest_Patch;

            //Disable drawing of wires when in adventure mode
            //editing a get accessor of a property and built in hooks don't have any of those
            MonoModHooks.Modify(typeof(Terraria.GameContent.UI.WiresUI.Settings).GetProperty("DrawWires").GetGetMethod(), new ILContext.Manipulator(DrawWires_Patch));


            if (ModContent.GetInstance<tsorcRevampConfig>().GravityFix)
            {
                //main screen flip
                Terraria.IL_Main.DoDraw += GravPatch_ReplaceAll;
                Terraria.IL_Main.DoDraw += GravPatch_Rasterizer;

                //text & health bars
                Terraria.IL_CombatText.NewText_Rectangle_Color_string_bool_bool += GravPatch_ReplaceOne;
                Terraria.IL_Main.DrawHealthBar += GravPatch_ReplaceOne;
                Terraria.IL_Main.DrawItemTextPopups += GravPatch_ReplaceOne;
                Terraria.IL_Main.DrawInterface_1_2_DrawEntityMarkersInWorld += GravPatch_ReplaceOne;

                //Emote bubbles, chat bubbles that appear when hovering over NPCs,
                //NPC house indicators, mouse over text
                Terraria.IL_Main.DrawNPCChatBubble += GravPatch_ReplaceOne;
                Terraria.GameContent.UI.IL_EmoteBubble.Draw += GravPatch_ReplaceOne;
                Terraria.IL_Main.DrawMouseOver += GravPatch_ReplaceOne;
                Terraria.IL_Main.DrawNPCHousesInWorld += GravPatch_ReplaceOne;

                Terraria.GameInput.IL_SmartSelectGamepadPointer.SmartSelectLookup_GetTargetTile += GravPatch_ReplaceAll;

                //Screencap mode stuff
                //TODO: snapshot boundaries still borked
                Terraria.Graphics.Capture.IL_CaptureInterface.ModeEdgeSelection.DrawCursors += GravPatch_ReplaceOne;
                Terraria.Graphics.Capture.IL_CaptureInterface.ModeDragBounds.DragBounds += GravPatch_ReplaceOne;
                Terraria.Graphics.Effects.IL_FilterManager.EndCapture += GravPatch_ReplaceAll;

                //MouseWorld property
                MonoModHooks.Modify(typeof(Main).GetProperty("MouseWorld").GetGetMethod(), new ILContext.Manipulator(GravPatch_ReplaceOne));

                //Aiming
                //TODO: portal gun (needed?)
                //Terraria.IL_Player.ItemCheck_UseRodOfDiscord += GravPatch_ReplaceOne;
                Terraria.IL_Player.ItemCheck_Shoot += GravPatch_ReplaceAll;
                Terraria.IL_Player.Update += GravPatch_TileAim;

                //Smart cursor
                Terraria.IL_Main.DrawSmartCursor += GravPatch_ReplaceOne;
                Terraria.IL_Main.DrawSmartInteract += GravPatch_ReplaceOne;

                //grappling
                Terraria.IL_Player.QuickGrapple += GravPatch_ReplaceOne;
            }

            //IL.Terraria.Main.DrawPlayer_DrawAllLayers += Rotate_Patch;

        }

        internal static void UnloadILs()
        {

        }

        internal static void PowerCell_Patch(ILContext il)
        {
            var c = new ILCursor(il);
            var label = il.DefineLabel();

            //go to where the item id for power cell (1293) is added to the stack
            if (!c.TryGotoNext(instr => instr.MatchLdcI4(1293)))
            {
                throw new Exception("Could not find instruction to patch (PowerCell_Patch) (1)");
            }

            //go to the next instance of 'stack--' (item stack, not eval stack)
            if (!c.TryGotoNext(instr => instr.Previous.MatchLdfld<Item>("stack") && instr.MatchLdcI4(1) && instr.Next.MatchSub()))
            {
                throw new Exception("Could not find instruction to patch (PowerCell_Patch) (2)");
            }
            //we're now before the instruction to add 1 to the eval stack (which will then be used to subtract from the item stack)

            //jump to our label
            //(skipping past ldc.i4.1 (add 1 to eval stack) and sub (subtract last two values from stack))
            c.Emit(OpCodes.Br, label);

            //move the cursor, this doesnt actually change anything in game
            c.Index++;
            c.Index++;

            //set the label to after the offending op codes
            c.MarkLabel(label);

            //outcome: sets 'stack' to 'stack', instead of setting 'stack' to 'stack - 1'.
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


        //Goes right after the current gravDir is loaded onto the stack. Eats that value, then places "1" on the stack. Useful to make code run as if the gravDir is 1.
        internal static float GravPatch_Delegate(float oldValue)
        {
            return 1;
        }

        internal static void GravPatch_ReplaceAll(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            while (c.TryGotoNext(instr => instr.MatchLdfld<Player>("gravDir")))
            {
                c.Index++;
                c.EmitDelegate<Func<float, float>>(GravPatch_Delegate);
            }

        }

        internal static void GravPatch_TileAim(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (!c.TryGotoNext(instr => instr.MatchLdcR4(16) && instr.Next.MatchDiv() && instr.Next.Next.MatchConvI4() && instr.Next.Next.Next.MatchStsfld<Player>("tileTargetY") && instr.Next.Next.Next.Next.MatchLdarg(0) && instr.Next.Next.Next.Next.Next.MatchLdfld<Player>("gravDir")))
            {
                throw new Exception("Could not find instruction to patch (Gravity_TileAim_Patch)");
            }
            c.Index += 6;
            c.EmitDelegate<Func<float, float>>(GravPatch_Delegate);
        }

        //Can be used to patch any function where gravDir is used only once.
        internal static void GravPatch_ReplaceOne(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (!c.TryGotoNext(instr => instr.MatchLdfld<Player>("gravDir")))
            {
                throw new Exception("Could not find instruction to patch (GravPatch_ReplaceOne)");
            }
            c.Index += 1;
            c.EmitDelegate<Func<float, float>>(GravPatch_Delegate);
        }

        internal static void GravPatch_Rasterizer(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (!c.TryGotoNext(instr => instr.MatchLdsfld<Microsoft.Xna.Framework.Graphics.RasterizerState>("CullClockwise")))
            {
                throw new Exception("Could not find instruction to patch (Gravity_Rasterizer_Patch)");
            }
            c.Index++;
            c.EmitDelegate<Func<Microsoft.Xna.Framework.Graphics.RasterizerState, Microsoft.Xna.Framework.Graphics.RasterizerState>>(GravPatch_Rasterizer_Delegate);
        }

        //Again, this exists to eat the old state and push the desired one
        internal static Microsoft.Xna.Framework.Graphics.RasterizerState GravPatch_Rasterizer_Delegate(Microsoft.Xna.Framework.Graphics.RasterizerState oldState)
        {
            return Microsoft.Xna.Framework.Graphics.RasterizerState.CullCounterClockwise;
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
    }
}
