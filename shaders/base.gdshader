shader_type spatial;

uniform sampler2D main_tex : source_color, hint_default_white;
instance uniform vec3 tint : source_color = vec3(1,1,1);
uniform sampler2D tint_mask : hint_default_white;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	// Called for every pixel the material is visible on.
	vec4 mainTexSample = texture(main_tex, UV);
	float tintMaskSample = texture(tint_mask, UV).r;
	vec3 tintAmount = mix(vec3(1.0), tint, tintMaskSample);
	ALBEDO = COLOR.rgb * mainTexSample.rgb * tintAmount.rgb;
}

void light() {
    DIFFUSE_LIGHT += clamp(
		step(0, dot(NORMAL, LIGHT)), 0.0, 1.0
	) * ATTENUATION * LIGHT_COLOR / PI;

	//DIFFUSE_LIGHT += 1.0;
}
