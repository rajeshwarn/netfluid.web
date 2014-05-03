using System;
using System.Timers;

namespace NetFluid.Cron
{
    internal class CronTask
    {
        private readonly Action action;
        private readonly string cron;
        private readonly Timer timer;

        public CronTask(string cron, Action action, Action completed, Action<Exception> error)
        {
            timer = new Timer {AutoReset = true, Interval = (Cron.Next(cron) - DateTime.Now).TotalMilliseconds};
            timer.Elapsed += timer_Elapsed;

            this.action = action;
            this.cron = cron;
            this.completed += completed;
            this.error += error;

            timer.Enabled = true;
        }

        public CronTask(string cron, DateTime from, Action action, Action completed, Action<Exception> error)
        {
            timer = new Timer {AutoReset = true, Interval = (Cron.Next(cron, from) - DateTime.Now).TotalMilliseconds};
            timer.Elapsed += timer_Elapsed;

            this.action = action;
            this.cron = cron;
            this.completed += completed;
            this.error += error;

            timer.Enabled = true;
        }

        private event Action completed;
        private event Action<Exception> error;

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Enabled = false;
            timer.Interval = (Cron.Next(cron) - DateTime.Now).TotalMilliseconds;

            try
            {
                action();

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