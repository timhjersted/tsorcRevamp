# Shader preview tooling

Two throwaway-cheap tools that answer the two questions you cannot answer by reading an `.fx` file:
**what does this texture actually contain**, and **what does this shader actually look like**.

Neither is part of the mod build. They live under `.agents/`, which MSBuild's default compile globs
skip — verified with a deliberately-broken probe `.cs`, which produced zero errors.

---

## 1. `ContactSheet.ps1` — pick textures by looking at them

Renders the noise pack as grayscale `R * A / 255`, which is what `tex2D(...).r` returns after
tModLoader premultiplies. Several textures in the pack are not what their names suggest.

```powershell
.\ContactSheet.ps1                                   # whole pack, single tile
.\ContactSheet.ps1 -Tile2x2                          # 2x2, to check the seam
.\ContactSheet.ps1 -Names Vein_04-512x512,T_NKQ443 -Tile2x2
```

**Always run the `-Tile2x2` pass before committing to a texture.** Every shader here scrolls its
samples, so a texture with a visible seam is unusable however good the single tile looks. Try:

```powershell
.\ContactSheet.ps1 -Names T_Wave43,T_LiquidVertical_Wave,Turbulence_07-512x512,T_Noise_6Yu1 -Tile2x2
```

The first two are genuinely flame-shaped and both carry a hard horizontal baseline that strobes past
every time you scroll them vertically. The second two tile cleanly. You cannot tell these apart from
the single-tile sheet, and you certainly cannot tell from the filenames.

## 2. `Preview.cs` — render the shader before you ship it

```powershell
cd .agents/skills/vfx-shader-tips/preview
$env:PREVIEW_NAME = 'GigasSunPillar'; dotnet run  # archives GigasSunPillar-YYYYMMDD-HHMMSS.png
```

Port each pixel-shader function to C# (the helpers are named `sat` / `lerp` / `smoothstep` / `V2` /
`V3` / `C(r,g,b)` so it is close to copy-paste), list it in `Panels()` with its **real** draw size
and blend mode, and run. `smoothstep` matches HLSL including the descending-edge form
(`smoothstep(hi, lo, x)`), which several shaders here use to invert a falloff without a `1 - x`.

Set `FOCUS=<name>` to render one family big instead of the whole sheet; see `Main()` for the
existing blocks. Set `PREVIEW_NAME` to the shader or effect family being examined: it is embedded in the sheet
and filename, so each render is retained instead of overwriting `preview.png`. Each panel renders
over a bright daytime sky **and** a dark cave, side by side.

That two-background split is the point. See §42–§43 of `../SKILL.md` for why.

### What it reproduces faithfully, and why each matters

| Detail | Why |
|---|---|
| Textures premultiplied (`R * A`) | A texture whose image lives in the alpha channel reads as a **constant** through `.r`. Reproduce it or the preview lies to you and hides the exact bug you are hunting. |
| Bilinear **wrap** sampling | Matches `SamplerState.LinearWrap`. Nearest sampling makes silhouettes look blocky and sends you chasing an artifact that does not exist. |
| `BlendState.Additive` = `dst + rgb * a` | XNA additive is `SourceAlpha` / `One`, **not** `One` / `One` — alpha scales the contribution. |
| `BlendState.AlphaBlend` = `rgb + dst * (1-a)` | XNA's AlphaBlend is **premultiplied**. Getting this backwards makes every alpha-blended effect look wrong in the preview and right in game, or vice versa. |
| The real draw size from the call site | Aspect matters. A technique tuned at 1:1 regularly falls apart at the 5:1 the caller actually uses. |

### Keep it honest

- Port the maths **verbatim**, including magic numbers. A preview of a shader you half-remember is
  worse than no preview.
- Re-sync after every HLSL edit. They diverge within about three tweaks.
- **Re-render after optimising for instruction slots, before you ship.** Slot cuts are not cosmetic:
  dropping a near-black colour term, folding a mask into alpha, or removing a radial term from a
  sample scale all change the image. On the Black Knight seal those cuts happened to *improve* it
  (angular-only noise gave cleaner tongues) — which is exactly why you look rather than assume. The
  approved preview and the shipped shader were not the same shader.
- Render at more than one `Progress`. A shader that looks right at `P=1` is regularly invisible or
  solid at `P=0.2`, and that is a caller/curve bug worth finding here.
- It is a **preview, not a proof**. It says nothing about slot count, draw order, `PostDrawTiles`
  layering, lighting, or how it reads in motion. Ship the playtest watchlist anyway.
