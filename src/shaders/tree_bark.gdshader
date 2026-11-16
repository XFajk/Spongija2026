shader_type spatial;
render_mode cull_disabled;

// Trunks and branches. Same bend as foliage.gdshader, so the leaves stay on their branches.
// Set up by FoliageWindImport.gd.

group_uniforms look;
uniform sampler2D albedo_texture : source_color, filter_linear_mipmap;
uniform float roughness : hint_range(0.0, 1.0) = 0.8;

group_uniforms placement;
// The vertex farthest from the root, in the model's space.
uniform float foliage_radius = 10.0;
group_uniforms;

#include "res://src/shaders/wind.gdshaderinc"

void vertex() {
	VERTEX += wind_offset(MODEL_MATRIX, root_bend(VERTEX, foliage_radius));
}

void fragment() {
	ALBEDO = texture(albedo_texture, UV).rgb;
	ROUGHNESS = roughness;
}
