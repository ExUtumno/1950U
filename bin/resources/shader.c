#version 330
out vec4 finalColor;


uniform vec2 resolution; //window size in pixels
uniform vec3 cameraPosition, cameraCorner, cameraHorizontal, cameraVertical;
uniform vec3 groundColor;

uniform int numObjects, numTextures;
uniform sampler2D data;
uniform sampler2D emojiTex;

uniform int DRAW_CONES;

const float PI = 3.1415926536;
const float MAXT = 1000000.0;
const int GROUND_CODE = -2;
const int TEXELS_PER_OBJECT = 4;

const float sunrise = 0.55 * 0.5 * PI;
const float sunangle = 0.25 * 0.5 * PI;
const vec3 toSun = vec3(cos(sunrise) * cos(sunangle), cos(sunrise) * sin(sunangle), sin(sunrise));
const float FLOOR_TILE_SIZE = 1.0; //size of each checker square in world units
const float FLOOR_COLOR_VARIATION = 0.025; //strength of the color tint between squares

struct Ray
{
    vec3 origin;
    vec3 vector;
};

struct Hit
{
    Ray ray;
    float t;
    vec3 normal;
    vec2 uv;
};

const Hit NOHIT = Hit(Ray(vec3(0,0,0), vec3(0,0,0)), MAXT, vec3(1,1,1), vec2(-1,-1));

Hit groundHit(float groundZ, in Ray ray)
{
    float vz = ray.vector.z;
    if(vz == 0.0) return NOHIT;

    float t = (groundZ - ray.origin.z) / vz;
    if(t <= 0.0) return NOHIT;

    vec3 normal = vz < 0.0 ? vec3(0,0,1) : vec3(0,0,-1);
    return Hit(ray, t, normal, vec2(-1));
}

Hit rectHit(vec3 center, float sizex, float sizey, in Ray ray)
{
    float vz = ray.vector.z;
    if(vz == 0.0) return NOHIT;

    float t = (center.z - ray.origin.z) / vz;
    if(t <= 0.0) return NOHIT;

    vec3 pHit = ray.origin + t * ray.vector;
    if (pHit.x < center.x - 0.5 * sizex || pHit.x > center.x + 0.5 * sizex || pHit.y < center.y - 0.5 * sizey || pHit.y > center.y + 0.5 * sizey) return NOHIT;

    vec3 normal = vz < 0.0 ? vec3(0,0,1) : vec3(0,0,-1);
    return Hit(ray, t, normal, vec2(-1));
}

vec2 lambertUV(vec3 n, float angle) //n is a *unit* normal
{
    float r = sqrt(max(0.0, 1.0 - n.z)); //√(1−z) ∈ [0…1]
    float theta = atan(n.y, n.x) - angle; //−π … +π
    vec2 disc  = vec2(cos(theta), sin(theta)) * r;
    return disc * 0.70710678 + 0.5; //disc lies in a unit circle; fit that circle snugly into the square [0…1]², 0.7071 = √½
}
Hit sphereHit(vec3 center, float radius, float angle, in Ray ray)
{
    vec3 tocenter = center - ray.origin;
    float b = dot(ray.vector, tocenter);
    float c = dot(tocenter, tocenter) - radius * radius;

    float D = b * b  - c;
    if (D < 0) return NOHIT;

    float t = b - sqrt(D);
    if (t <= 0) return NOHIT;

    vec3 hitPos = ray.origin + t * ray.vector;
    vec3 normal = (hitPos - center) / radius;
    return Hit(ray, t, normal, lambertUV(normal, angle));
}

