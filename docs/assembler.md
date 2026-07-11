# The Assembler Pipeline & Constant Pool

The VM uses a text-based assembly syntax compiled into binary format via a structured **three-pass compiler pipeline** implemented in [Assembler.cs](../RegisterBasedVM/Assembler.cs).

---

## 1. Pass 1: Lexical Cleaning & Macro Substitution

The first pass cleans raw text files, removes comments, and resolves variable/macro declarations.

1. **Scrubbing Whitespaces & Comments:**
   Empty lines are dropped. Comments starting with `;` are removed. If a comment starts on an instruction line, the line is split at `;` and the comment is stripped.
2. **`DEFINE` Macro Collection:**
   Lines beginning with `DEFINE` (e.g. `DEFINE x1 r0` or `DEFINE epochs 10000`) are collected. The assembler extracts the name (key) and substitution string (value) and stores them in a symbol table dictionary. The `DEFINE` line is then removed from the text stream.
3. **Macro Substitution:**
   The assembler scans every word in the remaining instruction lines. If a word matches a key in the macro symbol table, it is replaced with its associated value. 
   *Example:* `ADD x1 w1 w2` is replaced with `ADD r0 r2 r3`.

---

## 2. Pass 2: Label Resolution & Size Tracking

The second pass maps labels and method headers to their absolute instruction index in the final compiled array, resolving jump targets.

1. **Symbol Identification:**
   - Lines ending in `:` (e.g. `loop:`) are registered as jump labels.
   - Lines ending in `()` (e.g. `perceive()`) are registered as method entry points.
2. **Instruction Address Tracking:**
   The assembler maintains a `memoryAddress` offset counter. As it loops through lines:
   - If a jump label is found, the current `memoryAddress` is recorded as the label's target, and the label line is removed from the instruction stream.
   - If a method header is found, the current `memoryAddress` is recorded as the method's target in the global method table, and the method header is removed.
   - **Instruction Expansion Adjustment:**
     Most instructions expand to a single 32-bit word, incrementing `memoryAddress` by `1`. However, the compound `FOR` instruction compiles to **two contiguous 32-bit words**. To keep subsequent label offsets aligned, the assembler increments `memoryAddress` by **`2`** when encountering a `FOR` instruction:
     ```csharp
     else if (lines[i].StartsWith("FOR "))
         memoryAddress += 2;
     else
         memoryAddress++;
     ```

---

## 3. Pass 3: Codegen & Bit-Packing

The final pass loops through the cleaned assembly instructions, parses registers and immediate literals, and packs them into 32-bit words.

1. **Operand Resolution:**
   - Register strings (e.g. `r0`, `r21`) are parsed to integer indices.
   - Numeric literals (e.g. `128.0`, `1.5`) are added to the constant pool via `SetConstant` and resolved to constant index operands ($\ge 256$).
2. **Relative Branch Offset Calculation:**
   Jumps and loops are compiled using relative program counter offsets. The offset is calculated as:
   $$\text{jumpOffset} = \text{Address}_{\text{target}} - \text{PC}_{\text{current}}$$
3. **Bit-Packing:**
   The instruction opcode and operands are bit-shifted and combined using the `Instruction` struct:
   ```csharp
   instruction = Instruction.CreateABC(OpCode.ADD, destA, operandB, operandC);
   ```

---

## 4. Constant Pool Deduplication

To prevent constant pool overflow, [VMChunk.cs](../RegisterBasedVM/VMChunk.cs) implements **constant deduplication** inside `SetConstant`:

```csharp
public uint SetConstant(double value)
{
    // Search the active constant pool first
    for (int i = 0; i < currUsedConstantsIndex; i++)
    {
        if (Constants[i] == value)
        {
            return (uint)i; // Return existing index (sharing slot)
        }
    }
    // Allocate new slot if unique
    Constants[currUsedConstantsIndex] = value;
    return currUsedConstantsIndex++;
}
```

### Why Deduplication is Crucial
Because the bit-packed `ABC` instruction format dedicates **9 bits** for operand indexing, the VM can only address indices `0` to `511`. 
- Indices `0–255` are reserved for registers.
- Indices `256–511` are mapped to constant pool slots (giving a max limit of 256 constants).

If a program uses the same literal number multiple times (e.g. `0.0` or `1.0` in clamping blocks), deduplication ensures they share a single slot. Without deduplication, complex workloads like the raytracer would rapidly exceed 256 constants and trigger compiler crashes.
