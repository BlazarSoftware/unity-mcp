using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Handles physics operations including raycasting, collision queries, rigidbody configuration, and physics settings.
    /// </summary>
    [McpForUnityTool("manage_physics", AutoRegister = false)]
    public static class ManagePhysics
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required");
            }

            try
            {
                switch (action)
                {
                    case "ping":
                        return new SuccessResponse("pong", new { tool = "manage_physics" });

                    case "raycast":
                        return Raycast(@params);

                    case "overlap_sphere":
                        return OverlapSphere(@params);

                    case "overlap_box":
                        return OverlapBox(@params);

                    case "set_rigidbody":
                        return SetRigidbody(@params);

                    case "add_force":
                        return AddForce(@params);

                    case "add_torque":
                        return AddTorque(@params);

                    case "configure_collider":
                        return ConfigureCollider(@params);

                    case "get_physics_settings":
                        return GetPhysicsSettings();

                    case "set_physics_settings":
                        return SetPhysicsSettings(@params);

                    case "get_rigidbody_info":
                        return GetRigidbodyInfo(@params);

                    case "get_collider_info":
                        return GetColliderInfo(@params);

                    case "check_sphere":
                        return CheckSphere(@params);

                    case "linecast":
                        return Linecast(@params);

                    default:
                        return new ErrorResponse($"Unknown action: {action}. Valid actions: raycast, overlap_sphere, overlap_box, set_rigidbody, add_force, add_torque, configure_collider, get_physics_settings, set_physics_settings, get_rigidbody_info, get_collider_info, check_sphere, linecast");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        private static Vector3 ParseVector3(JToken token, Vector3 defaultValue = default)
        {
            if (token == null) return defaultValue;

            if (token is JArray arr && arr.Count >= 3)
            {
                return new Vector3(
                    arr[0].ToObject<float>(),
                    arr[1].ToObject<float>(),
                    arr[2].ToObject<float>()
                );
            }

            if (token is JObject obj)
            {
                return new Vector3(
                    obj["x"]?.ToObject<float>() ?? defaultValue.x,
                    obj["y"]?.ToObject<float>() ?? defaultValue.y,
                    obj["z"]?.ToObject<float>() ?? defaultValue.z
                );
            }

            return defaultValue;
        }

        private static object Raycast(JObject @params)
        {
            Vector3 origin = ParseVector3(@params["origin"]);
            Vector3 direction = ParseVector3(@params["direction"], Vector3.forward);
            float maxDistance = @params["maxDistance"]?.ToObject<float>() ?? Mathf.Infinity;
            int layerMask = @params["layerMask"]?.ToObject<int>() ?? Physics.DefaultRaycastLayers;
            bool hitAll = @params["hitAll"]?.ToObject<bool>() ?? false;

            if (direction == Vector3.zero)
            {
                return new ErrorResponse("direction cannot be zero");
            }

            direction = direction.normalized;

            if (hitAll)
            {
                RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, layerMask);
                var results = hits.OrderBy(h => h.distance).Select(h => new
                {
                    gameObject = h.collider.gameObject.name,
                    instanceID = h.collider.gameObject.GetInstanceID(),
                    point = new { x = h.point.x, y = h.point.y, z = h.point.z },
                    normal = new { x = h.normal.x, y = h.normal.y, z = h.normal.z },
                    distance = h.distance,
                    colliderName = h.collider.name
                }).ToList();

                return new SuccessResponse($"Raycast hit {hits.Length} objects", new
                {
                    origin = new { x = origin.x, y = origin.y, z = origin.z },
                    direction = new { x = direction.x, y = direction.y, z = direction.z },
                    maxDistance = maxDistance,
                    hitCount = hits.Length,
                    hits = results
                });
            }
            else
            {
                if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
                {
                    return new SuccessResponse($"Raycast hit '{hit.collider.gameObject.name}'", new
                    {
                        hit = true,
                        gameObject = hit.collider.gameObject.name,
                        instanceID = hit.collider.gameObject.GetInstanceID(),
                        point = new { x = hit.point.x, y = hit.point.y, z = hit.point.z },
                        normal = new { x = hit.normal.x, y = hit.normal.y, z = hit.normal.z },
                        distance = hit.distance,
                        colliderName = hit.collider.name
                    });
                }
                else
                {
                    return new SuccessResponse("Raycast did not hit anything", new
                    {
                        hit = false,
                        origin = new { x = origin.x, y = origin.y, z = origin.z },
                        direction = new { x = direction.x, y = direction.y, z = direction.z },
                        maxDistance = maxDistance
                    });
                }
            }
        }

        private static object OverlapSphere(JObject @params)
        {
            Vector3 center = ParseVector3(@params["center"]);
            float radius = @params["radius"]?.ToObject<float>() ?? 1f;
            int layerMask = @params["layerMask"]?.ToObject<int>() ?? Physics.AllLayers;
            int maxResults = @params["maxResults"]?.ToObject<int>() ?? 100;

            Collider[] colliders = new Collider[maxResults];
            int count = Physics.OverlapSphereNonAlloc(center, radius, colliders, layerMask);

            var results = new List<object>();
            for (int i = 0; i < count; i++)
            {
                var c = colliders[i];
                results.Add(new
                {
                    gameObject = c.gameObject.name,
                    instanceID = c.gameObject.GetInstanceID(),
                    colliderType = c.GetType().Name,
                    isTrigger = c.isTrigger
                });
            }

            return new SuccessResponse($"Found {count} colliders in sphere", new
            {
                center = new { x = center.x, y = center.y, z = center.z },
                radius = radius,
                count = count,
                colliders = results
            });
        }

        private static object OverlapBox(JObject @params)
        {
            Vector3 center = ParseVector3(@params["center"]);
            Vector3 halfExtents = ParseVector3(@params["halfExtents"], Vector3.one * 0.5f);
            Vector3 orientation = ParseVector3(@params["orientation"]);
            Quaternion rotation = Quaternion.Euler(orientation);
            int layerMask = @params["layerMask"]?.ToObject<int>() ?? Physics.AllLayers;
            int maxResults = @params["maxResults"]?.ToObject<int>() ?? 100;

            Collider[] colliders = new Collider[maxResults];
            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, colliders, rotation, layerMask);

            var results = new List<object>();
            for (int i = 0; i < count; i++)
            {
                var c = colliders[i];
                results.Add(new
                {
                    gameObject = c.gameObject.name,
                    instanceID = c.gameObject.GetInstanceID(),
                    colliderType = c.GetType().Name,
                    isTrigger = c.isTrigger
                });
            }

            return new SuccessResponse($"Found {count} colliders in box", new
            {
                center = new { x = center.x, y = center.y, z = center.z },
                halfExtents = new { x = halfExtents.x, y = halfExtents.y, z = halfExtents.z },
                count = count,
                colliders = results
            });
        }

        private static object SetRigidbody(JObject @params)
        {
            string target = @params["target"]?.ToString();
            if (string.IsNullOrEmpty(target))
            {
                return new ErrorResponse("target is required");
            }

            var goInstruction = new JObject { ["find"] = target };
            string searchMethod = @params["searchMethod"]?.ToString();
            if (!string.IsNullOrEmpty(searchMethod))
            {
                goInstruction["method"] = searchMethod;
            }

            GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
            if (go == null)
            {
                return new ErrorResponse($"GameObject not found: {target}");
            }

            Rigidbody rb = go.GetComponent<Rigidbody>();
            bool created = false;

            if (rb == null)
            {
                if (@params["createIfMissing"]?.ToObject<bool>() ?? true)
                {
                    Undo.RecordObject(go, "Add Rigidbody");
                    rb = Undo.AddComponent<Rigidbody>(go);
                    created = true;
                }
                else
                {
                    return new ErrorResponse($"Rigidbody not found on '{go.name}'. Set createIfMissing=true to add one.");
                }
            }

            Undo.RecordObject(rb, "Set Rigidbody Properties");

            var changes = new List<string>();

            if (@params["mass"] != null)
            {
                rb.mass = Mathf.Max(0.0001f, @params["mass"].ToObject<float>());
                changes.Add($"mass={rb.mass}");
            }

            if (@params["drag"] != null)
            {
                rb.linearDamping = Mathf.Max(0f, @params["drag"].ToObject<float>());
                changes.Add($"drag={rb.linearDamping}");
            }

            if (@params["angularDrag"] != null)
            {
                rb.angularDamping = Mathf.Max(0f, @params["angularDrag"].ToObject<float>());
                changes.Add($"angularDrag={rb.angularDamping}");
            }

            if (@params["useGravity"] != null)
            {
                rb.useGravity = @params["useGravity"].ToObject<bool>();
                changes.Add($"useGravity={rb.useGravity}");
            }

            if (@params["isKinematic"] != null)
            {
                rb.isKinematic = @params["isKinematic"].ToObject<bool>();
                changes.Add($"isKinematic={rb.isKinematic}");
            }

            if (@params["interpolation"] != null)
            {
                string interp = @params["interpolation"].ToString().ToLowerInvariant();
                switch (interp)
                {
                    case "none":
                        rb.interpolation = RigidbodyInterpolation.None;
                        break;
                    case "interpolate":
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                        break;
                    case "extrapolate":
                        rb.interpolation = RigidbodyInterpolation.Extrapolate;
                        break;
                }
                changes.Add($"interpolation={rb.interpolation}");
            }

            if (@params["collisionDetection"] != null)
            {
                string cd = @params["collisionDetection"].ToString().ToLowerInvariant();
                switch (cd)
                {
                    case "discrete":
                        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                        break;
                    case "continuous":
                        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                        break;
                    case "continuousdynamic":
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                        break;
                    case "continuousspeculative":
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                        break;
                }
                changes.Add($"collisionDetection={rb.collisionDetectionMode}");
            }

            if (@params["constraints"] != null)
            {
                int constraintFlags = @params["constraints"].ToObject<int>();
                rb.constraints = (RigidbodyConstraints)constraintFlags;
                changes.Add($"constraints={rb.constraints}");
            }

            if (@params["freezePosition"] != null)
            {
                JToken fp = @params["freezePosition"];
                RigidbodyConstraints c = rb.constraints;

                // Clear position constraints first
                c &= ~(RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ);

                if (fp is JObject fpObj)
                {
                    if (fpObj["x"]?.ToObject<bool>() == true) c |= RigidbodyConstraints.FreezePositionX;
                    if (fpObj["y"]?.ToObject<bool>() == true) c |= RigidbodyConstraints.FreezePositionY;
                    if (fpObj["z"]?.ToObject<bool>() == true) c |= RigidbodyConstraints.FreezePositionZ;
                }
                else if (fp.ToObject<bool>())
                {
                    c |= RigidbodyConstraints.FreezePosition;
                }

                rb.constraints = c;
                changes.Add($"freezePosition applied");
            }

            if (@params["freezeRotation"] != null)
            {
                JToken fr = @params["freezeRotation"];
                RigidbodyConstraints c = rb.constraints;

                // Clear rotation constraints first
                c &= ~(RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ);

                if (fr is JObject frObj)
                {
                    if (frObj["x"]?.ToObject<bool>() == true) c |= RigidbodyConstraints.FreezeRotationX;
                    if (frObj["y"]?.ToObject<bool>() == true) c |= RigidbodyConstraints.FreezeRotationY;
                    if (frObj["z"]?.ToObject<bool>() == true) c |= RigidbodyConstraints.FreezeRotationZ;
                }
                else if (fr.ToObject<bool>())
                {
                    c |= RigidbodyConstraints.FreezeRotation;
                }

                rb.constraints = c;
                changes.Add($"freezeRotation applied");
            }

            EditorUtility.SetDirty(rb);

            string message = created ? $"Created Rigidbody on '{go.name}'" : $"Updated Rigidbody on '{go.name}'";
            if (changes.Count > 0)
            {
                message += $": {string.Join(", ", changes)}";
            }

            return new SuccessResponse(message, new
            {
                gameObject = go.name,
                created = created,
                changes = changes
            });
        }

        private static object AddForce(JObject @params)
        {
            string target = @params["target"]?.ToString();
            if (string.IsNullOrEmpty(target))
            {
                return new ErrorResponse("target is required");
            }

            var goInstruction = new JObject { ["find"] = target };
            GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
            if (go == null)
            {
                return new ErrorResponse($"GameObject not found: {target}");
            }

            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null)
            {
                return new ErrorResponse($"Rigidbody not found on '{go.name}'");
            }

            Vector3 force = ParseVector3(@params["force"]);
            string modeStr = @params["mode"]?.ToString()?.ToLowerInvariant() ?? "force";

            ForceMode mode = ForceMode.Force;
            switch (modeStr)
            {
                case "force":
                    mode = ForceMode.Force;
                    break;
                case "impulse":
                    mode = ForceMode.Impulse;
                    break;
                case "velocitychange":
                    mode = ForceMode.VelocityChange;
                    break;
                case "acceleration":
                    mode = ForceMode.Acceleration;
                    break;
            }

            rb.AddForce(force, mode);

            return new SuccessResponse($"Added force to '{go.name}'", new
            {
                gameObject = go.name,
                force = new { x = force.x, y = force.y, z = force.z },
                mode = mode.ToString()
            });
        }

        private static object AddTorque(JObject @params)
        {
            string target = @params["target"]?.ToString();
            if (string.IsNullOrEmpty(target))
            {
                return new ErrorResponse("target is required");
            }

            var goInstruction = new JObject { ["find"] = target };
            GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
            if (go == null)
            {
                return new ErrorResponse($"GameObject not found: {target}");
            }

            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null)
            {
                return new ErrorResponse($"Rigidbody not found on '{go.name}'");
            }

            Vector3 torque = ParseVector3(@params["torque"]);
            string modeStr = @params["mode"]?.ToString()?.ToLowerInvariant() ?? "force";

            ForceMode mode = ForceMode.Force;
            switch (modeStr)
            {
                case "force":
                    mode = ForceMode.Force;
                    break;
                case "impulse":
                    mode = ForceMode.Impulse;
                    break;
                case "velocitychange":
                    mode = ForceMode.VelocityChange;
                    break;
                case "acceleration":
                    mode = ForceMode.Acceleration;
                    break;
            }

            rb.AddTorque(torque, mode);

            return new SuccessResponse($"Added torque to '{go.name}'", new
            {
                gameObject = go.name,
                torque = new { x = torque.x, y = torque.y, z = torque.z },
                mode = mode.ToString()
            });
        }

        private static object ConfigureCollider(JObject @params)
        {
            string target = @params["target"]?.ToString();
            if (string.IsNullOrEmpty(target))
            {
                return new ErrorResponse("target is required");
            }

            var goInstruction = new JObject { ["find"] = target };
            GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
            if (go == null)
            {
                return new ErrorResponse($"GameObject not found: {target}");
            }

            Collider collider = go.GetComponent<Collider>();
            if (collider == null)
            {
                return new ErrorResponse($"Collider not found on '{go.name}'");
            }

            Undo.RecordObject(collider, "Configure Collider");

            var changes = new List<string>();

            if (@params["isTrigger"] != null)
            {
                collider.isTrigger = @params["isTrigger"].ToObject<bool>();
                changes.Add($"isTrigger={collider.isTrigger}");
            }

            if (@params["enabled"] != null)
            {
                collider.enabled = @params["enabled"].ToObject<bool>();
                changes.Add($"enabled={collider.enabled}");
            }

            if (@params["material"] != null)
            {
                string matPath = @params["material"].ToString();
                PhysicsMaterial mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(matPath);
                if (mat != null)
                {
                    collider.sharedMaterial = mat;
                    changes.Add($"material={mat.name}");
                }
            }

            // Type-specific properties
            if (collider is BoxCollider box)
            {
                if (@params["center"] != null)
                {
                    box.center = ParseVector3(@params["center"]);
                    changes.Add($"center set");
                }
                if (@params["size"] != null)
                {
                    box.size = ParseVector3(@params["size"], Vector3.one);
                    changes.Add($"size set");
                }
            }
            else if (collider is SphereCollider sphere)
            {
                if (@params["center"] != null)
                {
                    sphere.center = ParseVector3(@params["center"]);
                    changes.Add($"center set");
                }
                if (@params["radius"] != null)
                {
                    sphere.radius = @params["radius"].ToObject<float>();
                    changes.Add($"radius={sphere.radius}");
                }
            }
            else if (collider is CapsuleCollider capsule)
            {
                if (@params["center"] != null)
                {
                    capsule.center = ParseVector3(@params["center"]);
                    changes.Add($"center set");
                }
                if (@params["radius"] != null)
                {
                    capsule.radius = @params["radius"].ToObject<float>();
                    changes.Add($"radius={capsule.radius}");
                }
                if (@params["height"] != null)
                {
                    capsule.height = @params["height"].ToObject<float>();
                    changes.Add($"height={capsule.height}");
                }
                if (@params["direction"] != null)
                {
                    capsule.direction = @params["direction"].ToObject<int>();
                    changes.Add($"direction={capsule.direction}");
                }
            }

            EditorUtility.SetDirty(collider);

            return new SuccessResponse($"Configured collider on '{go.name}'", new
            {
                gameObject = go.name,
                colliderType = collider.GetType().Name,
                changes = changes
            });
        }

        private static object GetPhysicsSettings()
        {
            return new SuccessResponse("Retrieved physics settings", new
            {
                gravity = new { x = Physics.gravity.x, y = Physics.gravity.y, z = Physics.gravity.z },
                defaultSolverIterations = Physics.defaultSolverIterations,
                defaultSolverVelocityIterations = Physics.defaultSolverVelocityIterations,
                bounceThreshold = Physics.bounceThreshold,
                sleepThreshold = Physics.sleepThreshold,
                defaultContactOffset = Physics.defaultContactOffset,
                autoSimulation = Physics.simulationMode != SimulationMode.Script,
                simulationMode = Physics.simulationMode.ToString(),
                queriesHitBackfaces = Physics.queriesHitBackfaces,
                queriesHitTriggers = Physics.queriesHitTriggers
            });
        }

        private static object SetPhysicsSettings(JObject @params)
        {
            var changes = new List<string>();

            if (@params["gravity"] != null)
            {
                Physics.gravity = ParseVector3(@params["gravity"], Physics.gravity);
                changes.Add($"gravity={Physics.gravity}");
            }

            if (@params["defaultSolverIterations"] != null)
            {
                Physics.defaultSolverIterations = @params["defaultSolverIterations"].ToObject<int>();
                changes.Add($"defaultSolverIterations={Physics.defaultSolverIterations}");
            }

            if (@params["defaultSolverVelocityIterations"] != null)
            {
                Physics.defaultSolverVelocityIterations = @params["defaultSolverVelocityIterations"].ToObject<int>();
                changes.Add($"defaultSolverVelocityIterations={Physics.defaultSolverVelocityIterations}");
            }

            if (@params["bounceThreshold"] != null)
            {
                Physics.bounceThreshold = @params["bounceThreshold"].ToObject<float>();
                changes.Add($"bounceThreshold={Physics.bounceThreshold}");
            }

            if (@params["sleepThreshold"] != null)
            {
                Physics.sleepThreshold = @params["sleepThreshold"].ToObject<float>();
                changes.Add($"sleepThreshold={Physics.sleepThreshold}");
            }

            if (@params["defaultContactOffset"] != null)
            {
                Physics.defaultContactOffset = @params["defaultContactOffset"].ToObject<float>();
                changes.Add($"defaultContactOffset={Physics.defaultContactOffset}");
            }

            if (@params["simulationMode"] != null)
            {
                string mode = @params["simulationMode"].ToString().ToLowerInvariant();
                switch (mode)
                {
                    case "fixedupdate":
                        Physics.simulationMode = SimulationMode.FixedUpdate;
                        break;
                    case "update":
                        Physics.simulationMode = SimulationMode.Update;
                        break;
                    case "script":
                        Physics.simulationMode = SimulationMode.Script;
                        break;
                }
                changes.Add($"simulationMode={Physics.simulationMode}");
            }

            if (@params["queriesHitBackfaces"] != null)
            {
                Physics.queriesHitBackfaces = @params["queriesHitBackfaces"].ToObject<bool>();
                changes.Add($"queriesHitBackfaces={Physics.queriesHitBackfaces}");
            }

            if (@params["queriesHitTriggers"] != null)
            {
                Physics.queriesHitTriggers = @params["queriesHitTriggers"].ToObject<bool>();
                changes.Add($"queriesHitTriggers={Physics.queriesHitTriggers}");
            }

            if (changes.Count == 0)
            {
                return new SuccessResponse("No physics settings changed");
            }

            return new SuccessResponse($"Updated physics settings: {string.Join(", ", changes)}", new
            {
                changes = changes
            });
        }

        private static object GetRigidbodyInfo(JObject @params)
        {
            string target = @params["target"]?.ToString();
            if (string.IsNullOrEmpty(target))
            {
                return new ErrorResponse("target is required");
            }

            var goInstruction = new JObject { ["find"] = target };
            GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
            if (go == null)
            {
                return new ErrorResponse($"GameObject not found: {target}");
            }

            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null)
            {
                return new ErrorResponse($"Rigidbody not found on '{go.name}'");
            }

            return new SuccessResponse($"Retrieved Rigidbody info for '{go.name}'", new
            {
                gameObject = go.name,
                mass = rb.mass,
                drag = rb.linearDamping,
                angularDrag = rb.angularDamping,
                useGravity = rb.useGravity,
                isKinematic = rb.isKinematic,
                interpolation = rb.interpolation.ToString(),
                collisionDetectionMode = rb.collisionDetectionMode.ToString(),
                constraints = rb.constraints.ToString(),
                velocity = new { x = rb.linearVelocity.x, y = rb.linearVelocity.y, z = rb.linearVelocity.z },
                angularVelocity = new { x = rb.angularVelocity.x, y = rb.angularVelocity.y, z = rb.angularVelocity.z },
                centerOfMass = new { x = rb.centerOfMass.x, y = rb.centerOfMass.y, z = rb.centerOfMass.z },
                inertiaTensor = new { x = rb.inertiaTensor.x, y = rb.inertiaTensor.y, z = rb.inertiaTensor.z }
            });
        }

        private static object GetColliderInfo(JObject @params)
        {
            string target = @params["target"]?.ToString();
            if (string.IsNullOrEmpty(target))
            {
                return new ErrorResponse("target is required");
            }

            var goInstruction = new JObject { ["find"] = target };
            GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
            if (go == null)
            {
                return new ErrorResponse($"GameObject not found: {target}");
            }

            Collider[] colliders = go.GetComponents<Collider>();
            if (colliders.Length == 0)
            {
                return new ErrorResponse($"No colliders found on '{go.name}'");
            }

            var results = new List<object>();
            foreach (var c in colliders)
            {
                var info = new Dictionary<string, object>
                {
                    { "type", c.GetType().Name },
                    { "enabled", c.enabled },
                    { "isTrigger", c.isTrigger },
                    { "bounds", new {
                        center = new { x = c.bounds.center.x, y = c.bounds.center.y, z = c.bounds.center.z },
                        size = new { x = c.bounds.size.x, y = c.bounds.size.y, z = c.bounds.size.z }
                    }},
                    { "material", c.sharedMaterial?.name }
                };

                if (c is BoxCollider box)
                {
                    info["center"] = new { x = box.center.x, y = box.center.y, z = box.center.z };
                    info["size"] = new { x = box.size.x, y = box.size.y, z = box.size.z };
                }
                else if (c is SphereCollider sphere)
                {
                    info["center"] = new { x = sphere.center.x, y = sphere.center.y, z = sphere.center.z };
                    info["radius"] = sphere.radius;
                }
                else if (c is CapsuleCollider capsule)
                {
                    info["center"] = new { x = capsule.center.x, y = capsule.center.y, z = capsule.center.z };
                    info["radius"] = capsule.radius;
                    info["height"] = capsule.height;
                    info["direction"] = capsule.direction;
                }
                else if (c is MeshCollider mesh)
                {
                    info["convex"] = mesh.convex;
                    info["sharedMesh"] = mesh.sharedMesh?.name;
                }

                results.Add(info);
            }

            return new SuccessResponse($"Retrieved {colliders.Length} collider(s) from '{go.name}'", new
            {
                gameObject = go.name,
                colliderCount = colliders.Length,
                colliders = results
            });
        }

        private static object CheckSphere(JObject @params)
        {
            Vector3 position = ParseVector3(@params["position"]);
            float radius = @params["radius"]?.ToObject<float>() ?? 1f;
            int layerMask = @params["layerMask"]?.ToObject<int>() ?? Physics.AllLayers;

            bool hit = Physics.CheckSphere(position, radius, layerMask);

            return new SuccessResponse(hit ? "Sphere overlaps colliders" : "Sphere is clear", new
            {
                position = new { x = position.x, y = position.y, z = position.z },
                radius = radius,
                hit = hit
            });
        }

        private static object Linecast(JObject @params)
        {
            Vector3 start = ParseVector3(@params["start"]);
            Vector3 end = ParseVector3(@params["end"]);
            int layerMask = @params["layerMask"]?.ToObject<int>() ?? Physics.DefaultRaycastLayers;

            if (Physics.Linecast(start, end, out RaycastHit hit, layerMask))
            {
                return new SuccessResponse($"Linecast hit '{hit.collider.gameObject.name}'", new
                {
                    hit = true,
                    gameObject = hit.collider.gameObject.name,
                    instanceID = hit.collider.gameObject.GetInstanceID(),
                    point = new { x = hit.point.x, y = hit.point.y, z = hit.point.z },
                    normal = new { x = hit.normal.x, y = hit.normal.y, z = hit.normal.z },
                    distance = hit.distance
                });
            }
            else
            {
                return new SuccessResponse("Linecast did not hit anything", new
                {
                    hit = false,
                    start = new { x = start.x, y = start.y, z = start.z },
                    end = new { x = end.x, y = end.y, z = end.z }
                });
            }
        }
    }
}
