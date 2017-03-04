using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Alisha;

namespace Alisha
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
                //Call to get the LUIS entity
                LuisResult stockLuisModel = await GetLUISEntity(activity.Text);


                    var res=BestResultFrom(new[] {stockLuisModel});
                //Check if the LUIS model has returned the entity
                if (stockLuisModel.Intents.Count > 0)
                {
                    //Connector used in conversation
                    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    string strStock;

                     //Call to get the stock price
                   switch (res.BestIntent.Intent)
                    {
                        case "StockPrice":
                            strStock = (res.Result.Entities.Count != 0)
                                ? "The Stock value for " + res.Result.Entities[0].Entity + ": is" +
                                  await GetStock(res.Result.Entities[0].Entity)
                                : "You did not mention the ticker symbol";
                            break;
                        case "GetTemperature":
                              strStock =
                                    (res.Result.Entities.Count != 0) 
                                        ? "The weather forecast for  " + res.Result.Entities[0].Entity + " :" +
                                          await GetWeather(res.Result.Entities[0].Entity)
                                        : "You did not mention the City";
                            break;
                        case "Greet":
                            strStock = "What can I do for you?";
                            break;
                        case "Marry":
                            strStock = "How much money do you have?";
                            break;
                        case "State":
                            strStock = "Hi, I am fine, how are you?";
                            break;
                        case "prompt":
                            strStock = "You can ask me questions like:"+
                                       "What is the weather in Newyork today?" +
                                       "        What is the price for MSFT?" +
                                       "        Ask me questions about chat bots.";
                            break;
                        case "Help":
                            strStock = "You can ask me questions like:" +
                                       "What is the weather in Irvine today?" +
                                       "        What is the price for MSFT ?" +
                                       "        Ask me questions about chat bots.";
                            break;
                        case "Express":
                            strStock = "If you ask me, I would say they are.";
                            break;
                        case "Capability":
                            strStock = "I can do a lot , just ask";
                            break;
                        case "sing":

                            strStock = "Hmm.. my inventors didn’t make me that smart."+"You can ask me questions like:" +
                                       "What is the weather in Newyork today?" +
                                       "        What is the price for MSFT?" +
                                       "        Ask me questions about chat bots.";
                            break;
                        case "Please":
                            strStock = "Hmm… you are hard to please.Tell me what you want me to do.";
                            break;
                        case "None":
                            strStock = "Sorry I was not able to find the stock price for :"+ res.Result.Query;
                            break;
                        default:
                            strStock = "";
                            break;
                    }
                      // return our reply to the user
                    var reply =activity.CreateReply(strStock);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                 
                // return our reply to the user
              }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }
        /// <summary>
        /// Handles the system activity
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
                return message.CreateReply("Delete User data");
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
                return message.CreateReply("Conversation update");
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
                return message.CreateReply("Contact Relationship update");
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
                return message.CreateReply("typing");
            }
            else if (message.Type == ActivityTypes.Ping)
            {
                return   message.CreateReply("ping");
            }

            return message;
        }


        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Api to to get the LUIS entity
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<LuisResult> GetLUISEntity(string query)
        {
            string strRet = string.Empty;
            //Reomve escape
            string strEscaped = Uri.EscapeDataString(query);
            LuisResult data = new LuisResult();

            using (HttpClient client = new HttpClient())
            {

                string requestUri =
                    " https://api.projectoxford.ai/luis/v1/application?id=d9e27ba7-28bc-417c-b1ac-4a9b30a70f03&subscription-key=e35bb28534054215806ae68e9d87c655&q=" +
                    strEscaped;
                //Ansyncronous wait for the responce URI
                HttpResponseMessage msg = await client.GetAsync(requestUri);

                //Check for the succcess status code
                if (msg.IsSuccessStatusCode)
                {
                    var jsonDataResponse = await msg.Content.ReadAsStringAsync();
                    data = JsonConvert.DeserializeObject<LuisResult>(jsonDataResponse);
                }
                return data;
            }

        }

        /// <summary>
        /// Gets the stock price
        /// </summary>
        /// <param name="strStock"></param>
        /// <returns></returns>
        private async Task<string> GetStock(string strStock)
        {
           //Get the stock price
            var stockValue = await Stock.GetStockPrice(strStock);
            
            return stockValue;
        }

        public async Task<string> GetWeather(string query)
        {
            WeatherTemplate weatherApi;
            string url =
              "http://api.openweathermap.org/data/2.5/weather?q=" + query + "&appid=eb7e585f30bdd79170f1db21a28c7d14&units=Imperial";

            string weatherCondition = string.Empty;
            string strEscaped = Uri.EscapeDataString(url);
            using (HttpClient weatherClient = new HttpClient())
            {
                //Ansyncronous wait for the responce URI
                HttpResponseMessage msg = await weatherClient.GetAsync(url);
                if (msg.IsSuccessStatusCode)
                {
                    var jsonDataResponse = await msg.Content.ReadAsStringAsync();
                    weatherApi = JsonConvert.DeserializeObject<WeatherTemplate>(jsonDataResponse);

                    weatherCondition = weatherApi.weather[0].main + " with max temp of " + weatherApi.main.temp_max +
                                       " F and min temp of " + weatherApi.main.temp_min + "F.";

                }

            }
            return weatherCondition;
        }

        /// <summary>
        /// Funtion to fetch the LUIS result
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        protected LuisServiceResult BestResultFrom(IEnumerable<LuisResult> results)
        {
            var allResults =
                from result in results
                from intent in result.Intents
                select new LuisServiceResult(result, intent);


            var nonNoneWinner = allResults
                                .Where(i => string.IsNullOrEmpty(i.BestIntent.Intent) == false)
                                .MaxBy(i => i.BestIntent.Score);
            return nonNoneWinner 
                ?? allResults.MaxBy(i => i.BestIntent.Score);
        }

    }
   





}