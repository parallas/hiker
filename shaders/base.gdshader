shader_type spatial;

uniform sampler2D main_tex : source_color, hint_default_white;


void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	// Called for every pixel the material is visible on.
	vec4 mainTexSample = texture(main_tex, UV);
	ALBEDO = COLOR.rgb * mainTexSample.rgb;
}

void light() {
    DIFFUSE_LIGHT += clamp(
		step(0, dot(NORMAL, LIGHT)), 0.0, 1.0
	) * ATTENUATION * LIGHT_COLOR / PI;

	//DIFFUSE_LIGHT += 1.0;
}
