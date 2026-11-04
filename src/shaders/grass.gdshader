shader_type spatial;
render_mode cull_disabled;

// Flat cards (grass and the plant): the texture runs from the tips (UV.y 0) to the roots (UV.y 1).

group_uniforms look;
uniform sampler2D albedo_texture : source_color, filter_linear_mipmap;
uniform float alpha_threshold : hint_range(0.0, 1.0) = 0.5;
uniform vec3 backlight : source_color = vec3(0.5);

group_uniforms placement;
// Generated cards use Height Offset 0.5 to touch the ground; the grass is at 0.75.
uniform float ground_offset = 0.25;

group_uniforms distance_fade;
uniform float fade_start = 96.0;
uniform float fade_end = 128.0;
group_uniforms;

#include "res://src/shaders/wind.gdshaderinc"

void vertex() {
	VERTEX.y -= ground_offset;
	VERTEX += wind_offset(MODEL_MATRIX, 1.0 - UV.y);
}

void fragment() {
	vec4 color = texture(albedo_texture, UV) * COLOR;
	float fade = 1.0 - smoothstep(fade_start, fade_end, length(VERTEX));

	ALBEDO = color.rgb;
	ALPHA = color.a * fade;
	ALPHA_SCISSOR_THRESHOLD = alpha_threshold;
	BACKLIGHT = backlight;
	ROUGHNESS = 1.0;
}
