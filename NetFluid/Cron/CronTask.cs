using System;
using System.Timers;

namespace NetFluid.Cron
{
    internal class CronTask
    {
        private readonly Action _action;
        private readonly string _cron;
        private readonly Timer _timer;

        public CronTask(string cron, Action action, Action completed, Action<Exception> error)
        {
            _timer = new Timer {AutoReset = true, Interval = (Cron.Next(cron) - DateTime.Now).TotalMilliseconds};
            _timer.Elapsed += timer_Elapsed;

            _action = action;
            _cron = cron;
            this.completed += completed;
            this.error += error;

            _timer.Enabled = true;
        }

        public CronTask(string cron, DateTime from, Action action, Action completed, Action<Exception> error)
        {
            _timer = new Timer {AutoReset = true, Interval = (Cron.Next(cron, from) - DateTime.Now).TotalMilliseconds};
            _timer.Elapsed += timer_Elapsed;

            _action = action;
            _cron = cron;
            this.completed += completed;
            this.error += error;

            _timer.Enabled = true;
        }

        private event Action completed;
        private event Action<Exception> error;

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            _timer.Interval = (Cron.Next(_cron) - DateTime.Now).TotalMilliseconds;

            try
            {
                _action();

                if (completed != null)
                    completed();
            }
            catch (Exception ex)
            {
                if (error != null)
                    error(ex);
            }
        }
    }
}