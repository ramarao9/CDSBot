using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vij.Bots.DynamicsCRMBot.Models;
using Vij.Bots.DynamicsCRMBot.Services;

namespace Vij.Bots.DynamicsCRMBot.Dialogs
{


    //create case, show the status of the case, last update, human handoff?
    public class NewCaseDialog : ComponentDialog
    {


        #region Variables
        private readonly StateService _stateService;
        #endregion  
        public NewCaseDialog(string dialogId, StateService stateService) : base(dialogId)
        {
            _stateService = stateService ?? throw new System.ArgumentNullException(nameof(stateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                IssueTypeStepAsync,
                DescriptionStepAsync,
                CallbackTimeStepAsync,
                PhoneNumberStepAsync,
                SummaryStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(NewCaseDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(NewCaseDialog)}.issueType"));
            AddDialog(new TextPrompt($"{nameof(NewCaseDialog)}.description"));
            AddDialog(new DateTimePrompt($"{nameof(NewCaseDialog)}.callbackTime", CallbackTimeValidatorAsync));
            AddDialog(new TextPrompt($"{nameof(NewCaseDialog)}.phoneNumber", PhoneNumberValidatorAsync));


            // Set the starting Dialog
            InitialDialogId = $"{nameof(NewCaseDialog)}.mainFlow";
        }


        private async Task<DialogTurnResult> IssueTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(NewCaseDialog)}.issueType",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please choose the type of issue you are experiencing."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Login", "Password Expiry", "Performance", "Usability", "Serious Bug", "Other" }),
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> DescriptionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {


            stepContext.Values["issueType"] = ((FoundChoice)stepContext.Result).Value;
            return await stepContext.PromptAsync($"{nameof(NewCaseDialog)}.description",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Enter a description for your issue")
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> CallbackTimeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["description"] = (string)stepContext.Result;
            return await stepContext.PromptAsync($"{nameof(NewCaseDialog)}.callbackTime",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please provide a callback time"),
                    RetryPrompt = MessageFactory.Text("The value entered must be between the hours of 9 am and 5 pm."),
                }, cancellationToken);
        }


        private async Task<DialogTurnResult> PhoneNumberStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["callbackTime"] = Convert.ToDateTime(((List<DateTimeResolution>)stepContext.Result).FirstOrDefault().Value);

            return await stepContext.PromptAsync($"{nameof(NewCaseDialog)}.phoneNumber",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please provide a best phone number to reach you"),
                    RetryPrompt = MessageFactory.Text("Please enter a valid phone number"),
                }, cancellationToken);
        }


        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["phoneNumber"] = (string)stepContext.Result;

            // Get the current profile object from user state.
            var userProfile = await _stateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
            var conversationData = await _stateService.ConversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData(), cancellationToken);

            // Save all of the data inside the user profile
            userProfile.Description = (string)stepContext.Values["description"];
            userProfile.CallbackTime = (DateTime)stepContext.Values["callbackTime"];
            userProfile.PhoneNumber = (string)stepContext.Values["phoneNumber"];
            userProfile.IssueType = (string)stepContext.Values["issueType"];

            // Show the summary to the user
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Here is a summary of your issue:"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Format("Description: {0}", userProfile.Description)), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Format("Callback Time: {0}", userProfile.CallbackTime.ToString())), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Format("Phone Number: {0}", userProfile.PhoneNumber)), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Format("Type: {0}", userProfile.IssueType)), cancellationToken);

            // Save data in userstate
            await _stateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);


            conversationData.NewIssueCaptured = true;
            //Save data in Conversation state to indicate New issue Captured
            await _stateService.ConversationDataAccessor.SetAsync(stepContext.Context, conversationData);

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }


        private Task<bool> CallbackTimeValidatorAsync(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                var resolution = promptContext.Recognized.Value.First();
                DateTime selectedDate = Convert.ToDateTime(resolution.Value);
                TimeSpan start = new TimeSpan(9, 0, 0); //9 o'clock
                TimeSpan end = new TimeSpan(17, 0, 0); //5 o'clock
                if ((selectedDate.TimeOfDay >= start) && (selectedDate.TimeOfDay <= end))
                {
                    valid = true;
                }
            }
            return Task.FromResult(valid);
        }

        private Task<bool> PhoneNumberValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                valid = Regex.Match(promptContext.Recognized.Value, @"^(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}$").Success;
            }
            return Task.FromResult(valid);
        }
    }
}
