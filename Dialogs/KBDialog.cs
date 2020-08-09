using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Vij.CDS.Bots.Dialogs
{

    //use the input from the user to identify if there is a KB to help, pass it off to the case dialog if a new issue needs to be created
    public class KBDialog : ComponentDialog
    {


        public KBDialog() : base(nameof(KBDialog))
        {

        }

    }
}
