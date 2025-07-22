using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

using Debug = UnityEngine.Debug;

namespace RS.Scene
{
    public class Chunk : MonoBehaviour
    {
        public BlockType[] blocks;
        public int width = 32;
        public int height = 32;

        public void ModifyBlock(Vector3 localPos, BlockType newBlockType)
        {
            var index = GetArrayIndex((int)localPos.x, (int)localPos.y, (int)localPos.z);
            blocks[index] = newBlockType;
            BuildMeshUsingJobSystem();
        }

        public void BuildMeshUsingJobSystem()
        {
            var sw = Stopwatch.StartNew();

            var vertices = new NativeList<Vector3>(Allocator.TempJob);
            var trianlges = new NativeList<int>(Allocator.TempJob);
            var uvs = new NativeList<Vector2>(Allocator.TempJob);

            var buildMeshJob = new BuildMeshJob
            {
                width = width,
                height = height,
                blocks = new NativeArray<BlockType>(blocks, Allocator.TempJob),
                uvTable = Block.uvTableArray,
                vertices = vertices,
                triangles = trianlges,
                uvs = uvs
            };

            var jobHandle = buildMeshJob.Schedule();
            jobHandle.Complete();

            var mesh = new Mesh();
            
            var verticesArray = new Vector3[vertices.Length];
            var vertexIndex = 0;
            foreach (var vertex in vertices)
            {
                verticesArray[vertexIndex++] = vertex;
            }
            
            var trianglesArray = new int[trianlges.Length];
            var triangleIndex = 0;
            foreach (var triangle in trianlges)
            {
                trianglesArray[triangleIndex++] = triangle;
            }
            
            var uvArray = new Vector2[uvs.Length];
            var uvIndex = 0;
            foreach (var uv in uvs)
            {
                uvArray[uvIndex++] = uv;
            }

            vertices.Dispose();
            trianlges.Dispose();
            uvs.Dispose();
            buildMeshJob.blocks.Dispose();
            
            mesh.vertices = verticesArray;
            mesh.triangles = trianglesArray;
            mesh.uv = uvArray;
            
            mesh.RecalculateNormals();
            
            GetComponent<MeshFilter>().mesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;

            sw.Stop();
            Debug.Log($"Chunk {transform.position} generated in {sw.ElapsedMilliseconds} ms");
        }

        [BurstCompile]
        private struct BuildMeshJob : IJob
        {
            [ReadOnly] public int width;
            [ReadOnly] public int height;
            [ReadOnly] public NativeArray<BlockType> blocks;
            [ReadOnly] public NativeArray<Vector2> uvTable;
            public NativeList<Vector3> vertices;
            public NativeList<int> triangles;
            public NativeList<Vector2> uvs;

