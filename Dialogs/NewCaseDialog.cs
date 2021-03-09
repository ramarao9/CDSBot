using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vij.Bots.DynamicsCRMBot.Interfaces;
using Vij.Bots.DynamicsCRMBot.Models;
using Vij.Bots.DynamicsCRMBot.Services;
using Entity = Microsoft.Xrm.Sdk.Entity;

namespace Vij.Bots.DynamicsCRMBot.Dialogs
{


    //create case, show the status of the case, last update, human handoff?
    public class NewCaseDialog : ComponentDialog
    {


        #region Variables
        private readonly StateService _stateService;

        private ICaseRepository _caseRepository;
        private IContactRepository _contactRepository;
        #endregion  
        public NewCaseDialog(string dialogId, StateService stateService,
            ICaseRepository subjectRepository, IContactRepository contactRepository) : base(dialogId)
        {
            _caseRepository = subjectRepository;
            _contactRepository = contactRepository;
            _stateService = stateService ?? throw new System.ArgumentNullException(nameof(stateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                IssueTypeStepAsync,
                EmailAddressStepAsync,
                DescriptionStepAsync,
                CallbackTimeStepAsync,
                PhoneNumberStepAsync,
                SummaryStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(NewCaseDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(NewCaseDialog)}.issueType"));
            AddDialog(new TextPrompt($"{nameof(NewCaseDialog)}.emailAddress"));
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

        private async Task<DialogTurnResult> EmailAddressStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {


            stepContext.Values["issueType"] = ((FoundChoice)stepContext.Result).Value;
            return await stepContext.PromptAsync($"{nameof(NewCaseDialog)}.emailAddress",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Email Address?")
                }, cancellationToken);
        }


        private async Task<DialogTurnResult> DescriptionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["emailAddress"] = (string)stepContext.Result;
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
            userProfile.EmailAddress = (string)stepContext.Values["emailAddress"];
            userProfile.IssueType = (string)stepContext.Values["issueType"];

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Here is a summary of your issue:");
            sb.AppendLine(string.Format("Email Address: {0}", userProfile.EmailAddress));
            sb.AppendLine(string.Format("Description: {0}", userProfile.Description));
            sb.AppendLine(string.Format("Callback Time: {0}", userProfile.CallbackTime.ToString()));
            sb.AppendLine(string.Format("Phone Number: {0}", userProfile.PhoneNumber));

            string issueSummary = sb.ToString();

            // Show the summary to the user
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(issueSummary), cancellationToken);

            // Save data in userstate
            await _stateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            await stepContext.Context.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);

            string ticketNumber = await CreateCaseInCRM(userProfile);

            //Give the user ticket number
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Your case {ticketNumber} has been successfully created!"), cancellationToken);

            conversationData.CurrentDialogCompleted = true;
            //Save data in Conversation state to indicate New issue Captured
            await _stateService.ConversationDataAccessor.SetAsync(stepContext.Context, conversationData);

            stepContext.Context.TurnState.Add("DialogCompleted", "Issue");

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

        private async Task<string> CreateCaseInCRM(UserProfile userProfile)
        {
            List<Subject> subjects = await _caseRepository.GetSubjects();
            Subject subject = subjects.FirstOrDefault(x => x.Name == userProfile.IssueType);

            Entity incident = new Entity("incident");
            incident["title"] = $"{userProfile.Name} - {userProfile.IssueType}";
            if (subject != null)
            {
                incident["subjectid"] = new EntityReference("subject", subject.Id);
            }

            EntityReference contactER = await _contactRepository.FindContact(userProfile);
            if (contactER == null)
            {
                contactER = await _contactRepository.CreateContact(userProfile);
            }
            incident["customerid"] = contactER;

            Entity caseRecord = await _caseRepository.CreateCase(incident);
            return caseRecord["ticketnumber"].ToString();
        }

    }
}
