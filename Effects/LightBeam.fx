sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float3 Color;
float3 SecondaryColor;
float FadeOut;
float Time;
float2 ProjectileSize;
float TextureSize;


float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    //const float4 redLaser = float4(1.0, 0.1, 0.1, 1.0);
    //const float4 blueLaser = float4(0.1, 0.75, 1, 1);
    const float centerIntensity = 1.5;


    //Define the laser effect and center color (these can be tuned to produce any color laser)
    float4 laserColor = float4(1, 1, 1, 1);
    laserColor.rgb = Color;
    
    float4 secondaryColor = float4(1, 1, 1, 1);
    //secondaryColor.rgb = SecondaryColor;
    
    float projectileWidth = ProjectileSize.x;
    float projectileHeight = ProjectileSize.y;
    

    // Normalize pixel coordinates (from 0 to 1) and compensate for projectile size distortion
    float2 uv = coords * TextureSize;
    uv.x = uv.x / ProjectileSize.x;
    uv.y = uv.y / ProjectileSize.y;
    
    //Uncomment for rainbow laser:
    //laserColor.rgb = 0.5 + 0.5 * cos(uTime * 0.01 + uv.xyx + float3(0,2,4));
    
    //Get the background color for this pixel
    float4 baseColor = sampleColor;
    
    //Calculate how close the current pixel is to the center line of the screen
    float intensity = 1.0 - abs(uv.y - 0.5);

    //Scale the start and end based on the scaleDown variable
    float startPercent = 200 / ProjectileSize.x;
    float endPercent = 0.8;
    
    //Raise it to the power of 4, resulting in sharply increased intensity at the center that trails off smoothly
    intensity = pow(intensity, 6.0);

    if (FadeOut < 1)
    {
        intensity *= 0.9;
    }    

    //Make the laser trail off at the start
    if (uv.x < startPercent)
    {
        intensity = lerp(0.0, intensity, pow(uv.x / startPercent, 0.5));
    }
    
    //Make the laser trail off at the end
    if (uv.x > endPercent)
    {
        intensity = lerp(0.0, intensity, pow((1.0 - uv.x) / (1 - endPercent), 0.5));
    }

    //Pick where to sample the texture used for the flowing effect
    float2 samplePoint = coords;    
    
    //Shift it over time to make it appear to be flowing
    float modifiedTime = 10 * Time / TextureSize;
    if (FadeOut < 1.0)
    {
        samplePoint.x += modifiedTime;
    }
    else
    {
        samplePoint.x -= modifiedTime;        
    }    

    //Compress it
    //samplePoint.x = samplePoint.x * 3;
    //samplePoint.y = samplePoint.y * 3;    

    //Get the noise texture at that point
    float sampleIntensity = tex2D(uImage0, samplePoint).r;
    float4 noiseColor = float4(1.0, 1.0, 1.0, 1.0);
    noiseColor.r = sampleIntensity * laserColor.r;
    noiseColor.b = sampleIntensity * laserColor.b;
    noiseColor.g = sampleIntensity * laserColor.g;

    //Mix it with 'intensity' to make it more intense near the center
    float4 effectColor = noiseColor * intensity * 2.0 * FadeOut;
    
    if (FadeOut == 1)
    {
        //Mix it with the color white raised to a higher exponent to make the center white beam
        effectColor = effectColor + 15 * secondaryColor * centerIntensity * (pow(intensity, 4.0) * sampleIntensity);
    }
    
    return effectColor;
}


technique LightBeam
{
    pass LightBeamPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}