using UnityEngine;
using Raptor;
using Raptor.Attributes;

/*
  =============================================================================
   Example RaptorScript Source (save this file as Assets/Scripts/enemy_ai.rapt)
  =============================================================================
  
  var targetDistance = enemy.getDistanceToPlayer();
  var state = enemy.getState(); // 0 = Idle, 1 = Chasing, 2 = Attacking
  
  if (targetDistance < 5.0) {
      // Transition to Attacking state
      enemy.setState(2.0);
      
      var cooldown = enemy.getAttackCooldown();
      if (cooldown == 0.0) {
          enemy.attackPlayer(15.0);     // Deal 15 damage
          enemy.setAttackCooldown(3.0);  // 3 second cooldown
      } else {
          // Reduce cooldown by deltaTime (desugared -= subtraction)
          cooldown -= 0.016; 
          if (cooldown < 0.0) {
              cooldown = 0.0;
          }
          enemy.setAttackCooldown(cooldown);
      }
  } else {
      if (targetDistance < 20.0) {
          // Player detected, Chase
          enemy.setState(1.0);
          enemy.moveTowardsPlayer(8.0); // Run speed
      } else {
          // Player out of range, Patrol
          enemy.setState(0.0);
          enemy.patrol(3.0);            // Walk speed
      }
  }

  =============================================================================
*/

/// <summary>
/// A real-world example of how to execute a complex C-like RaptorScript (.rapt) file
/// on a Unity GameObject to run AI behaviors.
/// </summary>
[RaptorModule("enemy")]
public class RaptorScriptGameplayExample : MonoBehaviour
{
    [Header("Script Settings")]
    [Tooltip("Path to the C-like .rapt file relative to the project directory.")]
    public string raptorScriptPath = "Assets/Scripts/enemy_ai.rapt";

    [Header("Enemy Attributes")]
    public Transform playerTransform;
    public float walkSpeed = 3.0f;
    public float runSpeed = 8.0f;

    private int _aiState = 0; // 0 = Idle, 1 = Chasing, 2 = Attacking
    private double _attackCooldown = 0.0;
    
    private ScriptEngine _engine;
    private ScriptWatcher _watcher;
    private string _raptSourceText = string.Empty;

    private void Awake()
    {
        // 1. Setup the FFI Table and register AI APIs
        var table = new FFIHostTable();
        table.RegisterModule(this); // Register this instance as FFI module

        // Generate and save autocomplete typings (.d.ts) next to the script
        try
        {
            string decls = table.GenerateAutocompleteDeclarations();
            string? dir = System.IO.Path.GetDirectoryName(raptorScriptPath);
            string declsPath = System.IO.Path.Combine(dir ?? "", "raptor-api.d.ts");
            System.IO.File.WriteAllText(declsPath, decls);
            Debug.Log($"[Raptor FFI] Autocomplete typings auto-saved to: {declsPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Raptor FFI] Failed to save autocomplete typings: {ex.Message}");
        }

        // 2. Setup the compiler engine
        _engine = new ScriptEngine();
        _engine.RegisterHostTable(table);

        // 3. Compile the .rapt script file and monitor it for hot reloading
        try
        {
            string absPath = System.IO.Path.GetFullPath(raptorScriptPath);
            string initialSource = System.IO.File.ReadAllText(raptorScriptPath);
            Debug.Log($"[Raptor AI Debug] Reading file from: {absPath} (Length: {initialSource.Length} chars)\nContent:\n{initialSource}");

            // Watch the .rapt file directly, compile it in memory, and keep source text updated
            _watcher = new ScriptWatcher(_engine, raptorScriptPath, (src) => {
                _raptSourceText = src;
                return Raptor.Compiler.RaptorScriptCompiler.Compile(src);
            });
            
            _watcher.OnReloaded += (chunk) => Debug.Log("[Raptor AI] AI script hot-reloaded successfully.");
            _watcher.OnReloadError += (ex) => Debug.LogError($"[Raptor AI Compiler Error] {ex.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Raptor AI Init Error] Failed to initialize AI compiler: {ex.Message}");
        }
    }

    private void Update()
    {
        if (_watcher == null || playerTransform == null) return;

        // 4. Run the AI Script VM chunk on every frame
        var runResult = _engine.Execute(_watcher.ActiveChunk);
        if (runResult.Status != VMStatus.Halted && runResult.Status != VMStatus.Success)
        {
            string errorDetails = ScriptEngine.TranslateError(_watcher.ActiveChunk, runResult.IpOffset, _raptSourceText);
            Debug.LogError($"[Raptor AI VM Error] Status: {runResult.Status}. Details: {errorDetails}");
            
            // Disable component to prevent log spamming
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        _watcher?.Dispose();
    }

    // ────────────────────────────────────────────────────────────────────────
    //  FFI APIs exposed to the C-like RaptorScript (.rapt) under the "enemy" module
    // ────────────────────────────────────────────────────────────────────────

    [RaptorMethod("getDistanceToPlayer")]
    public double GetDistanceToPlayer()
    {
        if (playerTransform == null) return 999.0;
        double dist = Vector3.Distance(transform.position, playerTransform.position);
        Debug.Log($"[Raptor AI Pos Debug] Enemy Pos: {transform.position}, Player Pos: {playerTransform.position}, Distance: {dist}");
        return dist;
    }

    [RaptorMethod("getState")]
    public double GetState() => _aiState;

    [RaptorMethod("setState")]
    public void SetState(double state)
    {
        _aiState = (int)state;
    }

    [RaptorMethod("getAttackCooldown")]
    public double GetAttackCooldown() => _attackCooldown;

    [RaptorMethod("setAttackCooldown")]
    public void SetAttackCooldown(double cooldown)
    {
        _attackCooldown = cooldown;
    }

    [RaptorMethod("attackPlayer")]
    public void AttackPlayer(double damage)
    {
        Debug.Log($"[Enemy AI] *SLASH* Dealt {damage} damage to player!");
    }

    [RaptorMethod("moveTowardsPlayer")]
    public void MoveTowardsPlayer(double speed)
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.position += direction * (float)speed * Time.deltaTime;
        Debug.Log($"[Enemy AI] Chasing player at speed: {speed}");
    }

    [RaptorMethod("patrol")]
    public void Patrol(double speed)
    {
        // Simple patrol left-and-right simulation
        float offset = Mathf.PingPong(Time.time * (float)speed, 6.0f) - 3.0f;
        transform.position = new Vector3(offset, transform.position.y, transform.position.z);
        Debug.Log($"[Enemy AI] Patrolling at speed: {speed}");
    }
}
