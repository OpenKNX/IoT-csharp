using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenKNX.IoT.Classes
{
    internal sealed class LoggerTextWriter : TextWriter
    {
        private readonly ILogger _logger;
        private readonly LogLevel _level;

        public LoggerTextWriter(string name, ILoggerFactory loggerFactory, LogLevel level = LogLevel.Information)
        {
            _logger = loggerFactory.CreateLogger(name);
            _level = level;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            // Einzelne Zeichen puffern ist ineffizient → delegieren an Write(string)
            Write(value.ToString());
        }

        public override void Write(string? value)
        {
            if (!string.IsNullOrEmpty(value))
                _logger.Log(_level, "{Message}", value);
        }

        public override void WriteLine(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            string[] parts = value.Split("\n");
            string message = string.Join("\n\t", parts);
            
            _logger.Log(_level, "{Message}", message);
        }
    }
}
