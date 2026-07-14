using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Raptor;
using Raptor.Attributes;

[RaptorModule("enemy")]
public unsafe class RaptorStressTest : MonoBehaviour
{
    public enum ExecutionMode
    {
        RaptorVM_FFI,
        RaptorVM_PropertyMapped,
        NativeCSharp
    }

    [Header("Benchmark Settings")]
    [Tooltip("Number of instances to spawn.")]
    public int spawnCount = 5000;
    
    [Tooltip("Toggle between VM and Native C# execution.")]
    public ExecutionMode mode = ExecutionMode.RaptorVM_PropertyMapped;

    [Tooltip("Disable mesh renderers to isolate CPU VM speed from GPU rendering.")]
    public bool renderMeshes = true;

    [Header("Visuals")]
    public Material cubeMaterial;

    private struct Agent
    {
        public GameObject GameObject;
        public Transform Transform;
        public VirtualMachine VM;
        public double X;
        public double Z;
        public double SpeedOffset;
    }

    private List<Agent> _agents = new();
    private ScriptEngine _engine;
    private VMChunk _ffiHeavyChunk;
    private VMChunk _propertyMappedChunk;
    private FFIHostTable _ffiTable;
    private VirtualMachine _sharedVM;
    
    // Shared FFI communication state
    private float _currentTime;
    private double _tempX;
    private double _tempZ;
    private double _tempOffset;
    private double _newX;
    private double _newZ;

    // Metrics tracking
    private float _fps;
    private float _fpsTimer;
    private int _fpsCount;
    private double _lastBatchExecutionTimeMs;

    private void Start()
    {
        // 1. Initialize Raptor Engine and FFI
        _engine = new ScriptEngine();
        _ffiTable = new FFIHostTable();
        _ffiTable.RegisterModule(this);
        
        // Add basic math functions
        _ffiTable.Register("math.cos", 100, (ref VMState s) => {
            double val = s.RegPtr[0]; // Align with standard parameter offset starting from index 0
            s.RegPtr[0] = Math.Cos(val);
        });
        _ffiTable.Register("math.sin", 101, (ref VMState s) => {
            double val = s.RegPtr[0]; // Align with standard parameter offset starting from index 0
            s.RegPtr[0] = Math.Sin(val);
        });

        _engine.RegisterHostTable(_ffiTable);

        // 2. Compile the old FFI-heavy script
        string ffiHeavyScript = @"
var x = enemy.getX();
var z = enemy.getZ();
var time = enemy.getTime();
var offset = enemy.getOffset();

// Compute circular orbital motion
var angle = time * 0.8 + offset;
var newX = math.cos(angle) * 15.0;
var newZ = math.sin(angle) * 15.0;

enemy.setPosition(newX, newZ);
";
        string ffiHeavyRasm = Raptor.Compiler.RaptorScriptCompiler.Compile(ffiHeavyScript);
        _ffiHeavyChunk = _engine.Compile(ffiHeavyRasm);

        // 3. Compile the new Property-Mapped script
        string propertyMappedScript = @"
var angle = enemy.time * 0.8 + enemy.offset;
enemy.x = math.cos(angle) * 15.0;
enemy.z = math.sin(angle) * 15.0;
";
        var propertyMappings = new Dictionary<string, int>
        {
            { "enemy.x", 1 },
            { "enemy.z", 2 },
            { "enemy.time", 3 },
            { "enemy.offset", 4 }
        };
        string mappedRasm = Raptor.Compiler.RaptorScriptCompiler.Compile(propertyMappedScript, propertyMappings);
        _propertyMappedChunk = _engine.Compile(mappedRasm);

        // 4. Initialize the shared VM for the property mapped mode
        _sharedVM = new VirtualMachine();
        _sharedVM.RegisterHostTable(_ffiTable);
        _sharedVM.LoadProgram(_propertyMappedChunk);

        // 5. Spawn the agents
        SpawnAgents();
    }

    private void SpawnAgents()
    {
        // Clean up old agents if any
        foreach (var agent in _agents)
        {
            if (agent.GameObject != null) Destroy(agent.GameObject);
        }
        _agents.Clear();

        // Create container
        GameObject container = new GameObject("SpawnedCubes");
        container.transform.SetParent(transform);

        // Grid spawning
        int side = Mathf.CeilToInt(Mathf.Sqrt(spawnCount));
        float spacing = 1.5f;

        for (int i = 0; i < spawnCount; i++)
        {
            int row = i / side;
            int col = i % side;

            float posX = (col - (side / 2f)) * spacing;
            float posZ = (row - (side / 2f)) * spacing;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(container.transform);
            cube.transform.position = new Vector3(posX, 0, posZ);
            
            // Assign material
            if (cubeMaterial != null)
            {
                cube.GetComponent<MeshRenderer>().sharedMaterial = cubeMaterial;
            }

            // Create VM instance for this agent
            var vm = new VirtualMachine();
            vm.RegisterHostTable(_ffiTable);
            vm.LoadProgram(_ffiHeavyChunk);

            _agents.Add(new Agent
            {
                GameObject = cube,
                Transform = cube.transform,
                VM = vm,
                X = posX,
                Z = posZ,
                SpeedOffset = UnityEngine.Random.Range(0f, 6.28f) // random orbit phase offset
            });
        }

        UpdateRenderers();
    }

