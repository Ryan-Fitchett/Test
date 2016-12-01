﻿using System;
using System.Collections.Generic;

namespace SFA.DAS.Commitments.Domain.Interfaces
{
    public interface ILog
    {
        void Trace(string message);
        void Trace(string message, IDictionary<string, object> properties);
        void Trace(string message, ILogEntry logEntry);

        void Debug(string message);
        void Debug(string message, IDictionary<string, object> properties);
        void Debug(string message, ILogEntry logEntry);

        void Info(string message);
        void Info(string message, IDictionary<string, object> properties);
        void Info(string message, ILogEntry logEntry);

        void Error(Exception ex, string message);
        void Error(Exception ex, string message, IDictionary<string, object> properties);
        void Error(Exception ex, string message, ILogEntry logEntry);

        void Fatal(Exception ex, string message);
        void Fatal(Exception ex, string message, IDictionary<string, object> properties);
        void Fatal(Exception ex, string message, ILogEntry logEntry);
    }

    public interface ILogEntry
    {
    }

    public interface IRequestContext
    {
        string Url { get; }
        string IpAddress { get; }
    }
}
