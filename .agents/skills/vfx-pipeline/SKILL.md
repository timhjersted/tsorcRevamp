---
name: vfx-pipeline
description: Comprehensive workflow guide for building, compiling, and implementing custom HLSL shaders and visual effects in tModLoader.
---

# 🎆 VFX Pipeline & Shader Development Guide

This skill covers the end-to-end pipeline for creating, compiling, and integrating 2D custom shaders and visual effects (VFX) into Terraria tModLoader.

---

## Related Skills & References
- **[VFX Shader Tips & HLSL Best Practices](file:///.agents/skills/vfx-shader-tips/SKILL.md)**: Supplemental cheatsheet detailing frame UV normalization (`uSourceRect`), multi-layer noise blending, Gwyn-style edge rim detection, analytical falloffs, and 3-tier color ramping — plus the field notes (§31–41) and the preview harness (§42–48).
- **[Documentation/VFX_ARSENAL.md](file:///Documentation/VFX_ARSENAL.md)**: the reusable `VFX/` arsenal only — particle, beam, ring and primitive-trail recipes, `/vfxshowcase`, and the arsenal runtime watchlist. Its former "Shader authoring" half now lives at the bottom of **this** file.

---

## Pipeline Overview

### 1. Authoring HLSL (`.fx` File)
- Place `.fx` shader files in `Effects/`.
- Target the Reach profile (`ps_2_0`) for full cross-platform compatibility across FNA and XNA backends.
- Define technique and pass names explicitly.
- For sprite-attached effects or animated quads, always accept `uSourceRect` and compute `frameUV = (c - uSourceRect.xy) / uSourceRect.zw` (see [vfx-shader-tips](file:///.agents/skills/vfx-shader-tips/SKILL.md)).

### 2. Compilation (`.fx` -> `.xnb`)
- Compile shaders to `.xnb` using the dedicated PowerShell script:
  ```powershell
  powershell -ExecutionPolicy Bypass -File "C:\Users\timhj\Documents\My Games\Terraria\tModLoader\ShaderCompiler\Compile-Effect.ps1" -SourceFile "c:\Users\timhj\Documents\My Games\Terraria\tModLoader\ModSources\tsorcRevamp\Effects\YourEffect.fx"
  ```
- Or use the staged toolchain in `.tmp/nito_shader_compile/` (`fxcompiler_reach.exe` + `fxc.exe`),
  which compiles every `.fx` sitting in that directory.

#### The two compilers disagree — in BOTH directions. Budget for it.

`fxc.exe` (standalone) and `fxcompiler_reach.exe` (the D3DX **effect** compiler the `.xnb` actually
comes from) allocate registers differently, so "it compiles" depends on which one you asked:

- **`fxc` accepts, wrapper rejects.** Seen at 61–63 arithmetic slots. Target **≤ ~60 with headroom**,
  not "whatever fxc lets through".
- **Wrapper accepts, `fxc` rejects at 65/64.** Do **not** ship that — you have not established the
  shader is valid for the target profile, you have established that one tool was lenient.

Get the real instruction count from `fxc` (the wrapper never reports one):
```bash
MSYS2_ARG_CONV_EXCL="*" ./fxc.exe MyEffect.fx /T ps_2_0 /E MyPixelShaderEntry /Cc
# -> // approximately 63 instruction slots used (2 texture, 61 arithmetic)   <- 61 is the number that matters
```
`MSYS2_ARG_CONV_EXCL="*"` is **required** under Git Bash or `/T` becomes a path. `/E` takes the
**pixel-shader function name**, not the technique name. The 64 limit is on the *arithmetic* count;
texture slots have their own (32) budget, so "65 total (2 texture, 63 arithmetic)" is fine.

#### Two ways a stale `.xnb` silently survives

1. **The wrapper aborts the whole batch on the first failing file.** It processes alphabetically, so
   one bad shader means every file after it keeps its previous `.xnb` — with no error naming them.
   **Always `ls *.xnb` after running it** and confirm you got the count you expected.
2. **The wrapper reports errors at the `technique` line**, not the offending code, and phrases them
   as "error compiling expression". That line number is nearly always a lie.

#### The `saturate()`-on-pure-uniform trap (costs an hour every time)

The D3DX effect compiler **cannot compile `saturate()` whose argument has no per-pixel dependency**,
and reports it as the misleading technique-line error above. `fxc` accepts it, so this shows up only
at `.xnb` time.

```hlsl
float grow = saturate(Progress * (2.0 - Progress)) * 0.9;   // BAD — pure uniform inside saturate()
float grow = Progress * (2.0 - Progress) * 0.9;             // GOOD — callers already clamp Progress
float flash = saturate((0.16 - r) * 6.0 - Progress * 3.0);  // GOOD — uniform biases a per-pixel term
```

#### Round-trip the toolchain before trusting it
Compile an **unmodified** `.fx` and `cmp` against the checked-in `.xnb`. Expect ~3 differing bytes of
uninitialised padding immediately after null-terminated strings in the XNB string table; identical
size and structure. More than that means something real changed.

### 2b. Offline Preview — do this BEFORE you launch the game

Compiling only proves the shader fits in `ps_2_0`; it says nothing about how it looks. Port the pixel
functions into the harness and render them to a PNG:

```bash
cd .agents/skills/vfx-shader-tips/preview && dotnet run     # writes preview.png
```

Each panel renders over a bright sky **and** a dark cave, because the two backgrounds fail in
opposite directions. Ten minutes to wire up per boss, and every iteration after that is a
`dotnet run` instead of a full tModLoader launch. Details and the pitfalls that invalidate a preview
are in §42 of the tips skill; `preview/ContactSheet.ps1` in the same folder is the companion tool for
choosing textures (§44).

For **live** tuning there is a second harness outside the repo — `tModLoader/ShaderHarness/`
(`node render.js <job>` for PNGs, `node server.js` for a WebGL page with sliders for
`Progress`/`Opacity`/`Active`/time and a premultiply toggle that reproduces the §43 rectangle bug on
demand). Use `preview/` for stills and contact sheets, `ShaderHarness/` when you are dragging a value
to find a curve. See its `README.md`; §42 of the tips skill has the comparison table.
**Do not build a third harness.**

### 3. C# Registration & Asset Loading
- Load effect assets in `tsorcRevamp.cs` or `EnemyVFX.cs`:
  ```csharp
  Asset<Effect> myEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/YourEffect");
  ```

### 4. SpriteBatch Rendering & Render State Safety
- Set uniforms (`DarkColor`, `MidColor`, `CoreColor`, `Opacity`, `Time`, `uSourceRect`).
- Wrap custom `SpriteBatch` rendering in `try ... finally` blocks to restore `GraphicsDevice.Textures[1]` and `SamplerStates[1]` state cleanly after drawing.
- **Match the shader's return to the blend state.** `BlendState.AlphaBlend` is premultiplied in XNA,
  so an alpha-blended technique must return `float4(color * alpha, alpha)`; additive must not
  premultiply. Getting this wrong paints a flat rectangle the size of the quad — see vfx-shader-tips
  §43. Give the `Draw` helper a `BlendState` parameter rather than hardcoding one, so a technique can
  pick the right one.

### 5. Auditing an existing `*VFX.cs` draw helper

These helpers were copy-pasted between bosses, so a defect in one is usually in all of them. Check:

- **Source-rectangle cropping.** `new Rectangle(0, 0, clamp((int)drawSize.X, 1, primary.Width), …)`
  crops the texture's **top-left corner** whenever the draw is smaller than the texture — e.g. a 72px
  effect takes a 72×72 corner out of a 512px centred flare, which is mostly empty. Pass `null` and use
  `primary.Size()` unless you genuinely need an atlas frame. Doing so also makes any `LocalUV()` /
  `UV()` helper an identity, so it can be deleted for ~4 slots per technique.
- **A hardcoded blend state** for every technique in the file (see above).
- **`Color.White` passed as the sprite colour**, which makes `sampleColor`/`vertexColor` a constant 1
  in every shader in that file — free slots if you drop it.

### 6. In-Game Verification
- Use `DebugTome` or `ModContent.GetInstance<tsorcRevampConfig>().DebugMode` to trigger attacks and verify visual quality, anchor origins, and edge masking in playtesting.

---

# Shader authoring reference

Moved here from `Documentation/VFX_ARSENAL.md` — that file now documents only the reusable `VFX/`
arsenal (the particle/beam/ring library). Keeping the toolchain described in two places is what let
them drift apart: neither copy mentioned that the two compilers disagree, which is a full hour lost
every time someone rediscovers it.

## Know what's actually in your input texture

**A top source of "my shader renders a flat shape with no motion".** Before sampling a texture for
*shape*, verify the channel you're reading actually contains shape data. There is a tool for this —
don't hand-roll a pixel dump:

```powershell
cd .agents\skills\vfx-shader-tips\preview
.\ContactSheet.ps1                                            # whole pack as grayscale R * A / 255
.\ContactSheet.ps1 -Names T_Windstreak3,T_trail12 -Tile2x2    # shortlist, 2x2 seam check
```

`R * A / 255` is exactly what `tex2D(...).r` returns after tModLoader premultiplies. **Always run the
`-Tile2x2` pass too:** every shader here scrolls its samples, so a texture with a visible seam is
unusable however good the single tile looks — see §44 of the tips skill for three flame textures that
failed only on that second pass.

Two live traps, both of which shipped as the shape source for wake/lane/trail techniques:

| Texture | Trap |
|---|---|
| `T_Windstreak3` | A **vertical teardrop blob**, not a horizontal streak. Stretched along a lane it renders as a hard, motionless lozenge. (An earlier note here claimed its RGB was uniformly white with the image only in alpha — that was **wrong**, verified by dumping `R * A`: the shape is real, it is just the wrong shape. The conclusion is unchanged: do not use it for streaks.) |
| `T_trail12` | A small centred 4-point **star flare**. Stretched along a long lane, nearly the whole quad samples its black background, and in ring space it smears into spokes. |

When in doubt, drop the texture dependency and build the shape **procedurally from noise** — it is
resolution-independent and cannot silently degrade.

## Techniques worth copying

### Multi-layer opposing noise (Marilith fire aura)

Sample the same noise twice with **independently drifting offsets moving in opposing directions**,
then combine through **non-linear power curves**. Two cheap samples read as genuine fluid motion, and
because the layers never share a phase there are no repeating scroll lines:

```hlsl
float n1 = tex2D(Noise, uv * 2.6 + float2( Time * 0.05, -Time * 0.04)).r;
float n2 = tex2D(Noise, uv * 4.1 + float2(-Time * 0.03,  Time * 0.06)).r;
float density = pow(saturate(n1 * 0.7 + n2 * 0.5), 1.5);
```

Marilith also runs a **different exponent per colour channel** (`pow(i, 8.0/5.0)` red, `6.0/5.0`
green, `3.0/5.0` blue), so the channels ramp at different rates and you get dark-red → orange →
yellow → white *for free*, with no lerp chain. When the palette is authored in C#
(`DarkColor`/`MidColor`/`CoreColor`), apply the same idea to the **mix factors** so the author keeps
control:

```hlsl
float3 color = DarkColor;
color = lerp(color, MidColor,  pow(density, 0.75));  // mid tone arrives early
color = lerp(color, CoreColor, pow(density, 2.40));  // hot core only at the very top
```

### Additive `x` + `x*x` colour layering (Marilith firewall)

Instead of chained `lerp`s, **accumulate** colour, adding a squared term for each hotter layer. The
squared term is naturally confined to the brightest regions, which reads like real HDR falloff:

```hlsl
float3 color  = float3(0.55, 0.02, 0.00) * body;
       color += float3(1.00, 0.17, 0.01) * body * body;
       color += float3(1.00, 0.72, 0.16) * core;
       color += float3(1.00, 0.96, 0.70) * core * core;
```

### Noise-modulated falloff distance (Marilith firewall)

The trick that kills hard edges. Rather than eroding a shape *after* the fact, make the **falloff
distance itself** noise-driven, so the body reaches further in some places than others:

```hlsl
float flameBody = saturate(1 - (boundaryDistance - halfThickness) / (48 + flameNoise * 105));
flameBody *= saturate((flameNoise + flameBody - 0.36) * 1.7);
```

Pair it with a rounded-rectangle / circle **SDF** for the base shape and the silhouette stays
controllable while the edge stays organic.

### Texel-neighbour rim glow (Gwyn cinder blade)

Detects a sprite's exact outline by comparing its alpha against its 4 neighbours — where the sprite
is opaque but a neighbour isn't, you're on the edge:

```hlsl
float2 texel = 1.0 / max(PrimaryTextureSize, float2(1.0, 1.0));
float neighborAlpha = min(
    min(tex2D(PrimaryTexture, coords + float2(texel.x, 0)).a,
        tex2D(PrimaryTexture, coords - float2(texel.x, 0)).a),
    min(tex2D(PrimaryTexture, coords + float2(0, texel.y)).a,
        tex2D(PrimaryTexture, coords - float2(0, texel.y)).a));
float edge = saturate((sprite.a - neighborAlpha) * 4.0);
```

**Only applies to sprite-based draws with real alpha**, and requires `PrimaryTextureSize` to be set
correctly. **Downscale caveat:** a 1-texel rim on a 512×512 source drawn at 54×18 on screen is
sub-pixel and will vanish or alias. Widen the offset to several texels for small draws — or, for a
purely procedural quad, take the rim from the SDF you already computed (`abs(d - edge)`), which is
resolution-independent.

## Effect parameter contract

`EnemyVFX.Draw(...)` / the per-boss VFX helpers set a fixed uniform set — `DarkColor`, `MidColor`,
`CoreColor`, `Opacity`, `Time`, `Progress`, `Active`, `Direction`, `DrawSize`, `PrimaryTextureSize`
(all via `?.SetValue`, so a shader may declare only what it uses). Keeping technique names and this
uniform set stable means a shader can be rewritten **without touching a single C# call site** — the
preferred way to iterate on visuals.

`Direction` and `Active` are effectively free per-technique scratch parameters; if you repurpose one,
say so in a comment at both ends. Worked example: `VesselSoulTrail` takes a **per-skull random phase**
in `Direction` (from `projectile.identity`) so that 100+ skulls don't all sample the identical noise
pattern and read as one stamp repeated — no new uniform, no new state, MP-safe.

## Progress plumbing / timing at the call site

The shader is usually fine; the values reaching it are not.

- A telegraph the player is meant to *react* to should ramp `Progress` 0→1 across its real duration.
  Watch for call sites passing a scaled fraction (e.g. `progress * 0.18f`) as a brightness hack —
  that silently destroys any `Progress`-driven animation in the shader. Dim via `Opacity` instead.
- Check that a caller's `progress` maths actually matches its `timeLeft`. A divisor that doesn't
  match the lifetime yields **negative** progress for the first part of the effect, and a fade curve
  like `saturate(1 - P*P)` then renders it fully invisible while it is supposedly at its strongest.
- Lingering effects (poison, fog, rot) want a **hold** curve, not a decay: fast fade-in, flat through
  most of the life, fade out only at the end.
- If a shader is written assuming `Progress` is clamped 0..1 (e.g. to drop a `saturate` that would
  otherwise be a pure-uniform argument), confirm every caller actually clamps it.
