using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Raptor.Compiler
{
    /// <summary>
    /// Maps VM instruction offsets (program counter values) back to original
    /// RaptorScript (.rapt) source code line numbers.
    /// </summary>
    public sealed class SourceMap
    {
        private readonly Dictionary<int, int> _instructionToLineMap = new();

        /// <summary>
        /// Registers the line number associated with a specific instruction program counter.
        /// </summary>
        public void AddMapping(int pc, int line)
        {
            _instructionToLineMap[pc] = line;
        }

        /// <summary>
        /// Looks up the original source line number for a given instruction pointer index.
        /// If no exact mapping exists, returns -1.
        /// </summary>
        public int GetLineNumber(int pc)
        {
            return _instructionToLineMap.TryGetValue(pc, out int line) ? line : -1;
        }
    }
}
