﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Vij.Bots.DynamicsCRMBot.Models
{
	public class ConversationData
	{
		// Track whether we have already asked the user's name
		public bool PromptedUserForName { get; set; } = false;

		public bool CurrentDialogCompleted { get; set; } = false;

		public bool WelcomeGreetingComplete;
	}
}
