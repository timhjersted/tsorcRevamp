sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 uTargetPosition;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;


float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    //const float4 redLaser = float4(1.0, 0.1, 0.1, 1.0);
    //const float4 blueLaser = float4(0.1, 0.75, 1, 1);
    const float centerIntensity = 11.0;
    float scaleDown = uOpacity;
    float2 projectileSize = uTargetPosition;


    //Define the laser effect and center color (these can be tuned to produce any color laser)
    float4 laserColor = float4(0.2, 0.6, 0.1, 1.0);
    float4 white = float4(1.0,1.0,1.0,1.0);

    float textureSize = 4096;
    
    // Normalize pixel coordinates (from 0 to 1) and compensate for projectile size distortion
    float2 uv = coords * textureSize;
    uv.x = uv.x / projectileSize.x;
    uv.y = uv.y / projectileSize.y;
    
    //Uncomment for rainbow laser:
    //laserColor.rgb = 0.5 + 0.5*cos(uTime * 4.0 +uv.xyx+float3(0,2,4));
    
    //Get the background color for this pixel
    float4 baseColor = sampleColor;
    
    //Calculate how close the current pixel is to the center line of the screen
    float intensity = 1 - distance(float2(0.5, 0.5), uv);
    //intensity = 1.0 - abs(uv.y - 0.5);


    //Tone down the intensity according to scaleDown, used for the charging and fade out effects
    //intensity = intensity * scaleDown;

    //Scale the start and end based on the scaleDown variable
    float startPercent = 200 * scaleDown / projectileSize.x;
    float endPercent = 0.8 * scaleDown;
    
    //Raise it to the power of 4, resulting in sharply increased intensity at the center that trails off smoothly
    intensity = pow(intensity, 1.5);

    if (scaleDown < 1) {
        intensity *= 0.9;
    }    

    //intensity *=  uv.x * uv.x;
    //intensity *= 3;
    //Make the laser trail off at the start
    if (uv.x < 0.3) {
        //intensity *= uv.x;
        //intensity = lerp(0.0, intensity, pow(uv.x / 0.3, 0.5));
    }
    
    //Make the laser trail off at the end
    if (uv.x > 0.3) {
        //intensity = lerp(0.0, intensity, pow((1.0 - uv.x) / (1 - 0.3), 0.5));
    }

    //Pick where to sample the texture used for the flowing effect
    float2 samplePoint = coords;
    //Stretch it horizontally and then shift it over time to make it appear to be flowing
    float time = uTime;

    //Make it flow the other way while charging
    if (scaleDown < 1.0) {
        time = uTime * -1;
    }

    samplePoint.x = (samplePoint.x - (time * 0.04)) ;
    //Compress it vertically
    //samplePoint.y = samplePoint.y * 0.3;

    //Get the noise texture at that point
    float sampleIntensity = tex2D(uImage0, samplePoint).r;
    float4 noiseColor = float4(1.0, 1.0, 1.0, 1.0);    
    noiseColor.r = sampleIntensity * laserColor.r;
    noiseColor.b = sampleIntensity * laserColor.b;
    noiseColor.g = sampleIntensity * laserColor.g;

    //Mix it with 'intensity' to make it more intense near the center
    float4 effectColor = noiseColor * intensity * intensity * intensity * 0.25;

    //Mix it with the color white raised to a higher exponent to make the center white beam
    effectColor = effectColor * centerIntensity * (pow(intensity, 8.0)) * 35.5;
    
    //Amplify the amount of of laserColor in the beam
    effectColor.r = effectColor.r * (1 + laserColor.r);
    effectColor.g = effectColor.g * (1 + laserColor.g);
    effectColor.b = effectColor.b * (1 + laserColor.b);
    
    return effectColor;
}


technique CursedMalestrom
{
    pass CursedMalestromPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}