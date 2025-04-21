using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.UI
{
    public class UiEvent
    {
        public string Trigger { get; set; }

        public string ID { get; set; }

        public Action Action { get; set; }

        public bool Triggered { get; set; }

        public bool TriggerOnce { get; set; }

        public UiEvent(string trigger, Action action, bool triggerOnce = true)
        {
            ID = Guid.NewGuid().ToString();
            Trigger = trigger;
            Action = action;
            TriggerOnce = triggerOnce;
        }
    }
}
