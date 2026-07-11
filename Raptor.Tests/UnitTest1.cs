using Raptor;

namespace Raptor.Tests;

public class UnitTest1
{
    [Fact]
    public void LinearFibonacciRunSucceeds()
    {
        string linearFib =
            @"DEFINE result r0
DEFINE last r1
DEFINE lastlast r2
DEFINE counter r4
DEFINE n 5
LOADC result 1
LOADC counter 1
loop:
    MOVE lastlast last
    MOVE last result
    ADD result last lastlast
    ADD counter counter 1
    PRINT counter
    LT 1 counter n
    JUMP loop
PRINT result
HALT";
        VMChunk chunk = new VMChunk();
        Assembler ass = new(chunk);
        ass.Parse(linearFib.Split("\n").ToList());

        VirtualMachine machine = new VirtualMachine();
        machine.LoadProgram(chunk);
        var result = machine.RunFast();
        Assert.Equal(result.Status, VMStatus.Halted);
    }

    [Fact]
    public void RecursiveFibonacciRunSucceeds()
    {
        string recursiveFib =
            @"DEFINE n 25
DEFINE result r0
LOADC result n

CALL method() result
PRINT result
HALT

method()
    PRINT r0
    LE 1 r0 2
    JUMP math
    LOADC r0 1
    RETURN r0 r0

math:
    SUB r1 r0 1
    CALL method() r1
    SUB r2 r0 2
    CALL method() r2
    ADD r1 r1 r2
    RETURN r1 r1";
        VMChunk chunk = new VMChunk();
        Assembler ass = new(chunk);
        ass.Parse(recursiveFib.Split("\n").ToList());

        VirtualMachine machine = new VirtualMachine();
        machine.LoadProgram(chunk);
        var result = machine.RunFast();
        Assert.Equal(result.Status, VMStatus.Halted);
    }

    [Fact]
    public void MonteCarloPiSimulationRunSucceeds()
    {
        string monteCarlo =
            @"DEFINE epochs 250000
DEFINE x r1
DEFINE y r2
DEFINE hits r4
DEFINE i r5
loop:
    RAND x
    RAND y
    MUL x x x
    MUL y y y
    ADD y y x
    LE 0 y 1
    ADD hits hits 1
    RAND x
    RAND y
    MUL x x x
    MUL y y y
    ADD y y x
    LE 0 y 1
    ADD hits hits 1
    RAND x
    RAND y
    MUL x x x
    MUL y y y
    ADD y y x
    LE 0 y 1
    ADD hits hits 1
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
PRINT result
HALT";
        VMChunk chunk = new VMChunk();
        Assembler ass = new(chunk);
        ass.Parse(monteCarlo.Split("\n").ToList());

        VirtualMachine machine = new VirtualMachine();
        machine.LoadProgram(chunk);
        var result = machine.RunFast();
        Assert.Equal(result.Status, VMStatus.Halted);
    }

    [Fact]
    public void PerceptronRunSucceeds()
    {
        string perceptron =
            @"
; --- CORE REGISTERS ---
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

; --- SCRATCHPAD REGISTERS ---
DEFINE temp1 r21
DEFINE temp2 r22
DEFINE temp3 r23

; --- INITIALIZATION ---
LOADC epochs 10
LOADC w1 1
LOADC w2 1
LOADC b 0

loop:
    ; --- Example 1: (0, 0) -> 0 ---
    LOADC x1 0
    LOADC x2 0
    LOADC expected 0
    CALL error() x1
    CALL update() x1

    ; --- Example 2: (1, 0) -> 1 ---
    LOADC x1 1
    LOADC x2 0
    LOADC expected 0
    CALL error() x1
    CALL update() x1

    ; --- Example 3: (0, 1) -> 1 ---
    LOADC x1 0
    LOADC x2 1
    LOADC expected 0
    CALL error() x1
    CALL update() x1

    ; --- Example 4: (1, 1) -> 1 ---
    LOADC x1 1
    LOADC x2 1
    LOADC expected 1
    CALL error() x1
    CALL update() x1
    FOR i epochs 1 < loop

; --- OUTPUT RESULTS ---
PRINT w1
PRINT w2
PRINT b
HALT

; --- FUNCTIONS ---

print()
    PRINT w1
    PRINT w2
    PRINT b
    PRINTA 10
    RETURN r0 r0

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
    ; Update w1: w1 = w1 + (error * x1)
    MUL temp1 error x1
    ADD w1 w1 temp1

    ; Update w2: w2 = w2 + (error * x2)
    MUL temp2 error x2
    ADD w2 w2 temp2

    ; Update Bias: b = b + error
    ADD b b error
    
    RETURN r0 r0";
        VMChunk chunk = new VMChunk();
        Assembler ass = new(chunk);
        ass.Parse(perceptron.Split("\n").ToList());

        VirtualMachine machine = new VirtualMachine();
        machine.LoadProgram(chunk);
        var result = machine.RunFast();
        Assert.Equal(result.Status, VMStatus.Halted);
    }

    [Fact]
    public void ArrayTestRunSucceeds()
    {
        string arrayTest =
            @"
NEWARR r0 100
NEWARR r1 100
NEWARR r2 100
FREEARR r0
FREEARR r2
FREEARR r1
NEWARR r0 100
HALT
";
        VMChunk chunk = new VMChunk();
        Assembler ass = new(chunk);
        ass.Parse(arrayTest.Split("\n").ToList());

        VirtualMachine machine = new VirtualMachine();
        machine.LoadProgram(chunk);
        var result = machine.RunFast();
        Assert.Equal(result.Status, VMStatus.Halted);
    }

