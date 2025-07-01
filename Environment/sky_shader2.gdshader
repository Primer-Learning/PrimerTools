shader_type sky;

// Base colors
uniform vec3 base_color : source_color = vec3(0.184, 0.2, 0.212);
uniform vec3 mist_color : source_color = vec3(0.184, 0.2, 0.212);

// Animation and appearance controls
uniform float mist_scale : hint_range(0.5, 100.0) = 3.0;
uniform float mist_speed : hint_range(0.0, 5.0) = 0.5;
uniform float mist_intensity : hint_range(0.0, 1.0) = 0.3;

// Noise type flags (binary encoded for 3 layers)
// Bit 0 (1) = Layer 1, Bit 1 (2) = Layer 2, Bit 2 (4) = Layer 3
// Examples: 7 = all layers, 5 = layers 1&3, 2 = layer 2 only
uniform int perlin_layers : hint_range(0, 7) = 7;
uniform int worley_layers : hint_range(0, 7) = 0;
uniform int voronoi_edge_layers : hint_range(0, 7) = 0;

// Geometric pattern flags
uniform int grid_layers : hint_range(0, 7) = 0;
uniform int crystalline_layers : hint_range(0, 7) = 0;
uniform int hexagonal_layers : hint_range(0, 7) = 0;
uniform int line_network_layers : hint_range(0, 7) = 0;
uniform int mondrian_layers : hint_range(0, 7) = 0;
uniform int circuit_layers : hint_range(0, 7) = 0;

// Geometric pattern parameters
uniform float grid_size : hint_range(0.5, 10.0) = 2.0;
uniform float line_thickness : hint_range(0.01, 0.1) = 0.02;

// Curl influence per layer
uniform float curl_influence : hint_range(0.0, 1.0) = 0.0;

// Octaves per layer
uniform int layer1_octaves : hint_range(1, 8) = 3;
uniform int layer2_octaves : hint_range(1, 8) = 3;
uniform int layer3_octaves : hint_range(1, 8) = 2;

// Distortion controls
uniform float distortion_scale : hint_range(0.0, 5.0) = 1.5;
uniform float distortion_speed : hint_range(0.0, 2.0) = 0.2;
uniform float domain_warp_strength : hint_range(0.0, 1.0) = 0.0;

// Artistic effects
uniform int quantization_levels : hint_range(0, 20) = 0; // 0 = off
uniform float edge_detection_strength_layer1 : hint_range(0.0, 1.0) = 0.0;
uniform float edge_detection_strength_layer2 : hint_range(0.0, 1.0) = 0.0;
uniform float edge_detection_strength_layer3 : hint_range(0.0, 1.0) = 0.0;
uniform float contrast_boost : hint_range(0.5, 3.0) = 1.5;

// Pattern breakers (using int as bool: 0 = false, 1 = true)
uniform int use_rotation : hint_range(0, 1) = 0;
uniform float rotation_speed : hint_range(0.0, 1.0) = 0.1;
uniform int use_spherical_mapping : hint_range(0, 1) = 0;

// Debug
uniform int show_noise_only : hint_range(0, 1) = 0;

// Hash function for randomness
vec3 hash3(vec3 p) {
    p = vec3(dot(p, vec3(127.1, 311.7, 74.7)),
             dot(p, vec3(269.5, 183.3, 246.1)),
             dot(p, vec3(113.5, 271.9, 124.6)));
    return -1.0 + 2.0 * fract(sin(p) * 43758.5453123);
}

