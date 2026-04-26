using System;
using System.Threading;
using Serilog;

namespace ShieldX.Utils
{
    /// <summary>
    /// Implements the Circuit Breaker pattern for handling cascading failures.
    /// </summary>
    public class CircuitBreaker
    {
        private readonly string _name;
        private readonly int _failureThreshold;
        private readonly int _resetTimeoutMs;
        private int _failureCount = 0;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private CircuitState _state = CircuitState.Closed;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Circuit states: Closed (normal), Open (failing), Half-Open (testing recovery)
        /// </summary>
        public enum CircuitState
        {
            Closed,      // Normal operation
            Open,        // Too many failures, rejecting requests
            HalfOpen     // Testing if service recovered
        }

        public CircuitBreaker(string name, int failureThreshold = 5, int resetTimeoutMs = 60000)
        {
            _name = name;
            _failureThreshold = failureThreshold;
            _resetTimeoutMs = resetTimeoutMs;
        }

        public CircuitState State
        {
            get
            {
                lock (_lockObject)
                {
                    return _state;
                }
            }
        }

        /// <summary>
        /// Executes operation with circuit breaker protection.
        /// </summary>
        public T Execute<T>(Func<T> operation, string operationName)
        {
            lock (_lockObject)
            {
                if (_state == CircuitState.Open)
                {
                    if (DateTime.UtcNow - _lastFailureTime > TimeSpan.FromMilliseconds(_resetTimeoutMs))
                    {
                        _state = CircuitState.HalfOpen;
                        Log.Information($"Circuit breaker '{_name}' entering half-open state");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Circuit breaker '{_name}' is OPEN after {_failureCount} failures");
                    }
                }
            }

            try
            {
                var result = operation();
                RecordSuccess();
                return result;
            }
            catch (Exception ex)
            {
                RecordFailure();
                Log.Warning(ex, $"Operation '{operationName}' failed in circuit breaker '{_name}'");
                throw;
            }
        }

        private void RecordSuccess()
        {
            lock (_lockObject)
            {
                _failureCount = 0;
                if (_state == CircuitState.HalfOpen)
                {
                    _state = CircuitState.Closed;
                    Log.Information($"Circuit breaker '{_name}' recovered and closed");
                }
            }
        }

        private void RecordFailure()
        {
            lock (_lockObject)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_failureCount >= _failureThreshold)
                {
                    _state = CircuitState.Open;
                    Log.Error($"Circuit breaker '{_name}' opened after {_failureCount} failures");
                }
            }
        }

        public void Reset()
        {
            lock (_lockObject)
            {
                _failureCount = 0;
                _state = CircuitState.Closed;
                Log.Information($"Circuit breaker '{_name}' manually reset");
            }
        }
    }
}