mat2 rotZ(float a)
{
    float c = cos(a), s = sin(a);
    return mat2(c, -s, s,  c);
}
Hit cylinderHit(vec3 center, float radius, float height, float angle, in Ray ray)
{
    float halfH = 0.5 * height;

    //solve quadratic for side wall in the x-y plane
    vec2 toCenter = ray.origin.xy - center.xy;
    vec2 dirXY    = ray.vector.xy;

    float a = dot(dirXY, dirXY);
    if (abs(a) < 1e-12) return NOHIT; //Hit(ray, MAXT, vec3(1,1,1)); //ray runs parallel to the axis

    float b = dot(dirXY, toCenter);
    float c = dot(toCenter, toCenter) - radius * radius;
    float D = b*b - a*c;

    float tSide = MAXT;
    if (D >= 0.0)
    {
        float sqrtD = sqrt(D);
        float t1 = (-b - sqrtD) / a;
        float t2 = (-b + sqrtD) / a;

        if (t1 > 0.0)
        {
            float zHit1 = ray.origin.z + t1 * ray.vector.z;
            if (zHit1 >= center.z - halfH && zHit1 <= center.z + halfH)
                tSide = t1;
        }
        if (t2 > 0.0)
        {
            float zHit2 = ray.origin.z + t2 * ray.vector.z;
            if (zHit2 >= center.z - halfH && zHit2 <= center.z + halfH && t2 < tSide)
                tSide = t2;
        }
    }

    //hit against the (positive-z) end-cap
    float tCap = MAXT;
    {
        float planeZ = center.z + halfH; //cap at +z
        if (abs(ray.vector.z) > 1e-12)
        {
            float tHit = (planeZ - ray.origin.z) / ray.vector.z;
            if (tHit > 0.0)
            {
                vec3 pHit = ray.origin + tHit * ray.vector;
                vec2 dxy  = pHit.xy - center.xy;
                if (dot(dxy, dxy) <= radius * radius)
                    tCap = tHit;
            }
        }
    }

    float tMin = min(tSide, tCap);
    if (tMin == MAXT) return NOHIT; //Hit(ray, MAXT, vec3(1,1,1)); //no intersection
    else if (tMin == tCap)
    {
        vec3 hitPos = ray.origin + tMin * ray.vector;
        vec3 pLocal = hitPos - center;
        vec2 xyRot = rotZ(angle) * pLocal.xy;
        vec2 uv = 1.25 * (xyRot / vec2(2.0 * radius)) + vec2(0.5);
        return Hit(ray, tMin, vec3(0, 0, 1), uv);
    }
    else //side wall hit
    {
        vec3 pHit = ray.origin + tMin * ray.vector;
        vec2 nxy  = (pHit.xy - center.xy) / radius;
        vec3 normal = vec3(nxy.x, nxy.y, 0.0);
        return Hit(ray, tMin, normal, vec2(-1));
    }
}

Hit cuboidHit(vec3 center, vec3 halfsize, in Ray ray)
{
    vec3 tMin = (center - halfsize - ray.origin) / ray.vector;
    vec3 tMax = (center + halfsize - ray.origin) / ray.vector;

    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);

    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);

    if (tNear > tFar || tFar < 0.0) return NOHIT;

    float tHit = (tNear > 0.0) ? tNear : tFar;
    vec3 hitPos = ray.origin + tHit * ray.vector;
    vec3 normal = vec3(1.0);
    vec2 uv = vec2(-1.0);

    if (abs(hitPos.x - (center.x - halfsize.x)) < 0.001) normal = vec3(-1, 0, 0);
    else if (abs(hitPos.x - (center.x + halfsize.x)) < 0.001) normal = vec3(1, 0, 0);
    else if (abs(hitPos.y - (center.y - halfsize.y)) < 0.001) normal = vec3(0, -1, 0);
    else if (abs(hitPos.y - (center.y + halfsize.y)) < 0.001) normal = vec3(0, 1, 0);
    else if (abs(hitPos.z - (center.z - halfsize.z)) < 0.001) normal = vec3(0, 0, -1);
    else if (abs(hitPos.z - (center.z + halfsize.z)) < 0.001)
    {
        normal = vec3(0, 0, 1);
        vec3 pLocal = hitPos - center;
        uv = 1.1 * (pLocal.xy / (2.0 * halfsize.xy)) + vec2(0.5);
    }

    return Hit(ray, tHit, normal, uv);
}

