sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2); 
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;


// .fx shader files must be compiled for Terraria to use them!
// the compiled version is TheAbyssShader.xnb
// this file has been included for readability

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float4 position: SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{  

    // the screen's color information
    float4 color = tex2D(uImage0, coords);

    // calculate the luminosity of each pixel. sampleColor is the original color space, and is included to clamp the luminosity
    float luminosity = (color.r + color.g + color.b) * sampleColor;

    // coords normally has a range from 0 to 1. let's just completely fuck it up and see what happens
    coords.x = (uTime / 4);

    // uImage1 is set in the Mod class, when the shader is being constructed
    // feed it the modified coordinates to create distortion
    float4 noise = tex2D(uImage1, coords);


    // desaturate the green channel to shift the color to a purple
    color.rgb = (float4(1, 0.6, 1, 1) + (color.rgb));

    // then apply the stored luminosity to recover the screen detail, and noise to add the distortion
    color.rgb = (color.rgb + noise) * luminosity;

    return color;
}

technique Technique1
{
    pass TheAbyssShaderPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}