// Perlin noise
float perlinNoise(vec3 p) {
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

// Worley/Cellular noise
float worleyNoise(vec3 p) {
    vec3 i = floor(p);
    vec3 f = fract(p);
    
    float minDist = 1.0;
    
    for (int x = -1; x <= 1; x++) {
        for (int y = -1; y <= 1; y++) {
            for (int z = -1; z <= 1; z++) {
                vec3 neighbor = vec3(float(x), float(y), float(z));
                vec3 point = hash3(i + neighbor) * 0.5 + 0.5;
                float dist = length(f - neighbor - point);
                minDist = min(minDist, dist);
            }
        }
    }
    
    return minDist;
}

// Voronoi edge noise
float voronoiEdgeNoise(vec3 p) {
    vec3 i = floor(p);
    vec3 f = fract(p);
    
    float minDist1 = 1.0;
    float minDist2 = 1.0;
    
    for (int x = -1; x <= 1; x++) {
        for (int y = -1; y <= 1; y++) {
            for (int z = -1; z <= 1; z++) {
                vec3 neighbor = vec3(float(x), float(y), float(z));
                vec3 point = hash3(i + neighbor) * 0.5 + 0.5;
                float dist = length(f - neighbor - point);
                
                if (dist < minDist1) {
                    minDist2 = minDist1;
                    minDist1 = dist;
                } else if (dist < minDist2) {
                    minDist2 = dist;
                }
            }
        }
    }
    
    return minDist2 - minDist1;
}

// Curl noise modifier
vec3 curlOffset(vec3 p) {
    float epsilon = 0.01;
    
    vec3 dx = vec3(epsilon, 0.0, 0.0);
    vec3 dy = vec3(0.0, epsilon, 0.0);
    vec3 dz = vec3(0.0, 0.0, epsilon);
    
    float x = perlinNoise(p + dy) - perlinNoise(p - dy) - perlinNoise(p + dz) + perlinNoise(p - dz);
    float y = perlinNoise(p + dz) - perlinNoise(p - dz) - perlinNoise(p + dx) + perlinNoise(p - dx);
    float z = perlinNoise(p + dx) - perlinNoise(p - dx) - perlinNoise(p + dy) + perlinNoise(p - dy);
    
    return vec3(x, y, z) / (2.0 * epsilon);
}

// Grid-based pattern with random offsets
float randomGrid(vec3 p) {
    vec3 grid = floor(p / grid_size);
    vec3 localPos = fract(p / grid_size);
    
    // Random offset per cell
    vec3 offset = hash3(grid) * 0.3;
    localPos += offset;
    
    // Sharp edges at grid boundaries
    float edges = 1.0 - smoothstep(0.0, line_thickness, min(localPos.x, localPos.y));
    edges = max(edges, 1.0 - smoothstep(0.0, line_thickness, min(1.0 - localPos.x, 1.0 - localPos.y)));
    
    return edges;
}

// Triangular/Crystalline pattern
float crystallinePattern(vec3 p) {
    // Create triangular grid
    vec2 p2 = p.xy * grid_size;
    float sqrt3 = 1.7320508;
    vec2 a = mod(p2, vec2(1.0, sqrt3));
    vec2 b = mod(p2 - vec2(0.5, sqrt3 * 0.5), vec2(1.0, sqrt3));
    
    // Distance to triangle edges
    float d1 = abs(a.x - a.y / sqrt3);
    float d2 = abs(b.x - b.y / sqrt3);
    float d3 = abs(a.x + a.y / sqrt3 - 1.0);
    
    float pattern = 1.0 - smoothstep(0.0, line_thickness, min(min(d1, d2), d3));
    
    // Add some randomness
    pattern *= 0.5 + 0.5 * hash3(floor(p * 3.0)).x;
    
    return pattern;
}

// Hexagonal grid with random heights
float hexagonalPattern(vec3 p) {
    vec2 p2 = p.xy * grid_size;
    vec2 hex = vec2(1.0, 1.7320508);
    vec2 a = mod(p2, hex) - hex * 0.5;
    vec2 b = mod(p2 - hex * 0.5, hex) - hex * 0.5;
    
    float d1 = length(a);
    float d2 = length(b);
    
    vec2 cellId = (d1 < d2) ? floor(p2 / hex) : floor((p2 - hex * 0.5) / hex);
    
    // Random height per hex cell
    float height = hash3(vec3(cellId, 0.0)).x;
    
    // Sharp hexagon edges
    float hexDist = min(d1, d2);
    float edge = 1.0 - smoothstep(0.4 - line_thickness, 0.4, hexDist);
    
    return edge * height;
}

// Random line networks (like cracks or lightning)
float lineNetwork(vec3 p) {
    float pattern = 0.0;
    vec3 pos = p * grid_size;
    
    // Multiple scales of line networks
    for (int i = 0; i < 3; i++) {
        vec3 cellId = floor(pos);
        vec3 localPos = fract(pos);
        
        // Random line direction per cell
        vec3 lineDir = normalize(hash3(cellId));
        
        // Distance to line through cell center
        vec3 toPoint = localPos - vec3(0.5);
        float distToLine = length(cross(toPoint, lineDir));
        
        // Sharp line
        float line = 1.0 - smoothstep(0.0, line_thickness * float(i + 1), distToLine);
        
        // Random intensity
        line *= abs(hash3(cellId * 7.13).y);
        
        pattern = max(pattern, line);
        pos *= 2.3;
    }
    
    return pattern;
}

// Rectangular Mondrian-style patterns
float mondrianPattern(vec3 p) {
    float pattern = 0.0;
    vec2 pos = p.xy * grid_size;
    
    // Recursive subdivision
    for (int i = 0; i < 4; i++) {
        vec2 cellId = floor(pos);
        vec2 localPos = fract(pos);
        
        // Random subdivision point
        vec2 split = hash3(vec3(cellId, float(i))).xy * 0.3 + 0.35;
        
        // Edge detection
        float edges = 1.0 - smoothstep(0.0, line_thickness, min(min(localPos.x, localPos.y), 
                                                      min(1.0 - localPos.x, 1.0 - localPos.y)));
        
        // Add subdivision lines
        if (hash3(vec3(cellId * 3.7, float(i))).x > 0.5) {
            // Vertical split
            edges = max(edges, 1.0 - smoothstep(0.0, line_thickness, abs(localPos.x - split.x)));
        } else {
            // Horizontal split
            edges = max(edges, 1.0 - smoothstep(0.0, line_thickness, abs(localPos.y - split.y)));
        }
        
        pattern = max(pattern, edges);
        pos *= 2.1;
    }
    
    return pattern;
}

// Angular circuit-like pattern
float circuitPattern(vec3 p) {
    vec3 gridPos = floor(p * grid_size * 2.0);
    vec3 localPos = fract(p * grid_size * 2.0);
    
    float pattern = 0.0;
    
    // Random connection pattern per cell
    float rand = hash3(gridPos).x;
    
    // Create L-shaped or T-shaped connections
    if (rand < 0.33) {
        // L-shape
        pattern = max(step(0.5 - line_thickness, localPos.x) * step(localPos.x, 0.5 + line_thickness) * step(localPos.y, 0.5 + line_thickness),
                     step(0.5 - line_thickness, localPos.y) * step(localPos.y, 0.5 + line_thickness) * step(localPos.x, 0.5 + line_thickness));
    } else if (rand < 0.66) {
        // T-shape
        pattern = max(step(0.5 - line_thickness, localPos.x) * step(localPos.x, 0.5 + line_thickness),
                     step(0.5 - line_thickness, localPos.y) * step(localPos.y, 0.5 + line_thickness));
    } else {
        // Cross
        pattern = max(step(0.5 - line_thickness, localPos.x) * step(localPos.x, 0.5 + line_thickness),
                     step(0.5 - line_thickness, localPos.y) * step(localPos.y, 0.5 + line_thickness));
    }
    
    return pattern;
}

// Check if a layer has a specific noise type enabled
bool hasNoiseType(int layers, int layerIndex) {
    return (layers & (1 << layerIndex)) != 0;
}

// Combined noise function for a specific layer
float layerNoise(vec3 p, int layerIndex) {
    float result = 0.0;
    float totalWeight = 0.0;
    
    // Check which noise types are enabled for this layer
    if (hasNoiseType(perlin_layers, layerIndex)) {
        result += perlinNoise(p);
        totalWeight += 1.0;
    }
    
    if (hasNoiseType(worley_layers, layerIndex)) {
        result += worleyNoise(p);
        totalWeight += 1.0;
    }
    
    if (hasNoiseType(voronoi_edge_layers, layerIndex)) {
        result += voronoiEdgeNoise(p);
        totalWeight += 1.0;
    }
    
    // Check geometric patterns
    if (hasNoiseType(grid_layers, layerIndex)) {
        result += randomGrid(p);
        totalWeight += 1.0;
    }
    
    if (hasNoiseType(crystalline_layers, layerIndex)) {
        result += crystallinePattern(p);
        totalWeight += 1.0;
    }
    
    if (hasNoiseType(hexagonal_layers, layerIndex)) {
        result += hexagonalPattern(p);
        totalWeight += 1.0;
    }
    
    if (hasNoiseType(line_network_layers, layerIndex)) {
        result += lineNetwork(p);
        totalWeight += 1.0;
    }
    
    if (hasNoiseType(mondrian_layers, layerIndex)) {
        result += mondrianPattern(p);
        totalWeight += 1.0;
    }
    
    if (hasNoiseType(circuit_layers, layerIndex)) {
        result += circuitPattern(p);
        totalWeight += 1.0;
    }
    
    // If no noise types are enabled for this layer, return 0
    if (totalWeight == 0.0) {
        return 0.0;
    }
    
    return result / totalWeight;
}

// FBM using layer-specific noise
float fbm(vec3 p, int octaves, int layerIndex) {
    float value = 0.0;
    float amplitude = 0.5;
    
    for (int i = 0; i < octaves; i++) {
        value += amplitude * layerNoise(p, layerIndex);
        p *= 2.0;
        amplitude *= 0.5;
    }
    
    return value;
}

// Rotation matrix
mat3 rotationMatrix(float angle) {
    float c = cos(angle);
    float s = sin(angle);
    return mat3(
        vec3(c, -s, 0.0),
        vec3(s, c, 0.0),
        vec3(0.0, 0.0, 1.0)
    );
}

void sky() {
    vec3 dir = normalize(EYEDIR);
    vec3 scaled_dir = dir * mist_scale;
    
    // Apply spherical mapping if enabled
    if (use_spherical_mapping == 1) {
        float theta = atan(dir.z, dir.x);
        float phi = asin(clamp(dir.y, -1.0, 1.0));
        scaled_dir = vec3(theta, phi, 1.0) * mist_scale;
    }
    
    // Apply rotation if enabled
    if (use_rotation == 1) {
        mat3 rot = rotationMatrix(TIME * rotation_speed);
        scaled_dir = rot * scaled_dir;
    }
    
    // Domain warping
    if (domain_warp_strength > 0.0) {
        vec3 warp1 = vec3(perlinNoise(scaled_dir * 2.0));
        vec3 warp2 = vec3(perlinNoise(scaled_dir * 4.0 + vec3(TIME * 0.1)));
        scaled_dir += (warp1 * 0.3 + warp2 * 0.15) * domain_warp_strength;
    }
    
    // Animate by adding time-based offsets
    //vec3 flow1 = scaled_dir * TIME * mist_speed;
	vec3 flow1 = scaled_dir + vec3(1.0, 1.0, 1.0) * TIME * mist_speed;
    vec3 flow2 = scaled_dir * 1.5 + vec3(0.0, TIME * mist_speed * 1.4, TIME * mist_speed * 0.6);
    vec3 flow3 = scaled_dir * 0.7 + vec3(TIME * mist_speed * 0.6, TIME * mist_speed * 1.0, 0.0);
    
    // Apply curl noise influence
    if (curl_influence > 0.0) {
        flow1 += curlOffset(flow1 * 0.5) * curl_influence;
        flow2 += curlOffset(flow2 * 0.5) * curl_influence;
        flow3 += curlOffset(flow3 * 0.5) * curl_influence;
    }
    
    // Add subtle distortion
    vec3 distort_coords = dir * distortion_scale * mist_scale + vec3(TIME * distortion_speed * 3.0);
    float distortion = perlinNoise(distort_coords) * 0.1;
    
    // Create mist layers with layer-specific noise
    float mist1 = fbm(flow1 + distortion, layer1_octaves, 0);        // Layer 0 (bit 0)
    float mist2 = fbm(flow2 - distortion * 0.5, layer2_octaves, 1);  // Layer 1 (bit 1)
    float mist3 = fbm(flow3 + distortion * 0.3, layer3_octaves, 2);  // Layer 2 (bit 2)
    
    // Apply edge detection per layer if enabled
    if (quantization_levels == 0) {  // Edge detection doesn't work well with quantization
        if (edge_detection_strength_layer1 > 0.0) {
            float edge_mist1 = fbm(flow1 + distortion + vec3(0.01, 0.0, 0.0), layer1_octaves, 0) - mist1;
            edge_mist1 += fbm(flow1 + distortion + vec3(0.0, 0.01, 0.0), layer1_octaves, 0) - mist1;
            edge_mist1 = abs(edge_mist1) * 10.0;
            mist1 = mix(mist1, 1.0 - edge_mist1, edge_detection_strength_layer1);
        }
        
        if (edge_detection_strength_layer2 > 0.0) {
            float edge_mist2 = fbm(flow2 - distortion * 0.5 + vec3(0.01, 0.0, 0.0), layer2_octaves, 1) - mist2;
            edge_mist2 += fbm(flow2 - distortion * 0.5 + vec3(0.0, 0.01, 0.0), layer2_octaves, 1) - mist2;
            edge_mist2 = abs(edge_mist2) * 10.0;
            mist2 = mix(mist2, 1.0 - edge_mist2, edge_detection_strength_layer2);
        }
        
        if (edge_detection_strength_layer3 > 0.0) {
            float edge_mist3 = fbm(flow3 + distortion * 0.3 + vec3(0.01, 0.0, 0.0), layer3_octaves, 2) - mist3;
            edge_mist3 += fbm(flow3 + distortion * 0.3 + vec3(0.0, 0.01, 0.0), layer3_octaves, 2) - mist3;
            edge_mist3 = abs(edge_mist3) * 10.0;
            mist3 = mix(mist3, 1.0 - edge_mist3, edge_detection_strength_layer3);
        }
    }
    
    // Combine layers
    float combined_mist = mist1 * 0.5 + mist2 * 0.3 + mist3 * 0.2;
    
    // Normalize to 0-1
    combined_mist = (combined_mist * 0.5 + 0.5);
    
    // Apply quantization if enabled
    if (quantization_levels > 0) {
        combined_mist = floor(combined_mist * float(quantization_levels)) / float(quantization_levels);
    }
    
    // Apply contrast
    combined_mist = pow(combined_mist, contrast_boost) * mist_intensity;
    
    // Check if in debug mode
    if (show_noise_only == 1) {
        // Show noise only
        COLOR = vec3(combined_mist);
    } else {
        // Normal rendering
        vec3 final_color = mix(base_color, mist_color, combined_mist);
        
        // Optional: Add subtle vertical gradient
        float gradient = 1.0 - abs(dir.y) * 0.2;
        final_color *= gradient;
        
        COLOR = final_color;
    }
}