            public void Execute()
            {
                for (var x = 0; x < width; x++)
                {
                    for (var z = 0; z < width; z++)
                    {
                        for (var y = 0; y < height; y++)
                        {
                            var index = GetArrayIndex(x, y, z);

                            if (blocks[index] == BlockType.Air)
                            {
                                continue;
                            }

                            var elevation = y * 0.5f;

                            var uv = new NativeArray<Vector4>(4, Allocator.Temp);
                            var uvIndex = (int)blocks[index] * 4;
                            for (var i = 0; i < 4; i++)
                            {
                                uv[i] = uvTable[uvIndex + i];
                            }

                            // Up
                            var upIndex = GetArrayIndex(x, y + 1, z);
                            if (upIndex == -1 || blocks[upIndex] == BlockType.Air)
                            {
                                var vertIndex = vertices.Length;
                                vertices.Add(new Vector3(x, elevation + 0.5f, z));
                                vertices.Add(new Vector3(x + 1, elevation + 0.5f, z));
                                vertices.Add(new Vector3(x + 1, elevation + 0.5f, z + 1));
                                vertices.Add(new Vector3(x, elevation + 0.5f, z + 1));

                                triangles.Add(vertIndex);
                                triangles.Add(vertIndex + 2);
                                triangles.Add(vertIndex + 1);
                                triangles.Add(vertIndex);
                                triangles.Add(vertIndex + 3);
                                triangles.Add(vertIndex + 2);

                                uvs.Add(uv[0]);
                                uvs.Add(uv[1]);
                                uvs.Add(uv[2]);
                                uvs.Add(uv[3]);
                            }

                            // Bottom
                            var downIndex = GetArrayIndex(x, y - 1, z);
                            if (downIndex == -1 || blocks[downIndex] == BlockType.Air)
                            {
                                var vertIndex = vertices.Length;
                                vertices.Add(new Vector3(x, elevation, z));
                                vertices.Add(new Vector3(x + 1, elevation, z));
                                vertices.Add(new Vector3(x + 1, elevation, z + 1));
                                vertices.Add(new Vector3(x, elevation, z + 1));

                                triangles.Add(vertIndex);
                                triangles.Add(vertIndex + 1);
                                triangles.Add(vertIndex + 2);
                                triangles.Add(vertIndex);
                                triangles.Add(vertIndex + 2);
                                triangles.Add(vertIndex + 3);

                                uvs.Add(uv[0]);
                                uvs.Add(uv[1]);
                                uvs.Add(uv[2]);
                                uvs.Add(uv[3]);
                            }

                            // Front
                            var frontIndex = GetArrayIndex(x, y, z - 1);
                            if (frontIndex == -1 || blocks[frontIndex] == BlockType.Air)
                            {
                                var vertIndex = vertices.Length;
                                vertices.Add(new Vector3(x, elevation, z));
                                vertices.Add(new Vector3(x + 1, elevation, z));
                                vertices.Add(new Vector3(x + 1, elevation + 0.5f, z));
                                vertices.Add(new Vector3(x, elevation + 0.5f, z));

                                triangles.Add(vertIndex);
                                triangles.Add(vertIndex + 2);
                                triangles.Add(vertIndex + 1);
                                triangles.Add(vertIndex);
                                triangles.Add(vertIndex + 3);
                                triangles.Add(vertIndex + 2);

                                uvs.Add(uv[0]);
                                uvs.Add(uv[1]);
                                uvs.Add(uv[2]);
                                uvs.Add(uv[3]);
                            }

                            // Back
                            var backIndex = GetArrayIndex(x, y, z + 1);
                            if (backIndex == -1 || blocks[backIndex] == BlockType.Air)
                            {
                                var vertIndex = vertices.Length;
                                vertices.Add(new Vector3(x, elevation, z + 1));
                                vertices.Add(new Vector3(x + 1, elevation, z + 1));
                                vertices.Add(new Vector3(x + 1, elevation + 0.5f, z + 1));
                                vertices.Add(new Vector3(x, elevation + 0.5f, z + 1));

                                triangles.Add(vertIndex);
                                triangles.Add(vertIndex + 1);
                                triangles.Add(vertIndex + 2);
                                triangles.Add(vertIndex);
                                triangles.Add(vertIndex + 2);
                                triangles.Add(vertIndex + 3);

                                uvs.Add(uv[0]);
                                uvs.Add(uv[1]);
                                uvs.Add(uv[2]);
                                uvs.Add(uv[3]);
                            }

                            // Left
                            var leftIndex = GetArrayIndex(x - 1, y, z);
                            if (leftIndex == -1 || blocks[leftIndex] == BlockType.Air)
                            {
                                var vertIndex = vertices.Length;
                                vertices.Add(new Vector3(x, elevation, z + 1));
                                vertices.Add(new Vector3(x, elevation, z));
                                vertices.Add(new Vector3(x, elevation + 0.5f, z));
                                vertices.Add(new Vector3(x, elevation + 0.5f, z + 1));

                                triangles.Add(vertIndex);
                                triangles.Add(vertIndex + 2);
                                triangles.Add(vertIndex + 1);
                                triangles.Add(vertIndex);
                                triangles.Add(vertIndex + 3);
                                triangles.Add(vertIndex + 2);

                                uvs.Add(uv[0]);
                                uvs.Add(uv[1]);
                                uvs.Add(uv[2]);
                                uvs.Add(uv[3]);
                            }

                            // right
                            var rightIndex = GetArrayIndex(x + 1, y, z);
                            if (rightIndex == -1 || blocks[rightIndex] == BlockType.Air)
                            {
                                var vertIndex = vertices.Length;
                                vertices.Add(new Vector3(x + 1, elevation, z));
                                vertices.Add(new Vector3(x + 1, elevation, z + 1));
                                vertices.Add(new Vector3(x + 1, elevation + 0.5f, z + 1));
                                vertices.Add(new Vector3(x + 1, elevation + 0.5f, z));

                                triangles.Add(vertIndex);
                                triangles.Add(vertIndex + 2);
                                triangles.Add(vertIndex + 1);
                                triangles.Add(vertIndex);
                                triangles.Add(vertIndex + 3);
                                triangles.Add(vertIndex + 2);

                                uvs.Add(uv[0]);
                                uvs.Add(uv[1]);
                                uvs.Add(uv[2]);
                                uvs.Add(uv[3]);
                            }

                            uv.Dispose();
                        }
                    }
                }
            }
            
