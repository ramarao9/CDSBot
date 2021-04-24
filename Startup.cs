// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vij.Bots.DynamicsCRMBot.Bots;
using Vij.Bots.DynamicsCRMBot.Dialogs;
using Vij.Bots.DynamicsCRMBot.Helpers;
using Vij.Bots.DynamicsCRMBot.Interfaces;
using Vij.Bots.DynamicsCRMBot.Repositories;
using Vij.Bots.DynamicsCRMBot.Services;
using Microsoft.Extensions.Azure;
using Azure.Storage.Queues;
using Azure.Storage.Blobs;
using Azure.Core.Extensions;
using System;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Microsoft.BotBuilderSamples
{
    public class Startup
    {

        public IConfiguration _configuration { get; }

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Register LUIS recognizer
            services.AddSingleton<CDSRecognizer>();


            services.AddSingleton(x=>new AIFormRecognizer(_configuration));

            // Configure State
            ConfigureState(services, _configuration);

            ConfigureDialogs(services);

            ConfigureRepositories(services, _configuration);

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, DialogBot<MainDialog>>();
            services.AddAzureClients(builder =>
            {
                builder.AddBlobServiceClient(_configuration["ConnectionStrings:StorageAccountConnectionString:blob"], preferMsi: true);
                builder.AddQueueServiceClient(_configuration["ConnectionStrings:StorageAccountConnectionString:queue"], preferMsi: true);
            });


            services.AddSingleton<CustomMiddleware>();

        }

        public void ConfigureState(IServiceCollection services, IConfiguration configuration)
        {
            var storageAccount = configuration["StorageAccountConnectionString"];
            var storageContainer = configuration["StorageAccountContainer"];

            services.AddSingleton<IStorage>(new AzureBlobStorage(storageAccount, storageContainer));

            // Create the User state. 
            services.AddSingleton<UserState>();

            // Create the Conversation state. 
            services.AddSingleton<ConversationState>();

            // Create an instanc of the state service 
            services.AddSingleton<StateService>();
        }

        public void ConfigureDialogs(IServiceCollection services)
        {
            services.AddSingleton<MainDialog>();
        }



        public void ConfigureRepositories(IServiceCollection services, IConfiguration configuration)
        {
            string dynamicsUrl = configuration["Dynamics365URL"];
            string clientId = configuration["Dynamics365ClientId"];
            string clientSecret = configuration["Dynamics365ClientSecret"];
            string connectionString = $"AuthType=ClientSecret;Url={dynamicsUrl};ClientId={clientId};ClientSecret={clientSecret};";
            ServiceClient cdsServiceClient = new ServiceClient(connectionString);
            services.AddSingleton<ICaseRepository>(x => new CaseRepository(cdsServiceClient));
            services.AddSingleton<IContactRepository>(x => new ContactRepository(cdsServiceClient));
            services.AddSingleton<IInvoiceRepository>(x => new InvoiceRepository(cdsServiceClient));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles()
            .UseStaticFiles()
            .UseWebSockets()
            .UseRouting()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
    internal static class StartupExtensions
    {
        public static IAzureClientBuilder<BlobServiceClient, BlobClientOptions> AddBlobServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi)
        {
            if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri serviceUri))
            {
                return builder.AddBlobServiceClient(serviceUri);
            }
            else
            {
                return builder.AddBlobServiceClient(serviceUriOrConnectionString);
            }
        }
        public static IAzureClientBuilder<QueueServiceClient, QueueClientOptions> AddQueueServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi)
        {
            if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri serviceUri))
            {
                return builder.AddQueueServiceClient(serviceUri);
            }
            else
            {
                return builder.AddQueueServiceClient(serviceUriOrConnectionString);
            }
        }
    }
}