    [Fact]
    public void HardArrayTestPass()
    {
        string hardArrayTest =
            @"DEFINE n 25
DEFINE result r0
LOADC result n

CALL method() result
PRINT result
HALT

method()
    LE 1 r0 2
    JUMP math
    LOADC r0 1
    RETURN r0 r0

math:
    SUB r1 r0 1
    CALL method() r1
    SUB r2 r0 2
    CALL method() r2
    ADD r1 r1 r2
    RETURN r1 r1";
        VMChunk chunk = new VMChunk();
        Assembler ass = new(chunk);
        ass.Parse(hardArrayTest.Split("\n").ToList());

        VirtualMachine machine = new VirtualMachine();
        machine.LoadProgram(chunk);
        var result = machine.RunFast();
        Assert.Equal(result.Status, VMStatus.Halted);
    }

    [Fact]
    public void RayTracerRunSucceeeds()
    {
        string rayTracer =
            @"
; --- REGISTER DEFINITIONS ---
; r0 is reserved as the index register for GETARR and GETARRA
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

; --- INITIALIZATION ---
LOADC width 2048
LOADC height 2048
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

; Harmless swap to showcase SWAP instruction
SWP temp1 temp2

; Allocate and write PPM header into heap
NEWARR header_ptr 18
SETARRA header_ptr 0 80   ; 'P'
SETARRA header_ptr 1 51   ; '3'
SETARRA header_ptr 2 10   ; '\n'
SETARRA header_ptr 3 50   ; '1'
SETARRA header_ptr 4 48   ; '0'
SETARRA header_ptr 5 52   ; '2'
SETARRA header_ptr 6 56   ; '4'
SETARRA header_ptr 7 32   ; ' '
SETARRA header_ptr 8 50   ; '1'
SETARRA header_ptr 9 48   ; '0'
SETARRA header_ptr 10 52  ; '2'
SETARRA header_ptr 11 56  ; '4'
SETARRA header_ptr 12 10  ; '\n'
SETARRA header_ptr 13 50  ; '2'
SETARRA header_ptr 14 53  ; '5'
SETARRA header_ptr 15 53  ; '5'
SETARRA header_ptr 16 10  ; '\n'
SETARRA header_ptr 17 0   ; Null-terminator

LOADC offset 0
print_header:
    MOVE r0 offset
    GETARRA temp1 header_ptr
    EQ 0 temp1 zero
    JUMP header_done
    PRINTA temp1
    ADD offset offset 1
    JUMP print_header
header_done:
    FREEARR header_ptr

; Initialize sphere data in heap (size 14: 2 spheres * 7 parameters)
NEWARR spheres 14
; Sphere 1: Center=(0, 0, 3), Radius=1, Color=(255, 100, 20)
SETARR spheres 0 0
SETARR spheres 1 0
SETARR spheres 2 3
SETARR spheres 3 1
SETARR spheres 4 255
SETARR spheres 5 100
SETARR spheres 6 20
; Sphere 2: Center=(0, -100.5, 3), Radius=100, Color=(20, 200, 20)
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
    ; Normalize ray direction D = (rx, ry, 1.0)
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

    ; Check Sphere 1
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
    LT 1 t t_min
    JUMP set_sp1
    JUMP check_sp2
set_sp1:
    MOVE t_min t
    LOADC hit_idx 0

check_sp2:
    ; Check Sphere 2
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
    LT 1 t t_min
    JUMP set_sp2
    JUMP end_check
set_sp2:
    MOVE t_min t
    LOADC hit_idx 1

end_check:
    EQ 0 hit_idx -1
    JUMP render_sky

    ; Shading for sphere hits
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
    ; Normal N = (P - C) / rad
    SUB nx px cx
    SUB ny py cy
    SUB nz pz cz
    DIV nx nx rad
    DIV ny ny rad
    DIV nz nz rad

    ; Light direction L = normalized(1, 1, -1)
    LOADC lx 0.57735
    LOADC ly 0.57735
    LOADC lz -0.57735

    ; Diffuse = N . L
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

    ; Add random grain to sky background
    RAND noise
    MUL noise noise 15.0
    ADD r r noise
    ADD g g noise
    ADD b_val b_val noise

print_pixel:
    ; Clamp and truncate r
    LT 0 r zero
    JUMP r_not_neg
    LOADC r 0.0
r_not_neg:
    LT 0 max_val r
    JUMP r_not_high
    LOADC r 255.0
r_not_high:
    BINAND r r r
    ; Clamp and truncate g
    LT 0 g zero
    JUMP g_not_neg
    LOADC g 0.0
g_not_neg:
    LT 0 max_val g
    JUMP g_not_high
    LOADC g 255.0
g_not_high:
    BINAND g g g
    ; Clamp and truncate b_val
    LT 0 b_val zero
    JUMP b_not_neg
    LOADC b_val 0.0
b_not_neg:
    LT 0 max_val b_val
    JUMP b_not_high
    LOADC b_val 255.0
b_not_high:
    BINAND b_val b_val b_val
    PRINTS r
    PRINTS g
    PRINTS b_val

    ADD rx rx step
    ADD i i 1
    LT 1 i width
    JUMP x_loop

    SUB ry ry step
    FOR j height 1 < y_loop

FREEARR spheres
HALT

; --- SPHERE INTERSECTION FUNCTION ---
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
    RETURN r0 r0
";
        VMChunk chunk = new VMChunk();
        Assembler ass = new(chunk);
        ass.Parse(rayTracer.Split("\n").ToList());
        VirtualMachine machine = new VirtualMachine();
        machine.LoadProgram(chunk);
        var result = machine.RunFast();
        Assert.Equal(VMStatus.Halted, result.Status);
    }
}
