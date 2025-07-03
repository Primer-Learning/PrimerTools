shader_type sky;

// Base color - corrected to RGB(47, 51, 54)
uniform vec3 base_color : source_color = vec3(0.184, 0.2, 0.212);
uniform vec3 mist_color : source_color = vec3(0.184, 0.2, 0.212);

// Animation and appearance controls
uniform float mist_scale : hint_range(0.5, 100.0) = 3.0;
uniform float mist_speed : hint_range(0.0, 5.0) = 0.5;
uniform float mist_intensity : hint_range(0.0, 1.0) = 0.3;
uniform float distortion_scale : hint_range(0.0, 5.0) = 1.5;
uniform float distortion_speed : hint_range(0.0, 2.0) = 0.2;

// Hash function for randomness
vec3 hash3(vec3 p) {
    p = vec3(dot(p, vec3(127.1, 311.7, 74.7)),
             dot(p, vec3(269.5, 183.3, 246.1)),
             dot(p, vec3(113.5, 271.9, 124.6)));
    return -1.0 + 2.0 * fract(sin(p) * 43758.5453123);
}

// Smooth noise using gradients for better continuity
float noise(vec3 p) {
    vec3 i = floor(p);
    vec3 f = fract(p);
    vec3 u = f * f * (3.0 - 2.0 * f);

    return mix(mix(mix(dot(hash3(i + vec3(0.0, 0.0, 0.0)), f - vec3(0.0, 0.0, 0.0)),
                       dot(hash3(i + vec3(1.0, 0.0, 0.0)), f - vec3(1.0, 0.0, 0.0)), u.x),
                   mix(dot(hash3(i + vec3(0.0, 1.0, 0.0)), f - vec3(0.0, 1.0, 0.0)),
                       dot(hash3(i + vec3(1.0, 1.0, 0.0)), f - vec3(1.0, 1.0, 0.0)), u.x), u.y),
               mix(mix(dot(hash3(i + vec3(0.0, 0.0, 1.0)), f - vec3(0.0, 0.0, 1.0)),
                       dot(hash3(i + vec3(1.0, 0.0, 1.0)), f - vec3(1.0, 0.0, 1.0)), u.x),
                   mix(dot(hash3(i + vec3(0.0, 1.0, 1.0)), f - vec3(0.0, 1.0, 1.0)),
                       dot(hash3(i + vec3(1.0, 1.0, 1.0)), f - vec3(1.0, 1.0, 1.0)), u.x), u.y), u.z);
}

// FBM for layered detail
float fbm(vec3 p, int octaves) {
    float value = 0.0;
    float amplitude = 0.5;

    for (int i = 0; i < octaves; i++) {
        value += amplitude * noise(p);
        p *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

void sky() {
    vec3 dir = normalize(EYEDIR);

    // Apply mist_scale to control the size of the noise patterns
    vec3 scaled_dir = dir * mist_scale;

    // Animate by adding time-based offsets
    vec3 flow1 = scaled_dir + vec3(TIME * mist_speed * 2.0, 0.0, 0.0);
    vec3 flow2 = scaled_dir * 1.5 + vec3(0.0, TIME * mist_speed * 1.4, TIME * mist_speed * 0.6);
    vec3 flow3 = scaled_dir * 0.7 + vec3(TIME * mist_speed * 0.6, TIME * mist_speed * 1.0, 0.0);

    // Add subtle distortion (also scale the distortion coordinates)
    vec3 distort_coords = dir * distortion_scale * mist_scale + vec3(TIME * distortion_speed * 3.0);
    float distortion = noise(distort_coords) * 0.1;

    // Create mist layers
    float mist1 = fbm(flow1 + distortion, 3);
    float mist2 = fbm(flow2 - distortion * 0.5, 3);
    float mist3 = fbm(flow3 + distortion * 0.3, 2);

    // Combine layers with different weights
    float combined_mist = mist1 * 0.5 + mist2 * 0.3 + mist3 * 0.2;

    // Adjust contrast and intensity
    combined_mist = (combined_mist * 0.5 + 0.5); // Normalize to 0-1
    combined_mist = pow(combined_mist, 1.5) * mist_intensity;

    // Mix colors
    vec3 final_color = mix(base_color, mist_color, combined_mist);

    // Optional: Add subtle vertical gradient
    float gradient = 1.0 - abs(dir.y) * 0.2;
    final_color *= gradient;

    COLOR = final_color;
}