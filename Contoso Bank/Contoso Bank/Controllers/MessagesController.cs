using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Contoso_Bank.Models;
using System.Text;
using System.Collections.Generic;

namespace Contoso_Bank
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {

            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                StateClient stateClient = activity.GetStateClient(); //Saves bot state
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id); //Invoking state client

                var userMessage = activity.Text; //The user's input

                Dictionary<string, int> currencyCodes = new Dictionary<string, int>()
                        { { "AUD", 1}, { "BGN", 1}, { "BRL", 3}, { "CAD", 1}, { "CHF", 1}, { "CNY", 6}, { "CZK", 2}, { "DKK", 6}, { "GBP", 0},
                            { "HKD", 7}, { "HRK", 7}, { "HUF", 2}, { "IDR", 0}, { "ILS", 3}, { "INR", 0}, { "JPY", 1}, { "KRW", 1}, { "MXN", 2},
                            { "MYR", 4}, { "NOK", 8}, { "NZD", 1}, { "PHP", 4}, { "PLN", 4}, { "RON", 4}, { "RUB", 6}, { "SEK", 9}, { "SGD", 1},
                            { "THB", 3}, { "TRY", 3}, { "ZAR", 1}, { "EUR", 0} };


                string endOutput = "Welcome to Contoso Bank! Would you like currency rates or stocks?";

                // calculate something for us to return
                if (userData.GetProperty<bool>("SentGreeting"))
                {
                    endOutput = "Hi again! Welcome to Contoso Bank's currency exchange rates! Would you like currency rates or stocks?";
                }

                else
                {
                    userData.SetProperty("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    //^Syncing updated userData
                    //If we didn't sync up, the changes will not be reflected if we asked for the user data again. 
                    //In our first greeting, the Bot will say "Hello".
                    //When we greet the Bot again, it will say "Hello again.".
                }

                if (userMessage.ToLower().Contains("currency rates"))
                { //The user wants Currency Exchange
                    userData.SetProperty("UserWantsCurrencyRates", true); //Sets "userWantsCurrencyRates" to TRUE
                    endOutput = "userMessage is: " + userMessage;
                }

                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "Your personal data has been cleared.";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                }




                // return our reply to the user
                Activity infoReply = activity.CreateReply(endOutput);
                await connector.Conversations.ReplyToActivityAsync(infoReply);

            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }








        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {

            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}
