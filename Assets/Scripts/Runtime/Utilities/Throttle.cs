namespace ZomboZ.Runtime
{
    /// <summary>
    /// A simple throttling helper to limit how often code executes.
    /// Useful for systems that don't need to run every frame.
    /// </summary>
    public struct Throttle
    {
        private double _lastCheckTime;
        private readonly double _intervalSeconds;

        /// <summary>
        /// Creates a new throttle with the specified interval.
        /// </summary>
        /// <param name="intervalSeconds">Minimum time between executions in seconds</param>
        public Throttle(double intervalSeconds)
        {
            _lastCheckTime = 0;
            _intervalSeconds = intervalSeconds;
        }

        /// <summary>
        /// Checks if enough time has passed since the last execution.
        /// Updates the internal timer if ready.
        /// </summary>
        /// <param name="currentTime">Current elapsed time (e.g., from SystemAPI.Time.ElapsedTime)</param>
        /// <returns>True if enough time has passed and code should execute</returns>
        public bool ShouldExecute(double currentTime)
        {
            if (currentTime - _lastCheckTime < _intervalSeconds)
                return false;

            _lastCheckTime = currentTime;
            return true;
        }

        /// <summary>
        /// Resets the throttle timer.
        /// </summary>
        public void Reset()
        {
            _lastCheckTime = 0;
        }
    }
}
