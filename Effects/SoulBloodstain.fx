float bloodstainTime;
float bloodstainIntensity;
float bloodstainSeed;
float bloodstainReactive;

float Hash(float2 samplePosition)
{
	return frac(sin(dot(samplePosition, float2(127.1, 311.7)) + bloodstainSeed * 17.0) * 43758.5453);
}

float4 BloodstainPixel(float2 uv : TEXCOORD0) : COLOR0
{
	float2 centered = (uv - 0.5) * 2.0;
	float radius = length(centered);
	float angle = atan2(centered.y, centered.x);
	float time = bloodstainTime * 0.72;

	float edgeNoise = sin(angle * 7.0 + time * 1.7 + bloodstainSeed) * 0.055;
	edgeNoise += sin(angle * 13.0 - time * 1.15) * 0.028;
	edgeNoise += (Hash(floor(uv * 18.0)) - 0.5) * 0.025;
	float shape = radius - edgeNoise;

	float body = 1.0 - smoothstep(0.72, 1.0, shape);
	float rim = smoothstep(0.68, 0.83, shape) * (1.0 - smoothstep(0.87, 1.04, shape));
	float inwardFlow = sin(radius * 30.0 + angle * 3.0 + time * 4.2);
	inwardFlow = inwardFlow * 0.5 + 0.5;
	float veins = pow(saturate(sin(angle * 5.0 - radius * 22.0 + time * 3.0)), 8.0) * body;

	float3 voidViolet = float3(0.065, 0.018, 0.105);
	float3 deepViolet = float3(0.28, 0.065, 0.42);
	float3 paleViolet = float3(0.70, 0.34, 0.92);
	float3 cyanEdge = float3(0.18, 0.78, 0.88);

	float flowStrength = 0.68 + inwardFlow * 0.22;
	float3 color = lerp(voidViolet, deepViolet, body * flowStrength);
	color = lerp(color, paleViolet, rim * 0.62);
	color = lerp(color, cyanEdge, saturate(rim * (0.16 + bloodstainReactive * 0.30) + veins * 0.12));

	float alpha = body * (0.62 + inwardFlow * 0.12) + rim * (0.34 + bloodstainReactive * 0.22);
	alpha *= saturate(bloodstainIntensity);
	alpha *= smoothstep(0.02, 0.13, uv.x) * smoothstep(0.02, 0.13, 1.0 - uv.x);
	alpha *= smoothstep(0.02, 0.16, uv.y) * smoothstep(0.02, 0.16, 1.0 - uv.y);

	// AlphaBlend expects premultiplied output.
	return float4(color * alpha, alpha);
}

technique SoulBloodstain
{
	pass BloodstainPass
	{
		PixelShader = compile ps_3_0 BloodstainPixel();
	}
};
