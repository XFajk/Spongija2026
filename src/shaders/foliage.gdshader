shader_type spatial;
render_mode cull_disabled;

// Modeled foliage (the ferns), with its origin at the root. Set up by FoliageWindImport.gd.

group_uniforms look;
uniform sampler2D albedo_texture : source_color, filter_linear_mipmap;
uniform float alpha_threshold : hint_range(0.0, 1.0) = 0.5;
uniform float roughness : hint_range(0.0, 1.0) = 0.5;

group_uniforms placement;
// The vertex farthest from the root, in the model's space.
uniform float foliage_radius = 4.0;
group_uniforms;

#include "res://src/shaders/wind.gdshaderinc"

void vertex() {
	// Fronds arch outward, so their tips are far from the root but not always high up.
	// Squared so they curve: the part near the root barely moves.
	float bend = clamp(length(VERTEX) / foliage_radius, 0.0, 1.0);
	VERTEX += wind_offset(MODEL_MATRIX, bend * bend);
}

void fragment() {
	vec4 color = texture(albedo_texture, UV);

	ALBEDO = color.rgb;
	ALPHA = color.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_threshold;
	ROUGHNESS = roughness;
}
