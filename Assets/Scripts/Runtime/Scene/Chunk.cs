using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
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
        Tree, // 树木生成中
        DataReady, // 数据准备完成，Mesh未生成
        MeshReady, // Mesh未加载active == false
        Loaded, // 当前场景已加载显示中
    }
    
    public struct MeshData
    {
        // normal block
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uvs;
        
        // water block
        public Vector3[] waterVertices;
        public int[] waterTriangles;
    }
    
    public class Chunk
    {
        public BlockType[] blocks;
        public BlockType[] topBlocks; // y轴上仅最下面的Chunk持有
        public int[] topBlockHeights;
        public float[] density;
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
            density = new float[32 * 32 * 32];
            status = ChunkStatus.Empty;
        }
        
        public void ModifyBlock(Vector3Int localPos, BlockType newBlockType)
        {
            var index = GetBlockIndex(localPos);
            blocks[index] = newBlockType;
            
            RsSceneManager.Instance.BlockModifyRecord(chunkPos, index, newBlockType);
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

        public static MeshData BuildMesh(BlockType[] blocks, int width, int height, BlockType[] extraBlocks)
        {
            var sw = Stopwatch.StartNew();
            
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            var wVertices = new List<Vector3>();
            var wTriangles = new List<int>();

            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < width; z++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var index = GetBlockIndex(x, y, z);
                        var upIndex = GetBlockIndex(x, y + 1, z);
                        var downIndex = GetBlockIndex(x, y - 1, z);
                        var frontIndex = GetBlockIndex(x, y, z - 1);
                        var backIndex = GetBlockIndex(x, y, z + 1);
                        var leftIndex = GetBlockIndex(x - 1, y, z);
                        var rightIndex = GetBlockIndex(x + 1, y, z);
                        var elevation = y * 0.5f;

                        if (blocks[index] == BlockType.Air)
                        {
                            continue;
                        }

                        if (blocks[index] == BlockType.Water)
                        {
                            // 水面只有遇到空气时才会生成面
                            // Up
                            if ((y == 31 && extraBlocks[upIndex] == BlockType.Air) || blocks[upIndex] == BlockType.Air)
                            {
                                var vertIndex = wVertices.Count;
                                wVertices.Add(new Vector3(x, elevation + 0.5f, z));
                                wVertices.Add(new Vector3(x + 1, elevation + 0.5f, z));
                                wVertices.Add(new Vector3(x + 1, elevation + 0.5f, z + 1));
                                wVertices.Add(new Vector3(x, elevation + 0.5f, z + 1));

                                wTriangles.Add(vertIndex);
                                wTriangles.Add(vertIndex + 2);
                                wTriangles.Add(vertIndex + 1);
                                wTriangles.Add(vertIndex);
                                wTriangles.Add(vertIndex + 3);
                                wTriangles.Add(vertIndex + 2);
                            }
                            
                            // Down
                            if ((y == 0 && extraBlocks[downIndex] == BlockType.Air) || blocks[downIndex] == BlockType.Air)
                            {
                                var vertIndex = wVertices.Count;
                                wVertices.Add(new Vector3(x, elevation, z));
                                wVertices.Add(new Vector3(x + 1, elevation, z));
                                wVertices.Add(new Vector3(x + 1, elevation, z + 1));
                                wVertices.Add(new Vector3(x, elevation, z + 1));

                                wTriangles.Add(vertIndex);
                                wTriangles.Add(vertIndex + 1);
                                wTriangles.Add(vertIndex + 2);
                                wTriangles.Add(vertIndex);
                                wTriangles.Add(vertIndex + 2);
                                wTriangles.Add(vertIndex + 3);
                            }
                            
                            // Front
                            if ((z == 0 && extraBlocks[frontIndex] == BlockType.Air) || blocks[frontIndex] == BlockType.Air)
                            {
                                var vertIndex = wVertices.Count;
                                wVertices.Add(new Vector3(x, elevation, z));
                                wVertices.Add(new Vector3(x + 1, elevation, z));
                                wVertices.Add(new Vector3(x + 1, elevation + 0.5f, z));
                                wVertices.Add(new Vector3(x, elevation + 0.5f, z));

                                wTriangles.Add(vertIndex);
                                wTriangles.Add(vertIndex + 2);
                                wTriangles.Add(vertIndex + 1);
                                wTriangles.Add(vertIndex);
                                wTriangles.Add(vertIndex + 3);
                                wTriangles.Add(vertIndex + 2);
                            }
                            
                            // Back
                            if ((z == 31 && extraBlocks[backIndex] == BlockType.Air) || blocks[backIndex] == BlockType.Air)
                            {
                                var vertIndex = wVertices.Count;
                                wVertices.Add(new Vector3(x, elevation, z + 1));
                                wVertices.Add(new Vector3(x + 1, elevation, z + 1));
                                wVertices.Add(new Vector3(x + 1, elevation + 0.5f, z + 1));
                                wVertices.Add(new Vector3(x, elevation + 0.5f, z + 1));

                                wTriangles.Add(vertIndex);
                                wTriangles.Add(vertIndex + 1);
                                wTriangles.Add(vertIndex + 2);
                                wTriangles.Add(vertIndex);
                                wTriangles.Add(vertIndex + 2);
                                wTriangles.Add(vertIndex + 3);
                            }
                            
                            // Left
                            if ((x == 0 && extraBlocks[leftIndex] == BlockType.Air) || blocks[leftIndex] == BlockType.Air)
                            {
                                var vertIndex = wVertices.Count;
                                wVertices.Add(new Vector3(x, elevation, z + 1));
                                wVertices.Add(new Vector3(x, elevation, z));
                                wVertices.Add(new Vector3(x, elevation + 0.5f, z));
                                wVertices.Add(new Vector3(x, elevation + 0.5f, z + 1));

                                wTriangles.Add(vertIndex);
                                wTriangles.Add(vertIndex + 2);
                                wTriangles.Add(vertIndex + 1);
                                wTriangles.Add(vertIndex);
                                wTriangles.Add(vertIndex + 3);
                                wTriangles.Add(vertIndex + 2);
                            }
                            
                            // right
                            if ((x == 31 && extraBlocks[rightIndex] == BlockType.Air) || blocks[rightIndex] == BlockType.Air)
                            {
                                var vertIndex = wVertices.Count;
                                wVertices.Add(new Vector3(x + 1, elevation, z));
                                wVertices.Add(new Vector3(x + 1, elevation, z + 1));
                                wVertices.Add(new Vector3(x + 1, elevation + 0.5f, z + 1));
                                wVertices.Add(new Vector3(x + 1, elevation + 0.5f, z));

                                wTriangles.Add(vertIndex);
                                wTriangles.Add(vertIndex + 2);
                                wTriangles.Add(vertIndex + 1);
                                wTriangles.Add(vertIndex);
                                wTriangles.Add(vertIndex + 3);
                                wTriangles.Add(vertIndex + 2);
                            }

                            continue;
                        }
                        

                        var uv = Block.uvTable[(int)blocks[index]];
                        
                        // Up
                        if ((y == 31 && IsTranslucent(extraBlocks[upIndex])) || IsTranslucent(blocks[upIndex]))
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
                        
                        // Down
                        if ((y == 0 && IsTranslucent(extraBlocks[downIndex])) || IsTranslucent(blocks[downIndex]))
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
                            
                            uvs.Add(uv[4]);
                            uvs.Add(uv[5]);
                            uvs.Add(uv[6]);
                            uvs.Add(uv[7]);
                        }
                        
                        // Front
                        if ((z == 0 && IsTranslucent(extraBlocks[frontIndex])) || IsTranslucent(blocks[frontIndex]))
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
                            
                            if (y % 2 == 0)
                            {
                                uvs.Add(uv[8]);
                                uvs.Add(uv[9]);
                                uvs.Add(uv[10]);
                                uvs.Add(uv[11]);
                            }
                            else
                            {
                                uvs.Add(uv[12]);
                                uvs.Add(uv[13]);
                                uvs.Add(uv[14]);
                                uvs.Add(uv[15]);
                            }
                        }
                        
                        // Back
                        if ((z == 31 && IsTranslucent(extraBlocks[backIndex])) || IsTranslucent(blocks[backIndex]))
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
                            
                            if (y % 2 == 0)
                            {
                                uvs.Add(uv[8]);
                                uvs.Add(uv[9]);
                                uvs.Add(uv[10]);
                                uvs.Add(uv[11]);
                            }
                            else
                            {
                                uvs.Add(uv[12]);
                                uvs.Add(uv[13]);
                                uvs.Add(uv[14]);
                                uvs.Add(uv[15]);
                            }
                        }
                        
                        // Left
                        if ((x == 0 && IsTranslucent(extraBlocks[leftIndex])) || IsTranslucent(blocks[leftIndex]))
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
                            
                            if (y % 2 == 0)
                            {
                                uvs.Add(uv[8]);
                                uvs.Add(uv[9]);
                                uvs.Add(uv[10]);
                                uvs.Add(uv[11]);
                            }
                            else
                            {
                                uvs.Add(uv[12]);
                                uvs.Add(uv[13]);
                                uvs.Add(uv[14]);
                                uvs.Add(uv[15]);
                            }
                        }
                        
                        // right
                        if ((x == 31 && IsTranslucent(extraBlocks[rightIndex])) || IsTranslucent(blocks[rightIndex]))
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
                            
                            if (y % 2 == 0)
                            {
                                uvs.Add(uv[8]);
                                uvs.Add(uv[9]);
                                uvs.Add(uv[10]);
                                uvs.Add(uv[11]);
                            }
                            else
                            {
                                uvs.Add(uv[12]);
                                uvs.Add(uv[13]);
                                uvs.Add(uv[14]);
                                uvs.Add(uv[15]);
                            }
                        }
                        
                        // leaf
                        if (blocks[index] == BlockType.Leaf)
                        {
                            var pos = new Vector3(x, elevation, z);
                            AddMoreTris(new Vector3(-0.375f, 0.3625f, -0.625f), new Vector3(1.25f, 0.3625f, 1.3625f),
                                pos, 0, -45.0f, ref vertices, ref triangles, ref uvs, ref uv);
                            AddMoreTris(new Vector3(-0.25f, 0.3f, -0.25625f), new Vector3(1.375f, 0.3f, 1.36875f),
                                pos, 0, 45.0f, ref vertices, ref triangles, ref uvs, ref uv);
                            AddMoreTris(new Vector3(0.4625f, -0.153125f, -0.3125f), new Vector3(0.4625f, 0.659375f, 1.3125f),
                                pos, 1, 45.0f, ref vertices, ref triangles, ref uvs, ref uv);
                        }
                    }
                }
            }

            var meshData = new MeshData
            {
                vertices = vertices.ToArray(), triangles = triangles.ToArray(), uvs = uvs.ToArray(),
                waterVertices = wVertices.ToArray(), waterTriangles = wTriangles.ToArray(),
            };
            
            sw.Stop();
            // Debug.Log($"Chunk Mesh generated in {sw.ElapsedMilliseconds} ms");

            return meshData;
        }

        private static void AddMoreTris(Vector3 from, Vector3 to, Vector3 pos, int mode, float angle,
            ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs, ref Vector2[] uv)
        {
            var vertIndex = vertices.Count;
            var pivot = new Vector3(pos.x + 0.5f, pos.y + 0.25f, pos.z + 0.5f);

            if (mode == 0)
            {
                var rot = Quaternion.Euler(angle, 0, 0);
                
                var v1 = new Vector3(pos.x + from.x, pos.y + from.y, pos.z + from.z);
                var v2 = new Vector3(pos.x + from.x, pos.y + from.y, pos.z + to.z);
                var v3 = new Vector3(pos.x + to.x, pos.y + from.y, pos.z + to.z);
                var v4 = new Vector3(pos.x + to.x, pos.y + from.y, pos.z + from.z);

                vertices.Add(rot * (v1 - pivot) + pivot);
                vertices.Add(rot * (v2 - pivot) + pivot);
                vertices.Add(rot * (v3 - pivot) + pivot);
                vertices.Add(rot * (v4 - pivot) + pivot);
            }
            else if (mode == 1)
            {
                var rot = Quaternion.Euler(0, angle, 0);
                
                var v1 = new Vector3(pos.x + from.x, pos.y + from.y, pos.z + from.z);
                var v2 = new Vector3(pos.x + from.x, pos.y + from.y, pos.z + to.z);
                var v3 = new Vector3(pos.x + from.x, pos.y + to.y, pos.z + to.z);
                var v4 = new Vector3(pos.x + from.x, pos.y + to.y, pos.z + from.z);
                
                vertices.Add(rot * (v1 - pivot) + pivot);
                vertices.Add(rot * (v2 - pivot) + pivot);
                vertices.Add(rot * (v3 - pivot) + pivot);
                vertices.Add(rot * (v4 - pivot) + pivot);
            }
            

            triangles.Add(vertIndex);
            triangles.Add(vertIndex + 2);
            triangles.Add(vertIndex + 1);
            triangles.Add(vertIndex);
            triangles.Add(vertIndex + 3);
            triangles.Add(vertIndex + 2);
                            
            // 双面渲染
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


        public void UpdateMesh()
        {
            var sw = Stopwatch.StartNew();

            var extraBlocks = ChunkManager.Instance.CollectNeighborBlocks(chunkPos);
            var meshData = BuildMesh(blocks, 32, 32, extraBlocks);
            
            var waterGo = go.transform.Find("Water").gameObject;

            if (meshData.vertices.Length > 0)
            {
                go.SetActive(true);
                var mesh = new Mesh();
                mesh.vertices = meshData.vertices;
                mesh.triangles = meshData.triangles;
                mesh.uv = meshData.uvs;
                mesh.RecalculateNormals();
                
                var chunkTf = go.GetComponent<MeshFilter>();
                chunkTf.mesh = mesh;
                    
                var chunkMc = go.GetComponent<MeshCollider>();
                chunkMc.sharedMesh = mesh;
            }
            

            if (meshData.waterVertices.Length > 0)
            {
                if (waterGo.activeSelf == false)
                {
                    ChunkManager.Instance.NotifyNeighborUpdateMesh(chunkPos);
                }
                
                waterGo.SetActive(true);
                var waterMesh = new Mesh();
                waterMesh.vertices = meshData.waterVertices;
                waterMesh.triangles = meshData.waterTriangles;
                waterMesh.RecalculateNormals();
                var waterTf = waterGo.GetComponent<MeshFilter>();
                waterTf.mesh = waterMesh;
                
                // var waterMc = waterGo.GetComponent<MeshCollider>();
                // waterMc.sharedMesh = waterMesh;

            }
            
            sw.Stop();
            Debug.Log($"Chunk Mesh Updated in {sw.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// 使用JobSystem来同时处理多个Chunk的mesh构建
        /// </summary>
        /// <param name="chunks"></param>
        public static void BuildMeshUsingJobSystem(List<Chunk> chunks)
        {
            var sw = Stopwatch.StartNew();
            var chunkCount = chunks.Count;
            var jobHandles = new NativeArray<JobHandle>(chunkCount, Allocator.Temp);
            var jobs = new NativeArray<BuildMeshJob>(chunkCount, Allocator.Temp);
            var verticesList = new NativeList<Vector3>[chunkCount];
            var trianglesList = new NativeList<int>[chunkCount];
            var uvsList = new NativeList<Vector2>[chunkCount];
            var waterVerticesList = new NativeList<Vector3>[chunkCount];
            var waterTrianglesList = new NativeList<int>[chunkCount];

            // var jobSw = Stopwatch.StartNew();
            
            for (var i = 0; i < chunkCount; i++)
            {
                verticesList[i] = new NativeList<Vector3>(Allocator.TempJob);
                trianglesList[i] = new NativeList<int>(Allocator.TempJob);
                uvsList[i] = new NativeList<Vector2>(Allocator.TempJob);
                waterVerticesList[i] = new NativeList<Vector3>(Allocator.TempJob);
                waterTrianglesList[i] = new NativeList<int>(Allocator.TempJob);

                var buildMeshJob = new BuildMeshJob()
                {
                    width = 32,
                    height = 32,
                    blocks = new NativeArray<BlockType>(chunks[i].blocks, Allocator.TempJob),
                    extraBlocks = new NativeArray<BlockType>(ChunkManager.Instance.CollectNeighborBlocks(chunks[i].chunkPos), Allocator.TempJob),
                    uvTable = Block.uvTableArray,
                    vertices = verticesList[i],
                    triangles = trianglesList[i],
                    uvs = uvsList[i],
                    wVertices = waterVerticesList[i],
                    wTriangles = waterTrianglesList[i],
                };

                jobs[i] = buildMeshJob;
                jobHandles[i] = buildMeshJob.Schedule();
            }
            
            // 等待全部完成
            JobHandle.CompleteAll(jobHandles);
            
            // jobSw.Stop();
            // Debug.Log($"JobFinish Updated in {jobSw.ElapsedMilliseconds} ms");
            
            // 处理生成数据
            // var dataSw = Stopwatch.StartNew();
            
            for (var i = 0; i < chunkCount; i++)
            {
                var chunk = chunks[i];
                
                var vertices = verticesList[i];
                var triangles = trianglesList[i];
                var uvs = uvsList[i];
                var waterVertices = waterVerticesList[i];
                var waterTriangles = waterTrianglesList[i];
            
                // 不生成mesh
                if (vertices.Length == 0 && waterVertices.Length == 0)
                {
                    if (chunk.go != null)
                    {
                        chunk.go.SetActive(false);
                    }
                    // 其实和Loaded状态不同，但是暂时当作一样，可能会在卸载时出现问题，需要注意
                    chunk.status = ChunkStatus.Loaded;
                    continue;
                }
                
                var go = chunks[i].go;
                go.SetActive(true);
                var waterGo = go.transform.Find("Water").gameObject;
                
                var mesh = new Mesh();
                mesh.SetVertices(vertices.AsArray());
                mesh.SetTriangles(triangles.AsArray().ToArray(), 0);
                mesh.SetUVs(0, uvs.AsArray());
                mesh.RecalculateNormals();
                
                var chunkTf = go.GetComponent<MeshFilter>();
                chunkTf.mesh = mesh;
                    
                var chunkMc = go.GetComponent<MeshCollider>();
                chunkMc.sharedMesh = mesh;
            
                if (waterVertices.Length == 0)
                {
                    waterGo.SetActive(false);
                }
                else
                {
                    var waterMesh = new Mesh();
                    waterMesh.SetVertices(waterVertices.AsArray());
                    waterMesh.SetTriangles(waterTriangles.AsArray().ToArray(), 0);
                    waterMesh.RecalculateNormals();
                    var waterTf = waterGo.GetComponent<MeshFilter>();
                    waterTf.mesh = waterMesh;
            
                    // var waterMc = waterGo.GetComponent<MeshCollider>();
                    // waterMc.sharedMesh = waterMesh;
                }
                chunk.status = ChunkStatus.Loaded;
            }

            // dataSw.Stop();
            // Debug.Log($"DataFinish Updated in {dataSw.ElapsedMilliseconds} ms");

            // Dispose所有数据
            for (var i = 0; i < chunkCount; i++)
            {
                verticesList[i].Dispose();
                trianglesList[i].Dispose();
                uvsList[i].Dispose();
                waterVerticesList[i].Dispose();
                waterTrianglesList[i].Dispose();
                jobs[i].blocks.Dispose();
                jobs[i].extraBlocks.Dispose();
            }

            jobs.Dispose();
            jobHandles.Dispose();
            
            sw.Stop();
            Debug.Log($"{chunkCount} chunks meshes generated in {sw.ElapsedMilliseconds} ms");
        }

        private static bool IsTranslucent(BlockType type)
        {
            return type == BlockType.Air || type == BlockType.Leaf || type == BlockType.Water;
        }

        [BurstCompile]
        private struct BuildMeshJob : IJob
        {
            [ReadOnly] public int width;
            [ReadOnly] public int height;
            [ReadOnly] public NativeArray<BlockType> blocks;
            [ReadOnly] public NativeArray<BlockType> extraBlocks;
            [ReadOnly] public NativeArray<Vector2> uvTable;
            public NativeList<Vector3> vertices;
            public NativeList<int> triangles;
            public NativeList<Vector2> uvs;
            public NativeList<Vector3> wVertices;
            public NativeList<int> wTriangles;
            

            public void Execute()
            {
                for (var x = 0; x < width; x++)
                {
                    for (var z = 0; z < width; z++)
                    {
                        for (var y = 0; y < height; y++)
                        {
                            var index = GetBlockIndex(x, y, z);
                            var upIndex = GetBlockIndex(x, y + 1, z);
                            var downIndex = GetBlockIndex(x, y - 1, z);
                            var frontIndex = GetBlockIndex(x, y, z - 1);
                            var backIndex = GetBlockIndex(x, y, z + 1);
                            var leftIndex = GetBlockIndex(x - 1, y, z);
                            var rightIndex = GetBlockIndex(x + 1, y, z);
                            var elevation = y * 0.5f;

                            if (blocks[index] == BlockType.Air)
                            {
                                continue;
                            }

                            if (blocks[index] == BlockType.Water)
                            {
                                // 水面只有遇到空气时才会生成面
                                // Up
                                if ((y == 31 && IsTranslucent(extraBlocks[upIndex])) || blocks[upIndex] == BlockType.Air)
                                {
                                    var vertIndex = wVertices.Length;
                                    wVertices.Add(new Vector3(x, elevation + 0.5f, z));
                                    wVertices.Add(new Vector3(x + 1, elevation + 0.5f, z));
                                    wVertices.Add(new Vector3(x + 1, elevation + 0.5f, z + 1));
                                    wVertices.Add(new Vector3(x, elevation + 0.5f, z + 1));

                                    wTriangles.Add(vertIndex);
                                    wTriangles.Add(vertIndex + 2);
                                    wTriangles.Add(vertIndex + 1);
                                    wTriangles.Add(vertIndex);
                                    wTriangles.Add(vertIndex + 3);
                                    wTriangles.Add(vertIndex + 2);
                                }
                                
                                // Down
                                if ((y == 0 && extraBlocks[downIndex] == BlockType.Air) || blocks[downIndex] == BlockType.Air)
                                {
                                    var vertIndex = wVertices.Length;
                                    wVertices.Add(new Vector3(x, elevation, z));
                                    wVertices.Add(new Vector3(x + 1, elevation, z));
                                    wVertices.Add(new Vector3(x + 1, elevation, z + 1));
                                    wVertices.Add(new Vector3(x, elevation, z + 1));

                                    wTriangles.Add(vertIndex);
                                    wTriangles.Add(vertIndex + 1);
                                    wTriangles.Add(vertIndex + 2);
                                    wTriangles.Add(vertIndex);
                                    wTriangles.Add(vertIndex + 2);
                                    wTriangles.Add(vertIndex + 3);
                                }
                                
                                // Front
                                if ((z == 0 && extraBlocks[frontIndex] == BlockType.Air) || blocks[frontIndex] == BlockType.Air)
                                {
                                    var vertIndex = wVertices.Length;
                                    wVertices.Add(new Vector3(x, elevation, z));
                                    wVertices.Add(new Vector3(x + 1, elevation, z));
                                    wVertices.Add(new Vector3(x + 1, elevation + 0.5f, z));
                                    wVertices.Add(new Vector3(x, elevation + 0.5f, z));

                                    wTriangles.Add(vertIndex);
                                    wTriangles.Add(vertIndex + 2);
                                    wTriangles.Add(vertIndex + 1);
                                    wTriangles.Add(vertIndex);
                                    wTriangles.Add(vertIndex + 3);
                                    wTriangles.Add(vertIndex + 2);
                                }
                                
                                // Back
                                if ((z == 31 && extraBlocks[backIndex] == BlockType.Air) || blocks[backIndex] == BlockType.Air)
                                {
                                    var vertIndex = wVertices.Length;
                                    wVertices.Add(new Vector3(x, elevation, z + 1));
                                    wVertices.Add(new Vector3(x + 1, elevation, z + 1));
                                    wVertices.Add(new Vector3(x + 1, elevation + 0.5f, z + 1));
                                    wVertices.Add(new Vector3(x, elevation + 0.5f, z + 1));

                                    wTriangles.Add(vertIndex);
                                    wTriangles.Add(vertIndex + 1);
                                    wTriangles.Add(vertIndex + 2);
                                    wTriangles.Add(vertIndex);
                                    wTriangles.Add(vertIndex + 2);
                                    wTriangles.Add(vertIndex + 3);
                                }
                                
                                // Left
                                if ((x == 0 && extraBlocks[leftIndex] == BlockType.Air) || blocks[leftIndex] == BlockType.Air)
                                {
                                    var vertIndex = wVertices.Length;
                                    wVertices.Add(new Vector3(x, elevation, z + 1));
                                    wVertices.Add(new Vector3(x, elevation, z));
                                    wVertices.Add(new Vector3(x, elevation + 0.5f, z));
                                    wVertices.Add(new Vector3(x, elevation + 0.5f, z + 1));

                                    wTriangles.Add(vertIndex);
                                    wTriangles.Add(vertIndex + 2);
                                    wTriangles.Add(vertIndex + 1);
                                    wTriangles.Add(vertIndex);
                                    wTriangles.Add(vertIndex + 3);
                                    wTriangles.Add(vertIndex + 2);
                                }
                                
                                // right
                                if ((x == 31 && extraBlocks[rightIndex] == BlockType.Air) || blocks[rightIndex] == BlockType.Air)
                                {
                                    var vertIndex = wVertices.Length;
                                    wVertices.Add(new Vector3(x + 1, elevation, z));
                                    wVertices.Add(new Vector3(x + 1, elevation, z + 1));
                                    wVertices.Add(new Vector3(x + 1, elevation + 0.5f, z + 1));
                                    wVertices.Add(new Vector3(x + 1, elevation + 0.5f, z));

                                    wTriangles.Add(vertIndex);
                                    wTriangles.Add(vertIndex + 2);
                                    wTriangles.Add(vertIndex + 1);
                                    wTriangles.Add(vertIndex);
                                    wTriangles.Add(vertIndex + 3);
                                    wTriangles.Add(vertIndex + 2);
                                }

                                continue;
                            }
                            

                            var uv = new NativeArray<Vector2>(16, Allocator.Temp);
                            var uvIndex = (int)blocks[index] * 16;
                            for (var i = 0; i < 16; i++)
                            {
                                uv[i] = uvTable[uvIndex + i];
                            }
                            
                            // Up
                            if ((y == 31 && IsTranslucent(extraBlocks[upIndex])) || IsTranslucent(blocks[upIndex]))
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
                            
                            // Down
                            if ((y == 0 && IsTranslucent(extraBlocks[downIndex])) || IsTranslucent(blocks[downIndex]))
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
                                
                                uvs.Add(uv[4]);
                                uvs.Add(uv[5]);
                                uvs.Add(uv[6]);
                                uvs.Add(uv[7]);
                            }
                            
                            // Front
                            if ((z == 0 && IsTranslucent(extraBlocks[frontIndex])) || IsTranslucent(blocks[frontIndex]))
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
                                
                                if (y % 2 == 0)
                                {
                                    uvs.Add(uv[8]);
                                    uvs.Add(uv[9]);
                                    uvs.Add(uv[10]);
                                    uvs.Add(uv[11]);
                                }
                                else
                                {
                                    uvs.Add(uv[12]);
                                    uvs.Add(uv[13]);
                                    uvs.Add(uv[14]);
                                    uvs.Add(uv[15]);
                                }
                            }
                            
                            // Back
                            if ((z == 31 && IsTranslucent(extraBlocks[backIndex])) || IsTranslucent(blocks[backIndex]))
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
                                
                                if (y % 2 == 0)
                                {
                                    uvs.Add(uv[8]);
                                    uvs.Add(uv[9]);
                                    uvs.Add(uv[10]);
                                    uvs.Add(uv[11]);
                                }
                                else
                                {
                                    uvs.Add(uv[12]);
                                    uvs.Add(uv[13]);
                                    uvs.Add(uv[14]);
                                    uvs.Add(uv[15]);
                                }
                            }
                            
                            // Left
                            if ((x == 0 && IsTranslucent(extraBlocks[leftIndex])) || IsTranslucent(blocks[leftIndex]))
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
                                
                                if (y % 2 == 0)
                                {
                                    uvs.Add(uv[8]);
                                    uvs.Add(uv[9]);
                                    uvs.Add(uv[10]);
                                    uvs.Add(uv[11]);
                                }
                                else
                                {
                                    uvs.Add(uv[12]);
                                    uvs.Add(uv[13]);
                                    uvs.Add(uv[14]);
                                    uvs.Add(uv[15]);
                                }
                            }
                            
                            // right
                            if ((x == 31 && IsTranslucent(extraBlocks[rightIndex])) || IsTranslucent(blocks[rightIndex]))
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
                                
                                if (y % 2 == 0)
                                {
                                    uvs.Add(uv[8]);
                                    uvs.Add(uv[9]);
                                    uvs.Add(uv[10]);
                                    uvs.Add(uv[11]);
                                }
                                else
                                {
                                    uvs.Add(uv[12]);
                                    uvs.Add(uv[13]);
                                    uvs.Add(uv[14]);
                                    uvs.Add(uv[15]);
                                }
                            }
                            
                            // leaf
                            if (blocks[index] == BlockType.Leaf)
                            {
                                var pos = new Vector3(x, elevation, z);
                                AddMoreTris(new Vector3(-0.375f, 0.3625f, -0.625f), new Vector3(1.25f, 0.3625f, 1.3625f), 
                                    pos, 0, -45.0f, ref vertices, ref triangles, ref uvs, ref uv);
                                AddMoreTris(new Vector3(-0.25f, 0.3f, -0.25625f), new Vector3(1.375f, 0.3f, 1.36875f),
                                    pos, 0, 45.0f, ref vertices, ref triangles, ref uvs, ref uv);
                                AddMoreTris(new Vector3(0.4625f, -0.153125f, -0.3125f), new Vector3(0.4625f, 0.659375f, 1.3125f),
                                    pos, 1, 45.0f, ref vertices, ref triangles, ref uvs, ref uv);
                            }
                            
                            uv.Dispose();
                        }
                    }
                }
            }
            
            private void AddMoreTris(Vector3 from, Vector3 to, Vector3 pos, int mode, float angle, ref NativeList<Vector3> vertices,
                ref NativeList<int> triangles, ref NativeList<Vector2> uvs, ref NativeArray<Vector2> uv)
            {
                var vertIndex = vertices.Length;
                var pivot = new Vector3(pos.x + 0.5f, pos.y + 0.25f, pos.z + 0.5f);
                
                if (mode == 0)
                {
                    var rot = Quaternion.Euler(angle, 0, 0);
                    
                    var v1 = new Vector3(pos.x + from.x, pos.y + from.y, pos.z + from.z);
                    var v2 = new Vector3(pos.x + from.x, pos.y + from.y, pos.z + to.z);
                    var v3 = new Vector3(pos.x + to.x, pos.y + from.y, pos.z + to.z);
                    var v4 = new Vector3(pos.x + to.x, pos.y + from.y, pos.z + from.z);

                    vertices.Add(rot * (v1 - pivot) + pivot);
                    vertices.Add(rot * (v2 - pivot) + pivot);
                    vertices.Add(rot * (v3 - pivot) + pivot);
                    vertices.Add(rot * (v4 - pivot) + pivot);
                }
                else
                {
                    var rot = Quaternion.Euler(0, angle, 0);
                
                    var v1 = new Vector3(pos.x + from.x, pos.y + from.y, pos.z + from.z);
                    var v2 = new Vector3(pos.x + from.x, pos.y + from.y, pos.z + to.z);
                    var v3 = new Vector3(pos.x + from.x, pos.y + to.y, pos.z + to.z);
                    var v4 = new Vector3(pos.x + from.x, pos.y + to.y, pos.z + from.z);
                
                    vertices.Add(rot * (v1 - pivot) + pivot);
                    vertices.Add(rot * (v2 - pivot) + pivot);
                    vertices.Add(rot * (v3 - pivot) + pivot);
                    vertices.Add(rot * (v4 - pivot) + pivot);
                }

                triangles.Add(vertIndex);
                triangles.Add(vertIndex + 2);
                triangles.Add(vertIndex + 1);
                triangles.Add(vertIndex);
                triangles.Add(vertIndex + 3);
                triangles.Add(vertIndex + 2);
                            
                // 双面渲染
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
            
            private int GetBlockIndex(int x, int y, int z)
            {
                // 当超出边界时(只能超过1且只能有一个维度超过)，可以从一个32x32x6的数组中获取一个索引，顺序是上下前后左右
                // 上
                if (y == 32)
                {
                    return x * 32 + z;;
                }

                // 下
                if (y == -1)
                {
                    return 1024 + x * 32 + z;
                }
            
                // 前
                if (z == -1)
                {
                    return 2048 + x * 32 + y;
                }

                // 后
                if (z == 32)
                {
                    return 3072 + x * 32 + y;
                }

                // 左
                if (x == -1)
                {
                    return 4096 + z * 32 + y;
                }
            
                // 右
                if (x == 32)
                {
                    return 5120 + z * 32 + y;
                }
            
                if (x < 0 || x >= 32 || y < 0 || y >= 32 || z < 0 || z >= 32)
                {
                    return -1;
                }
            
                return x * 1024 + z * 32 + y;
            }
        }
        
        public static int GetBlockIndex(int x, int y, int z)
        {
            // 当超出边界时(只能超过1且只能有一个维度超过)，可以从一个32x32x6的数组中获取一个索引，顺序是上下前后左右
            // 上
            if (y == 32)
            {
                return x * 32 + z;;
            }

            // 下
            if (y == -1)
            {
                return 1024 + x * 32 + z;
            }
            
            // 前
            if (z == -1)
            {
                return 2048 + x * 32 + y;
            }

            // 后
            if (z == 32)
            {
                return 3072 + x * 32 + y;
            }

            // 左
            if (x == -1)
            {
                return 4096 + z * 32 + y;
            }
            
            // 右
            if (x == 32)
            {
                return 5120 + z * 32 + y;
            }
            
            if (x < 0 || x >= 32 || y < 0 || y >= 32 || z < 0 || z >= 32)
            {
                return -1;
            }
            
            return x * 1024 + z * 32 + y;
        }

        public static int GetBlockIndex(Vector3Int blockLocalPos)
        {
            return GetBlockIndex(blockLocalPos.x, blockLocalPos.y, blockLocalPos.z);
        }
    }
}