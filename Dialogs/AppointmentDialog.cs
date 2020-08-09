using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Vij.CDS.Bots.Dialogs
{
    //use to create an Appointment in CDS
    public class AppointmentDialog : ComponentDialog
    {
        public AppointmentDialog() : base(nameof(AppointmentDialog))
        {

        }

    }
}
