using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Raptor;

namespace Raptor.Benchmarks;

[MemoryDiagnoser]
public class GameplayBenchmark
{
    private VirtualMachine _vm = null!;
    private VMChunk _ecsUpdateChunk = null!;
    private VMChunk _aStarChunk = null!;
    private VMChunk _dialogueTreeChunk = null!;
    private VMChunk _inventorySortChunk = null!;

    [GlobalSetup]
    public void Setup()
    {


        _vm = new VirtualMachine();
        var engine = new ScriptEngine();

        // 1. ECS Update: Parallel arrays for 1000 entities
        _ecsUpdateChunk = engine.Compile(@"
            DEFINE size 2000
            DEFINE dt 0.016
            DEFINE pos_arr r1
            DEFINE vel_arr r2
            DEFINE i r3
            DEFINE px r4
            DEFINE py r5
            DEFINE vx r6
            DEFINE vy r7
            DEFINE temp r8
            
            NEWARR pos_arr size
            NEWARR vel_arr size
            LOADC i 0
            loop:
                GETARR px pos_arr i
                GETARR vx vel_arr i
                MUL temp vx dt
                ADD px px temp
                SETARR pos_arr i px
                
                ADD i i 1
                GETARR py pos_arr i
                GETARR vy vel_arr i
                MUL temp vy dt
                ADD py py temp
                SETARR pos_arr i py
                
                FOR i size 1 < loop
            FREEARR pos_arr
            FREEARR vel_arr
            HALT");

        // 2. BFS Wavefront Grid Search on a 16x16 grid
        _aStarChunk = engine.Compile(@"
            DEFINE grid_size 256
            DEFINE visited r1
            DEFINE queue r2
            DEFINE head r3
            DEFINE tail r4
            DEFINE current r5
            DEFINE neighbor r6
            DEFINE row r7
            DEFINE col r8
            
            NEWARR visited grid_size
            NEWARR queue grid_size
            LOADC head 0
            LOADC tail 0
            
            ; Push start node (0)
            SETARR visited 0 1
            SETARR queue tail 0
            ADD tail tail 1
            
            search_loop:
                ; While head < tail
                LT 1 head tail
                JUMP search_done
                
            pop_node:
                GETARR current queue head
                ADD head head 1
                
                ; Check if current == target (255)
                EQ 0 current 255
                JUMP search_done
                
                ; Calculate row and col of current node: row = current / 16, col = current % 16
                DIV row current 16.0
                BINAND row row row
                MUL neighbor row 16.0
                SUB col current neighbor
                
                ; Check left neighbor (col > 0)
                LE 0 col 0
                JUMP check_right
                SUB neighbor current 1
                GETARR r10 visited neighbor
                EQ 0 r10 0
                JUMP visit_left
                JUMP check_right
            visit_left:
                SETARR visited neighbor 1
                SETARR queue tail neighbor
                ADD tail tail 1
                
            check_right:
                ; Check right neighbor (col < 15)
                LT 1 col 15.0
                JUMP check_up
            visit_right:
                ADD neighbor current 1
                GETARR r10 visited neighbor
                EQ 0 r10 0
                JUMP push_right
                JUMP check_up
            push_right:
                SETARR visited neighbor 1
                SETARR queue tail neighbor
                ADD tail tail 1
                
            check_up:
                ; Check up neighbor (row > 0)
                LE 0 row 0
                JUMP check_down
                SUB neighbor current 16
                GETARR r10 visited neighbor
                EQ 0 r10 0
                JUMP visit_up
                JUMP check_down
            visit_up:
                SETARR visited neighbor 1
                SETARR queue tail neighbor
                ADD tail tail 1
                
            check_down:
                ; Check down neighbor (row < 15)
                LT 1 row 15.0
                JUMP next_iteration
            visit_down:
                ADD neighbor current 16
                GETARR r10 visited neighbor
                EQ 0 r10 0
                JUMP push_down
                JUMP next_iteration
            push_down:
                SETARR visited neighbor 1
                SETARR queue tail neighbor
                ADD tail tail 1
                
            next_iteration:
                JUMP search_loop
                
            search_done:
                FREEARR visited
                FREEARR queue
                HALT");

        // 3. Dialogue tree condition evaluator (run 10k times)
        _dialogueTreeChunk = engine.Compile(@"
            DEFINE epochs 10000
            DEFINE i r10
            DEFINE gold r1
            DEFINE level r2
            DEFINE quest_active r3
            DEFINE response r4
            
            LOADC gold 15.0
            LOADC level 4.0
            LOADC quest_active 1.0
            LOADC i 0
            loop:
                ; Dialogue node 1: check quest
                EQ 1 quest_active 1.0
                JUMP quest_branch
                JUMP normal_branch
                
            quest_branch:
                ; Check if level >= 5
                LT 0 level 5.0
                JUMP quest_level_high
                JUMP quest_level_low
                
            quest_level_high:
                ; Check if gold >= 10
                LT 0 gold 10.0
                JUMP quest_buy
                JUMP quest_poor
                
            quest_buy:
                LOADC response 101.0
                JUMP end_dialogue
                
            quest_poor:
                LOADC response 102.0
                JUMP end_dialogue
                
            quest_level_low:
                LOADC response 103.0
                JUMP end_dialogue
                
            normal_branch:
                LOADC response 200.0
                JUMP end_dialogue
                
            end_dialogue:
                FOR i epochs 1 < loop
            HALT");

        // 4. Inventory Selection Sort: sorts 100 loot items by rarity
        _inventorySortChunk = engine.Compile(@"
            DEFINE size 100
            DEFINE arr r1
            DEFINE i r2
            DEFINE j r3
            DEFINE min_idx r4
            DEFINE val_i r7
            DEFINE val_j r8
            
            NEWARR arr size
            
            ; Fill with some test data
            LOADC i 0
            fill_loop:
                RAND val_i
                SETARR arr i val_i
                FOR i size 1 < fill_loop
            
            LOADC i 0
            outer_loop:
                MOVE min_idx i
                ADD j i 1
            inner_loop:
                GETARR val_j arr j
                GETARR val_i arr min_idx
                LT 1 val_j val_i
                JUMP set_min
                JUMP check_next
            set_min:
                MOVE min_idx j
            check_next:
                FOR j size 1 < inner_loop
                
                ; swap arr[i] and arr[min_idx]
                GETARR val_i arr i
                GETARR val_j arr min_idx
                SETARR arr i val_j
                SETARR arr min_idx val_i
                
                FOR i size 1 < outer_loop
                
            FREEARR arr
            HALT");
    }

    [Benchmark]
    public void Gameplay_EcsUpdate()
    {
        _vm.LoadProgram(_ecsUpdateChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Gameplay_GridPathfinding()
    {
        _vm.LoadProgram(_aStarChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Gameplay_DialogueTree()
    {
        _vm.LoadProgram(_dialogueTreeChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Gameplay_InventorySort()
    {
        _vm.LoadProgram(_inventorySortChunk);
        _vm.RunFast();
    }
}
