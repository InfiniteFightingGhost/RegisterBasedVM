# The Assembler Pipeline & Constant Pool

The assembler in [Assembler.cs](../Raptor/Assembler.cs) compiles assembly text into bytecode using a three-pass pipeline.

## 1. Pass 1: Lexical Cleaning & Macro Substitution

Pass 1 cleans raw text, removes comments, and expands macro definitions.

1. **Scrubbing Whitespace & Comments:**
   Empty lines are dropped. Comments starting with `;` are removed. Inline comments (following `;`) are stripped.
2. **`DEFINE` Macro Collection:**
   Lines starting with `DEFINE` (e.g. `DEFINE x1 r0` or `DEFINE epochs 10000`) populate a symbol table dictionary with key-value pairs. `DEFINE` lines are removed after extraction.
3. **Macro Substitution:**
   Words matching macro symbol keys are replaced with their values.
   Example: `ADD x1 w1 w2` becomes `ADD r0 r2 r3`.

## 2. Pass 2: Label Resolution & Size Tracking

Pass 2 maps jump labels and method headers to instruction indices.

1. **Symbol Identification:**
   - Lines ending in `:` (e.g. `loop:`) are jump labels.
   - Lines ending in `()` (e.g. `perceive()`) are method entry points.
2. **Instruction Address Tracking:**
   Maintains a `memoryAddress` offset counter:
   - Jump labels store `memoryAddress` as target and are removed from instructions.
   - Method headers store `memoryAddress` in the global method table and are removed.
   - Most instructions increment `memoryAddress` by 1 word. The compound `FOR` instruction occupies 2 contiguous words and increments `memoryAddress` by 2:
     ```csharp
     else if (lines[i].StartsWith("FOR "))
         memoryAddress += 2;
     else
         memoryAddress++;
     ```

## 3. Pass 3: Codegen & Bit-Packing

Pass 3 parses registers and literals into packed 32-bit instructions.

1. **Operand Resolution:**
   - Register strings (`r0`..`r255`) parse to integer indices.
   - Numeric literals (`128.0`, `1.5`) pass to `SetConstant` and map to constant pool indices ($\ge 256$).
2. **Relative Branch Offset Calculation:**
   $$\text{jumpOffset} = \text{Address}_{\text{target}} - \text{PC}_{\text{current}}$$
3. **Bit-Packing:**
   ```csharp
   instruction = Instruction.CreateABC(OpCode.ADD, destA, operandB, operandC);
   ```

## 4. Constant Pool Deduplication

Constant pool allocation in [VMChunk.cs](../Raptor/VMChunk.cs) deduplicates literals via `SetConstant`:

```csharp
public uint SetConstant(double value)
{
    for (int i = 0; i < currUsedConstantsIndex; i++)
    {
        if (Constants[i] == value)
        {
            return (uint)i;
        }
    }
    Constants[currUsedConstantsIndex] = value;
    return currUsedConstantsIndex++;
}
```

The 9-bit operand encoding field allows indexing up to 512 entries:
- Indices `0–255` map to registers.
- Indices `256–511` map to constant pool slots (maximum 256 active constants).

Deduplication allows duplicate literal values in assembly source to share constant pool slots.

## 5. Source Maps (`#LINE` Directive)

Assembly files may contain `#LINE <number>` directives for source mapping:
- Pass 2: Directives are skipped during `memoryAddress` calculation to maintain alignment.
- Pass 3: Directives update `VMChunk.SourceMap` to map runtime Instruction Pointer (`IP`) values to source line numbers for exception reporting.

## 6. Binary Bytecode Serialization (`.rbc` Format)

`VMChunk` objects serialize to `.rbc` binary files via `RaptorBinary.cs`.

### Binary Layout (Little-Endian)

1. Header (20 bytes):
   - `[0..3]` Magic Signature (4 bytes): `0x52415054` (`"RAPT"` in ASCII).
   - `[4]` Version Major (1 byte): `1`.
   - `[5]` Version Minor (1 byte): `0`.
   - `[6..7]` Reserved (2 bytes): `0x0000`.
   - `[8..11]` Constants Count (4 bytes, `uint32`).
   - `[12..15]` Method Table Count (4 bytes, `uint32`).
   - `[16..19]` Instructions Count (4 bytes, `uint32`).
2. Constants Section: `Constants Count` × 8 bytes (IEEE 754 `double`).
3. Method Table Section: `Method Table Count` × 4 bytes (`uint32` entry point offsets).
4. Instructions Section: `Instructions Count` × 4 bytes (`uint32` packed opcode words).

