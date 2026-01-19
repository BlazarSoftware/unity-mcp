using System;
using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages Unity Terrain creation and modification for outdoor level design.
    /// </summary>
    [McpForUnityTool("manage_terrain", AutoRegister = false,
        Description = "Create and modify Unity Terrain for outdoor levels. Actions: create (new terrain with size/resolution), set_height (set height at position/radius), flatten (flatten area to specific height), smooth (smooth terrain in radius), raise_lower (raise/lower by delta), paint_texture (apply terrain layer), add_tree (place tree instances), add_detail (place grass/details), get_height (query height at position), get_info (terrain information). Height operations work in world space or normalized 0-1 space.")]
    public static class ManageTerrain
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: create, set_height, flatten, smooth, raise_lower, paint_texture, add_tree, add_detail, get_height, get_info, ping");
            }

            try
            {
                return action switch
                {
                    "ping" => new SuccessResponse("pong", new { tool = "manage_terrain" }),
                    "create" => CreateTerrain(@params),
                    "set_height" => SetHeight(@params),
                    "flatten" => Flatten(@params),
                    "smooth" => Smooth(@params),
                    "raise_lower" => RaiseLower(@params),
                    "paint_texture" => PaintTexture(@params),
                    "add_tree" => AddTree(@params),
                    "add_detail" => AddDetail(@params),
                    "get_height" => GetHeight(@params),
                    "get_info" => GetTerrainInfo(@params),
                    _ => new ErrorResponse($"Unknown action: {action}. Valid actions: create, set_height, flatten, smooth, raise_lower, paint_texture, add_tree, add_detail, get_height, get_info, ping")
                };
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Creates a new terrain.
        /// </summary>
        private static object CreateTerrain(JObject @params)
        {
            float width = @params["width"]?.ToObject<float>() ?? 500f;
            float length = @params["length"]?.ToObject<float>() ?? 500f;
            float height = @params["height"]?.ToObject<float>() ?? 100f;
            int resolution = @params["resolution"]?.ToObject<int>() ?? 513;
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            string name = @params["name"]?.ToString() ?? "Terrain";

            // Clamp resolution to power of 2 + 1
            resolution = Mathf.ClosestPowerOfTwo(resolution - 1) + 1;
            resolution = Mathf.Clamp(resolution, 33, 4097);

            // Create terrain data
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = resolution;
            terrainData.size = new Vector3(width, height, length);

            // Create terrain game object
            GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
            terrainObj.name = name;
            terrainObj.transform.position = position;

            // Save terrain data as asset
            string assetPath = $"Assets/{name}_TerrainData.asset";
            AssetDatabase.CreateAsset(terrainData, assetPath);
            AssetDatabase.SaveAssets();

            Undo.RegisterCreatedObjectUndo(terrainObj, $"Create Terrain '{name}'");
            Selection.activeGameObject = terrainObj;

            return new SuccessResponse($"Created terrain '{name}' ({width}x{length}, height: {height})",
                new
                {
                    name = name,
                    instanceId = terrainObj.GetInstanceID(),
                    position = new { x = position.x, y = position.y, z = position.z },
                    size = new { width = width, length = length, height = height },
                    resolution = resolution,
                    assetPath = assetPath
                });
        }

        /// <summary>
        /// Sets terrain height at a position or within a radius.
        /// </summary>
        private static object SetHeight(JObject @params)
        {
            Terrain terrain = GetTargetTerrain(@params);
            if (terrain == null)
            {
                return new ErrorResponse("No terrain found. Create a terrain first or specify 'target'.");
            }

            Vector3 worldPosition = VectorParsing.ParseVector3OrDefault(@params["position"]);
            float targetHeight = @params["targetHeight"]?.ToObject<float>() ?? 0f;
            float radius = @params["radius"]?.ToObject<float>() ?? 0f;
            float falloff = @params["falloff"]?.ToObject<float>() ?? 0.5f;
            bool useWorldHeight = @params["useWorldHeight"]?.ToObject<bool>() ?? true;

            TerrainData data = terrain.terrainData;
            Undo.RecordObject(data, "Set Terrain Height");

            // Convert world position to terrain coordinates
            Vector3 terrainPos = terrain.transform.position;
            Vector3 relativePos = worldPosition - terrainPos;

            int heightmapWidth = data.heightmapResolution;
            int heightmapHeight = data.heightmapResolution;

            float normalizedX = relativePos.x / data.size.x;
            float normalizedZ = relativePos.z / data.size.z;

            // Convert target height to normalized height (0-1)
            float normalizedHeight = useWorldHeight ? targetHeight / data.size.y : targetHeight;
            normalizedHeight = Mathf.Clamp01(normalizedHeight);

            if (radius <= 0)
            {
                // Single point
                int x = Mathf.RoundToInt(normalizedX * (heightmapWidth - 1));
                int z = Mathf.RoundToInt(normalizedZ * (heightmapHeight - 1));

                if (x >= 0 && x < heightmapWidth && z >= 0 && z < heightmapHeight)
                {
                    float[,] heights = data.GetHeights(x, z, 1, 1);
                    heights[0, 0] = normalizedHeight;
                    data.SetHeights(x, z, heights);
                }
            }
            else
            {
                // Area with radius
                float normalizedRadius = radius / data.size.x;
                int pixelRadius = Mathf.CeilToInt(normalizedRadius * heightmapWidth);

                int startX = Mathf.Max(0, Mathf.RoundToInt(normalizedX * (heightmapWidth - 1)) - pixelRadius);
                int startZ = Mathf.Max(0, Mathf.RoundToInt(normalizedZ * (heightmapHeight - 1)) - pixelRadius);
                int sizeX = Mathf.Min(heightmapWidth - startX, pixelRadius * 2 + 1);
                int sizeZ = Mathf.Min(heightmapHeight - startZ, pixelRadius * 2 + 1);

                float[,] heights = data.GetHeights(startX, startZ, sizeX, sizeZ);

                int centerX = Mathf.RoundToInt(normalizedX * (heightmapWidth - 1)) - startX;
                int centerZ = Mathf.RoundToInt(normalizedZ * (heightmapHeight - 1)) - startZ;

                for (int z = 0; z < sizeZ; z++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        float dist = Mathf.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                        float normalizedDist = dist / pixelRadius;

                        if (normalizedDist <= 1f)
                        {
                            float falloffFactor = 1f - Mathf.Pow(normalizedDist, 1f / falloff);
                            heights[z, x] = Mathf.Lerp(heights[z, x], normalizedHeight, falloffFactor);
                        }
                    }
                }

                data.SetHeights(startX, startZ, heights);
            }

            terrain.Flush();

            return new SuccessResponse($"Set terrain height to {targetHeight} at ({worldPosition.x}, {worldPosition.z})",
                new
                {
                    position = new { x = worldPosition.x, y = worldPosition.y, z = worldPosition.z },
                    targetHeight = targetHeight,
                    radius = radius
                });
        }

        /// <summary>
        /// Flattens terrain to a specific height.
        /// </summary>
        private static object Flatten(JObject @params)
        {
            Terrain terrain = GetTargetTerrain(@params);
            if (terrain == null)
            {
                return new ErrorResponse("No terrain found. Create a terrain first or specify 'target'.");
            }

            Vector3 worldPosition = VectorParsing.ParseVector3OrDefault(@params["position"]);
            float targetHeight = @params["targetHeight"]?.ToObject<float>() ?? 0f;
            float radius = @params["radius"]?.ToObject<float>() ?? 10f;
            bool useWorldHeight = @params["useWorldHeight"]?.ToObject<bool>() ?? true;

            // Use SetHeight with no falloff for flat result
            @params["falloff"] = 0.01f;
            @params["useWorldHeight"] = useWorldHeight;
            return SetHeight(@params);
        }

        /// <summary>
        /// Smooths terrain in a radius.
        /// </summary>
        private static object Smooth(JObject @params)
        {
            Terrain terrain = GetTargetTerrain(@params);
            if (terrain == null)
            {
                return new ErrorResponse("No terrain found. Create a terrain first or specify 'target'.");
            }

            Vector3 worldPosition = VectorParsing.ParseVector3OrDefault(@params["position"]);
            float radius = @params["radius"]?.ToObject<float>() ?? 10f;
            float strength = @params["strength"]?.ToObject<float>() ?? 0.5f;
            int iterations = @params["iterations"]?.ToObject<int>() ?? 1;

            iterations = Mathf.Clamp(iterations, 1, 10);

            TerrainData data = terrain.terrainData;
            Undo.RecordObject(data, "Smooth Terrain");

            Vector3 terrainPos = terrain.transform.position;
            Vector3 relativePos = worldPosition - terrainPos;

            int heightmapWidth = data.heightmapResolution;
            int heightmapHeight = data.heightmapResolution;

            float normalizedX = relativePos.x / data.size.x;
            float normalizedZ = relativePos.z / data.size.z;
            float normalizedRadius = radius / data.size.x;
            int pixelRadius = Mathf.CeilToInt(normalizedRadius * heightmapWidth);

            int startX = Mathf.Max(1, Mathf.RoundToInt(normalizedX * (heightmapWidth - 1)) - pixelRadius);
            int startZ = Mathf.Max(1, Mathf.RoundToInt(normalizedZ * (heightmapHeight - 1)) - pixelRadius);
            int sizeX = Mathf.Min(heightmapWidth - startX - 1, pixelRadius * 2 + 1);
            int sizeZ = Mathf.Min(heightmapHeight - startZ - 1, pixelRadius * 2 + 1);

            for (int iter = 0; iter < iterations; iter++)
            {
                float[,] heights = data.GetHeights(startX, startZ, sizeX, sizeZ);
                float[,] smoothed = new float[sizeZ, sizeX];

                int centerX = Mathf.RoundToInt(normalizedX * (heightmapWidth - 1)) - startX;
                int centerZ = Mathf.RoundToInt(normalizedZ * (heightmapHeight - 1)) - startZ;

                for (int z = 1; z < sizeZ - 1; z++)
                {
                    for (int x = 1; x < sizeX - 1; x++)
                    {
                        float dist = Mathf.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                        float normalizedDist = dist / pixelRadius;

                        if (normalizedDist <= 1f)
                        {
                            // Average of 3x3 kernel
                            float avg = 0f;
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    avg += heights[z + dz, x + dx];
                                }
                            }
                            avg /= 9f;

                            float factor = (1f - normalizedDist) * strength;
                            smoothed[z, x] = Mathf.Lerp(heights[z, x], avg, factor);
                        }
                        else
                        {
                            smoothed[z, x] = heights[z, x];
                        }
                    }
                }

                // Copy edges
                for (int x = 0; x < sizeX; x++)
                {
                    smoothed[0, x] = heights[0, x];
                    smoothed[sizeZ - 1, x] = heights[sizeZ - 1, x];
                }
                for (int z = 0; z < sizeZ; z++)
                {
                    smoothed[z, 0] = heights[z, 0];
                    smoothed[z, sizeX - 1] = heights[z, sizeX - 1];
                }

                data.SetHeights(startX, startZ, smoothed);
            }

            terrain.Flush();

            return new SuccessResponse($"Smoothed terrain at ({worldPosition.x}, {worldPosition.z}) with radius {radius}",
                new
                {
                    position = new { x = worldPosition.x, y = worldPosition.y, z = worldPosition.z },
                    radius = radius,
                    strength = strength,
                    iterations = iterations
                });
        }

        /// <summary>
        /// Raises or lowers terrain by a delta amount.
        /// </summary>
        private static object RaiseLower(JObject @params)
        {
            Terrain terrain = GetTargetTerrain(@params);
            if (terrain == null)
            {
                return new ErrorResponse("No terrain found. Create a terrain first or specify 'target'.");
            }

            Vector3 worldPosition = VectorParsing.ParseVector3OrDefault(@params["position"]);
            float delta = @params["delta"]?.ToObject<float>() ?? 1f;
            float radius = @params["radius"]?.ToObject<float>() ?? 10f;
            float falloff = @params["falloff"]?.ToObject<float>() ?? 0.5f;

            TerrainData data = terrain.terrainData;
            Undo.RecordObject(data, "Raise/Lower Terrain");

            Vector3 terrainPos = terrain.transform.position;
            Vector3 relativePos = worldPosition - terrainPos;

            int heightmapWidth = data.heightmapResolution;
            int heightmapHeight = data.heightmapResolution;

            float normalizedX = relativePos.x / data.size.x;
            float normalizedZ = relativePos.z / data.size.z;
            float normalizedDelta = delta / data.size.y;
            float normalizedRadius = radius / data.size.x;
            int pixelRadius = Mathf.CeilToInt(normalizedRadius * heightmapWidth);

            int startX = Mathf.Max(0, Mathf.RoundToInt(normalizedX * (heightmapWidth - 1)) - pixelRadius);
            int startZ = Mathf.Max(0, Mathf.RoundToInt(normalizedZ * (heightmapHeight - 1)) - pixelRadius);
            int sizeX = Mathf.Min(heightmapWidth - startX, pixelRadius * 2 + 1);
            int sizeZ = Mathf.Min(heightmapHeight - startZ, pixelRadius * 2 + 1);

            float[,] heights = data.GetHeights(startX, startZ, sizeX, sizeZ);

            int centerX = Mathf.RoundToInt(normalizedX * (heightmapWidth - 1)) - startX;
            int centerZ = Mathf.RoundToInt(normalizedZ * (heightmapHeight - 1)) - startZ;

            for (int z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    float dist = Mathf.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                    float normalizedDistFromCenter = dist / pixelRadius;

                    if (normalizedDistFromCenter <= 1f)
                    {
                        float falloffFactor = 1f - Mathf.Pow(normalizedDistFromCenter, 1f / falloff);
                        heights[z, x] = Mathf.Clamp01(heights[z, x] + normalizedDelta * falloffFactor);
                    }
                }
            }

            data.SetHeights(startX, startZ, heights);
            terrain.Flush();

            return new SuccessResponse($"Raised/lowered terrain by {delta} at ({worldPosition.x}, {worldPosition.z})",
                new
                {
                    position = new { x = worldPosition.x, y = worldPosition.y, z = worldPosition.z },
                    delta = delta,
                    radius = radius
                });
        }

        /// <summary>
        /// Paints a texture on the terrain.
        /// </summary>
        private static object PaintTexture(JObject @params)
        {
            Terrain terrain = GetTargetTerrain(@params);
            if (terrain == null)
            {
                return new ErrorResponse("No terrain found. Create a terrain first or specify 'target'.");
            }

            int layerIndex = @params["layerIndex"]?.ToObject<int>() ?? 0;
            Vector3 worldPosition = VectorParsing.ParseVector3OrDefault(@params["position"]);
            float radius = @params["radius"]?.ToObject<float>() ?? 10f;
            float strength = @params["strength"]?.ToObject<float>() ?? 1f;

            TerrainData data = terrain.terrainData;

            if (data.terrainLayers == null || data.terrainLayers.Length == 0)
            {
                return new ErrorResponse("No terrain layers configured. Add terrain layers in the Terrain Inspector first.");
            }

            if (layerIndex < 0 || layerIndex >= data.terrainLayers.Length)
            {
                return new ErrorResponse($"Invalid layer index {layerIndex}. Available layers: 0-{data.terrainLayers.Length - 1}");
            }

            Undo.RecordObject(data, "Paint Terrain Texture");

            Vector3 terrainPos = terrain.transform.position;
            Vector3 relativePos = worldPosition - terrainPos;

            int alphamapWidth = data.alphamapWidth;
            int alphamapHeight = data.alphamapHeight;

            float normalizedX = relativePos.x / data.size.x;
            float normalizedZ = relativePos.z / data.size.z;
            float normalizedRadius = radius / data.size.x;
            int pixelRadius = Mathf.CeilToInt(normalizedRadius * alphamapWidth);

            int startX = Mathf.Max(0, Mathf.RoundToInt(normalizedX * (alphamapWidth - 1)) - pixelRadius);
            int startZ = Mathf.Max(0, Mathf.RoundToInt(normalizedZ * (alphamapHeight - 1)) - pixelRadius);
            int sizeX = Mathf.Min(alphamapWidth - startX, pixelRadius * 2 + 1);
            int sizeZ = Mathf.Min(alphamapHeight - startZ, pixelRadius * 2 + 1);

            float[,,] alphamaps = data.GetAlphamaps(startX, startZ, sizeX, sizeZ);
            int numLayers = data.terrainLayers.Length;

            int centerX = Mathf.RoundToInt(normalizedX * (alphamapWidth - 1)) - startX;
            int centerZ = Mathf.RoundToInt(normalizedZ * (alphamapHeight - 1)) - startZ;

            for (int z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    float dist = Mathf.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                    float normalizedDist = dist / pixelRadius;

                    if (normalizedDist <= 1f)
                    {
                        float factor = (1f - normalizedDist) * strength;

                        // Increase target layer
                        alphamaps[z, x, layerIndex] = Mathf.Min(1f, alphamaps[z, x, layerIndex] + factor);

                        // Normalize all layers
                        float total = 0f;
                        for (int layer = 0; layer < numLayers; layer++)
                        {
                            total += alphamaps[z, x, layer];
                        }
                        if (total > 0)
                        {
                            for (int layer = 0; layer < numLayers; layer++)
                            {
                                alphamaps[z, x, layer] /= total;
                            }
                        }
                    }
                }
            }

            data.SetAlphamaps(startX, startZ, alphamaps);

            return new SuccessResponse($"Painted texture layer {layerIndex} at ({worldPosition.x}, {worldPosition.z})",
                new
                {
                    layerIndex = layerIndex,
                    layerName = data.terrainLayers[layerIndex].name,
                    position = new { x = worldPosition.x, y = worldPosition.y, z = worldPosition.z },
                    radius = radius,
                    strength = strength
                });
        }

        /// <summary>
        /// Adds a tree instance to the terrain.
        /// </summary>
        private static object AddTree(JObject @params)
        {
            Terrain terrain = GetTargetTerrain(@params);
            if (terrain == null)
            {
                return new ErrorResponse("No terrain found. Create a terrain first or specify 'target'.");
            }

            int prototypeIndex = @params["prototypeIndex"]?.ToObject<int>() ?? 0;
            Vector3 worldPosition = VectorParsing.ParseVector3OrDefault(@params["position"]);
            float heightScale = @params["heightScale"]?.ToObject<float>() ?? 1f;
            float widthScale = @params["widthScale"]?.ToObject<float>() ?? 1f;
            float rotation = @params["rotation"]?.ToObject<float>() ?? 0f;
            Color color = VectorParsing.ParseColorOrDefault(@params["color"], Color.white);

            TerrainData data = terrain.terrainData;

            if (data.treePrototypes == null || data.treePrototypes.Length == 0)
            {
                return new ErrorResponse("No tree prototypes configured. Add tree prototypes in the Terrain Inspector first.");
            }

            if (prototypeIndex < 0 || prototypeIndex >= data.treePrototypes.Length)
            {
                return new ErrorResponse($"Invalid prototype index {prototypeIndex}. Available prototypes: 0-{data.treePrototypes.Length - 1}");
            }

            Undo.RecordObject(data, "Add Tree");

            Vector3 terrainPos = terrain.transform.position;
            Vector3 relativePos = worldPosition - terrainPos;

            TreeInstance tree = new TreeInstance
            {
                prototypeIndex = prototypeIndex,
                position = new Vector3(
                    relativePos.x / data.size.x,
                    0f, // Height will be calculated by terrain
                    relativePos.z / data.size.z
                ),
                heightScale = heightScale,
                widthScale = widthScale,
                rotation = rotation * Mathf.Deg2Rad,
                color = color,
                lightmapColor = Color.white
            };

            // Add to existing trees
            var trees = new List<TreeInstance>(data.treeInstances);
            trees.Add(tree);
            data.treeInstances = trees.ToArray();

            terrain.Flush();

            return new SuccessResponse($"Added tree at ({worldPosition.x}, {worldPosition.z})",
                new
                {
                    prototypeIndex = prototypeIndex,
                    prototypeName = data.treePrototypes[prototypeIndex].prefab?.name,
                    position = new { x = worldPosition.x, y = worldPosition.y, z = worldPosition.z },
                    heightScale = heightScale,
                    widthScale = widthScale,
                    totalTrees = data.treeInstances.Length
                });
        }

        /// <summary>
        /// Adds detail (grass) instances to the terrain.
        /// </summary>
        private static object AddDetail(JObject @params)
        {
            Terrain terrain = GetTargetTerrain(@params);
            if (terrain == null)
            {
                return new ErrorResponse("No terrain found. Create a terrain first or specify 'target'.");
            }

            int prototypeIndex = @params["prototypeIndex"]?.ToObject<int>() ?? 0;
            Vector3 worldPosition = VectorParsing.ParseVector3OrDefault(@params["position"]);
            float radius = @params["radius"]?.ToObject<float>() ?? 5f;
            int density = @params["density"]?.ToObject<int>() ?? 8;

            density = Mathf.Clamp(density, 0, 16);

            TerrainData data = terrain.terrainData;

            if (data.detailPrototypes == null || data.detailPrototypes.Length == 0)
            {
                return new ErrorResponse("No detail prototypes configured. Add detail prototypes in the Terrain Inspector first.");
            }

            if (prototypeIndex < 0 || prototypeIndex >= data.detailPrototypes.Length)
            {
                return new ErrorResponse($"Invalid prototype index {prototypeIndex}. Available prototypes: 0-{data.detailPrototypes.Length - 1}");
            }

            Undo.RecordObject(data, "Add Detail");

            Vector3 terrainPos = terrain.transform.position;
            Vector3 relativePos = worldPosition - terrainPos;

            int detailWidth = data.detailWidth;
            int detailHeight = data.detailHeight;

            float normalizedX = relativePos.x / data.size.x;
            float normalizedZ = relativePos.z / data.size.z;
            float normalizedRadius = radius / data.size.x;
            int pixelRadius = Mathf.CeilToInt(normalizedRadius * detailWidth);

            int startX = Mathf.Max(0, Mathf.RoundToInt(normalizedX * (detailWidth - 1)) - pixelRadius);
            int startZ = Mathf.Max(0, Mathf.RoundToInt(normalizedZ * (detailHeight - 1)) - pixelRadius);
            int sizeX = Mathf.Min(detailWidth - startX, pixelRadius * 2 + 1);
            int sizeZ = Mathf.Min(detailHeight - startZ, pixelRadius * 2 + 1);

            int[,] details = data.GetDetailLayer(startX, startZ, sizeX, sizeZ, prototypeIndex);

            int centerX = Mathf.RoundToInt(normalizedX * (detailWidth - 1)) - startX;
            int centerZ = Mathf.RoundToInt(normalizedZ * (detailHeight - 1)) - startZ;

            for (int z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    float dist = Mathf.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                    float normalizedDist = dist / pixelRadius;

                    if (normalizedDist <= 1f)
                    {
                        int densityValue = Mathf.RoundToInt(density * (1f - normalizedDist));
                        details[z, x] = Mathf.Max(details[z, x], densityValue);
                    }
                }
            }

            data.SetDetailLayer(startX, startZ, prototypeIndex, details);
            terrain.Flush();

            return new SuccessResponse($"Added detail at ({worldPosition.x}, {worldPosition.z})",
                new
                {
                    prototypeIndex = prototypeIndex,
                    position = new { x = worldPosition.x, y = worldPosition.y, z = worldPosition.z },
                    radius = radius,
                    density = density
                });
        }

        /// <summary>
        /// Gets terrain height at a position.
        /// </summary>
        private static object GetHeight(JObject @params)
        {
            Terrain terrain = GetTargetTerrain(@params);
            if (terrain == null)
            {
                return new ErrorResponse("No terrain found. Create a terrain first or specify 'target'.");
            }

            Vector3 worldPosition = VectorParsing.ParseVector3OrDefault(@params["position"]);

            float height = terrain.SampleHeight(worldPosition);
            float normalizedHeight = height / terrain.terrainData.size.y;

            return new SuccessResponse($"Terrain height at ({worldPosition.x}, {worldPosition.z}): {height}",
                new
                {
                    position = new { x = worldPosition.x, y = worldPosition.y, z = worldPosition.z },
                    height = height,
                    normalizedHeight = normalizedHeight,
                    worldY = terrain.transform.position.y + height
                });
        }

        /// <summary>
        /// Gets terrain information.
        /// </summary>
        private static object GetTerrainInfo(JObject @params)
        {
            Terrain terrain = GetTargetTerrain(@params);
            if (terrain == null)
            {
                return new ErrorResponse("No terrain found. Create a terrain first or specify 'target'.");
            }

            TerrainData data = terrain.terrainData;

            return new SuccessResponse($"Terrain info for '{terrain.name}'",
                new
                {
                    name = terrain.name,
                    instanceId = terrain.gameObject.GetInstanceID(),
                    position = new { x = terrain.transform.position.x, y = terrain.transform.position.y, z = terrain.transform.position.z },
                    size = new { width = data.size.x, height = data.size.y, length = data.size.z },
                    heightmapResolution = data.heightmapResolution,
                    alphamapResolution = data.alphamapResolution,
                    detailResolution = data.detailResolution,
                    layerCount = data.terrainLayers?.Length ?? 0,
                    treePrototypeCount = data.treePrototypes?.Length ?? 0,
                    treeInstanceCount = data.treeInstances?.Length ?? 0,
                    detailPrototypeCount = data.detailPrototypes?.Length ?? 0
                });
        }

        #region Helper Methods

        private static Terrain GetTargetTerrain(JObject @params)
        {
            string target = @params["target"]?.ToString();

            if (!string.IsNullOrEmpty(target))
            {
                // Try as instance ID
                if (int.TryParse(target, out int instanceId))
                {
                    var obj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                    if (obj != null)
                        return obj.GetComponent<Terrain>();
                }

                // Try as name
                var found = GameObject.Find(target);
                if (found != null)
                    return found.GetComponent<Terrain>();
            }

            // Return active terrain or first terrain in scene
            return Terrain.activeTerrain ?? UnityEngine.Object.FindFirstObjectByType<Terrain>();
        }

        #endregion
    }
}
