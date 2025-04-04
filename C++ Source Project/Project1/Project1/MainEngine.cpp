#include <iostream>
#include <vector>
#include <cmath>  // For sqrt()
#include <cstring>  // For memset
#include <algorithm> // For std::fill
#include <execution> // For parallel fill (C++17+)
#include <thread>
#include <xmmintrin.h>   // SSE intrinsics
#include <immintrin.h>
#include <atomic>


__declspec(dllexport) void Test()
{
	std::cout << "Raster reporting for duty!" << std::endl;
}

__declspec(dllexport) int AddTest(int numb)
{

	return numb + 2;
}



//STRUCTS AND MATH


struct Vector4Int {
	int x, y, z, w;


};

struct Vector2 {
	float x, y;
};

struct Vector4 {
	float x, y, z, w;

};

struct Vector3 {
	float x, y, z;
};

struct Color32  // Define COLOR
{
	uint8_t r, g, b, a;

};

struct BgTrans
{
	Vector3 position;
	Vector4 scale;
	Vector4 rotation;
	int meshindex;
	Color32 tint;

};





struct Color  // Define COLOR
{
	float r, g, b, a;
	/*	Color() = default;

		constexpr Color(float r, float g, float b, float a) noexcept : r(r), g(g), b(b), a(a) {}

		constexpr inline Color operator+(const Color& other) const noexcept {
			return Color(r + other.r, g + other.g, b + other.b, a + other.a);
		}

		constexpr inline Color operator-(const Color& other) const noexcept {
			return Color(r - other.r, g - other.g, b - other.b, a - other.a);
		}

		constexpr inline Color operator*(const Color& other) const noexcept {
			return Color(r * other.r, g * other.g, b * other.b, a * other.a);
		}*/
};



struct PointLight
{
	Color color;
	Vector3 position;
	float Range;


};


float repeat(float& value) {
	return value - std::floorf(value);

}


inline float Distance(const Vector3& a, const Vector3& b) {
	float dx = a.x - b.x;
	float dy = a.y - b.y;
	float dz = a.z - b.z;
	return std::sqrt(dx * dx + dy * dy + dz * dz);
}

inline float max(float value, float value2)
{
	if (value > value2)return value;
	else return value2;
}

inline float min(float value, float value2)
{
	if (value < value2)return value;
	else return value2;
}

inline float fast_min(float a, float b) {
	return _mm_cvtss_f32(_mm_min_ss(_mm_set_ss(a), _mm_set_ss(b)));
}

inline float fast_max(float a, float b) {
	return _mm_cvtss_f32(_mm_max_ss(_mm_set_ss(a), _mm_set_ss(b)));
}

int Width;
int Height;

Vector3 cameraForward;
Vector3 cameraPosition;
Vector3 cameraUp;
Vector3 cameraRight;

float aspectRatio;
float tanFovHalf;




inline Vector3 Normalize(Vector3 v) {
	float length = std::sqrt(v.x * v.x + v.y * v.y + v.z * v.z);

	if (length == 0.0f) {
		v.x = v.y = v.z = 0.0f;  // Set to zero vector if input is zero
		return v;
	}

	float invLength = 1.0f / length; // Avoid division in three places
	v.x *= invLength;
	v.y *= invLength;
	v.z *= invLength;

	return v;
}


inline Vector3 cross(const Vector3& a, const Vector3& b) {
	return {
		a.y * b.z - a.z * b.y,
		a.z * b.x - a.x * b.z,
		a.x * b.y - a.y * b.x
	};
}

inline Vector3 generateNormal(const Vector3& p1, const Vector3& p2, const Vector3& p3) {
	// Compute edge vectors directly
	float ux = p2.x - p1.x, uy = p2.y - p1.y, uz = p2.z - p1.z;
	float vx = p3.x - p1.x, vy = p3.y - p1.y, vz = p3.z - p1.z;

	// Compute cross product
	float nx = uy * vz - uz * vy;
	float ny = uz * vx - ux * vz;
	float nz = ux * vy - uy * vx;

	// Compute squared length (avoiding sqrt if already zero)
	float lengthSq = nx * nx + ny * ny + nz * nz;
	if (lengthSq == 0.0f) return { 0.0f, 0.0f, 0.0f }; // Handle degenerate triangle

	// Normalize using reciprocal square root for performance
	float invLength = 1.0f / std::sqrt(lengthSq);
	return { nx * invLength, ny * invLength, nz * invLength };
}

inline Vector3 rotateq(const Vector3& v, const Vector4& q) {
	// Extract quaternion vector part
	float qx = q.x, qy = q.y, qz = q.z, qw = q.w;

	// Compute t = 2 * cross(q.xyz, v) directly
	float tx = 2 * (qy * v.z - qz * v.y);
	float ty = 2 * (qz * v.x - qx * v.z);
	float tz = 2 * (qx * v.y - qy * v.x);

	// Compute cross(q.xyz, t) directly
	float cx = qy * tz - qz * ty;
	float cy = qz * tx - qx * tz;
	float cz = qx * ty - qy * tx;

	// Compute final rotated vector
	return {
		v.x + qw * tx + cx,
		v.y + qw * ty + cy,
		v.z + qw * tz + cz
	};
}



// Compute the cross product of two 2D vectors (scalar result)
inline float CrossProduct(const Vector2& v0, const Vector2& v1) {
	return v0.x * v1.y - v0.y * v1.x;
}




// Perspective-correct barycentric interpolation
inline Vector3 BGInterpolate(const Vector2& a, const Vector2& b, const Vector2& c, const Vector2& p,
	float depthA, float depthB, float depthC) {
	// Precompute edge vectors
	float v0x = b.x - a.x, v0y = b.y - a.y;
	float v1x = c.x - a.x, v1y = c.y - a.y;
	float vpx = p.x - a.x, vpy = p.y - a.y;

	// Compute triangle area once (avoiding function calls)
	float invArea = 1.0f / (v0x * v1y - v0y * v1x);

	// Compute raw barycentric coordinates
	float v = std::abs((vpx * v1y - vpy * v1x) * invArea);
	float w = std::abs((vpx * v0y - vpy * v0x) * invArea);
	float u = 1.0f - v - w;

	// Perspective correction (avoid divisions by multiplying by inverse depths)
	u *= 1.0f / depthA;
	v *= 1.0f / depthB;
	w *= 1.0f / depthC;

	// Normalize weights in one step
	float invSum = 1.0f / (u + v + w);
	return { u * invSum, v * invSum, w * invSum };
}


inline Vector3 BGInterpolateUnnorm(const Vector2& a, const Vector2& b, const Vector2& c, const Vector2& p,
	float depthA, float depthB, float depthC) {
	// Precompute edge vectors.
	float v0x = b.x - a.x;
	float v0y = b.y - a.y;
	float v1x = c.x - a.x;
	float v1y = c.y - a.y;
	float vpx = p.x - a.x;
	float vpy = p.y - a.y;

	// Compute triangle area once.
	float invArea = 1.0f / (v0x * v1y - v0y * v1x);

	// Compute raw barycentrics.
	float v = std::abs((vpx * v1y - vpy * v1x) * invArea);
	float w = std::abs((vpx * v0y - vpy * v0x) * invArea);
	float u = 1.0f - v - w;

	// Multiply each by the inverse depth (but do not normalize).
	u *= 1.0f / depthA;
	v *= 1.0f / depthB;
	w *= 1.0f / depthC;

	Vector3 result;
	result.x = u;
	result.y = v;
	result.z = w;
	return result;
}






//BUFFERS
int rowcount;

int** Tris;
Vector3** Verts;
Vector3** Norms;

Vector3* Positions;
int* TrisCounts;
int* VertCounts;
int* MeshIndexes;

//TEXTURES
Color32* RenderTexture;
Color32* ScreenNorms;

float* Depth;


//TEXTURES

std::vector<std::vector<Color32>> Textures;

//std::vector<PointLight> PointLights;

struct BgMesh
{
	std::vector<Vector3> Verts;
	std::vector<Vector3> Norms;
	std::vector<Vector3> Faces;
	std::vector<Color32> TrisCols;
	std::vector<Vector2> uv;
	std::vector<int> Tris;
	float Spec;
	float Met;
	bool UseAlpha;

	int groupmeshcount;
	int groupedmeshes[20];
	int textureindex;
	int twidth;
	int theight;
	bool staticsingle;
	bool baked;
};
std::vector<BgMesh> AllMeshes;

