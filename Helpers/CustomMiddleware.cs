using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vij.Bots.DynamicsCRMBot.Helpers
{
    public class CustomMiddleware : IMiddleware
    {


        /// <summary>
        /// Processes an incoming activity.
        /// </summary>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="next">The delegate to call to continue the bot middleware pipeline.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }



            turnContext.OnSendActivities(async (newContext, activities, nextSend) =>
            {

                foreach (Activity currentActivity in activities.Where(a => a.Type == ActivityTypes.Message))
                {
                   
                    if(currentActivity.Text.Equals("requestescalation", StringComparison.InvariantCultureIgnoreCase))
                    {
                        currentActivity.Text = "Transferring to an agent..";
                        Dictionary<string, object> contextVars = new Dictionary<string, object>() { { "BotHandoffTopic", "CreditCard" } };
                        OmnichannelBotClient.AddEscalationContext(currentActivity, contextVars);
                    }
                    else
                    {
                        OmnichannelBotClient.BridgeBotMessage(currentActivity);
                    }
                
                }

                return await nextSend();
            });

            turnContext.OnUpdateActivity(async (newContext, activity, nextUpdate) =>
            {


                // Translate messages sent to the user to user language
                if (activity.Type == ActivityTypes.Message)
                {
                
                }

                return await nextUpdate();
            });

            await next(cancellationToken).ConfigureAwait(false);
        }

    }
}
