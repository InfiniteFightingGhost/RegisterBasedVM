using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Raptor;
using Raptor.Attributes;

namespace Raptor.Benchmarks;

[MemoryDiagnoser]
public class VmBenchmarks
{
    private VMChunk _fibChunk = null!;
    private VMChunk _monteCarloChunk = null!;
    private VMChunk _perceptronChunk = null!;
    private VMChunk _rayTracerChunk = null!;
    private VMChunk _physicsMovementChunk = null!;
    private VMChunk _combatDamageChunk = null!;
    private VirtualMachine _vm = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Redirect Console.Out and Console.Error to avoid polluting output
        Console.SetOut(TextWriter.Null);
        Console.SetError(TextWriter.Null);

        // Pre-allocate VirtualMachine instance to verify Zero-GC runtime execution
        _vm = new VirtualMachine();

        // Setup FFI host table for benchmarks
        var table = new FFIHostTable();
        table.RegisterModule(typeof(FfiBenchmarkBindings));
        table.RegisterModule(typeof(FallbackBenchmarkBindings));

        _vm.RegisterHostTable(table);

        var engine = new ScriptEngine();
        engine.RegisterHostTable(table);

        _physicsMovementChunk = engine.Compile(PhysicsMovementAsm);
        _combatDamageChunk = engine.Compile(CombatDamageAsm);

        // Compile and verify Fibonacci
        _fibChunk = new VMChunk();
        new Assembler(_fibChunk).Parse(LinearFibAsm.Split('\n').ToList());
        BytecodeVerifier.Verify(_fibChunk, 16 * 1024 * 1024);

        // Compile and verify Monte Carlo
        _monteCarloChunk = new VMChunk();
        new Assembler(_monteCarloChunk).Parse(MonteCarloAsm.Split('\n').ToList());
        BytecodeVerifier.Verify(_monteCarloChunk, 16 * 1024 * 1024);

        // Compile and verify Perceptron
        _perceptronChunk = new VMChunk();
        new Assembler(_perceptronChunk).Parse(PerceptronAsm.Split('\n').ToList());
        BytecodeVerifier.Verify(_perceptronChunk, 16 * 1024 * 1024);

        // Compile and verify RayTracer
        _rayTracerChunk = new VMChunk();
        new Assembler(_rayTracerChunk).Parse(RayTracerAsm.Split('\n').ToList());
        BytecodeVerifier.Verify(_rayTracerChunk, 16 * 1024 * 1024);

        int sinIndex = Array.IndexOf(_rayTracerChunk.Constants, -999.123);
        int cosIndex = Array.IndexOf(_rayTracerChunk.Constants, -999.456);
        int camXIndex = Array.IndexOf(_rayTracerChunk.Constants, -999.789);
        int camYIndex = Array.IndexOf(_rayTracerChunk.Constants, -999.012);
        int camZIndex = Array.IndexOf(_rayTracerChunk.Constants, -999.345);

