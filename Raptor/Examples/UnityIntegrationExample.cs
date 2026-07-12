using Raptor;
using Raptor.Attributes;
using UnityEngine;

/// <summary>
/// Example MonoBehaviour showing how to integrate the Raptor scripting VM
/// with Unity, including FFI registration and hot-reloading support.
/// </summary>
public class RaptorScriptRunner : MonoBehaviour
{
    [Tooltip("Path to the .rasm script file (e.g. Assets/Scripts/player.rasm).")]
    public string scriptPath = "Assets/Scripts/player.rasm";

    private ScriptEngine _engine;
    private ScriptWatcher _watcher;

    private void Awake()
    {
        // 1. Initialize FFI host table and register FFI modules
        var table = new FFIHostTable();
        table.RegisterModule(typeof(GameMathBindings)); // Static module
        table.RegisterModule(this); // Instance module (exposes current MonoBehaviour)

        // 2. Initialize ScriptEngine and bulk-register FFI methods
        _engine = new ScriptEngine();
        _engine.RegisterHostTable(table);

        // 3. Setup ScriptWatcher for thread-safe hot reloading in editor/development
        try
        {
            _watcher = new ScriptWatcher(_engine, scriptPath);

            // Wire reload events to Unity's console
            _watcher.OnReloaded += (chunk) =>
                Debug.Log($"[Raptor] Successfully hot-reloaded script: {scriptPath}");
            _watcher.OnReloadError += (ex) =>
                Debug.LogError($"[Raptor Compiler Error] {ex.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Raptor Init Error] Failed to compile script: {ex.Message}");
        }
    }

    private void Update()
    {
        if (_watcher == null)
            return;

        // 4. Execute the currently active compiled script chunk
        try
        {
            _engine.Execute(_watcher.ActiveChunk);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Raptor VM Runtime Error] {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        // Clean up the file system watcher resource
        _watcher?.Dispose();
    }

    // --------------------------------------------
    //  Example Instance FFI Method Exposing Unity APIs
    // --------------------------------------------

    [RaptorMethod("moveTarget")]
    public void MoveTarget(double x, double y, double z)
    {
        // Access standard Unity APIs directly from the script FFI boundary
        transform.position = new Vector3((float)x, (float)y, (float)z);
    }
}

/// <summary>
/// Example static helper module.
/// </summary>
[RaptorModule("math")]
public static class GameMathBindings
{
    [RaptorMethod("clamp")]
    public static double Clamp(double val, double min, double max)
    {
        return val < min ? min : (val > max ? max : val);
    }
}
