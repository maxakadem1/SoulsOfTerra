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
float2 uImageSize2;
float4 uShaderSpecificData;

float beamTime;
float beamIntensity;
float beamSeed;
float beamMode;
sampler beamNoiseTexture : register(s1);

float LayeredNoise(float2 samplePosition)
{
	float noise = tex2D(beamNoiseTexture, samplePosition).r * 0.58;
	noise += tex2D(beamNoiseTexture, samplePosition * 2.07 + 0.37).g * 0.29;
	noise += tex2D(beamNoiseTexture, samplePosition * 4.13 - 0.19).b * 0.13;
	return noise;
}

float4 BeamPixel(float2 textureCoordinates : TEXCOORD0) : COLOR0
{
	float2 coordinates = textureCoordinates;
	float along = coordinates.x;
	float across = abs(coordinates.y - 0.5) * 2.0;
	float2 flowingCoordinates = float2(along * 8.0 - beamTime * 0.72, coordinates.y * 2.35 + beamSeed);
	float slowNoise = LayeredNoise(flowingCoordinates);
	float fastNoise = LayeredNoise(float2(along * 15.0 - beamTime * 1.48, coordinates.y * 4.1 - beamSeed));

	float startFade = smoothstep(0.0, 0.026, along);
	float endThreshold = 0.955 + (slowNoise - 0.5) * 0.035;
	float endFade = 1.0 - smoothstep(endThreshold, 1.0, along);

	// Telegraph mode is thin and animated without resembling the damaging beam body.
	if (beamMode > 1.5)
	{
		float telegraphLine = pow(saturate(1.0 - across), 5.0);
		float movingPips = 0.42 + pow(sin((along * 31.0 - beamTime * 3.7) * 3.14159265) * 0.5 + 0.5, 7.0) * 0.58;
		float telegraphAlpha = telegraphLine * movingPips * startFade * endFade * beamIntensity;
		float3 telegraphColor = lerp(float3(0.12, 0.72, 0.68), float3(0.68, 1.0, 0.9), telegraphLine);
		return float4(telegraphColor, telegraphAlpha);
	}

	float edgeWarp = (slowNoise - 0.5) * 0.24 + (fastNoise - 0.5) * 0.08;
	float body = 1.0 - smoothstep(0.56 + edgeWarp, 1.02 + edgeWarp, across);
	float center = pow(saturate(1.0 - across), 2.35);
	float innerFlow = pow(saturate(fastNoise * 1.14 - 0.18), 2.2) * center;
	float soulKnots = pow(sin((along * 17.0 - beamTime * 1.9 + slowNoise * 2.4) * 3.14159265) * 0.5 + 0.5, 9.0) * center;
	float rim = pow(saturate(1.0 - abs(across - 0.72) * 5.3), 2.0) * (0.45 + fastNoise * 0.55);

	float3 deepTeal = float3(0.018, 0.2, 0.2);
	float3 congregationTeal = float3(0.055, 0.63, 0.58);
	float3 spectralCyan = float3(0.22, 0.96, 0.82);
	float3 paleSoul = float3(0.76, 1.0, 0.91);
	float whiteCore = pow(saturate(1.0 - across), 7.0);
	float3 beamColor = lerp(deepTeal, congregationTeal, body);
	beamColor = lerp(beamColor, spectralCyan, center * 0.68 + innerFlow * 0.42);
	beamColor += paleSoul * (whiteCore * 1.35 + soulKnots * 0.52 + rim * 0.22);

	float alpha = saturate(body * (0.52 + slowNoise * 0.28 + innerFlow * 0.22) + whiteCore * 0.35);
	if (beamMode > 0.5)
	{
		// Bloom is wider, softer, and deliberately lacks a hard center stripe.
		alpha = pow(saturate(1.0 - across), 1.45) * (0.16 + slowNoise * 0.17);
		beamColor = lerp(float3(0.0, 0.22, 0.23), congregationTeal, center) * 0.7;
	}

	alpha *= startFade * endFade * beamIntensity;
	return float4(beamColor, alpha);
}

technique CongregationBeam
{
	pass BeamPass
	{
		PixelShader = compile ps_3_0 BeamPixel();
	}
};