        if (sinIndex != -1)
            _rayTracerChunk.Constants[sinIndex] = 0.0;
        if (cosIndex != -1)
            _rayTracerChunk.Constants[cosIndex] = 1.0;
        if (camXIndex != -1)
            _rayTracerChunk.Constants[camXIndex] = 0.0;
        if (camYIndex != -1)
            _rayTracerChunk.Constants[camYIndex] = 0.0;
        if (camZIndex != -1)
            _rayTracerChunk.Constants[camZIndex] = 3.5;
    }

    [Benchmark]
    public void Benchmark_Fibonacci()
    {
        _vm.LoadProgram(_fibChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_MonteCarlo()
    {
        _vm.LoadProgram(_monteCarloChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_Perceptron()
    {
        _vm.LoadProgram(_perceptronChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_RayTracerSingleFrame()
    {
        _vm.LoadProgram(_rayTracerChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_PhysicsMovement()
    {
        _vm.LoadProgram(_physicsMovementChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_CombatDamage()
    {
        _vm.LoadProgram(_combatDamageChunk);
        _vm.RunFast();
    }

    // --- WORKLOAD SPECIFICATIONS ---

    private const string LinearFibAsm =
        @"
DEFINE result r0
DEFINE last r1
DEFINE lastlast r2
DEFINE counter r4
DEFINE n 50000
LOADC result 1
LOADC counter 1
loop:
    MOVE lastlast last
    MOVE last result
    ADD result last lastlast
    ADD counter counter 1
    LT 0 counter n
    JUMP loop
PRINT result
HALT";

    private const string MonteCarloAsm =
        @"
DEFINE epochs 100000
DEFINE x r1
DEFINE y r2
DEFINE hits r4
DEFINE i r5
LOADC hits 0
LOADC i 0
loop:
    RAND x
    RAND y
    MUL x x x
    MUL y y y
    ADD y y x
    LE 0 y 1
    ADD hits hits 1
    FOR i epochs 1 < loop
DEFINE result r6
DIV result hits epochs
HALT";

    private const string PerceptronAsm =
        @"
DEFINE x1 r0
DEFINE x2 r1
DEFINE w1 r2
DEFINE w2 r3
DEFINE b r4
DEFINE expected r5
DEFINE result r6
DEFINE error r7
DEFINE epochs r8
DEFINE i r10
DEFINE temp1 r21
DEFINE temp2 r22

LOADC epochs 1000
LOADC w1 1
LOADC w2 1
LOADC b 0
LOADC i 0

loop:
    LOADC x1 0
    LOADC x2 0
    LOADC expected 0
    CALL error() x1
    CALL update() x1

    LOADC x1 1
    LOADC x2 0
    LOADC expected 0
    CALL error() x1
    CALL update() x1

    LOADC x1 0
    LOADC x2 1
    LOADC expected 0
    CALL error() x1
    CALL update() x1

    LOADC x1 1
    LOADC x2 1
    LOADC expected 1
    CALL error() x1
    CALL update() x1
    FOR i epochs 1 < loop
HALT

dot()
    MUL temp1 x1 w1
    MUL temp2 x2 w2
    ADD result temp1 temp2
    RETURN r0 r0

perceive()
    CALL dot() x1
    ADD result result b
    LE 1 result 0
    JUMP perceive_if
    LOADC result 0
    JUMP perceive_return
perceive_if:
    LOADC result 1
perceive_return:
    RETURN r0 r0

error()
    CALL perceive() x1
    SUB error expected result
    RETURN r0 r0

update()
    MUL temp1 error x1
    ADD w1 w1 temp1
    MUL temp2 error x2
    ADD w2 w2 temp2
    ADD b b error
    RETURN r0 r0";

    private const string RayTracerAsm =
        @"
DEFINE width r1
DEFINE height r2
DEFINE i r3
DEFINE j r4
DEFINE spheres r5
DEFINE t_min r6
DEFINE hit_idx r7
DEFINE rx r8
DEFINE ry r9
DEFINE rz r10
DEFINE dx r11
DEFINE dy r12
DEFINE dz r13
DEFINE len_sq r14
DEFINE inv_len r15
DEFINE sp_x r16
DEFINE sp_y r17
DEFINE sp_z r18
DEFINE sp_r r19
DEFINE sp_cr r20
DEFINE sp_cg r21
DEFINE sp_cb r22
DEFINE b_coeff r23
DEFINE c r24
DEFINE disc r25
DEFINE t r26
DEFINE vx r27
DEFINE vy r28
DEFINE vz r29
DEFINE vv r30
DEFINE r_sq r31
DEFINE px r32
DEFINE py r33
DEFINE pz r34
DEFINE nx r35
DEFINE ny r36
DEFINE nz r37
DEFINE lx r38
DEFINE ly r39
DEFINE lz r40
DEFINE diffuse r41
DEFINE temp1 r42
DEFINE temp2 r43
DEFINE temp3 r44
DEFINE r r45
DEFINE g r46
DEFINE b_val r47
DEFINE sky_t r48
DEFINE noise r49
DEFINE cx r50
DEFINE cy r51
DEFINE cz r52
DEFINE rad r53
DEFINE offset r54
DEFINE b_neg r55
DEFINE sq_disc r56
DEFINE header_ptr r57
DEFINE one_minus_sky_t r58
DEFINE zero r59
DEFINE max_val r60
DEFINE one_val r61
DEFINE r_temp r62
DEFINE g_temp r63
DEFINE b_temp r64
DEFINE sin_theta r65
DEFINE cos_theta r66
DEFINE cam_x r67
DEFINE cam_y r68
DEFINE cam_z r69
DEFINE step r70

LOADC width 32
LOADC height 32
LOADC zero 0.0
LOADC max_val 255.0
LOADC one_val 1.0
LOADC sin_theta -999.123
LOADC cos_theta -999.456
LOADC cam_x -999.789
LOADC cam_y -999.012
LOADC cam_z -999.345
SUB temp1 width one_val
LOADC temp2 2.0
DIV step temp2 temp1

NEWARR spheres 14
SETARR spheres 0 0
SETARR spheres 1 0
SETARR spheres 2 3
SETARR spheres 3 1
SETARR spheres 4 255
SETARR spheres 5 100
SETARR spheres 6 20
SETARR spheres 7 0
SETARR spheres 8 -100.5
SETARR spheres 9 3
SETARR spheres 10 100
SETARR spheres 11 20
SETARR spheres 12 200
SETARR spheres 13 20

LOADC j 0
LOADC ry 1.0

y_loop:
    LOADC i 0
    LOADC rx -1.0
x_loop:
    MUL dx rx cos_theta
    SUB dx dx sin_theta
    MOVE dy ry
    MUL dz rx sin_theta
    ADD dz dz cos_theta
    MUL len_sq rx rx
    MUL temp1 ry ry
    ADD len_sq len_sq temp1
    ADD len_sq len_sq 1.0
    MOVE inv_len len_sq
    FISR inv_len inv_len
    MUL dx dx inv_len
    MUL dy dy inv_len
    MUL dz dz inv_len

    LOADC t_min 999999.0
    LOADC hit_idx -1

    LOADC r0 0
    GETARR sp_x spheres
    LOADC r0 1
    GETARR sp_y spheres
    LOADC r0 2
    GETARR sp_z spheres
    LOADC r0 3
    GETARR sp_r spheres
    CALL intersect() r0
    LE 0 t zero
    JUMP check_sp2
    LT 0 t t_min
    JUMP set_sp1
    JUMP check_sp2
set_sp1:
    MOVE t_min t
    LOADC hit_idx 0

check_sp2:
    LOADC r0 7
    GETARR sp_x spheres
    LOADC r0 8
    GETARR sp_y spheres
    LOADC r0 9
    GETARR sp_z spheres
    LOADC r0 10
    GETARR sp_r spheres
    CALL intersect() r0
    LE 0 t zero
    JUMP end_check
    LT 0 t t_min
    JUMP set_sp2
    JUMP end_check
set_sp2:
    MOVE t_min t
    LOADC hit_idx 1

end_check:
    EQ 0 hit_idx -1
    JUMP render_sky

    MUL px dx t_min
    ADD px px cam_x
    MUL py dy t_min
    ADD py py cam_y
    MUL pz dz t_min
    ADD pz pz cam_z

    EQ 0 hit_idx zero
    JUMP hit_sphere1
    JUMP hit_sphere2

hit_sphere1:
    LOADC cx 0.0
    LOADC cy 0.0
    LOADC cz 3.0
    LOADC rad 1.0
    LOADC r0 4
    GETARR sp_cr spheres
    LOADC r0 5
    GETARR sp_cg spheres
    LOADC r0 6
    GETARR sp_cb spheres
    JUMP compute_shading

hit_sphere2:
    LOADC cx 0.0
    LOADC cy -100.5
    LOADC cz 3.0
    LOADC rad 100.0
    LOADC sp_cr 10.0
    LOADC sp_cg 80.0
    LOADC sp_cb 10.0

compute_shading:
    SUB nx px cx
    SUB ny py cy
    SUB nz pz cz
    DIV nx nx rad
    DIV ny ny rad
    DIV nz nz rad

    LOADC lx 0.57735
    LOADC ly 0.57735
    LOADC lz -0.57735

    MUL diffuse nx lx
    MUL temp1 ny ly
    ADD diffuse diffuse temp1
    MUL temp1 nz lz
    ADD diffuse diffuse temp1

    LE 0 diffuse zero
    JUMP set_ambient
    JUMP apply_diffuse
set_ambient:
    LOADC diffuse 0
apply_diffuse:
    ADD diffuse diffuse 0.15
    LE 0 diffuse one_val
    JUMP output_color
    LOADC diffuse 1.0

output_color:
    MUL r sp_cr diffuse
    MUL g sp_cg diffuse
    MUL b_val sp_cb diffuse
    JUMP print_pixel

render_sky:
    MUL sky_t dy 0.5
    ADD sky_t sky_t 0.5
    SUB one_minus_sky_t 1.0 sky_t
    MUL r one_minus_sky_t 255.0
    MUL temp1 sky_t 128.0
    ADD r r temp1
    MUL g one_minus_sky_t 255.0
    MUL temp1 sky_t 178.0
    ADD g g temp1
    MUL b_val one_minus_sky_t 255.0
    MUL temp1 sky_t 255.0
    ADD b_val b_val temp1

    RAND noise
    MUL noise noise 15.0
    ADD r r noise
    ADD g g noise
    ADD b_val b_val noise

print_pixel:
    LT 1 r zero
    JUMP r_not_neg
    LOADC r 0.0
r_not_neg:
    LT 1 max_val r
    JUMP r_not_high
    LOADC r 255.0
r_not_high:
    BINAND r r r
    LT 1 g zero
    JUMP g_not_neg
    LOADC g 0.0
g_not_neg:
    LT 1 max_val g
    JUMP g_not_high
    LOADC g 255.0
g_not_high:
    BINAND g g g
    LT 1 b_val zero
    JUMP b_not_neg
    LOADC b_val 0.0
b_not_neg:
    LT 1 max_val b_val
    JUMP b_not_high
    LOADC b_val 255.0
b_not_high:
    BINAND b_val b_val b_val

    ADD rx rx step
    ADD i i 1
    LT 1 i width
    JUMP x_loop

    SUB ry ry step
    FOR j height 1 < y_loop

FREEARR spheres
HALT

intersect()
    SUB vx cam_x sp_x
    SUB vy cam_y sp_y
    SUB vz cam_z sp_z
    MUL temp1 vx vx
    MUL temp2 vy vy
    MUL temp3 vz vz
    ADD vv temp1 temp2
    ADD vv vv temp3
    MUL r_sq sp_r sp_r
    SUB c vv r_sq
    MUL b_coeff vx dx
    MUL temp1 vy dy
    ADD b_coeff b_coeff temp1
    MUL temp1 vz dz
    ADD b_coeff b_coeff temp1
    MUL temp1 b_coeff b_coeff
    SUB disc temp1 c
    LT 1 disc zero
    JUMP no_hit
    SQRT sq_disc disc
    UNM b_neg b_coeff
    SUB t b_neg sq_disc
    RETURN r0 r0
no_hit:
    LOADC t -1.0
    RETURN r0 r0";

    private const string FfiDirectBindAsm =
        @"
DEFINE epochs 10000
DEFINE i r2
LOADC r1 2.0
LOADC i 0
loop:
    CALL directAdd() r1
    FOR i epochs 1 < loop
HALT";

    private const string FfiTypedWrapperAsm =
        @"
DEFINE epochs 10000
DEFINE i r2
LOADC r1 2.0
LOADC i 0
loop:
    CALL typedAdd() r1
    FOR i epochs 1 < loop
HALT";

    private const string FfiFallbackAsm =
        @"
DEFINE epochs 10000
DEFINE i r6
LOADC r1 1.0
LOADC r2 2.0
LOADC r3 3.0
LOADC r4 4.0
LOADC r5 5.0
LOADC i 0
loop:
    CALL sumFive() r1
    FOR i epochs 1 < loop
HALT";

    private const string InternalCallAsm =
        @"
DEFINE epochs 10000
DEFINE i r2
LOADC r1 2.0
LOADC i 0
loop:
    CALL internalAdd() r1
    FOR i epochs 1 < loop
HALT

internalAdd()
    ADD r0 r0 r0
    RETURN r0 r0";

    private const string PhysicsMovementAsm =
        @"
DEFINE epochs 10000
DEFINE i r8
LOADC r1 0.0
LOADC r2 10.0
LOADC r3 2.5
LOADC r4 0.0
LOADC r5 9.81
LOADC r6 0.016
LOADC r7 0.0
LOADC i 0
loop:
    MUL r9 r5 r6
    SUB r4 r4 r9
    MUL r10 r3 r6
    ADD r1 r1 r10
    MUL r11 r4 r6
    ADD r2 r2 r11
    LT 1 r2 r7
    JUMP skip_ground
    MOVE r2 r7
    LOADC r4 0.0
skip_ground:
    FOR i epochs 1 < loop
HALT";

    private const string CombatDamageAsm =
        @"
DEFINE epochs 10000
DEFINE i r10
DEFINE player_hp r1
DEFINE enemy_hp r2
DEFINE attack_dmg r3
DEFINE defense r4
DEFINE hit_rate r5
DEFINE damage_dealt r6
DEFINE roll r7
DEFINE zero r8

LOADC player_hp 100.0
LOADC enemy_hp 150.0
LOADC attack_dmg 25.0
LOADC defense 5.0
LOADC hit_rate 0.8
LOADC zero 0.0
LOADC i 0
loop:
    LT 0 zero enemy_hp
    JUMP check_hit
    JUMP end_combat
check_hit:
    RAND roll
    LT 1 roll hit_rate
    JUMP deal_dmg
    JUMP next_round
deal_dmg:
    SUB damage_dealt attack_dmg defense
    LT 0 zero damage_dealt
    JUMP apply_damage
    LOADC damage_dealt 0.0
apply_damage:
    SUB enemy_hp enemy_hp damage_dealt
next_round:
    FOR i epochs 1 < loop
end_combat:
    HALT";
}

[RaptorModule]
public static class FfiBenchmarkBindings
{
    [RaptorMethod("typedAdd", 101)]
    public static double TypedAdd(double a) => a + a;
}

[RaptorModule]
public static class FallbackBenchmarkBindings
{
    [RaptorMethod("sumFive", 200)]
    public static double SumFive(double a, double b, double c, double d, double e) =>
        a + b + c + d + e;
}
