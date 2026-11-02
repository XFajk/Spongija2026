shader_type canvas_item;

uniform float strength : hint_range(0.0, 1.0) = 1.0;
uniform float center_alpha : hint_range(0.0, 1.0) = 0.0;
uniform float radius : hint_range(0.0, 1.0) = 0.45;
uniform float softness : hint_range(0.01, 1.0) = 0.35;

void fragment() {
    vec2 uv = UV;

    float dist = distance(uv, vec2(0.5));

    float vignette = smoothstep(
        radius,
        radius + softness,
        dist
    );

    float alpha = mix(center_alpha, 1.0, vignette);

    COLOR = vec4(0.0, 0.0, 0.0, alpha * strength);
}