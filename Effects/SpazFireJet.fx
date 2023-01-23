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
    float4 laserColor = float4(1.0, 0.1, 0.1, 1.0);
    float4 white = float4(1.0,1.0,1.0,1.0);
    laserColor.rgb = uColor;

    float textureSize = 4096;
    float projectileWidth = 1200;
    float projectileHeight = 250;
    

    // Normalize pixel coordinates (from 0 to 1) and compensate for projectile size distortion
    float2 uv = coords * textureSize;
    uv.x = uv.x / projectileSize.x;
    uv.y = uv.y / projectileSize.y;
    
    //Uncomment for rainbow laser:
    //laserColor.rgb = 0.5 + 0.5*cos(uTime * 4.0 +uv.xyx+float3(0,2,4));
    
    //Get the background color for this pixel
    float4 baseColor = sampleColor;
    
    //Calculate how close the current pixel is to the center line of the screen
    float intensity = 1.0 - abs(uv.y - 0.5);

    //Tone down the intensity according to scaleDown, used for the charging and fade out effects
    intensity = intensity;

    //Scale the start and end based on the scaleDown variable
    float startPercent = 0.2;
    float endPercent = 0.2;
    
    //Raise it to the power of 4, resulting in sharply increased intensity at the center that trails off smoothly
    intensity = pow(intensity, 4.0);

    

    //Make the laser trail off at the start
    if (uv.x < startPercent) {
        intensity = lerp(0.0, intensity, pow(uv.x / startPercent, 0.5));
    }
    
    //Make the laser trail off at the end
    if (uv.x > endPercent) {
        intensity = lerp(0.0, intensity, pow((1.0 - uv.x) / (1 - endPercent), 0.5));
    }

    //Pick where to sample the texture used for the flowing effect
    float2 samplePoint1 = coords;
    float2 samplePoint2 = coords;
    //Stretch it horizontally and then shift it over time to make it appear to be flowing
    float time = uSaturation;

    //Make it flow with time
    samplePoint1.x = ((samplePoint1.x / 50) - time * 0.025);
    samplePoint2.x = (samplePoint2.x - time * 0.15);
    
    //Compress it vertically
    samplePoint1.y = samplePoint1.y * 1;

    //Get the noise texture at that point
    float sampleIntensity1 = tex2D(uImage0, samplePoint1).r;
    sampleIntensity1 = pow(sampleIntensity1, 3);
    float sampleIntensity2 = tex2D(uImage0, samplePoint2).r;
    float4 noiseColor = float4(1.0, 1.0, 1.0, 1.0);    
    noiseColor.r = sampleIntensity1 * laserColor.r;
    noiseColor.b = sampleIntensity1 * laserColor.b;
    noiseColor.g = sampleIntensity1 * laserColor.g;

    //Mix it with 'intensity' to make it more intense near the center
    float4 effectColor = noiseColor * pow(intensity, 1.3) * 0.1;
    //return effectColor;
    //effectColor = 0;
    
    float4 coreColor = lerp(white, laserColor, 0.75);

    //Mix it with the color white raised to a higher exponent to make the center white beam
    effectColor = effectColor * scaleDown + coreColor * 1.6 * (pow(intensity, 1.0) * sampleIntensity2 * scaleDown);
    effectColor *=  scaleDown;
    
    //Amplify the amount of of laserColor in the beam
    effectColor.r = effectColor.r * (1 + laserColor.r);
    effectColor.g = effectColor.g * (1 + laserColor.g);
    effectColor.b = effectColor.b * (1 + laserColor.b);
    
    return effectColor ;
}


technique SpazFireJet
{
    pass SpazFireJetPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}