shader_type canvas_item;

// Animated fill for the mana ProgressBar. The material sits on the whole bar, which also draws the rim, so only
// pixels drawn in fill_key_color (the theme's fill color, pure blue) get the effect; everything else is untouched.
// Two layers of the noise texture scroll past each other: where they're dim the fill is deep_color, where they're
// bright it's bright_color, and where both peak together there are sparks.

// Any texture that tiles works; a seamless NoiseTexture2D by default (assets/ui/mana_bar_noise.tres)
uniform sampler2D noise_texture : hint_default_white, repeat_enable, filter_linear;

// The fill's color in the theme; pixels within key_tolerance of it are treated as fill
uniform vec4 fill_key_color : source_color = vec4(0.0, 0.0, 1.0, 1.0);
uniform float key_tolerance : hint_range(0.0, 1.0) = 0.1;

uniform vec4 deep_color : source_color = vec4(0.03, 0.06, 0.35, 1.0);
uniform vec4 bright_color : source_color = vec4(0.3, 0.65, 1.0, 1.0);
uniform vec4 spark_color : source_color = vec4(0.9, 0.97, 1.0, 1.0);

// Size of one noise tile on screen, in pixels
uniform float noise_scale = 200.0;
// Noise tiles per second for each layer; different directions make the fill swirl instead of slide
uniform vec2 scroll_speed_a = vec2(0.08, 0.03);
uniform vec2 scroll_speed_b = vec2(-0.05, 0.04);
// How much brighter than the rest both layers must be for a spark; higher means fewer
uniform float spark_threshold : hint_range(0.0, 1.0) = 0.55;
uniform float brightness : hint_range(0.0, 3.0) = 1.2;

// Pixel position in the bar, so the noise stays fixed in size whatever the bar's size or fill
varying vec2 local_position;

void vertex() {
	local_position = VERTEX;
}

void fragment() {
	if (distance(COLOR.rgb, fill_key_color.rgb) <= key_tolerance) {
		vec2 uv = local_position / noise_scale;
		float a = texture(noise_texture, uv + TIME * scroll_speed_a).r;
		// The second layer is smaller, so the two never line up into a repeating pattern
		float b = texture(noise_texture, uv * 1.7 + TIME * scroll_speed_b).r;

		vec3 color = mix(deep_color.rgb, bright_color.rgb, smoothstep(0.3, 0.75, (a + b) * 0.5));
		float spark = smoothstep(spark_threshold, 1.0, a * b * 1.6);
		color = mix(color, spark_color.rgb, spark);

		// Keeps the fill's own alpha, so its anti-aliased edges stay soft
		COLOR = vec4(color * brightness, COLOR.a);
	}
}
