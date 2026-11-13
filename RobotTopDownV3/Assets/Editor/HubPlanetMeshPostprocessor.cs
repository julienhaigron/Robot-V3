using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class HubPlanetMeshPostprocessor : AssetPostprocessor
{
	private const string HUB_MODEL = "Macro_Hub_3D.fbx";
	private const string PLANET_PREFIX = "Giant-Planet";
	private const int SUBDIVISIONS = 2;
	private const int UV_CHANNELS = 8;

	public override uint GetVersion () => 1;

	private void OnPostprocessModel ( GameObject _root )
	{
		if (Path.GetFileName(assetPath) != HUB_MODEL)
			return;

		foreach (MeshFilter filter in _root.GetComponentsInChildren<MeshFilter>(true))
		{
			Mesh source = filter.sharedMesh;
			if (source == null || !filter.gameObject.name.StartsWith(PLANET_PREFIX))
				continue;

			Mesh subdivided = BuildSubdividedSphere(source, SUBDIVISIONS);
			if (subdivided == null)
			{
				Debug.LogWarning($"{nameof(HubPlanetMeshPostprocessor)}: could not read '{source.name}', the planet keeps its original mesh.");
				continue;
			}

			context.AddObjectToAsset(source.name + "_Subdivided", subdivided);
			filter.sharedMesh = subdivided;
		}
	}

	private static Mesh BuildSubdividedSphere ( Mesh _source, int _levels )
	{
		Vector3 center = _source.bounds.center;
		List<Vector3> positions = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		_source.GetVertices(positions);
		_source.GetNormals(normals);
		if (positions.Count == 0 || positions.Count != _source.vertexCount)
			return null;
		bool hasNormals = normals.Count == positions.Count;
		bool hasTangents = _source.HasVertexAttribute(VertexAttribute.Tangent);

		List<Vector2>[] uvs = new List<Vector2>[UV_CHANNELS];
		for (int channel = 0; channel < UV_CHANNELS; channel++)
		{
			if (!_source.HasVertexAttribute(VertexAttribute.TexCoord0 + channel))
				continue;
			uvs[channel] = new List<Vector2>();
			_source.GetUVs(channel, uvs[channel]);
			if (uvs[channel].Count != positions.Count)
				return null;
		}

		int subMeshCount = _source.subMeshCount;
		List<int>[] triangles = new List<int>[subMeshCount];
		for (int s = 0; s < subMeshCount; s++)
		{
			triangles[s] = new List<int>();
			_source.GetTriangles(triangles[s], s);
			if (triangles[s].Count == 0)
				return null;
		}

		Dictionary<long, int> midpoints = new Dictionary<long, int>();
		int Midpoint ( int _a, int _b )
		{
			long key = _a < _b ? ((long)_a << 32) | (uint)_b : ((long)_b << 32) | (uint)_a;
			if (midpoints.TryGetValue(key, out int index))
				return index;

			Vector3 a = positions[_a] - center;
			Vector3 b = positions[_b] - center;
			Vector3 direction = (a + b).normalized;
			positions.Add(center + direction * ((a.magnitude + b.magnitude) * 0.5f));
			if (hasNormals)
				normals.Add(direction);
			foreach (List<Vector2> channel in uvs)
			{
				if (channel != null)
					channel.Add((channel[_a] + channel[_b]) * 0.5f);
			}

			index = positions.Count - 1;
			midpoints.Add(key, index);
			return index;
		}

		for (int level = 0; level < _levels; level++)
		{
			for (int s = 0; s < subMeshCount; s++)
			{
				List<int> source = triangles[s];
				List<int> result = new List<int>(source.Count * 4);
				for (int t = 0; t < source.Count; t += 3)
				{
					int a = source[t], b = source[t + 1], c = source[t + 2];
					int ab = Midpoint(a, b), bc = Midpoint(b, c), ca = Midpoint(c, a);
					result.AddRange(new[] { a, ab, ca, ab, b, bc, ca, bc, c, ab, bc, ca });
				}
				triangles[s] = result;
			}
		}

		Mesh mesh = new Mesh { name = _source.name };
		mesh.indexFormat = positions.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
		mesh.SetVertices(positions);
		if (hasNormals)
			mesh.SetNormals(normals);
		for (int channel = 0; channel < UV_CHANNELS; channel++)
		{
			if (uvs[channel] != null)
				mesh.SetUVs(channel, uvs[channel]);
		}
		mesh.subMeshCount = subMeshCount;
		for (int s = 0; s < subMeshCount; s++)
			mesh.SetTriangles(triangles[s], s, false);
		mesh.RecalculateBounds();
		if (hasTangents)
			mesh.RecalculateTangents();
		return mesh;
	}
}
