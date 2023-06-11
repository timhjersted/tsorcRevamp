sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float time;
float2 textureSize;
float2 effectSize;
float splitAngle;
float rotation;
float width;
float4 effectColor;

//Precomputed values to save on instruction count
float RotationMinus2PIMinusSplitAngle;
float splitAnglePlusRotation;
float splitAnglePlusRotationMinusPI;
float rotationMinusPI;
float RotationMinus2PIMinusSplitAngleMinusPI; //Peak self-documenting code. Even this comment isn't required. You already know why this exists in the exact form it does here.
float OneThirdMinusTwoPiMinusSplit;
float rotationOverTwoPi;

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
    float2 samplePoint = float2(dist - time, mad(angle, INVERSETWOPI, rotationOverTwoPi));
    
    //Calculate how intense a pixel should be based on the noise generator
    float intensity = tex2D(uImage0, samplePoint).r;
    
    
    
    
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
    if (abs(edgeDistance) < .311)
    {
        intensity = lerp(0.0, intensity, edgeDistance / .311);
    }    
    
    //Make it taper off near the source
    if (dist < .02)
    {
        //intensity = lerp(0.0, intensity, dist / .02);
    }

    return float4(pow(intensity, 4.5), pow(intensity, 4.8) * dist, 0, 1) * 100;
}

technique FireWaveShader
{
    pass FireWaveShaderPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}