Hit myCylinderHit(vec3 center, float radius, float height, in Ray ray)
{
    vec2 tocenter = ray.origin.xy - center.xy;
    float a = dot(ray.vector.xy, ray.vector.xy);
    float b = dot(ray.vector.xy, tocenter);
    float c = dot(tocenter, tocenter) - radius * radius;

    float D = b * b - a * c;
    if (D < 0) return NOHIT;

    float t = (-b - sqrt(D)) / a;
    if (t <= 0) return NOHIT;
    if (abs(ray.origin.z + t * ray.vector.z - center.z) > 0.5 * height) return NOHIT;

    vec2 nxy = (tocenter + t * ray.vector.xy) / radius;
    return Hit(ray, t, vec3(nxy.x, nxy.y, 0.0), vec2(-1));
}
Hit capsuleHit(vec3 center, float radius, float height, float angle, in Ray ray)
{
    Hit argmin;
    argmin.t = MAXT;

    Hit cylhit = myCylinderHit(center, radius, height - 2.0 * radius, ray);
    if (cylhit.t < argmin.t) argmin = cylhit;

    float ztop = center.z + 0.5 * height - radius;
    Hit tophit = sphereHit(vec3(center.x, center.y, ztop), radius, angle, ray);
    if (tophit.t < argmin.t) argmin = tophit;

    float zbot = center.z - 0.5 * height + radius;
    Hit bothit = sphereHit(vec3(center.x, center.y, zbot), radius, angle, ray);
    if (bothit.t < argmin.t) argmin = bothit;

    return argmin;
}

Hit firstHit(in Ray ray, out int hitIndex, bool anyHit, int dontIgnore) //anyHit=true enables early exit
{
    Hit argmin = groundHit(-0.001, ray);
    hitIndex = -1;
    if (argmin.t < MAXT) hitIndex = GROUND_CODE;

    for (int i = 0; i < numObjects; i++)
    {
        int base = i * TEXELS_PER_OBJECT;
        vec4 p0 = texelFetch(data, ivec2(base + 0,0), 0); //size
        vec4 p1 = texelFetch(data, ivec2(base + 1,0), 0); //center
        vec3 size = p0.rgb;
        float shape = p0.a;
        vec3 center = p1.rgb;
        float angle = p1.a;

        Hit hit = NOHIT;
        if (shape == 0 && dontIgnore < 0) hit = rectHit(center, size.x, size.y, ray);
        else if (shape == 1 && (dontIgnore < 0 || dontIgnore == i)) hit = sphereHit(center, (size.x + size.y + size.z) / 6.0, angle, ray);
        else if (shape == 2) hit = cylinderHit(center, 0.25 * (size.x + size.y), size.z, angle, ray);
        else if (shape == 3) hit = cuboidHit(center, 0.5 * size, ray);
        else if (shape == 4 && (dontIgnore < 0 || dontIgnore == i)) hit = capsuleHit(center, 0.25 * (size.x + size.y), size.z, angle, ray);

        if (hit.t < argmin.t)
        {
            argmin = hit;
            hitIndex = i;
            if (anyHit) return argmin;
        }
    }
    return argmin;
}

