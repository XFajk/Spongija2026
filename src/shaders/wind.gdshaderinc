// Wind shared by all foliage. Change these values here, so everything stays in the same gusts.
const vec3 WIND_DIRECTION = vec3(1.0, 0.0, 1.0);
const float WIND_FREQUENCY = 3.0;
const float WIND_NOISE_SCALE = 0.01; // 0.01 = the noise repeats every 100 m
const float WIND_GUST_SPEED = 0.1;   // how fast the gusts travel
const float WIND_FLUTTER = 0.2;      // how much each plant moves on its own

group_uniforms wind;
uniform sampler2D wind_noise : repeat_enable, filter_linear;
uniform float amount = 1.0;
group_uniforms;

// How far to move a vertex, in the model's space. bend is 0 at the roots and 1 at the tips.
vec3 wind_offset(mat4 model_matrix, float bend) {
	// Same value for the whole plant, so it doesn't stretch.
	vec2 root_pos = model_matrix[3].xz;

	// Big soft gusts: the noise slides across the map with the wind.
	vec2 noise_uv = root_pos * WIND_NOISE_SCALE - normalize(WIND_DIRECTION.xz) * TIME * WIND_GUST_SPEED;
	float gust = texture(wind_noise, noise_uv).r;

	// Small movement: every plant gets its own timing from its position.
	float plant_flutter = sin(TIME * WIND_FREQUENCY + root_pos.x * 7.0 + root_pos.y * 13.0);

	float sway = (gust + plant_flutter * WIND_FLUTTER) * bend * amount;
	return (inverse(model_matrix) * vec4(WIND_DIRECTION, 0.0)).xyz * sway;
}