    private void Update()
    {
        _currentTime = Time.time;

        // FPS calculation
        _fpsTimer += Time.deltaTime;
        _fpsCount++;
        if (_fpsTimer >= 0.5f)
        {
            _fps = _fpsCount / _fpsTimer;
            _fpsTimer = 0;
            _fpsCount = 0;
        }

        // Handle dynamically changing settings
        if (_agents.Count != spawnCount)
        {
            SpawnAgents();
        }
        UpdateRenderers();

        // 4. Run the benchmark batch
        Stopwatch sw = Stopwatch.StartNew();

        if (mode == ExecutionMode.RaptorVM_FFI)
        {
            for (int i = 0; i < _agents.Count; i++)
            {
                var agent = _agents[i];
                
                // Set FFI communication state variables
                _tempX = agent.X;
                _tempZ = agent.Z;
                _tempOffset = agent.SpeedOffset;
                
                // Execute VM
                agent.VM.RunFast();

                // Apply new coordinates calculated by the script
                agent.X = _newX;
                agent.Z = _newZ;
            }
            sw.Stop();

            // Apply transforms outside of the timed VM block
            for (int i = 0; i < _agents.Count; i++)
            {
                _agents[i].Transform.position = new Vector3((float)_agents[i].X, 0, (float)_agents[i].Z);
            }
        }
        else if (mode == ExecutionMode.RaptorVM_PropertyMapped)
        {
            for (int i = 0; i < _agents.Count; i++)
            {
                var agent = _agents[i];
                
                // Write inputs directly to registers
                _sharedVM.SetRegister(1, agent.X);
                _sharedVM.SetRegister(2, agent.Z);
                _sharedVM.SetRegister(3, _currentTime);
                _sharedVM.SetRegister(4, agent.SpeedOffset);
                
                // Execute on the shared VM
                _sharedVM.RunFast();

                // Read outputs directly from registers
                agent.X = _sharedVM.GetRegister(1);
                agent.Z = _sharedVM.GetRegister(2);
            }
            sw.Stop();

            // Apply transforms outside of the timed VM block
            for (int i = 0; i < _agents.Count; i++)
            {
                _agents[i].Transform.position = new Vector3((float)_agents[i].X, 0, (float)_agents[i].Z);
            }
        }
        else
        {
            // Baseline Native C# equivalent logic
            for (int i = 0; i < _agents.Count; i++)
            {
                var agent = _agents[i];
                double angle = _currentTime * 0.8 + agent.SpeedOffset;
                double newX = Math.Cos(angle) * 15.0;
                double newZ = Math.Sin(angle) * 15.0;
                agent.X = newX;
                agent.Z = newZ;
            }
            sw.Stop();

            // Apply transforms outside of the timed C# block
            for (int i = 0; i < _agents.Count; i++)
            {
                _agents[i].Transform.position = new Vector3((float)_agents[i].X, 0, (float)_agents[i].Z);
            }
        }
        
        // Calculate total execution time in milliseconds
        _lastBatchExecutionTimeMs = (sw.ElapsedTicks / (double)Stopwatch.Frequency) * 1000.0;
    }

    private void UpdateRenderers()
    {
        for (int i = 0; i < _agents.Count; i++)
        {
            var mr = _agents[i].GameObject.GetComponent<MeshRenderer>();
            if (mr != null && mr.enabled != renderMeshes)
            {
                mr.enabled = renderMeshes;
            }
        }
    }

    // FFI Callbacks
    [RaptorMethod("getX")]
    public double GetAgentX() => _tempX;

    [RaptorMethod("getZ")]
    public double GetAgentZ() => _tempZ;

    [RaptorMethod("getTime")]
    public double GetTime() => _currentTime;

    [RaptorMethod("getOffset")]
    public double GetOffset() => _tempOffset;

    [RaptorMethod("setPosition")]
    public void SetAgentPosition(double x, double z)
    {
        _newX = x;
        _newZ = z;
    }

    private void OnGUI()
    {
        // Render a premium, semi-transparent dashboard overlay
        GUI.Box(new Rect(10, 10, 330, 220), "Raptor VM Stress Test Benchmark");

        GUILayout.BeginArea(new Rect(20, 40, 310, 180));

        GUILayout.Label($"Active Instances: {spawnCount}");
        GUILayout.Label($"Execution Mode: <b>{mode}</b>");
        GUILayout.Label($"FPS: {_fps:F1}");
        
        GUILayout.Space(5);
        
        GUILayout.Label($"Total Frame Batch Time: <b>{_lastBatchExecutionTimeMs:F3} ms</b>");
        
        double avgUsPerInstance = (_lastBatchExecutionTimeMs * 1000.0) / spawnCount;
        GUILayout.Label($"Avg Time Per Script: <b>{avgUsPerInstance:F3} us (microseconds)</b>");

        GUILayout.Space(10);

        // Control buttons
        string nextModeName = "";
        ExecutionMode nextMode = ExecutionMode.RaptorVM_PropertyMapped;
        if (mode == ExecutionMode.RaptorVM_PropertyMapped)
        {
            nextModeName = "Native C#";
            nextMode = ExecutionMode.NativeCSharp;
        }
        else if (mode == ExecutionMode.NativeCSharp)
        {
            nextModeName = "RaptorVM (FFI-Heavy)";
            nextMode = ExecutionMode.RaptorVM_FFI;
        }
        else if (mode == ExecutionMode.RaptorVM_FFI)
        {
            nextModeName = "RaptorVM (Property-Mapped)";
            nextMode = ExecutionMode.RaptorVM_PropertyMapped;
        }
        if (GUILayout.Button($"Switch to {nextModeName}"))
        {
            mode = nextMode;
        }

        if (GUILayout.Button($"Toggle Mesh Rendering (Renderer: {(renderMeshes ? "ON" : "OFF")})"))
        {
            renderMeshes = !renderMeshes;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("1k Cubes")) spawnCount = 1000;
        if (GUILayout.Button("5k Cubes")) spawnCount = 5000;
        if (GUILayout.Button("10k Cubes")) spawnCount = 10000;
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }
}