bool HasLineOfSight(vec3 start, vec3 target, int targetIndex)
{
    vec3 delta = target - start;
    float distanceToTarget = length(delta);
    if (distanceToTarget <= 0.0001) return true;

    vec3 dir = delta / distanceToTarget;
    vec3 currentOrigin = start + 0.001 * dir;
    float remaining = distanceToTarget;

    for (int step = 0; step < 32; ++step)
    {
        int hitIndex = -1;
        Hit hit = firstHit(Ray(currentOrigin, dir), hitIndex, false, -1);
        if (hit.t >= MAXT) return true;

        if (hit.t > remaining) return true;
        if (hitIndex == targetIndex) return true;
        if (hitIndex < 0) return false;

        vec4 blockInfo = texelFetch(data, ivec2(hitIndex * TEXELS_PER_OBJECT + 3,0), 0);
        if (blockInfo.z > 0.5) return false;

        float advance = min(hit.t + 0.01, remaining);
        if (advance <= 0.0001) return true;
        remaining -= advance;
        if (remaining <= 0.001) return true;
        currentOrigin += advance * dir;
    }
    return true;
}

float sdGround(vec3 p) { return abs(p.z); }
float sdSphere(vec3 p, float radius) { return length(p) - radius; }
float sdCylinder(vec3 p, float radius, float height)
{
    float hh = 0.5 * height;
    vec2 d = vec2(length(p.xy) - radius, abs(p.z) - hh);
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}
float sdCuboid(vec3 p, vec3 b)
{
    vec3 q = abs(p) - b;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}
float sdCapsule(vec3 p, float halfheight, float r)
{
    p.z -= clamp(p.z, r - halfheight, halfheight - r);
    return length(p) - r;
}

float field(vec3 p)
{
    float result = MAXT;
    for (int i = 0; i < numObjects; i++)
    {
        int base = i * TEXELS_PER_OBJECT;
        vec4 p0 = texelFetch(data, ivec2(base + 0,0), 0); //size
        vec4 p1 = texelFetch(data, ivec2(base + 1,0), 0); //center
        float shape = p0.a;
        vec3 size = p0.rgb;
        vec3 v = p - p1.rgb;
        
        float value = 10.0;
        if (shape == 0) value = sdGround(v);
        else if (shape == 1) value = sdSphere(v, 0.5 * (size.x + size.y + size.z) / 3.0);
        else if (shape == 2) value = sdCylinder(v, 0.25 * (size.x + size.y), size.z);
        else if (shape == 3) value = sdCuboid(v, 0.5 * size);
        else if (shape == 4) value = sdCapsule(v, 0.5 * size.z, 0.25 * (size.x + size.y));

        if (value < result) result = value;
    }
    return result;
}

float occlusion(in vec3 pos, in vec3 nor) //needs comments on the range of values
{
    const float height = 0.08, bound = 0.5;
    float linear = (1.0 - bound) * field(pos + height * nor) / height + bound;
    return clamp(linear, bound, 1.0);
}

vec3 pushToGreen(vec3 color, float n)
{
    float f = 1.0 - exp2(-n);
    return mix(color, vec3(0.0, 1.0, 0.0), f);
}

vec3 checkerboardFloor(vec3 position)
{
    vec2 floorSize = texelFetch(data, ivec2(0,0), 0).xy;
    vec2 floorCenter = texelFetch(data, ivec2(1,0), 0).xy;
    vec3 baseColor = texelFetch(data, ivec2(2,0), 0).rgb;

    vec2 tileSize = vec2(FLOOR_TILE_SIZE);
    vec2 remainder = mod(floorSize, tileSize);
    vec2 minCorner = floorCenter - 0.5 * floorSize;
    vec2 start = minCorner + 0.5 * remainder;
    vec2 local = position.xy - start;
    vec2 gridCoord = floor(local / tileSize);
    float parity = mod(gridCoord.x + gridCoord.y, 2.0);
    if (parity < 0.0) parity += 2.0;
    float checker = parity < 1.0 ? 0.0 : 1.0;

    vec3 lighter = clamp(baseColor * (1.0 + FLOOR_COLOR_VARIATION), 0.0, 1.0);
    vec3 darker = clamp(baseColor * (1.0 - FLOOR_COLOR_VARIATION), 0.0, 1.0);
    return mix(lighter, darker, checker);
}

