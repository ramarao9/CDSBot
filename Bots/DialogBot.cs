using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vij.Bots.DynamicsCRMBot.Models;
using Vij.Bots.DynamicsCRMBot.Services;

namespace Vij.Bots.DynamicsCRMBot.Bots
{
    public class DialogBot<T> : ActivityHandler
       where T : Dialog
    {
        protected readonly Dialog _dialog;
        protected readonly StateService _stateService;
        protected readonly ILogger _logger;

        private const string WelcomeMessage = @"👋Hi there! I'm a D365 bot. I can help you with Issues, find solutions and schedule an appointment.";

        public DialogBot(StateService stateService, T dialog, ILogger<DialogBot<T>> logger)
        {
            _stateService = stateService ?? throw new System.ArgumentNullException(nameof(stateService));
            _dialog = dialog;
            _logger = logger;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.

            await _stateService.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _stateService.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running dialog with Message Activity.");


            // Run the Dialog with the new message Activity.
            await _dialog.RunAsync(turnContext, _stateService.DialogStateAccessor, cancellationToken);
        }


        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    ConversationData conversationData = await _stateService.ConversationDataAccessor.GetAsync(turnContext, () => new ConversationData());
                    if (!conversationData.WelcomeGreetingComplete)
                    {
                        conversationData.WelcomeGreetingComplete = true;
                        await turnContext.SendActivityAsync(WelcomeMessage);
                        await _stateService.ConversationDataAccessor.SetAsync(turnContext, conversationData);
                    }
                }
            }
        }



    }
}
