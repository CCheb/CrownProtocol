shader_type spatial;

uniform sampler2D camo_tex : source_color;
uniform float speed = 0.2;
uniform float scale = 1.0;
uniform vec3 tint : source_color = vec3(1.0);
uniform float emission_strength = 1.2;

void fragment() {
    vec2 uv = UV * scale;
    uv.x += cos(TIME) * speed;
	uv.y += sin(TIME) * speed;
    vec4 c = texture(camo_tex, uv);
    ALBEDO = c.rgb * tint;
	EMISSION = c.rgb * tint * emission_strength;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
