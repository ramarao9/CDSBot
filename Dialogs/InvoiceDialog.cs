using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Vij.Bots.DynamicsCRMBot.Helpers;
using Vij.Bots.DynamicsCRMBot.Interfaces;
using Vij.Bots.DynamicsCRMBot.Models;
using Vij.Bots.DynamicsCRMBot.Services;

namespace Vij.Bots.DynamicsCRMBot.Dialogs
{
    public class InvoiceDialog : ComponentDialog
    {
        private readonly StateService _stateService;

        private IInvoiceRepository _invoiceRepository;

        private IContactRepository _contactRepository;

        private AIFormRecognizer _aiFormRecognizer;

        public InvoiceDialog(string dialogId, StateService stateService, IContactRepository contactRepository, IInvoiceRepository invoiceRepository, AIFormRecognizer aiFormRecognizer) : base(dialogId)
        {
            _invoiceRepository = invoiceRepository;
            _contactRepository = contactRepository;
            _stateService = stateService ?? throw new System.ArgumentNullException(nameof(stateService));

            _aiFormRecognizer = aiFormRecognizer;
            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                AttachmentStepAsync,
                ProcessInvoiceAsync,
                SummaryStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(InvoiceDialog)}.mainFlow", waterfallSteps));
            AddDialog(new AttachmentPrompt($"{nameof(InvoiceDialog)}.attachment"));




            // Set the starting Dialog
            InitialDialogId = $"{nameof(InvoiceDialog)}.mainFlow";
        }


        private async Task<DialogTurnResult> AttachmentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(InvoiceDialog)}.attachment",
                                                    new PromptOptions
                                                    {
                                                        Prompt = MessageFactory.Text($"Please upload the file"),
                                                    });
        }


        private async Task<DialogTurnResult> ProcessInvoiceAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Show the summary to the user
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Verifying the Invoice. This might take a couple of seconds..."), cancellationToken);

            await stepContext.Context.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);

            string invoiceNumber = "";
            List<Attachment> attachments = (List<Attachment>)stepContext.Result;
            string replyText = string.Empty;
            foreach (var file in attachments)
            {
                // Determine where the file is hosted.
                var remoteFileUrl = file.ContentUrl;
               
                InvoiceData invoiceData = await _aiFormRecognizer.ProcessAttachmentAsync(new Uri(remoteFileUrl));

                if (invoiceData == null || invoiceData.CustomerName == null || invoiceData.InvoiceLines == null)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Unable to read the Invoice. Please verify the format of the Invoice file and try again"), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Read complete. Processing the Invoice..."), cancellationToken);

                    await stepContext.Context.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);

                    EntityReference customer = await _contactRepository.FindContact(invoiceData.CustomerName);

                    invoiceNumber = await _invoiceRepository.CreateInvoice(customer, invoiceData);

                }
            }



            return await stepContext.NextAsync(invoiceNumber, cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {


            string invoiceNumber = (string)stepContext.Result;
            stepContext.Values["invoicenumber"] = invoiceNumber;

            // Get the current profile object from user state.
            var userProfile = await _stateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
            var conversationData = await _stateService.ConversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData(), cancellationToken);



            //Give the user ticket number
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Your invoice {invoiceNumber} has been successfully created!"), cancellationToken);

            conversationData.CurrentDialogCompleted = true;
            //Save data in Conversation state to indicate New issue Captured
            await _stateService.ConversationDataAccessor.SetAsync(stepContext.Context, conversationData);

            stepContext.Context.TurnState.Add("DialogCompleted", "InvoiceUpload");

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }



        private async Task<string> CreateInvoiceInCRM(UserProfile userProfile)
        {


            return "";
        }

    }
}
