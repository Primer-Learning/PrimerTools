shader_type spatial;

uniform sampler2D texture_albedo : source_color;
uniform vec2 zoom_center : hint_range(0.0, 1.0) = vec2(0.5, 0.5);
uniform float zoom_factor : hint_range(1.0, 10.0) = 2.0;

void fragment() {
    vec2 uv = UV;

    // Shift UV to center the zoom
    uv -= zoom_center;

    // Apply zoom
    uv *= zoom_factor;

    // Shift UV back
    uv += zoom_center;

    // Sample the texture with modified UV
    ALBEDO = texture(texture_albedo, uv).rgb;
}