            private int GetArrayIndex(int x, int y, int z)
            {
                if (x < 0 || x >= width || y < 0 || y >= height || z < 0 || z >= width)
                {
                    return -1;
                }
            
                return x * width * height + z * height + y;
            }
        }

        public void BuildMesh()
        {
            var sw = Stopwatch.StartNew();
            
            var mesh = new Mesh();
            
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < width; z++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var index = GetArrayIndex(x, y, z);

                        if (blocks[index] == BlockType.Air)
                        {
                            continue;
                        }

                        var elevation = y * 0.5f;

                        var uv = Block.uvTable[(int)blocks[index]];
                        
                        // Up
                        var upIndex = GetArrayIndex(x, y + 1, z);
                        if (upIndex == -1 || blocks[upIndex] == BlockType.Air)
                        {
                            var vertIndex = vertices.Count;
                            vertices.Add(new Vector3(x, elevation + 0.5f, z));
                            vertices.Add(new Vector3(x + 1, elevation + 0.5f, z));
                            vertices.Add(new Vector3(x + 1, elevation + 0.5f, z + 1));
                            vertices.Add(new Vector3(x, elevation + 0.5f, z + 1));

                            triangles.Add(vertIndex);
                            triangles.Add(vertIndex + 2);
                            triangles.Add(vertIndex + 1);
                            triangles.Add(vertIndex);
                            triangles.Add(vertIndex + 3);
                            triangles.Add(vertIndex + 2);
                            
                            uvs.Add(uv[0]);
                            uvs.Add(uv[1]);
                            uvs.Add(uv[2]);
                            uvs.Add(uv[3]);
                        }
                        
                        // Bottom
                        var downIndex = GetArrayIndex(x, y - 1, z);
                        if (downIndex == -1 || blocks[downIndex] == BlockType.Air)
                        {
                            var vertIndex = vertices.Count;
                            vertices.Add(new Vector3(x, elevation, z));
                            vertices.Add(new Vector3(x + 1, elevation, z));
                            vertices.Add(new Vector3(x + 1, elevation, z + 1));
                            vertices.Add(new Vector3(x, elevation, z + 1));

                            triangles.Add(vertIndex);
                            triangles.Add(vertIndex + 1);
                            triangles.Add(vertIndex + 2);
                            triangles.Add(vertIndex);
                            triangles.Add(vertIndex + 2);
                            triangles.Add(vertIndex + 3);
                            
                            uvs.Add(uv[0]);
                            uvs.Add(uv[1]);
                            uvs.Add(uv[2]);
                            uvs.Add(uv[3]);
                        }
                        
                        // Front
                        var frontIndex = GetArrayIndex(x, y, z - 1);
                        if (frontIndex == -1 || blocks[frontIndex] == BlockType.Air)
                        {
                            var vertIndex = vertices.Count;
                            vertices.Add(new Vector3(x, elevation, z));
                            vertices.Add(new Vector3(x + 1, elevation, z));
                            vertices.Add(new Vector3(x + 1, elevation + 0.5f, z));
                            vertices.Add(new Vector3(x, elevation + 0.5f, z));

                            triangles.Add(vertIndex);
                            triangles.Add(vertIndex + 2);
                            triangles.Add(vertIndex + 1);
                            triangles.Add(vertIndex);
                            triangles.Add(vertIndex + 3);
                            triangles.Add(vertIndex + 2);
                            
                            uvs.Add(uv[0]);
                            uvs.Add(uv[1]);
                            uvs.Add(uv[2]);
                            uvs.Add(uv[3]);
                        }
                        
                        // Back
                        var backIndex = GetArrayIndex(x, y, z + 1);
                        if (backIndex == -1 || blocks[backIndex] == BlockType.Air)
                        {
                            var vertIndex = vertices.Count;
                            vertices.Add(new Vector3(x, elevation, z + 1));
                            vertices.Add(new Vector3(x + 1, elevation, z + 1));
                            vertices.Add(new Vector3(x + 1, elevation + 0.5f, z + 1));
                            vertices.Add(new Vector3(x, elevation + 0.5f, z + 1));

                            triangles.Add(vertIndex);
                            triangles.Add(vertIndex + 1);
                            triangles.Add(vertIndex + 2);
                            triangles.Add(vertIndex);
                            triangles.Add(vertIndex + 2);
                            triangles.Add(vertIndex + 3);
                            
                            uvs.Add(uv[0]);
                            uvs.Add(uv[1]);
                            uvs.Add(uv[2]);
                            uvs.Add(uv[3]);
                        }
                        
                        // Left
                        var leftIndex = GetArrayIndex(x - 1, y, z);
                        if (leftIndex == -1 || blocks[leftIndex] == BlockType.Air)
                        {
                            var vertIndex = vertices.Count;
                            vertices.Add(new Vector3(x, elevation, z + 1));
                            vertices.Add(new Vector3(x, elevation, z));
                            vertices.Add(new Vector3(x, elevation + 0.5f, z));
                            vertices.Add(new Vector3(x, elevation + 0.5f, z + 1));

                            triangles.Add(vertIndex);
                            triangles.Add(vertIndex + 2);
                            triangles.Add(vertIndex + 1);
                            triangles.Add(vertIndex);
                            triangles.Add(vertIndex + 3);
                            triangles.Add(vertIndex + 2);
                            
                            uvs.Add(uv[0]);
                            uvs.Add(uv[1]);
                            uvs.Add(uv[2]);
                            uvs.Add(uv[3]);
                        }
                        
                        // right
                        var rightIndex = GetArrayIndex(x + 1, y, z);
                        if (rightIndex == -1 || blocks[rightIndex] == BlockType.Air)
                        {
                            var vertIndex = vertices.Count;
                            vertices.Add(new Vector3(x + 1, elevation, z));
                            vertices.Add(new Vector3(x + 1, elevation, z + 1));
                            vertices.Add(new Vector3(x + 1, elevation + 0.5f, z + 1));
                            vertices.Add(new Vector3(x + 1, elevation + 0.5f, z));

                            triangles.Add(vertIndex);
                            triangles.Add(vertIndex + 2);
                            triangles.Add(vertIndex + 1);
                            triangles.Add(vertIndex);
                            triangles.Add(vertIndex + 3);
                            triangles.Add(vertIndex + 2);
                            
                            uvs.Add(uv[0]);
                            uvs.Add(uv[1]);
                            uvs.Add(uv[2]);
                            uvs.Add(uv[3]);
                        }
                    }
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();

            
            var mf = GetComponent<MeshFilter>();
            if (mf.mesh != null)
            {
                DestroyImmediate(mf.mesh);
            }

            mf.mesh = mesh;
            
            // GetComponent<MeshFilter>().mesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;

            sw.Stop();
            Debug.Log($"Chunk {transform.position} generated in {sw.ElapsedMilliseconds} ms");
        }

        private int GetArrayIndex(int x, int y, int z)
        {
            if (x < 0 || x >= width || y < 0 || y >= height || z < 0 || z >= width)
            {
                return -1;
            }
            
            return x * width * height + z * height + y;
        }
        
    }
}