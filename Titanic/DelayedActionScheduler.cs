using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titanic
{
    /// <summary>
    /// this class is used to prevent multiple tasks running at the same time,
    /// the delay serves to ensure no tasks start running at the same time 
    /// (without it they won't be able to notice each other in the schedule)
    /// in essence this makes it so theres a fixed delay before running an action which resets whenever a new action is added
    /// </summary>
    public class DelayedActionScheduler
    {
        private System.Timers.Timer executeActionsTimer;
        private List<Action> actionsScheduled = new List<Action>();
        public bool RunningActions { get; private set; }

        public DelayedActionScheduler(int delay = 1000)
        {
            executeActionsTimer = new System.Timers.Timer();
            executeActionsTimer.AutoReset = false;
            executeActionsTimer.Elapsed += ExecuteScheduledActions;
            executeActionsTimer.Interval = delay;
        }

        public void ScheduleAction(Action action)
        {
            actionsScheduled.Add(action);
            if(!RunningActions)
            {
                executeActionsTimer.Stop();
                executeActionsTimer.Start();
            }
        }

        private void ExecuteScheduledActions(object? sender, System.Timers.ElapsedEventArgs e)
        {
            RunningActions = true;
            var schedule = actionsScheduled;
            actionsScheduled = new List<Action>();
            schedule.ForEach((action) => action.Invoke());
            RunningActions = false;
            if (actionsScheduled.Count > 0)
                executeActionsTimer.Start();
        }
    }
}
