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

float writeProgress;
float sentenceTime;
float sentenceIntensity;
float sentenceMode;

float4 SentencePixel(float2 textureCoordinates : TEXCOORD0) : COLOR0
{
	float along = textureCoordinates.x;
	float across = abs(textureCoordinates.y - 0.5) * 2.0;
	// Thin at the tip, full toward the cross, then ease off so the join is a knot not a square.
	float taper = lerp(0.28, 1.0, smoothstep(0.0, 0.55, along));
	across /= max(taper, 0.08);
	float written = saturate((writeProgress + 0.06 - along) * 16.0);
	float tipFade = smoothstep(0.0, 0.08, along);
	float joinFade = 1.0 - smoothstep(0.84, 1.0, along) * 0.55;
	float head = exp(-pow((along - writeProgress) * 11.0, 2.0));
	head *= 1.0 - saturate((writeProgress - 0.96) * 18.0);
	float flow = 0.86 + 0.14 * sin(along * 28.0 - sentenceTime * 10.0);

	float3 deepTeal = float3(0.02, 0.18, 0.2);
	float3 congregationTeal = float3(0.06, 0.78, 0.7);
	float3 paleSoul = float3(0.82, 1.0, 0.95);

	if (sentenceMode > 1.5)
	{
		float bloom = exp(-across * across * 1.55);
		float3 color = lerp(deepTeal, congregationTeal, saturate(bloom));
		float alpha = bloom * 0.22 * written * tipFade * joinFade * flow * saturate(sentenceIntensity);
		return float4(color, alpha);
	}

	float body = exp(-across * across * 3.4);
	float core = exp(-across * across * 16.0);
	float3 color = lerp(deepTeal, congregationTeal, saturate(body));
	color = lerp(color, paleSoul, saturate(core * 0.82 + head * 0.9));
	float alpha = saturate(body * 0.42 + core * 0.7 + head * 0.65) * written * tipFade * joinFade * flow;
	alpha *= saturate(sentenceIntensity);
	return float4(color, alpha);
}

technique CruxSentence
{
	pass SentencePass
	{
		PixelShader = compile ps_3_0 SentencePixel();
	}
};
