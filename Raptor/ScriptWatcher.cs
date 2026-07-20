using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Raptor
{
    /// <summary>
    /// Monitors a .rasm assembly source file on the filesystem and automatically
    /// recompiles it when changes are saved, swapping the active program chunk
    /// thread-safely without stopping VM execution.
    /// </summary>
    public sealed class ScriptWatcher : IDisposable
    {
        private readonly ScriptEngine _engine;
        private readonly FileSystemWatcher _watcher;
        private VMChunk _activeChunk;
        private readonly string _filePath;
        private readonly object _lock = new();
        private readonly Func<string, string>? _preprocessor;

        /// <summary>
        /// Event fired when the script is successfully recompiled.
        /// </summary>
        public event Action<VMChunk>? OnReloaded;

        /// <summary>
        /// Event fired when a compilation error occurs during hot reloading.
        /// </summary>
        public event Action<Exception>? OnReloadError;

        /// <summary>
        /// The currently active, compiled, and verified VMChunk.
        /// </summary>
        public VMChunk ActiveChunk
        {
            get
            {
                lock (_lock)
                {
                    return _activeChunk;
                }
            }
        }

        /// <summary>
        /// Creates a ScriptWatcher that compiles the file at startup and monitors it for changes.
        /// </summary>
        /// <param name="engine">The ScriptEngine to use for compilation.</param>
        /// <param name="filePath">Path to the script file.</param>
        /// <param name="preprocessor">Optional preprocessor callback (e.g., to compile RaptorScript to RaptorAssembly).</param>
        public ScriptWatcher(ScriptEngine engine, string filePath, Func<string, string>? preprocessor = null)
        {
            _engine = engine;
            _filePath = Path.GetFullPath(filePath);
            _preprocessor = preprocessor;

            // Compile initial chunk
            _activeChunk = CompileFileWithPreprocessor(_filePath);

            // Setup FileSystemWatcher
            string? directory = Path.GetDirectoryName(_filePath);
            string? filename = Path.GetFileName(_filePath);

            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("Invalid file path directory.", nameof(filePath));

            _watcher = new FileSystemWatcher(directory, filename)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileChanged;
        }

        private VMChunk CompileFileWithPreprocessor(string path)
        {
            string text = File.ReadAllText(path);
            if (_preprocessor != null)
            {
                text = _preprocessor(text);
            }
            return _engine.Compile(text);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Add a slight delay to allow the editor/IDE to finish writing to the file
            System.Threading.Thread.Sleep(50);

            try
            {
                VMChunk newChunk;
                lock (_lock)
                {
                    newChunk = CompileFileWithPreprocessor(_filePath);
                    _activeChunk = newChunk;
                }
                OnReloaded?.Invoke(newChunk);
            }
            catch (Exception ex)
            {
                OnReloadError?.Invoke(ex);
            }
        }

        /// <summary>
        /// Disposes the file watcher.
        /// </summary>
        public void Dispose()
        {
            _watcher.Dispose();
        }
    }
}
