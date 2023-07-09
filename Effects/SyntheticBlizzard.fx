sampler uImage0 : register(s0);
texture noiseTexture;
sampler uImage1 = sampler_state
{
    Texture = (noiseTexture);
    AddressU = wrap;
    AddressV = wrap;
};


float time; //Causes the flames to flow with time
float splitAngle; //How wide (in radians) the angle of fire is
float rotation; //Rotates the fire
float length; //The maximum length
float opacity; //Multiplies the output by this to let it fade in
float texScale;
float texScale2;


//I precomputed what values I could, to save on instruction count
float rotationMinusPI;
float splitAnglePlusRotationMinusPI;
float RotationMinus2PIMinusSplitAngleMinusPI; 

//Various constants
const float PI = 3.141592;
const float TWOPI = 6.283184;
const float INVERSETWOPI = 0.1591549;
const float4 empty = float4(0, 0, 0, 0);

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{    
    //Convert uv from rectangular to polar coordinates
    float2 dir = coords - float2(0.5, 0.5);
    float angle = atan2(dir.y, dir.x);        
        
    //Calculate how close the current pixel is to the center line of the screen
    float dist = distance(coords, float2(0.5, 0.5));
    
    //Convert uv from rectangular to polar coordinates
    //Make two sample points, one for the voronoi noise and the other for the turbulent noise
    //They are different size textures so they must be scaled differently
    float2 samplePoint = float2(dist - time, angle * INVERSETWOPI);
    float2 samplePoint2 = samplePoint;
    
    samplePoint *= texScale;
    samplePoint2 *= texScale2;
    
    //Calculate how intense a pixel should be based on the noise generator
    float intensity = (tex2D(uImage0, samplePoint).r + tex2D(uImage1, samplePoint2).r) / 2;
    
    //Only draw a slice with angles between 'rotation' and 'splitAngle'. The final && is to stop it from going doing a fucky wucky when it crosses the 2Pi > 0 border.    
    if (angle > splitAnglePlusRotationMinusPI)
    {
        return empty;
    }    
    if (angle < rotationMinusPI && angle > RotationMinus2PIMinusSplitAngleMinusPI)
    {
        return empty;
    }    
    
    //Make it taper off on the edges    
    float edgeDistance = min(min(abs(angle - rotationMinusPI), abs(angle - rotationMinusPI + TWOPI)), min(abs(splitAnglePlusRotationMinusPI - angle), abs(angle - RotationMinus2PIMinusSplitAngleMinusPI)));
    if (abs(edgeDistance) < 0.311)
    {
        intensity = lerp(0.0, intensity, edgeDistance / 0.311);
    }    
    
    //Make it taper off near the source
    //This pushed it past the ps_2_0 instruction limit
    if (dist < 0.02)
    {
        intensity = lerp(0.0, intensity, dist / 0.02);
    }
    
    if (dist > length)
    {
        intensity = lerp(intensity, 0, min((dist - length) * 10, 1));
    }

    //Scale 'intensity' into the RGB channels. Values are fine-tuned to turn noise into a fire-like effect.
    return float4(pow(intensity, 6) * dist * dist, pow(intensity, 4.8) * dist, pow(intensity, 4.5), 1) * 100 * opacity;
}

technique SyntheticBlizzard
{
    pass SyntheticBlizzardPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}