vec3 render(in Ray ray)
{
    float sunIntensity = 0.5;
    float skyIntensity = 1.0 - sunIntensity;

    int hitIndex;
    Hit hit = firstHit(ray, hitIndex, false, -1);

    if (hit.t < MAXT)
    {
        float cosinus = max(dot(hit.normal, toSun), 0.0);
        vec3 p = ray.origin + hit.t * ray.vector; //в этой точке мы захитили

        int shadowIndex = -1;
        if (firstHit(Ray(p + 0.001 * hit.normal, toSun), shadowIndex, true, -1).t < MAXT) cosinus *= 0.125; //*= 0.0 for complete shadow

        vec3 albedo = vec3(1,0,1);
        if (hitIndex == GROUND_CODE) albedo = groundColor;
        else if (hitIndex == 0) albedo = checkerboardFloor(p);
        else
        {
            int baseIndex = hitIndex * TEXELS_PER_OBJECT;
            vec4 p1 = texelFetch(data, ivec2(baseIndex + 1,0), 0); //center
            vec4 p2 = texelFetch(data, ivec2(baseIndex + 2,0), 0); //color

            vec3 center = p1.rgb;
            vec3 color = p2.rgb;

            vec2 uv = hit.uv;
            float textureIndex = p2.a;
            uv.x = (uv.x + textureIndex) / numTextures; //тут может быть деление на 0, аккуратно

            if (textureIndex >= 0 && uv.x >= textureIndex / numTextures && uv.x <= (textureIndex + 1.0) / numTextures && uv.y >= 0 && uv.y <= 1)
            {
                vec4 tex = texture(emojiTex, uv);

                const float tintStrength = 0.1;
                vec3 texFinal = mix(tex.rgb, color * tex.rgb, tintStrength);
                albedo = mix(color, texFinal, tex.a);
            }
            else albedo = color;
        }        
        vec3 result = (cosinus * sunIntensity + occlusion(p, hit.normal) * skyIntensity) * albedo;
        if (DRAW_CONES == 0 || hitIndex == GROUND_CODE || hitIndex > 0) return result;

        float sum = 0;
        for (int i = 0; i < numObjects; i++)
        {
            if (i == hitIndex) continue;
            int base = i * TEXELS_PER_OBJECT;
            vec4 p0 = texelFetch(data, ivec2(base + 0,0), 0); //size
            vec4 p1 = texelFetch(data, ivec2(base + 1,0), 0); //center and angle
            vec4 p3 = texelFetch(data, ivec2(base + 3,0), 0); //sensory params

            vec3 center = p1.rgb;
            float dist = distance(center, p);
            float angle = p1.a;
            float visionCos = p3.x;
            float hearingRad = p3.y;

            bool heard = hearingRad > 0.0 && dist <= hearingRad;
            bool seen = false;
            if (visionCos < 1.0)
            {
                vec2 v = p.xy - center.xy;
                float lenV = length(v);
                bool withinCone = lenV <= 0.0001;
                if (!withinCone)
                {
                    vec2 dir = vec2(cos(angle), sin(angle));
                    float cosa = dot(dir, v / lenV);
                    withinCone = cosa >= visionCos;
                }
                if (withinCone && (lenV <= 0.0001 || HasLineOfSight(p, center, i))) seen = true;
            }
            if (heard || seen) sum += exp(-dist * dist * 0.1);
        }
        return pushToGreen(result, 0.15 * sum);
    }
    else return vec3(1, 1, 1);
}

void main()
{
    //vec2 uv = (-resolution.xy + 2.0 * gl_FragCoord.xy) / resolution.y;
    vec2 uv = gl_FragCoord.xy / resolution.y;
    vec3 cameraVector = normalize(cameraCorner + uv.x * cameraHorizontal + uv.y * cameraVertical - cameraPosition);
    vec3 color = render(Ray(cameraPosition, cameraVector));
    color = pow(color, vec3(0.7));
    finalColor = vec4(color, 1.0);
}
