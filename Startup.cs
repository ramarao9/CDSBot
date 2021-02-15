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
using Microsoft.PowerPlatform.Cds.Client;
using Vij.Bots.DynamicsCRMBot.Bots;
using Vij.Bots.DynamicsCRMBot.Dialogs;
using Vij.Bots.DynamicsCRMBot.Helpers;
using Vij.Bots.DynamicsCRMBot.Interfaces;
using Vij.Bots.DynamicsCRMBot.Repositories;
using Vij.Bots.DynamicsCRMBot.Services;

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

            // Configure State
            ConfigureState(services, _configuration);

            ConfigureDialogs(services);

            ConfigureRepositories(services, _configuration);

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, DialogBot<MainDialog>>();
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
            string connectionString = $"AuthType=ClientCredentials;Url={dynamicsUrl};Client Id={clientId};Client Secret={clientSecret};";
            CdsServiceClient cdsServiceClient = new CdsServiceClient(connectionString);
            services.AddSingleton<ISubjectRepository>(x => new SubjectRepository(cdsServiceClient));


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
}
