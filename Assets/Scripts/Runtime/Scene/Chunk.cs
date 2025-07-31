using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

using RS.Item;
using RS.Utils;
using Debug = UnityEngine.Debug;

namespace RS.Scene
{
    public enum ChunkStatus
    {
        Empty, // 空Chunk
        BaseData, // 基础数据准备中
        Aquifer, // 水域数据准备中
        Surface, // 地表数据准备中
        DataReady, // 数据准备完成，Mesh未生成
        MeshReady, // Mesh未加载active == false
        Loaded, // 当前场景已加载显示中
        MeshGenerating, // Mesh生成中
    }
    
    public struct MeshData
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uvs;
    }
    
    public class Chunk
    {
        public BlockType[] blocks;
        public ChunkStatus status;
        public MeshData meshData;
        public GameObject go;
        public Vector3Int chunkPos; // 是Chunk自用的本地坐标
        
        private int m_width = 32;
        private int m_height = 32;

        public Chunk(Vector3Int chunkPos)
        {
            this.chunkPos = chunkPos;
            blocks = new BlockType[32 * 32 * 32];
            status = ChunkStatus.Empty;
        }
        
        public void ModifyBlock(Vector3Int localPos, BlockType newBlockType)
        {
            var index = GetBlockIndex(localPos);
            blocks[index] = newBlockType;
        }

        public static Vector3Int BlockWorldPosToChunkPos(Vector3Int blockWorldPos)
        {
            return new Vector3Int(
                Mathf.FloorToInt(blockWorldPos.x / 32.0f),
                Mathf.FloorToInt(blockWorldPos.y / 32.0f),
                Mathf.FloorToInt(blockWorldPos.z / 32.0f)
            );
        }

        public static Vector3Int BlockWorldPosToBlockLocalPos(Vector3Int blockWorldPos)
        {
            return new Vector3Int(
                RsMath.Mod(blockWorldPos.x, 32),
                blockWorldPos.y % 32,
                RsMath.Mod(blockWorldPos.z, 32)
            );
        }

        public static Vector3Int WorldPosToBlockWorldPos(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPos.x),
                Mathf.FloorToInt(worldPos.y * 2),
                Mathf.FloorToInt(worldPos.z)
            );
        }

        public static Vector3Int WorldPosToBlockLocalPos(Vector3 worldPos)
        {
            return new Vector3Int(
                RsMath.Mod((int)worldPos.x, 32),
                (int)(worldPos.y * 2 % 32),
                RsMath.Mod((int)worldPos.z, 32)
            );
        }
        
        
        public static Vector3Int WorldPosToChunkPos(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPos.x / 32.0f),
                Mathf.FloorToInt(worldPos.y / 16.0f),
                Mathf.FloorToInt(worldPos.z / 32.0f)
            );
        }

        public static Vector3 ChunkPosToWorldPos(Vector3Int chunkPos)
        {
            return new Vector3(
                chunkPos.x * 32.0f,
                chunkPos.y * 16.0f,
                chunkPos.z * 32.0f
            );
        }

        public static MeshData BuildMesh(BlockType[] blocks, int width, int height)
        {
            var sw = Stopwatch.StartNew();
            
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < width; z++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var index = GetBlockIndex(x, y, z);

                        if (blocks[index] == BlockType.Air)
                        {
                            continue;
                        }

                        var elevation = y * 0.5f;

                        var uv = Block.uvTable[(int)blocks[index]];
                        
                        // Up
                        var upIndex = GetBlockIndex(x, y + 1, z);
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
                        var downIndex = GetBlockIndex(x, y - 1, z);
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
                        var frontIndex = GetBlockIndex(x, y, z - 1);
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
                        var backIndex = GetBlockIndex(x, y, z + 1);
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
                        var leftIndex = GetBlockIndex(x - 1, y, z);
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
                        var rightIndex = GetBlockIndex(x + 1, y, z);
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

            var meshData = new MeshData
                { vertices = vertices.ToArray(), triangles = triangles.ToArray(), uvs = uvs.ToArray() };
            
            sw.Stop();
            // Debug.Log($"Chunk Mesh generated in {sw.ElapsedMilliseconds} ms");

            return meshData;
        }
        
        public void BuildMeshUsingJobSystem()
        {
            var sw = Stopwatch.StartNew();

            var vertices = new NativeList<Vector3>(Allocator.TempJob);
            var trianlges = new NativeList<int>(Allocator.TempJob);
            var uvs = new NativeList<Vector2>(Allocator.TempJob);

            var buildMeshJob = new BuildMeshJob
            {
                width = m_width,
                height = m_height,
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
            
            go.GetComponent<MeshFilter>().mesh = mesh;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;

            sw.Stop();
            Debug.Log($"Chunk {chunkPos} generated in {sw.ElapsedMilliseconds} ms");
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

            for (var x = 0; x < m_width; x++)
            {
                for (var z = 0; z < m_width; z++)
                {
                    for (var y = 0; y < m_height; y++)
                    {
                        var index = GetBlockIndex(x, y, z);

                        if (blocks[index] == BlockType.Air)
                        {
                            continue;
                        }

                        var elevation = y * 0.5f;

                        var uv = Block.uvTable[(int)blocks[index]];
                        
                        // Up
                        var upIndex = GetBlockIndex(x, y + 1, z);
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
                        var downIndex = GetBlockIndex(x, y - 1, z);
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
                        var frontIndex = GetBlockIndex(x, y, z - 1);
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
                        var backIndex = GetBlockIndex(x, y, z + 1);
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
                        var leftIndex = GetBlockIndex(x - 1, y, z);
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
                        var rightIndex = GetBlockIndex(x + 1, y, z);
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
            
            go.GetComponent<MeshFilter>().mesh = mesh;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;

            sw.Stop();
            Debug.Log($"Chunk {chunkPos} generated in {sw.ElapsedMilliseconds} ms");
        }
        
        public static int GetBlockIndex(int x, int y, int z)
        {
            if (x < 0 || x >= 32 || y < 0 || y >= 32 || z < 0 || z >= 32)
            {
                return -1;
            }
            
            return x * 32 * 32 + z * 32 + y;
        }

        public static int GetBlockIndex(Vector3Int blockLocalPos)
        {
            return GetBlockIndex(blockLocalPos.x, blockLocalPos.y, blockLocalPos.z);
        }
    }
}