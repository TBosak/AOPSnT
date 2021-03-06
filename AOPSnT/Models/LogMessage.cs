using AOPSnT.Interfaces;
using LiteDB;
using System;
using System.Collections.Generic;

namespace AOPSnT.Models
{
    public class LogMessage : ILogMessage
    {
#nullable enable
        public int _id { get; set; }
        public object? Instance { get; set; }
        public IList<string>? Types { get; set; }
        public string? Method { get; set; }
        public IDictionary<object, object>? Parameters { get; set; }
        public string? Exception { get; set; }
        public dynamic? Response { get; set; }
        public DateTime? OnEntry { get; set; }
        public DateTime? OnExit { get; set; }
        public DateTime? OnException { get; set; }
#nullable disable
    }
}
