using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vij.CDS.Bots.Helpers;
using Vij.CDS.Bots.Services;

namespace Vij.CDS.Bots.Dialogs
{
    public class MainDialog : ComponentDialog
    {

        CDSRecognizer _luisRecognizer;
        private readonly StateService _stateService;
        protected readonly ILogger _logger;

        public MainDialog(StateService stateService, CDSRecognizer luisRecognizer, ILogger<MainDialog> logger) : base(nameof(MainDialog))
        {
            _logger = logger;
            _luisRecognizer = luisRecognizer;
            _stateService = stateService;

            AddDialog(new GreetingDialog( _stateService));
            AddDialog(new KBDialog());
            AddDialog(new CaseDialog());
            AddDialog(new AppointmentDialog());


            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                FinalStepAsync
            };

            AddDialog(new WaterfallDialog($"{nameof(MainDialog)}.mainFlow", waterfallSteps));

            // The initial child Dialog to run.
            InitialDialogId = $"{nameof(MainDialog)}.mainFlow";

        }



        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // First, we use the dispatch model to determine which cognitive service (LUIS or QnA) to use.
            var recognizerResult = await _luisRecognizer.RecognizeAsync(stepContext.Context, cancellationToken);

            // Top intent tell us which cognitive service to use.
            var topIntent = recognizerResult.GetTopScoringIntent();

            switch (topIntent.intent)
            {
                case "GreetingIntent":
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.greeting", null, cancellationToken);
                default:
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I'm sorry I don't know what you mean."), cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }





        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