// Ensure the function names are not mangled by the C++ compiler
extern "C"
{


	__declspec(dllexport) void DestroyBuffers(int count)
	{

		Textures.clear();
		AllMeshes.clear();

		/*	delete[] Tris;
			delete[] Norms;
			delete[] Verts;*/

			//		Textures.clear;

					/*	delete[] Positions;
						delete[] TrisCounts;
						delete[] VertCounts;
						delete[] MeshIndexes;*/

						//	delete[] meshes;

		rowcount = 0;
	}


	__declspec(dllexport) void AddMesh(Vector3* verts, Vector3* norms, Vector3* faces, Vector2* uvs, Color32* triscols, int* tris, int vcount, int tcount,
		int texindex, int texwidth, int texheight, int groupcount, int* group, float spec, float met, bool isstatic, bool usealpha)
	{

		BgMesh nm;
		nm.Verts.assign(verts, verts + vcount);
		nm.Norms.assign(norms, norms + vcount);
		nm.uv.assign(uvs, uvs + vcount);
		nm.Tris.assign(tris, tris + tcount);
		nm.Faces.assign(faces, faces + (tcount / 3));
		nm.TrisCols.assign(triscols, triscols + (tcount / 3));
		nm.textureindex = texindex;
		nm.twidth = texwidth;
		nm.theight = texheight;
		nm.groupmeshcount = groupcount;
		nm.Spec = spec;
		nm.staticsingle = isstatic;
		nm.baked = false;
		nm.UseAlpha = usealpha;
		//met = round(met * 4) * 0.25;

		nm.Met = round(met * 4) * 0.25;

		for (int i = 0; i < 20; i++)
		{
			nm.groupedmeshes[i] = -1;
		}


		if (groupcount > 1)
		{
			for (int i = 0; i < min(20, groupcount - 1); i++)
			{
				nm.groupedmeshes[i] = group[i];
			}

		}


		AllMeshes.push_back(nm);

		//nm.Verts = 

		//Textures.push_back(Text);

	}

	__declspec(dllexport) void UpdateMeshProps(int meshindex, int texindex, int texwidth, int texheight, float spec, float met)
	{
		AllMeshes[meshindex].textureindex = texindex;
		AllMeshes[meshindex].twidth = texwidth;
		AllMeshes[meshindex].theight = texheight;
		AllMeshes[meshindex].Spec = spec;
		AllMeshes[meshindex].Met = met;

	}




	__declspec(dllexport) void AddTexture(Color32* texture, int count)
	{
		std::vector<Color32> Text(texture, texture + count);
		Textures.push_back(Text);

	}

	//__declspec(dllexport) void InitializeBuffers(int MeshCount, int* meshIndexes, Color32* rendertex, float* depth, int* trisCounts, int* vertCounts, Vector3* positions, int width, int height)
	__declspec(dllexport) void InitializeBuffers(Color32* rendertex, Color32* normsScreen, float* depth, int width, int height)
	{

		RenderTexture = rendertex;
		ScreenNorms = normsScreen;
		Depth = depth;

		Width = width;
		Height = height;

		//	std::vector<std::thread> threads;
		Textures.clear();
		AllMeshes.clear();
	}


	__declspec(dllexport) void SetMeshData(int row, Vector3* verts, Vector3* norms, int* tris)
	{
		Verts[row] = verts;
		Norms[row] = norms;
		Tris[row] = tris;

		rowcount += 1;
	}


	float Dot(const Vector3& a, const Vector3& b) {
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	__declspec(dllexport) void SetCameraSettings(int width, int height, Vector3 cameraforward, Vector3 cameraposition,
		Vector3 cameraup, Vector3 cameraright, float aspectratio, float tanFovhalf)
	{
		Width = width;
		Height = height;
		cameraForward = cameraforward;
		cameraPosition = cameraposition;
		cameraUp = cameraup;
		cameraRight = cameraright;
		aspectRatio = aspectratio;
		tanFovHalf = tanFovhalf;
	}


	Vector3 localPoint;

	inline void WorldToScreenPoint(const Vector3& worldPoint, Vector3& VOut)
	{
		//Vector3 toPoint;
		VOut.x = worldPoint.x - cameraPosition.x;
		VOut.y = worldPoint.y - cameraPosition.y;
		VOut.z = worldPoint.z - cameraPosition.z;

		// Use a local variable for the intermediate "local" space
		Vector3 local;
		local.x = Dot(VOut, cameraRight);
		local.y = Dot(VOut, cameraUp);
		local.z = Dot(VOut, cameraForward);

		VOut = local;

		// Apply perspective projection using the local variable.
		Vector2 projectedPoint;
		float val = (VOut.z * tanFovHalf * aspectRatio);

		projectedPoint.x = VOut.x / val;

		val = (VOut.z * tanFovHalf);

		projectedPoint.y = VOut.y / val;

		// Map to screen space.
		//Vector3 screenpoint;
		VOut.x = (projectedPoint.x + 1) * 0.5f * (float)Width;
		VOut.y = ((projectedPoint.y + 1) * 0.5f) * (float)Height;
		//VOut.z = local.z;  // You can return the depth via z.

		//VOut = screenpoint;
	}

	void ExtractVector3(__m128 mult, Vector3& lnorm) {
		lnorm.x = _mm_cvtss_f32(mult);                     // Extract first float (index 0)
		lnorm.y = _mm_cvtss_f32(_mm_shuffle_ps(mult, mult, _MM_SHUFFLE(1, 1, 1, 1))); // Extract second float (index 1)
		lnorm.z = _mm_cvtss_f32(_mm_shuffle_ps(mult, mult, _MM_SHUFFLE(2, 2, 2, 2))); // Extract third float (index 2)
	}



	inline Vector3 MulVec(const Vector3& vec, float scalar) {
		return { vec.x * scalar, vec.y * scalar, vec.z * scalar };
	}

	inline Vector3 MulVecWhole(const Vector3& vec, const Vector3& vec2) {
		return { vec.x * vec2.x, vec.y * vec2.y, vec.z * vec2.z };
	}

	inline Color32 MulColScalar(const Color32& vec, float scalar) {


		return { (uint8_t)min(255, (float)vec.r * scalar), (uint8_t)min(255, (float)vec.g * scalar), (uint8_t)min(255, (float)vec.b * scalar), vec.a };
	}

	Color32 colmultiply(const Color32 &c1, const Color32 &c2) {
		Color32 result;
		result.r = static_cast<uint8_t>((c1.r * c2.r) / 255);
		result.g = static_cast<uint8_t>((c1.g * c2.g) / 255);
		result.b = static_cast<uint8_t>((c1.b * c2.b) / 255);
		result.a = static_cast<uint8_t>((c1.a * c2.a) / 255);
		return result;
	}

	inline Color32 MulColor(const Color32& col1, const Color32& col2) {


		float r = (float)col1.r * 0.0039215;
		float g = (float)col1.g * 0.0039215;
		float b = (float)col1.b * 0.0039215;

		float r2 = (float)col2.r * 0.0039215;
		float g2 = (float)col2.g * 0.0039215;
		float b2 = (float)col2.b * 0.0039215;

		return { (uint8_t)min(255, r * r2 * 255), (uint8_t)min(255, g * g2 * 255), (uint8_t)min(255, b * b2 * 255), col1.a };
	}

	inline Vector3 AddVec(const Vector3& v1, const Vector3& v2)
	{
		Vector3 nv;
		nv = v1;
		nv.x += v2.x;
		nv.y += v2.y;
		nv.z += v2.z;

		return nv;
	}


	inline float lerp(float a, float b, float t) {
		return a + t * (b - a);
	}


	inline Vector3 SubVec(Vector3& v1, Vector3& v2)
	{
		Vector3 nv;

		nv = v1;
		nv.x -= v2.x;
		nv.y -= v2.y;
		nv.z -= v2.z;
		return nv;
	}

	Vector3 ScreenToDir(int index, float linearDepth)
	{
		int pixelX = index % Width;
		int pixelY = index / Width;

		Vector2 screenPixelPos;

		screenPixelPos.x = pixelX;
		screenPixelPos.y = pixelY;

		// Convert screen pixel positions to NDC (Normalized Device Coordinates) [-1, 1]
		float ndcX = (screenPixelPos.x / (float)Width) * 2.0f - 1.0f;
		float ndcY = (screenPixelPos.y / (float)Height) * 2.0f - 1.0f;

		// Adjust for aspect ratio and FOV
		Vector3 rayDir;
		rayDir.x = ndcX * aspectRatio * tanFovHalf;
		rayDir.y = ndcY * tanFovHalf;
		rayDir.z = 1.0f; // Forward direction in NDC space

		// Normalize the ray direction since it's not a unit vector
		rayDir = Normalize(rayDir);

		// Transform the ray direction from camera space to world space:
		return AddVec(
			AddVec(MulVec(cameraRight, rayDir.x),
				MulVec(cameraUp, rayDir.y)),
			MulVec(cameraForward, rayDir.z)
		);

	}


	__declspec(dllexport) void Clear(int numThreads, Color32 clearColor, Color32 colhorizon)
	{
		// Create a zero color for ScreenNorms.
		Color32 zeroColor = { 0, 0, 0, 0 };

		// Depth clear value.
		const float depthClear = 100000000.0f;

		// Total number of pixels.
		int totalPixels = Width * Height;

		int res = totalPixels;

		// Prepack the clear values into SIMD registers.
		// For RenderTexture and ScreenNorms, we reinterpret the Color32 as a 32-bit integer.
		__m128i clearColorSIMD = _mm_set1_epi32(*reinterpret_cast<int*>(&clearColor));
		__m128i zeroColorSIMD = _mm_set1_epi32(*reinterpret_cast<int*>(&zeroColor));

		// For Depth, broadcast the depthClear float.
		__m128 depthSIMD = _mm_set1_ps(depthClear);

		// Process in blocks of 4 pixels.
		int simdCount = totalPixels & ~3; // largest multiple of 4 less than or equal to totalPixels
		int i = 0;
		for (; i < simdCount; i += 4)
		{
			// Store four copies of zeroColor into ScreenNorms.
			_mm_storeu_si128(reinterpret_cast<__m128i*>(&ScreenNorms[i]), zeroColorSIMD);

			// Store four copies of depthClear into Depth.
			_mm_storeu_ps(&Depth[i], depthSIMD);
		}

		// Process any remaining pixels (if totalPixels isn't divisible by 4)
		for (; i < totalPixels; i++)
		{
			//RenderTexture[i] = clearColor;
			ScreenNorms[i] = zeroColor;
			Depth[i] = depthClear;
		}



		//SKY GRADIENT
		auto process = [&](int start, int end)
		{


			Vector3 col1;
			Vector3 col2;
			Vector3 colsum;
			Color32 collerp;
			Vector3 dir;
			float lrp;

			col1.x = (float)clearColor.r * 0.00390625;
			col1.y = (float)clearColor.g * 0.00390625;
			col1.z = (float)clearColor.b * 0.00390625;

			col2.x = (float)colhorizon.r * 0.00390625;
			col2.y = (float)colhorizon.g * 0.00390625;
			col2.z = (float)colhorizon.b * 0.00390625;

			for (int y = start; y < end; y++)
			{
				uint32_t curPixel = y * Width;
				dir = ScreenToDir(curPixel, 1);
				lrp = pow(min(1, max(0, dir.y * 1.5)), 0.5);

				colsum.x = lerp(col2.x, col1.x, lrp);
				colsum.y = lerp(col2.y, col1.y, lrp);
				colsum.z = lerp(col2.z, col1.z, lrp);

				collerp.r = colsum.x * 256;
				collerp.g = colsum.y * 256;
				collerp.b = colsum.z * 256;

				__m256i skycol = _mm256_set1_epi32(*reinterpret_cast<int*>(&collerp));

				int x = 0;
				int wcount = Width & ~7;

				for (; x < wcount; x += 8)
				{
					_mm256_storeu_si256(reinterpret_cast<__m256i*>(&RenderTexture[curPixel]), skycol);

					curPixel += 8;
				}
			}



		};


		if (numThreads <= 1) {
			process(0, Height);


		}
		else {
			// Set up threading containers.
			std::vector<std::thread> threads;
			std::atomic<int> nextPixel(0);  // Atomic counter for dynamic work distribution

			// Thread function using dynamic scheduling
			auto threadWorker = [&]() {
				int pixelIndex;
				while ((pixelIndex = nextPixel.fetch_add(64)) < Height) {
					int end = std::min(pixelIndex + 64, Height);
					process(pixelIndex, end);
				}
			};

			// Launch threads
			for (int i = 0; i < numThreads; i++) {
				threads.emplace_back(threadWorker);
			}

			// Wait for all threads to finish
			for (auto &t : threads) {
				t.join();
			}
		}



	}

	

	// Precomputed edge function in linear form:
// Given two vertices v0 and v1, compute coefficients such that for any pixel (x,y):
//   E(x,y) = A * x + B * y + C
// where:
	inline void ComputeEdgeCoeffs(const Vector3 &v0, const Vector3 &v1, float &A, float &B, float &C) {
		A = v1.y - v0.y;           // derivative with respect to x
		B = -(v1.x - v0.x);        // derivative with respect to y
		C = (v1.x - v0.x) * v0.y - (v1.y - v0.y) * v0.x;
	}

	inline float edge(const Vector3& a, const Vector3& b, float x, float y) {
		return (x - a.x) * (b.y - a.y) - (y - a.y) * (b.x - a.x);
	}

	inline __m256 edge_avx(const Vector3& a, const Vector3& b, __m256 x, __m256 y) {
		__m256 a_x = _mm256_set1_ps(a.x);
		__m256 a_y = _mm256_set1_ps(a.y);
		__m256 b_x = _mm256_set1_ps(b.x);
		__m256 b_y = _mm256_set1_ps(b.y);
		__m256 term1 = _mm256_mul_ps(_mm256_sub_ps(x, a_x), _mm256_set1_ps(b.y - a.y));
		__m256 term2 = _mm256_mul_ps(_mm256_sub_ps(y, a_y), _mm256_set1_ps(b.x - a.x));
		return _mm256_sub_ps(term1, term2);
	}





	Vector3 Unproject(int index, float depth) {
		// Convert 1D index to 2D screen coordinates (pixelX, pixelY)
		int pixelX = index % Width;
		int pixelY = index / Width;

		// Convert pixel coordinates to normalized device coordinates (NDC)
		// NDC x ranges from -1 (left) to +1 (right)
		// NDC y ranges from +1 (top) to -1 (bottom)
		float ndcX = (2.0f * pixelX) / Width - 1.0f;
		float ndcY = 1.0f - (2.0f * pixelY) / Height; // flip y-axis

		// Compute the offset in x and y using the precomputed tanFovHalf and aspectRatio
		float offsetX = ndcX * tanFovHalf * aspectRatio;
		float offsetY = ndcY * tanFovHalf;

		// Calculate the contributions from the camera's right and up vectors.
		Vector3 rightComponent = MulVec(cameraRight, offsetX);
		Vector3 upComponent = MulVec(cameraUp, offsetY);

		// Combine the components with the camera's forward vector.
		Vector3 rayDir = AddVec(cameraForward, AddVec(rightComponent, upComponent));

		// Normalize the ray direction.
		rayDir = Normalize(rayDir);

		// Compute the world-space position along the ray at the given depth.
		float cosTheta = Dot(rayDir, cameraForward); // assuming Dot returns the dot product
		Vector3 scaledRay = MulVec(rayDir, depth / cosTheta);
		Vector3 worldPos = AddVec(cameraPosition, scaledRay);

		return worldPos;
	}


	//Vector3 screentoworld(float2 screenpixelpos, float linearDepth) {





	Vector3 ScreenToWorld(int index, float linearDepth)
	{
		int pixelX = index % Width;
		int pixelY = index / Width;

		Vector2 screenPixelPos;

		screenPixelPos.x = pixelX;
		screenPixelPos.y = pixelY;

		// Convert screen pixel positions to NDC (Normalized Device Coordinates) [-1, 1]
		float ndcX = (screenPixelPos.x / (float)Width) * 2.0f - 1.0f;
		float ndcY = (screenPixelPos.y / (float)Height) * 2.0f - 1.0f;

		// Adjust for aspect ratio and FOV
		Vector3 rayDir;
		rayDir.x = ndcX * aspectRatio * tanFovHalf;
		rayDir.y = ndcY * tanFovHalf;
		rayDir.z = 1.0f; // Forward direction in NDC space

		// Normalize the ray direction since it's not a unit vector
		rayDir = Normalize(rayDir);

		// Transform the ray direction from camera space to world space:
		// worldRayDir = cameraRight * rayDir.x + cameraUp * rayDir.y + cameraForward * rayDir.z
		Vector3 worldRayDir = AddVec(
			AddVec(MulVec(cameraRight, rayDir.x),
				MulVec(cameraUp, rayDir.y)),
			MulVec(cameraForward, rayDir.z)
		);

		// Compute the dot product of the world ray direction with the camera's forward vector
		float dotValue = Dot(worldRayDir, cameraForward);

		// Calculate the distance along the ray using the linear depth value
		float dst = linearDepth / dotValue;

		// Compute the world position: cameraPosition + worldRayDir * dst
		Vector3 worldPos = AddVec(cameraPosition, MulVec(worldRayDir, dst));

		return worldPos;
	}


	Vector3 Skycol;
	Vector3 HorizonCol;
	Vector3 GroundCol;

	inline Vector3 GetReflectionColor(Vector3& direction, float smooth)
	{
		smooth = (1 - smooth) + 0.001;
		float lrp = max(0, min(1, ((direction.y - 0.1) + (smooth * 0.5)) * (1 / smooth)));

		Vector3 skgr = Skycol;
		float sgl = max(0, direction.y);
		skgr.x = lerp(HorizonCol.x, skgr.x, sgl);
		skgr.y = lerp(HorizonCol.y, skgr.y, sgl);
		skgr.z = lerp(HorizonCol.z, skgr.z, sgl);


		Vector3 outcol = skgr;
		outcol.x = lerp(GroundCol.x, skgr.x, lrp);
		outcol.y = lerp(GroundCol.y, skgr.y, lrp);
		outcol.z = lerp(GroundCol.z, skgr.z, lrp);

		return outcol;
	}

	inline Vector3 reflect(const Vector3& forward, const Vector3& normal, float& rimout) {
		//rimout = Dot(forward, normal);
		Vector3 r;
		r.x = forward.x - 2.0f * rimout * normal.x;
		r.y = forward.y - 2.0f * rimout * normal.y;
		r.z = forward.z - 2.0f * rimout * normal.z;
		return r;
	}


	Vector3 sundir; Color suncol; float sunintense; Color ambient;
	Color groundambient; Color skycolor; Color horzcolor;
	PointLight* pointlights; int pointlightcount; int numThreads; Color fogcolor; float fogdist;

	__declspec(dllexport) void SetLighting(Vector3 Ssundir, Color Ssuncol, float Ssunintense, Color Sambient,
		Color Sgroundambient, Color Sskycolor, Color Shorzcolor, PointLight* Spointlights, int Spointlightcount, Color Sfogcolor, float Sfogdist)
	{
		sundir = Ssundir;
		suncol = Ssuncol;
		sunintense = Ssunintense;
		ambient = Sambient;
		groundambient = Sgroundambient;
		skycolor = Sskycolor;
		horzcolor = Shorzcolor;
		pointlights = Spointlights;
		pointlightcount = Spointlightcount;
		fogcolor = Sfogcolor;
		fogdist = Sfogdist;
	}


	__declspec(dllexport) void Differed(int numThreads/*Vector3 sundir, Color suncol, float sunintense, Color ambient,
		Color groundambient, Color skycolor, Color horzcolor, PointLight* pointlights, int pointlightcount, int numThreads, Color fogcolor, float fogdist*/)
	{
		Skycol.x = skycolor.r;
		Skycol.y = skycolor.g;
		Skycol.z = skycolor.b;

		HorizonCol.x = horzcolor.r;
		HorizonCol.y = horzcolor.g;
		HorizonCol.z = horzcolor.b;

		GroundCol.x = groundambient.r;
		GroundCol.y = groundambient.g;
		GroundCol.z = groundambient.b;

		int res = Width * Height;


		// Lambda to process a segment of objects.
		auto process = [&](int start, int end)
		{

			Vector3 fgc;
			fgc.x = (float)fogcolor.r;
			fgc.y = (float)fogcolor.g;
			fgc.z = (float)fogcolor.b;

			Color32 newcol;

			Vector3 ambcol;

			Vector3 finalcol;

			newcol.a = 255;
			Vector3 norm;
			Vector3 tempcol;


			Vector3 vsuncol;
			vsuncol.x = suncol.r;
			vsuncol.y = suncol.g;
			vsuncol.z = suncol.b;

			Vector3 vamb;
			vamb.x = ambient.r;
			vamb.y = ambient.g;
			vamb.z = ambient.b;

			Vector3 gvamb;
			gvamb.x = groundambient.r;
			gvamb.y = groundambient.g;
			gvamb.z = groundambient.b;


			Vector3 reflc;

			for (int i = start; i < end; i++)
			{

				if (ScreenNorms[i].a > 0)
				{
					norm.x = ((float)ScreenNorms[i].r * 0.0078431) - 1;
					norm.y = ((float)ScreenNorms[i].g * 0.0078431) - 1;
					norm.z = ((float)ScreenNorms[i].b * 0.0078431) - 1;

					norm = Normalize(norm);

					float fac = min(1, max(0, Dot(norm, sundir)));

					Color32 curcol = RenderTexture[i];


					tempcol.x = (float)curcol.r * 0.0039215;
					tempcol.y = (float)curcol.g * 0.0039215;
					tempcol.z = (float)curcol.b * 0.0039215;

					finalcol = MulVec(MulVecWhole(tempcol, vsuncol), (fac * sunintense));

					float amlerp = (norm.y + 1) * 0.5;

					ambcol.x = lerp(gvamb.x, vamb.x, amlerp);
					ambcol.y = lerp(gvamb.y, vamb.y, amlerp);
					ambcol.z = lerp(gvamb.z, vamb.z, amlerp);

					ambcol = MulVecWhole(tempcol, ambcol);
					//	ambcol = MulVecWhole(tempcol, vamb);


					finalcol.x = finalcol.x + ambcol.x;
					finalcol.y = finalcol.y + ambcol.y;
					finalcol.z = finalcol.z + ambcol.z;


					//DO POINT LIGHTS
					if (pointlightcount > 0)
					{
						//Vector3 wpos = Unproject(i, Depth[i]);
						Vector3 wpos = ScreenToWorld(i, Depth[i]);

						for (int p = 0; p < pointlightcount; p++)
						{
							Vector3 ppos = pointlights[p].position;
							float range = pointlights[p].Range;

							if (abs(ppos.x - wpos.x) > range) continue;
							if (abs(ppos.z - wpos.z) > range) continue;

							float dist = Distance(wpos, pointlights[p].position);

							if (dist > range)continue;

							float dfac = 1 - (dist / range);


							dfac *= max(0, Dot(norm, Normalize(SubVec(pointlights[p].position, wpos))));

							if (dfac > 0.001) {
								ambcol.x = pointlights[p].color.r;
								ambcol.y = pointlights[p].color.g;
								ambcol.z = pointlights[p].color.b;

								ambcol = MulVecWhole(tempcol, ambcol);

								finalcol.x = finalcol.x + ambcol.x * dfac;
								finalcol.y = finalcol.y + ambcol.y * dfac;
								finalcol.z = finalcol.z + ambcol.z * dfac;
							}
						}
					}




					//CALCULATE FOG
					float fgl = powf(fast_min(1, (float)Depth[i] / fogdist), 0.5);
					//fgl = 1;

					finalcol.x = lerp(finalcol.x, fgc.x, fgl);
					finalcol.y = lerp(finalcol.y, fgc.y, fgl);
					finalcol.z = lerp(finalcol.z, fgc.z, fgl);



					//REFLECTIONS
					if (curcol.a > 1)
						//if(1>2)
					{
						Vector3 dir = ScreenToDir(i, Depth[i]);


						float rimout = Dot(dir, norm);
						reflc = reflect(dir, norm, rimout);

						float met = (float)(curcol.a - 0.01) * 0.02;


						float smth = (met - (int)met);

						met = min(1, met * 0.25);

						Vector3 refcol = GetReflectionColor(reflc, smth);

						float weak = powf(smth, 4);
						float spc = powf(max(0, min(1, Dot(reflc, sundir))), max(1, 100 * weak));

					
						smth = min(1, smth + 0.3);

						dir.x = -dir.x;
						dir.y = -dir.y;
						dir.z = -dir.z;


						rimout = Dot(dir, norm);

						float rim = min(1,powf((1 - max(0, min(1, rimout)) + 0.1), 3) + 0.1);


						float mfac = max(0.01, min(0.98, rim));

						mfac = lerp(mfac, met, met);


						refcol.x *= mfac;
						refcol.y *= mfac;
						refcol.z *= mfac;

						refcol = AddVec(refcol, MulVec(vsuncol, (spc * sunintense/* * (1 - mfac)*/ * 1.5 * (powf(smth, 4) + 0.1))));



						refcol.x = lerp(refcol.x, refcol.x * tempcol.x*1.1, met);
						refcol.y = lerp(refcol.y, refcol.y * tempcol.y*1.1, met);
						refcol.z = lerp(refcol.z, refcol.z * tempcol.z*1.1, met);

						finalcol = MulVec(finalcol, 1 - mfac);
						finalcol = AddVec(finalcol, refcol);

				
					}

					RenderTexture[i].r = min(255, finalcol.x * 255);
					RenderTexture[i].g = min(255, finalcol.y * 255);
					RenderTexture[i].b = min(255, finalcol.z * 255);
				}

			}

		};

		if (numThreads <= 1) {
			process(0, res);
		}
		else {
			// Set up threading containers.
			std::vector<std::thread> threads;
			std::atomic<int> nextPixel(0);  // Atomic counter for dynamic work distribution

			// Thread function using dynamic scheduling
			int amper = ceil((float)res / (float)numThreads);
			amper = 256;
			auto threadWorker = [&]() {
				int pixelIndex;
				while ((pixelIndex = nextPixel.fetch_add(amper)) < res) {
					int end = std::min(pixelIndex + amper, res);
					process(pixelIndex, end);
				}
			};

			// Launch threads
			for (int i = 0; i < numThreads; i++) {
				threads.emplace_back(threadWorker);
			}

			// Wait for all threads to finish
			for (auto &t : threads) {
				t.join();
			}
		}


	}


	void simd_add(float* a, float* b, float* result, int size) {
		for (int i = 0; i < size; i += 4) {
			__m128 vecA = _mm_loadu_ps(&a[i]);   // Load 4 floats into a SIMD register
			__m128 vecB = _mm_loadu_ps(&b[i]);   // Load 4 more floats
			__m128 vecC = _mm_add_ps(vecA, vecB); // Add them together
			_mm_storeu_ps(&result[i], vecC);     // Store result back to memory
		}
	}





	/////////////////////////////////////////////////////////////

	//////////MAIN RENDER FUNCTION!!

	///////
	__declspec(dllexport) Vector4Int RenderObjectsPooled(
		BgTrans* Objects, int numThreads, int Count, bool NoTextures, bool UseSimd, bool vertexLight)
	{
		Vector4Int finalStats = { 0, 0, 0, 0 };


		// Lambda to compute the 2D edge function value.
		auto edge = [](const Vector3& a, const Vector3& b, float x, float y) -> float {
			return (x - a.x) * (b.y - a.y) - (y - a.y) * (b.x - a.x);
		};



		// Lambda to process a segment of objects.
		auto processObjects = [&](int start, int end, Vector4Int &stats)
		{
			Vector3 vsuncol;
			vsuncol.x = suncol.r;
			vsuncol.y = suncol.g;
			vsuncol.z = suncol.b;

			Vector3 gvamb;
			gvamb.x = groundambient.r;
			gvamb.y = groundambient.g;
			gvamb.z = groundambient.b;

			Vector3 vamb;
			vamb.x = ambient.r;
			vamb.y = ambient.g;
			vamb.z = ambient.b;

			Vector3 fgc;
			fgc.x = (float)fogcolor.r * 255;
			fgc.y = (float)fogcolor.g * 255;
			fgc.z = (float)fogcolor.b * 255;

			int res = Width * Height;

			// Initialize local stats.
			stats.x = stats.y = stats.z = stats.w = 0;

			for (int o = start; o < end; o++) {

				if (Objects[o].meshindex < 0)continue;



				int mymeshindex = Objects[o].meshindex;
				int gcount = AllMeshes[mymeshindex].groupmeshcount;

				int mindexoriginal = mymeshindex;
				//gcount = 1;
				for (int g = 0; g < gcount; g++)
				{

					if (g > 0) {
						if (AllMeshes[mindexoriginal].groupedmeshes[g - 1] > -1) {
							mymeshindex = AllMeshes[mindexoriginal].groupedmeshes[g - 1];
						}

					}


					Vector3 obpos = Objects[o].position;
					Vector4 obscale = Objects[o].scale;
					Vector3 objDir = Normalize(SubVec(cameraPosition, obpos));



					//obpos.x += 2;

					// Object-level backface culling.
					if (Dot(objDir, cameraForward) < 0 || Distance(cameraPosition, obpos) < max(obscale.z, max(obscale.x, obscale.y)) * obscale.w) {

						int mytrisCount = 0;
						int myvertCount = 0;

						int mytexindex = 0;

						mytexindex = AllMeshes[mymeshindex].textureindex;

						float	Texw = AllMeshes[mymeshindex].twidth;
						float	Texh = AllMeshes[mymeshindex].theight-1;

						__m256 one = _mm256_set1_ps(1.0f);
						__m256 scale = _mm256_set1_ps(127.5f); // (n+1)*0.5*255
						__m256 texwVec = _mm256_set1_ps((float)Texw);
						__m256 texhVec = _mm256_set1_ps((float)Texh);


						// Lane indices [0,1,...,7] for per-lane offsets.
						__m256 indexVec = _mm256_setr_ps(0.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f);

						int texsize = (Texw * Texh) - 1;

						Vector4 rotation = Objects[o].rotation;

						Color32 Tint = Objects[o].tint;
					
						bool notex = false;



						//mymeshindex = Objects[o].meshindex;
						mytexindex = AllMeshes[mymeshindex].textureindex;

						mytrisCount = AllMeshes[mymeshindex].Tris.size();
						myvertCount = AllMeshes[mymeshindex].Verts.size();

						//ASSIGN LOCAL MESH PROPERTIES
						std::vector<int>& trisLoc = AllMeshes[mymeshindex].Tris;
						std::vector<Vector3>& vertLoc = AllMeshes[mymeshindex].Verts;
						std::vector<Vector3>& normsLoc = AllMeshes[mymeshindex].Norms;
						std::vector<Vector3>& facenorms = AllMeshes[mymeshindex].Faces;
						std::vector<Vector2>& uvs = AllMeshes[mymeshindex].uv;
						std::vector<Color32>& vertcols = AllMeshes[mymeshindex].TrisCols;
						//ASSIGN LOCAL TEXTURE
						if (mytexindex <= 0) {
							notex = true;
							mytexindex = 0;
						}
						std::vector<Color32>& Texture = Textures[mytexindex];



						Vector2 stsv1;
						Vector2 stsv2;
						Vector2 stsv3;
						Color32 tzero = Texture[0];

						uint8_t spec = AllMeshes[mymeshindex].Spec * 50;
						uint8_t met = AllMeshes[mymeshindex].Met * 200;

						bool usealpha = AllMeshes[mymeshindex].UseAlpha;

						spec += met;
						Tint.a = spec;

						bool statc = AllMeshes[mymeshindex].staticsingle;

						//statc = false;
						//BAKE STATIC MESH
						if (statc) {

							if (!AllMeshes[mymeshindex].baked)
							{
								AllMeshes[mymeshindex].baked = true;

								//Objects[o].tint = { 0,0,0,255};

								for (int i = 0; i < myvertCount; i++)
								{
									Vector3 v1 = (vertLoc[i]);
									v1.x *= obscale.x;
									v1.y *= obscale.y;
									v1.z *= obscale.z;
									v1 = rotateq(v1, rotation);
									v1 = AddVec(v1, obpos);

									//BAKE INTO DATA
									vertLoc[i] = v1;

									normsLoc[i] = rotateq(normsLoc[i], rotation);
								}


								for (int i = 0; i < mytrisCount / 3; i++) {

									Vector3 norm = facenorms[i];
									norm = rotateq(norm, rotation);
									norm = { -norm.x, -norm.y, -norm.z };

									AllMeshes[mymeshindex].Faces[i] = norm;

								}
							}

						}

						// Process each triangle in the object.
						for (int i = 0; i < mytrisCount / 3; i++) {
							int curtris = i * 3;
							// Compute the world-space triangle vertices.
							Vector3 v1 = (vertLoc[trisLoc[curtris]]);

							if (!statc)
							{
								v1.x *= obscale.x;
								v1.y *= obscale.y;
								v1.z *= obscale.z;



								//ROTATE
								v1 = rotateq(v1, rotation);


								//LOCAL TO WORLD POSITIONS
								v1 = AddVec(v1, obpos);
							}


							// Early culling using the first vertex normal.
							Vector3 v1Dir = Normalize(SubVec(v1, cameraPosition));
							Vector3 norm = facenorms[i];

							if (!statc)
							{
								norm = rotateq(norm, rotation);

								norm = { -norm.x, -norm.y, -norm.z };
							}

							if (Dot(v1Dir, norm) > 0) {

								Vector3 v2 = (vertLoc[trisLoc[curtris + 1]]);
								Vector3 v3 = (vertLoc[trisLoc[curtris + 2]]);

								if (!statc)
								{
									v2.x *= obscale.x;
									v2.y *= obscale.y;
									v2.z *= obscale.z;

									v3.x *= obscale.x;
									v3.y *= obscale.y;
									v3.z *= obscale.z;

									//rotate
									v2 = rotateq(v2, rotation);
									v3 = rotateq(v3, rotation);

									//local to world
									v2 = AddVec(v2, obpos);
									v3 = AddVec(v3, obpos);
								}


								// Transform vertices to screen space.
								Vector3 sv1, sv2, sv3;
								WorldToScreenPoint(v1, sv1);
								WorldToScreenPoint(v2, sv2);
								WorldToScreenPoint(v3, sv3);

								int cnt = 0;


								// --- Compute the triangle's bounding box ---
								float minX = min(min(sv1.x, sv2.x), sv3.x);
								float maxX = max(max(sv1.x, sv2.x), sv3.x);

								if (ceil(minX) > (int)maxX) continue;

									float minY = min(min(sv1.y, sv2.y), sv3.y);
									float maxY = max(max(sv1.y, sv2.y), sv3.y);

									if (ceil(minY) > (int)maxY) continue;

									

										//IF ALL BEHIND CAMERA, CULL
										if (sv1.z < 0.01 && sv2.z < 0.01 && sv3.z < 0.01) {
											continue;

										}

										bool redo = false;
										Vector3 pushdir;


										if (sv1.z <= 0.01f)
										{
											redo = true;

											// Calculate the current depth of v1 relative to the camera along cameraForward.
											Vector3 camToVert = SubVec(v1, cameraPosition);
											float depth = Dot(camToVert, cameraForward);

											// Compute the distance to push v1 along cameraForward so that it reaches the near plane.
											float t = 0.01f - depth;
											if (t > 0.0f) {
												v1 = AddVec(v1, MulVec(cameraForward, t));
												WorldToScreenPoint(v1, sv1);
											}
											else continue;
										}


										if (sv2.z <= 0.01f)
										{
											redo = true;

											// Calculate the current depth of v1 relative to the camera along cameraForward.
											Vector3 camToVert = SubVec(v2, cameraPosition);
											float depth = Dot(camToVert, cameraForward);

											// Compute the distance to push v1 along cameraForward so that it reaches the near plane.
											float t = 0.01f - depth;
											if (t > 0.0f) {
												v2 = AddVec(v2, MulVec(cameraForward, t));
												WorldToScreenPoint(v2, sv2);
											}
											else continue;
										}
										if (sv3.z <= 0.01f)
										{
											redo = true;

											// Calculate the current depth of v1 relative to the camera along cameraForward.
											Vector3 camToVert = SubVec(v3, cameraPosition);
											float depth = Dot(camToVert, cameraForward);

											// Compute the distance to push v1 along cameraForward so that it reaches the near plane.
											float t = 0.01f - depth;
											if (t > 0.0f) {
												v3 = AddVec(v3, MulVec(cameraForward, t));
												WorldToScreenPoint(v3, sv3);
											}
											else continue;
										}


										if (redo)
										{
											minX = min(min(sv1.x, sv2.x), sv3.x);
											maxX = max(max(sv1.x, sv2.x), sv3.x);

											minY = min(min(sv1.y, sv2.y), sv3.y);
											maxY = max(max(sv1.y, sv2.y), sv3.y);
										}


										minX = (int)floor(minX);
										maxX = (int)ceil(maxX);
										minY = (int)floor(minY);
										maxY = (int)ceil(maxY);


										if (maxX < 0 || maxY < 0 || minX >= Width || minY >= Height) {
											continue;
										}

										// Count triangle processed.
										stats.y++;


										Vector3 bottom = sv3;
										Vector3 middle = sv1;
										Vector3 top = sv2;

										Vector3 normbot = normsLoc[trisLoc[curtris + 2]];
										Vector3 normmid = normsLoc[trisLoc[curtris]];
										Vector3 normtop = normsLoc[trisLoc[curtris + 1]];

										if (!statc)
										{
											normbot = rotateq(normbot, rotation);
											normmid = rotateq(normmid, rotation);
											normtop = rotateq(normtop, rotation);
										}

										Vector2 uvbot = uvs[trisLoc[curtris + 2]];

										Vector2 uvmid = uvs[trisLoc[curtris]];

										Vector2 uvtop = uvs[trisLoc[curtris + 1]];


										Vector2 u3 = uvbot;
										Vector2 u1 = uvmid;
										Vector2 u2 = uvtop;

										Vector3 n3 = normbot;
										Vector3 n1 = normmid;
										Vector3 n2 = normtop;

										Vector3 lnorm;

										if (top.y < sv1.y)
										{
											top = sv1;
											middle = sv2;

											normtop = n1;
											normmid = n2;

											uvtop = u1;
											uvmid = u2;
										}
										if (top.y < sv3.y)
										{
											top = sv3;
											bottom = sv1;
											middle = sv2;

											normtop = n3;
											normbot = n1;
											normmid = n2;

											uvtop = u3;
											uvbot = u1;
											uvmid = u2;
										}
										if (middle.y < bottom.y)
										{
											Vector3 temp = bottom;
											bottom = middle;
											middle = temp;

											temp = normbot;
											normbot = normmid;
											normmid = temp;

											Vector2 temp2 = uvbot;
											uvbot = uvmid;
											uvmid = temp2;
										}

										sv1 = bottom;
										sv2 = middle;
										sv3 = top;

										Color32 curcol = vertcols[i];
										Color32 normcol;
										normcol.a = 255;

										// Total height for scanline interpolation.
										float totalHeight = top.y - bottom.y;

										int lpendy = min((int)floor(top.y), Height - 1);
										int lpstrty = max((int)ceil(bottom.y), 0);




										stsv1.x = sv1.x; stsv1.y = sv1.y;
										stsv2.x = sv2.x; stsv2.y = sv2.y;
										stsv3.x = sv3.x; stsv3.y = sv3.y;


										Vector2 uv;
										Vector3 uvw;
										float hmlt = 1 / totalHeight;

										// Precompute these once per triangle (before entering the scanline loop):
										float invZ1 = 1.0f / sv1.z;
										float invZ2 = 1.0f / sv2.z;
										float invZ3 = 1.0f / sv3.z;

										// Precompute perspective?corrected texture coordinates for each vertex:
										float u1p = uvbot.x * invZ1;  // Bottom vertex texture coordinate (u) divided by depth.
										float v1p = uvbot.y * invZ1;
										float u2p = uvmid.x * invZ2;   // Middle vertex texture coordinate.
										float v2p = uvmid.y * invZ2;
										float u3p = uvtop.x * invZ3;   // Top vertex texture coordinate.
										float v3p = uvtop.y * invZ3;


										bool DoPerspec = false;


										__m128 lm1 = _mm_setr_ps(normbot.x, normbot.y, normbot.z, 0.0f);
										__m128 lm2 = _mm_setr_ps(normmid.x, normmid.y, normmid.z, 0.0f);
										__m128 lm3 = _mm_setr_ps(normtop.x, normtop.y, normtop.z, 0.0f);

										float tm = top.y - middle.y;
										float mb = middle.y - bottom.y;

										bool simd = false;
										if (UseSimd/* && !redo*/)
										{
											if (maxX - minX >= 4)simd = true;
										}

										// Normal interpolation constants.
										__m256 normbotX = _mm256_set1_ps(normbot.x);
										__m256 normbotY = _mm256_set1_ps(normbot.y);
										__m256 normbotZ = _mm256_set1_ps(normbot.z);
										__m256 normmidX = _mm256_set1_ps(normmid.x);
										__m256 normmidY = _mm256_set1_ps(normmid.y);
										__m256 normmidZ = _mm256_set1_ps(normmid.z);
										__m256 normtopX = _mm256_set1_ps(normtop.x);
										__m256 normtopY = _mm256_set1_ps(normtop.y);
										__m256 normtopZ = _mm256_set1_ps(normtop.z);

										// UV interpolation constants.
										__m256 uvbotX = _mm256_set1_ps(uvbot.x);
										__m256 uvbotY = _mm256_set1_ps(uvbot.y);
										__m256 uvmidX = _mm256_set1_ps(uvmid.x);
										__m256 uvmidY = _mm256_set1_ps(uvmid.y);
										__m256 uvtopX = _mm256_set1_ps(uvtop.x);
										__m256 uvtopY = _mm256_set1_ps(uvtop.y);

										// Depth interpolation constants.
										__m256 sv1z = _mm256_set1_ps(sv1.z);
										__m256 sv2z = _mm256_set1_ps(sv2.z);
										__m256 sv3z = _mm256_set1_ps(sv3.z);


										//VERTEX LIGHT AND COLOR

										Vector3 vcol1;
										Vector3 vcol2;
										Vector3 vcol3;


										//CALCULATE SUNLIGHT AND AMBIENT FOR 3 VERTS
										if (vertexLight)
										{
											float fac = min(1, max(0, Dot(normbot, sundir)));

											Vector3 finalcol;
											Vector3 ambcol;

											finalcol = MulVec(vsuncol, (fac * sunintense));

											float amlerp = (normbot.y + 1) * 0.5;

											ambcol.x = lerp(gvamb.x, vamb.x, amlerp);
											ambcol.y = lerp(gvamb.y, vamb.y, amlerp);
											ambcol.z = lerp(gvamb.z, vamb.z, amlerp);

											vcol1.x = finalcol.x + ambcol.x;
											vcol1.y = finalcol.y + ambcol.y;
											vcol1.z = finalcol.z + ambcol.z;


											fac = min(1, max(0, Dot(normmid, sundir)));
											finalcol = MulVec(vsuncol, (fac * sunintense));
											amlerp = (normmid.y + 1) * 0.5;

											ambcol.x = lerp(gvamb.x, vamb.x, amlerp);
											ambcol.y = lerp(gvamb.y, vamb.y, amlerp);
											ambcol.z = lerp(gvamb.z, vamb.z, amlerp);

											vcol2.x = finalcol.x + ambcol.x;
											vcol2.y = finalcol.y + ambcol.y;
											vcol2.z = finalcol.z + ambcol.z;

											fac = min(1, max(0, Dot(normtop, sundir)));
											finalcol = MulVec(vsuncol, (fac * sunintense));
											amlerp = (normtop.y + 1) * 0.5;

											ambcol.x = lerp(gvamb.x, vamb.x, amlerp);
											ambcol.y = lerp(gvamb.y, vamb.y, amlerp);
											ambcol.z = lerp(gvamb.z, vamb.z, amlerp);

											vcol3.x = finalcol.x + ambcol.x;
											vcol3.y = finalcol.y + ambcol.y;
											vcol3.z = finalcol.z + ambcol.z;


										}



										for (int y = lpstrty; y <= lpendy; y++)
										{
											// Interpolate x along the long edge from bottom to top.
											float alpha = (y - bottom.y) * hmlt;
											float xLong = lerp(bottom.x, top.x, alpha);

											// Determine which half of the triangle we're in.
											bool secondHalf = (y > middle.y);
											float segmentHeight = secondHalf ? tm : mb;
											if (segmentHeight == 0)
												continue;
											float beta = secondHalf ? ((y - middle.y) / tm)
												: ((y - bottom.y) / (middle.y - bottom.y));
											// Interpolate x along the relevant short edge.
											float xEdge = secondHalf ? lerp(middle.x, top.x, beta)
												: lerp(bottom.x, middle.x, beta);

											// Determine left/right boundaries.
											float xLeft = std::min(xLong, xEdge);
											float xRight = std::max(xLong, xEdge);

											int xStart = std::max((int)ceil(xLeft), (int)minX);
											int xEnd = std::min((int)floor(xRight), (int)maxX);

											// --- Loop over the horizontal span for this scanline ---

											int lpend = std::min(Width, xEnd);
											int lpstrt = std::max(0, xStart);

											uv.y = (float)y;
											uint32_t curPixel = y * Width + lpstrt;
											float uvPixel;

											// Compute barycentrics at the boundaries of this scanline.
											Vector2 startUV, endUV;
											startUV.x = (float)lpstrt;
											startUV.y = (float)y;
											endUV.x = (float)lpend;
											endUV.y = (float)y;

											Vector3 baryStart = BGInterpolate(stsv1, stsv2, stsv3, startUV, sv1.z, sv2.z, sv3.z);
											Vector3 baryEnd = BGInterpolate(stsv1, stsv2, stsv3, endUV, sv1.z, sv2.z, sv3.z);

											int span = lpend - lpstrt;
											float spanmlt = 1 / (float)span;
											// Compute per-pixel barycentric step.
											float baryStepX = ((baryEnd.x - baryStart.x)  * spanmlt);
											float baryStepY = ((baryEnd.y - baryStart.y) * spanmlt);
											float baryStepZ = ((baryEnd.z - baryStart.z) * spanmlt);

											// Set initial barycentrics.
											float baryX = baryStart.x;
											float baryY = baryStart.y;
											float baryZ = baryStart.z;

											// Pre-calculate unnormalized barycentrics at the endpoints.
											Vector3 unnormBaryStart = BGInterpolateUnnorm(stsv1, stsv2, stsv3, startUV, sv1.z, sv2.z, sv3.z);
											Vector3 unnormBaryEnd = BGInterpolateUnnorm(stsv1, stsv2, stsv3, endUV, sv1.z, sv2.z, sv3.z);

											// Calculate the weight (r) at each endpoint.
											float rStart = unnormBaryStart.x + unnormBaryStart.y + unnormBaryStart.z;
											float rEnd = unnormBaryEnd.x + unnormBaryEnd.y + unnormBaryEnd.z;

											// Compute per-pixel unnormalized step sizes.
											span = lpend - lpstrt;
											Vector3 unnormBaryStep;
											unnormBaryStep.x = (unnormBaryEnd.x - unnormBaryStart.x) * spanmlt;
											unnormBaryStep.y = (unnormBaryEnd.y - unnormBaryStart.y) * spanmlt;
											unnormBaryStep.z = (unnormBaryEnd.z - unnormBaryStart.z) * spanmlt;
											float rStep = (rEnd - rStart)  * spanmlt;

											// Initialize current unnormalized barycentrics and weight.
											Vector3 currentUnnormBary = unnormBaryStart;
											float currentR = rStart;
											Vector2 uvf;

											if (y > Height - 2)
												simd = false;

											if (simd)
											{
												// SIMD processing.
												constexpr int simdWidth = 8;
												int totalPixels = (lpend - lpstrt + 1);  // inclusive range
												int simdCount = totalPixels / simdWidth;
												int remainder = totalPixels % simdWidth;

												// Copy current unnormalized barycentrics & weight.
												float curUnnormX = currentUnnormBary.x;
												float curUnnormY = currentUnnormBary.y;
												float curUnnormZ = currentUnnormBary.z;
												float curR = currentR;

												// Prepare SIMD constants.
												__m256 unnormStepX = _mm256_set1_ps(unnormBaryStep.x);
												__m256 unnormStepY = _mm256_set1_ps(unnormBaryStep.y);
												__m256 unnormStepZ = _mm256_set1_ps(unnormBaryStep.z);
												__m256 rStepVec = _mm256_set1_ps(rStep);

												// a) Compute per-lane offsets.
												__m256 offsetX = _mm256_mul_ps(indexVec, unnormStepX);
												__m256 offsetY = _mm256_mul_ps(indexVec, unnormStepY);
												__m256 offsetZ = _mm256_mul_ps(indexVec, unnormStepZ);
												__m256 offsetR = _mm256_mul_ps(indexVec, rStepVec);

												// Process pixels in chunks of 8.
												for (int block = 0; block < simdCount; block++)
												{
													__m256 baseX = _mm256_set1_ps(curUnnormX);
													__m256 baseY = _mm256_set1_ps(curUnnormY);
													__m256 baseZ = _mm256_set1_ps(curUnnormZ);
													__m256 baseR = _mm256_set1_ps(curR);

													__m256 unnormX = _mm256_add_ps(baseX, offsetX);
													__m256 unnormY = _mm256_add_ps(baseY, offsetY);
													__m256 unnormZ = _mm256_add_ps(baseZ, offsetZ);
													__m256 rVec = _mm256_add_ps(baseR, offsetR);

													// Perspective-correct barycentrics.
													__m256 baryXVec = _mm256_div_ps(unnormX, rVec);
													__m256 baryYVec = _mm256_div_ps(unnormY, rVec);
													__m256 baryZVec = _mm256_div_ps(unnormZ, rVec);

													// Compute new depths.
													__m256 computedDepth = _mm256_add_ps(
														_mm256_add_ps(_mm256_mul_ps(baryXVec, sv1z),
															_mm256_mul_ps(baryYVec, sv2z)),
														_mm256_mul_ps(baryZVec, sv3z)
													);

													__m256 currentDepth = _mm256_loadu_ps(&Depth[curPixel]);
													__m256 depthMask = _mm256_cmp_ps(computedDepth, currentDepth, _CMP_LT_OS);
													__m256 updatedDepth = _mm256_blendv_ps(currentDepth, computedDepth, depthMask);
													_mm256_storeu_ps(&Depth[curPixel], updatedDepth);

													// Texture lookup if needed.
													__m256i computedTexels;
													if (!notex)
													{
														__m256 uvX = _mm256_add_ps(
															_mm256_add_ps(_mm256_mul_ps(baryXVec, uvbotX),
																_mm256_mul_ps(baryYVec, uvmidX)),
															_mm256_mul_ps(baryZVec, uvtopX)
														);
														__m256 uvY = _mm256_add_ps(
															_mm256_add_ps(_mm256_mul_ps(baryXVec, uvbotY),
																_mm256_mul_ps(baryYVec, uvmidY)),
															_mm256_mul_ps(baryZVec, uvtopY)
														);
														__m256 uvXFloor = _mm256_floor_ps(uvX);
														__m256 uvYFloor = _mm256_floor_ps(uvY);
														uvX = _mm256_sub_ps(uvX, uvXFloor);
														uvY = _mm256_sub_ps(uvY, uvYFloor);

														__m256 texCoordX = _mm256_mul_ps(uvX, texwVec);
														__m256 texCoordY = _mm256_mul_ps(uvY, texhVec);

														__m256i texCoordXInt = _mm256_cvttps_epi32(texCoordX);
														__m256i texCoordYInt = _mm256_cvttps_epi32(texCoordY);
														__m256i texwVecInt = _mm256_set1_epi32(Texw);
														__m256i texIndex = _mm256_add_epi32(_mm256_mullo_epi32(texCoordYInt, texwVecInt), texCoordXInt);

														computedTexels = _mm256_i32gather_epi32(reinterpret_cast<const int*>(Texture.data()), texIndex, 4);
													}
													else
													{
														computedTexels = _mm256_set1_epi32(*reinterpret_cast<const int*>(&Tint));
													}

													// Apply vertex lighting if enabled.
													if (vertexLight)
													{
														__m256i red_int = _mm256_and_si256(computedTexels, _mm256_set1_epi32(0xFF));
														__m256i green_int = _mm256_and_si256(_mm256_srli_epi32(computedTexels, 8), _mm256_set1_epi32(0xFF));
														__m256i blue_int = _mm256_and_si256(_mm256_srli_epi32(computedTexels, 16), _mm256_set1_epi32(0xFF));

														__m256 red_f = _mm256_cvtepi32_ps(red_int);
														__m256 green_f = _mm256_cvtepi32_ps(green_int);
														__m256 blue_f = _mm256_cvtepi32_ps(blue_int);

														__m256 lightR = _mm256_add_ps(
															_mm256_add_ps(_mm256_mul_ps(baryXVec, _mm256_set1_ps(vcol1.x)),
																_mm256_mul_ps(baryYVec, _mm256_set1_ps(vcol2.x))),
															_mm256_mul_ps(baryZVec, _mm256_set1_ps(vcol3.x))
														);
														__m256 lightG = _mm256_add_ps(
															_mm256_add_ps(_mm256_mul_ps(baryXVec, _mm256_set1_ps(vcol1.y)),
																_mm256_mul_ps(baryYVec, _mm256_set1_ps(vcol2.y))),
															_mm256_mul_ps(baryZVec, _mm256_set1_ps(vcol3.y))
														);
														__m256 lightB = _mm256_add_ps(
															_mm256_add_ps(_mm256_mul_ps(baryXVec, _mm256_set1_ps(vcol1.z)),
																_mm256_mul_ps(baryYVec, _mm256_set1_ps(vcol2.z))),
															_mm256_mul_ps(baryZVec, _mm256_set1_ps(vcol3.z))
														);

														__m256 final_red_f = _mm256_mul_ps(red_f, lightR);
														__m256 final_green_f = _mm256_mul_ps(green_f, lightG);
														__m256 final_blue_f = _mm256_mul_ps(blue_f, lightB);

														__m256 maxVal = _mm256_set1_ps(255.0f);
														final_red_f = _mm256_min_ps(final_red_f, maxVal);
														final_green_f = _mm256_min_ps(final_green_f, maxVal);
														final_blue_f = _mm256_min_ps(final_blue_f, maxVal);

														__m256i final_red = _mm256_cvtps_epi32(final_red_f);
														__m256i final_green = _mm256_cvtps_epi32(final_green_f);
														__m256i final_blue = _mm256_cvtps_epi32(final_blue_f);

														// Conditionally set alpha from spec.
														if (!usealpha)
														{
															__m256i alpha = _mm256_set1_epi32(spec << 24);
															computedTexels = _mm256_or_si256(
																_mm256_or_si256(final_red, _mm256_slli_epi32(final_green, 8)),
																_mm256_or_si256(_mm256_slli_epi32(final_blue, 16), alpha)
															);
														}
														else
														{
															computedTexels = _mm256_or_si256(
																_mm256_or_si256(final_red, _mm256_slli_epi32(final_green, 8)),
																_mm256_slli_epi32(final_blue, 16)
															);
														}

														{
															__m256 fogdistVec = _mm256_set1_ps(fogdist);
															__m256 oneVec = _mm256_set1_ps(1.0f);
															__m256 depthDiv = _mm256_div_ps(computedDepth, fogdistVec);
															__m256 minFactor = _mm256_min_ps(oneVec, depthDiv);
															__m256 fogFactor = _mm256_sqrt_ps(minFactor);

															__m256i red_int_final = _mm256_and_si256(computedTexels, _mm256_set1_epi32(0xFF));
															__m256i green_int_final = _mm256_and_si256(_mm256_srli_epi32(computedTexels, 8), _mm256_set1_epi32(0xFF));
															__m256i blue_int_final = _mm256_and_si256(_mm256_srli_epi32(computedTexels, 16), _mm256_set1_epi32(0xFF));
															__m256 red_f_final = _mm256_cvtepi32_ps(red_int_final);
															__m256 green_f_final = _mm256_cvtepi32_ps(green_int_final);
															__m256 blue_f_final = _mm256_cvtepi32_ps(blue_int_final);

															__m256 fogR = _mm256_set1_ps(fgc.x);
															__m256 fogG = _mm256_set1_ps(fgc.y);
															__m256 fogB = _mm256_set1_ps(fgc.z);

															__m256 oneMinusFog = _mm256_sub_ps(oneVec, fogFactor);
															__m256 final_red_f_fog = _mm256_add_ps(_mm256_mul_ps(red_f_final, oneMinusFog),
																_mm256_mul_ps(fogR, fogFactor));
															__m256 final_green_f_fog = _mm256_add_ps(_mm256_mul_ps(green_f_final, oneMinusFog),
																_mm256_mul_ps(fogG, fogFactor));
															__m256 final_blue_f_fog = _mm256_add_ps(_mm256_mul_ps(blue_f_final, oneMinusFog),
																_mm256_mul_ps(fogB, fogFactor));

															final_red_f_fog = _mm256_min_ps(final_red_f_fog, maxVal);
															final_green_f_fog = _mm256_min_ps(final_green_f_fog, maxVal);
															final_blue_f_fog = _mm256_min_ps(final_blue_f_fog, maxVal);

															__m256i final_red_int = _mm256_cvtps_epi32(final_red_f_fog);
															__m256i final_green_int = _mm256_cvtps_epi32(final_green_f_fog);
															__m256i final_blue_int = _mm256_cvtps_epi32(final_blue_f_fog);

															if (!usealpha)
															{
																__m256i alpha = _mm256_set1_epi32(spec << 24);
																computedTexels = _mm256_or_si256(
																	_mm256_or_si256(final_red_int, _mm256_slli_epi32(final_green_int, 8)),
																	_mm256_or_si256(_mm256_slli_epi32(final_blue_int, 16), alpha)
																);
															}
															else
															{
																computedTexels = _mm256_or_si256(
																	_mm256_or_si256(final_red_int, _mm256_slli_epi32(final_green_int, 8)),
																	_mm256_slli_epi32(final_blue_int, 16)
																);
															}
														}
													}
													else
													{
														// If vertex lighting is not enabled.
														if (!usealpha)
														{
															__m256i alpha = _mm256_set1_epi32(spec << 24);
															__m256i colorMask = _mm256_set1_epi32(0x00FFFFFF);
															computedTexels = _mm256_or_si256(_mm256_and_si256(computedTexels, colorMask), alpha);
														}
													}

													// Conditionally update the color buffer.
													__m256i oldTexels = _mm256_loadu_si256((__m256i*)&RenderTexture[curPixel]);
													__m256i newTexels = _mm256_blendv_epi8(oldTexels, computedTexels, _mm256_castps_si256(depthMask));
													_mm256_storeu_si256((__m256i*)&RenderTexture[curPixel], newTexels);

													// Interpolate normals if vertex lighting is not enabled.
													if (!vertexLight)
													{
														__m256 normX = _mm256_add_ps(
															_mm256_add_ps(_mm256_mul_ps(baryXVec, normbotX),
																_mm256_mul_ps(baryYVec, normmidX)),
															_mm256_mul_ps(baryZVec, normtopX)
														);
														__m256 normY = _mm256_add_ps(
															_mm256_add_ps(_mm256_mul_ps(baryXVec, normbotY),
																_mm256_mul_ps(baryYVec, normmidY)),
															_mm256_mul_ps(baryZVec, normtopY)
														);
														__m256 normZ = _mm256_add_ps(
															_mm256_add_ps(_mm256_mul_ps(baryXVec, normbotZ),
																_mm256_mul_ps(baryYVec, normmidZ)),
															_mm256_mul_ps(baryZVec, normtopZ)
														);

														__m256 normXColor = _mm256_mul_ps(_mm256_add_ps(normX, one), scale);
														__m256 normYColor = _mm256_mul_ps(_mm256_add_ps(normY, one), scale);
														__m256 normZColor = _mm256_mul_ps(_mm256_add_ps(normZ, one), scale);

														__m256i normXInt = _mm256_cvtps_epi32(normXColor);
														__m256i normYInt = _mm256_cvtps_epi32(normYColor);
														__m256i normZInt = _mm256_cvtps_epi32(normZColor);

														__m256i red = normXInt;
														__m256i green = _mm256_slli_epi32(normYInt, 8);
														__m256i blue = _mm256_slli_epi32(normZInt, 16);
														__m256i alpha = _mm256_set1_epi32(0xFF << 24);

														__m256i computedNormals = _mm256_or_si256(alpha, _mm256_or_si256(blue, _mm256_or_si256(green, red)));
														__m256i oldNorms = _mm256_loadu_si256((__m256i*)&ScreenNorms[curPixel]);
														__m256i newNorms = _mm256_blendv_epi8(oldNorms, computedNormals, _mm256_castps_si256(depthMask));
														_mm256_storeu_si256((__m256i*)&ScreenNorms[curPixel], newNorms);
													}

													// Advance to next block.
													curPixel += simdWidth;
													curUnnormX += unnormBaryStep.x * simdWidth;
													curUnnormY += unnormBaryStep.y * simdWidth;
													curUnnormZ += unnormBaryStep.z * simdWidth;
													curR += rStep * simdWidth;
												}

												// Write back updated barycentrics.
												currentUnnormBary.x = curUnnormX;
												currentUnnormBary.y = curUnnormY;
												currentUnnormBary.z = curUnnormZ;
												currentR = curR;

												// Process any leftover pixels (scalar remainder loop).
												for (int i = 0; i < remainder; i++)
												{
													float invR = 1.0f / currentR;
													float baryX = currentUnnormBary.x * invR;
													float baryY = currentUnnormBary.y * invR;
													float baryZ = currentUnnormBary.z * invR;

													float pixelDepth = baryX * sv1.z + baryY * sv2.z + baryZ * sv3.z;
													if (pixelDepth < Depth[curPixel])
													{
														Depth[curPixel] = pixelDepth;
														if (!vertexLight)
														{
															float nx = baryX * normbot.x + baryY * normmid.x + baryZ * normtop.x;
															float ny = baryX * normbot.y + baryY * normmid.y + baryZ * normtop.y;
															float nz = baryX * normbot.z + baryY * normmid.z + baryZ * normtop.z;
															ScreenNorms[curPixel].a = 255;
															ScreenNorms[curPixel].r = (uint8_t)((nx + 1.0f) * 127.5f);
															ScreenNorms[curPixel].g = (uint8_t)((ny + 1.0f) * 127.5f);
															ScreenNorms[curPixel].b = (uint8_t)((nz + 1.0f) * 127.5f);
														}

														Color32 computedColor;
														if (!notex)
														{
															float uvx = baryX * uvbot.x + baryY * uvmid.x + baryZ * uvtop.x;
															float uvy = baryX * uvbot.y + baryY * uvmid.y + baryZ * uvtop.y;
															uvx -= floorf(uvx);
															uvy -= floorf(uvy);
															int tx = (int)(uvx * Texw);
															int ty = (int)(uvy * Texh);
															int texIndex = ty * Texw + tx;
															computedColor = Texture[texIndex];
															if (!usealpha)
																computedColor.a = spec;
														}
														else
														{
															computedColor = Tint;
															if (!usealpha)
																computedColor.a = spec;
														}

														if (vertexLight)
														{
															float lightR = baryX * vcol1.x + baryY * vcol2.x + baryZ * vcol3.x;
															float lightG = baryX * vcol1.y + baryY * vcol2.y + baryZ * vcol3.y;
															float lightB = baryX * vcol1.z + baryY * vcol2.z + baryZ * vcol3.z;
															uint8_t final_r = min(255.0f, computedColor.r * lightR);
															uint8_t final_g = min(255.0f, computedColor.g * lightG);
															uint8_t final_b = min(255.0f, computedColor.b * lightB);
															if (!usealpha)
																computedColor = { final_r, final_g, final_b, spec };
															else
																computedColor = { final_r, final_g, final_b, computedColor.a };

															float fac = powf(min(1.0f, pixelDepth / fogdist), 0.5f);
															final_r = (int)min(255.0f, computedColor.r * (1.0f - fac) + fgc.x * fac);
															final_g = (int)min(255.0f, computedColor.g * (1.0f - fac) + fgc.y * fac);
															final_b = (int)min(255.0f, computedColor.b * (1.0f - fac) + fgc.z * fac);
															if (!usealpha)
																computedColor = { final_r, final_g, final_b, spec };
															else
																computedColor = { final_r, final_g, final_b, computedColor.a };
														}

														RenderTexture[curPixel] = computedColor;
													}
													curPixel++;
													currentUnnormBary.x += unnormBaryStep.x;
													currentUnnormBary.y += unnormBaryStep.y;
													currentUnnormBary.z += unnormBaryStep.z;
													currentR += rStep;
												}
											}
											else
											{
												// Non-SIMD processing.
												for (int x = lpstrt; x <= lpend; x++)
												{
													float baryX = currentUnnormBary.x / currentR;
													float baryY = currentUnnormBary.y / currentR;
													float baryZ = currentUnnormBary.z / currentR;

													float pixelDepth = baryX * sv1.z + baryY * sv2.z + baryZ * sv3.z;
													if (pixelDepth < Depth[curPixel])
													{
														Depth[curPixel] = pixelDepth;

														if (!NoTextures)
														{
															if (!vertexLight)
															{
																lnorm.x = baryX * normbot.x + baryY * normmid.x + baryZ * normtop.x;
																lnorm.y = baryX * normbot.y + baryY * normmid.y + baryZ * normtop.y;
																lnorm.z = baryX * normbot.z + baryY * normmid.z + baryZ * normtop.z;
																Color32& tmcol = ScreenNorms[curPixel];
																tmcol.a = 255;
																tmcol.r = ((lnorm.x + 1) * 0.5f) * 255;
																tmcol.g = ((lnorm.y + 1) * 0.5f) * 255;
																tmcol.b = ((lnorm.z + 1) * 0.5f) * 255;
															}

															if (!notex)
															{
																uvf = uvtop;
																uvf.x = baryX * uvbot.x + baryY * uvmid.x + baryZ * uvtop.x;
																uvf.y = baryX * uvbot.y + baryY * uvmid.y + baryZ * uvtop.y;
																uvf.x = repeat(uvf.x);
																uvf.y = repeat(uvf.y);

																uvPixel = ((int)(uvf.y * Texh) * Texw) + (uvf.x * Texw);
																RenderTexture[curPixel] = Texture[uvPixel];
																if (!usealpha)
																	RenderTexture[curPixel].a = spec;
															}
															else
															{
																RenderTexture[curPixel] = Tint;
																if (!usealpha)
																	RenderTexture[curPixel].a = spec;
															}
														}
														else
														{
															RenderTexture[curPixel] = curcol;
														}

														if (vertexLight)
														{
															Color32 computedColor = RenderTexture[curPixel];
															float lightR = baryX * vcol1.x + baryY * vcol2.x + baryZ * vcol3.x;
															float lightG = baryX * vcol1.y + baryY * vcol2.y + baryZ * vcol3.y;
															float lightB = baryX * vcol1.z + baryY * vcol2.z + baryZ * vcol3.z;
															uint8_t final_r = min(255.0f, computedColor.r * lightR);
															uint8_t final_g = min(255.0f, computedColor.g * lightG);
															uint8_t final_b = min(255.0f, computedColor.b * lightB);
															if (!usealpha)
																computedColor = { final_r, final_g, final_b, spec };
															else
																computedColor = { final_r, final_g, final_b, computedColor.a };

															float fac = powf(min(1.0f, pixelDepth / fogdist), 0.5f);
															final_r = (int)min(255.0f, computedColor.r * (1.0f - fac) + fgc.x * fac);
															final_g = (int)min(255.0f, computedColor.g * (1.0f - fac) + fgc.y * fac);
															final_b = (int)min(255.0f, computedColor.b * (1.0f - fac) + fgc.z * fac);
															if (!usealpha)
																computedColor = { final_r, final_g, final_b, spec };
															else
																computedColor = { final_r, final_g, final_b, computedColor.a };

															RenderTexture[curPixel] = computedColor;
														}

														stats.x++; // Count drawn pixel.
													}

													curPixel++;
													currentUnnormBary.x += unnormBaryStep.x;
													currentUnnormBary.y += unnormBaryStep.y;
													currentUnnormBary.z += unnormBaryStep.z;
													currentR += rStep;
												}

											}
										} // end scanline loop
									//}
								//	}
								//} // end triangle culling check
							}
						}
					} // end triangle loop
				}
			} // end object loop


		};

		// If numThreads is 1 or less, process on the main thread.
		if (numThreads <= 1) {
			processObjects(0, Count, finalStats);
		}
		else {
			// Set up threading containers.
			std::vector<std::thread> threads;
			std::vector<Vector4Int> threadStats(numThreads);

			std::atomic<int> nextObject(0);  // Atomic counter to dynamically distribute work

			// Thread function using dynamic scheduling

			int perthread = (float)Count / (float)numThreads;
			perthread = max(1, min(perthread, 8));
			auto threadWorker = [&](int threadIndex) {
				Vector4Int localStats = { 0, 0, 0, 0 };

				int objIndex;
				while ((objIndex = nextObject.fetch_add(perthread)) < Count) {
					processObjects(objIndex, min(Count, objIndex + perthread), localStats);
				}



				threadStats[threadIndex] = localStats;
			};

			// Launch threads
			for (int i = 0; i < numThreads; i++) {
				threads.emplace_back(threadWorker, i);
			}

			// Wait for all threads to finish
			for (auto &t : threads)
				t.join();

			// Combine statistics from all threads
			for (int i = 0; i < numThreads; i++) {
				finalStats.x += threadStats[i].x;
				finalStats.y += threadStats[i].y;
				finalStats.z += threadStats[i].z;
				finalStats.w += threadStats[i].w;
			}
		}

		return finalStats;
	}


	Vector3* pos;
	float* distances;

	__declspec(dllexport) void ShareMem(Vector3* Pos, float* dist)
	{
		pos = Pos;
		distances = dist;

	}

	__declspec(dllexport) void GetDistance(int size, Vector3 compare)
	{
		for (int i = 0; i < size; i++)
		{
			distances[i] = Distance(compare, pos[i]);
		}

	}

}