﻿using AlexaSkillsKit.Speechlet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AlexaSkillsKit.Authentication;
using AlexaSkillsKit.Json;
using AlexaSkillsKit.UI;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace AlexaGrowthZone.Business
{
    public class GrowthzoneSpeechlet : Speechlet
    {
        public string result;
        public int ccid = ****;
        public string ApiCall(string callURL)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://api.micronetonline.com/V1/");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-ApiKey", "**************");

            return client.GetStringAsync(callURL).Result;
        }
        public override SpeechletResponse OnIntent(IntentRequest intentRequest, Session session)
        {
            string intentName = intentRequest.Intent.Name;
            Dictionary<string, AlexaSkillsKit.Slu.Slot> slots = intentRequest.Intent.Slots;

            //Eventually should be done automatically by getting Id from request, that's not supported yet though
            Dictionary<string, int> resolutions = new Dictionary<string, int>
            {
                {"how many", 1 },
                {"what time", 2 },
                {"at what time", 2 },
                {"what is", 2 },
                {"when", 2 },
                {"what", 3 }
            };

            //ActiveMemberCount
            if (intentName == "ActiveMemberCount")
            {
                result = ApiCall("associations(" + ccid + ")/members");
                var members = JsonConvert.DeserializeObject<List<ApiMemberResult>>(result);
                List<ApiMemberResult> activeMembers = new List<ApiMemberResult>();
                for (int i = 0; i < members.Count(); i++)
                {
                    if (members[i].Status == 2)
                    {
                        activeMembers.Add(members[i]);
                    }
                }

                int activeMemberCount = activeMembers.Count;
                
                return BuildSpeechletResponse("Your active member count is " + activeMemberCount, true);
            }
            else if (intentName == "MyNextEvent")
            {
                DateTime localDate = DateTime.Now;
                result = ApiCall("associations(" + ccid + ")/events");
                var events = JsonConvert.DeserializeObject<List<ApiEventResult>>(result);
                int difference;
                List<ApiEventResult> futureEvents = new List<ApiEventResult>();
                for (int i = 0; i < events.Count(); i++)
                {
                    difference = DateTime.Compare(localDate, Convert.ToDateTime(events[i].StartTime));
                    if (difference < 0)
                    {
                        futureEvents.Add(events[i]);
                    }
                }
                var orderedEvents =
                    from futureEvent in futureEvents
                    orderby futureEvent.StartTime
                    select futureEvent;
                var nextEvent = orderedEvents.ElementAt(0);
                string nextEventId = nextEvent.Id;

                AlexaSkillsKit.Slu.Slot question;
                if (slots.TryGetValue("question", out question))
                {
                    if (resolutions.ContainsKey(question.Value))
                    {
                        if (resolutions[question.Value] == 1)
                        {
                            result = ApiCall("associations(" + ccid + ")/events/attendees");
                            var eventsAttendees = JsonConvert.DeserializeObject<List<ApiEventsAttendeesResult>>(result);
                            int attendeeCount = 0;
                            for (int i = 0; i < eventsAttendees.Count(); i++)
                            {
                                if (eventsAttendees[i].EventId.ToString() == nextEventId)
                                {
                                    attendeeCount = attendeeCount + 1;
                                }
                            }
                            return BuildSpeechletResponse("There are " + attendeeCount + " attendees registered for " + nextEvent.Name, true);
                        }
                        else if (resolutions[question.Value] == 2)
                        {
                            var nextEventStart = DateTime.Parse(nextEvent.StartTime).ToString();
                            var dateTime = DateTime.Parse(nextEvent.StartTime);
                            var date = DateTime.Parse(nextEvent.StartTime).Date - dateTime.TimeOfDay;
                            var time = date.ToString("hh:mm");
                            var dateOnly = date.ToString("dd/MM/yyyy");

                            return BuildSpeechletResponse(nextEvent.Name + " is on " + dateOnly + " at " + time, true);
                        }
                        else if (resolutions[question.Value] == 3)
                        {
                            return BuildSpeechletResponse("Your next event is " + nextEvent.Name, false);
                        }
                        else
                        {
                            return BuildSpeechletResponse("I'm sorry, I don't know how to respond to that.", false);
                        }
                    }
                    else
                    {
                        return BuildSpeechletResponse("I'm sorry, I don't know how to respond to that.", false);
                    }
                }
                else
                {
                    return BuildSpeechletResponse("I'm sorry, I didn't catch that.", false);
                }
            }
            else if (intentName == "CallMember")
            {
                //What if my friends don’t have an Echo, can I still call or message them from mine?
                //Yes!Those friends would simply need to download the free Amazon Alexa App on their phone, available on iOS and Android, and enable Alexa calling and messaging.
                result = ApiCall("associations(" + ccid + ")/members/details");
                return BuildSpeechletResponse("I'm sorry, I didn't catch that.", true);
            }
            else
            {
                return BuildSpeechletResponse("I'm sorry, I don't know how to respond to that.", false);
            }
        }

        public override SpeechletResponse OnLaunch(LaunchRequest launchRequest, Session session)
        {
            return BuildSpeechletResponse("Welcome to Chambermaster", false);
        }

        public override bool OnRequestValidation(SpeechletRequestValidationResult result, DateTime referenceTimeUtc, SpeechletRequestEnvelope requestEnvelope)
        {
            return true;
        }

        public override void OnSessionEnded(SessionEndedRequest sessionEndedRequest, Session session)
        {
        }

        public override void OnSessionStarted(SessionStartedRequest sessionStartedRequest, Session session)
        {
        }
        private SpeechletResponse BuildSpeechletResponse(string output, bool shouldEndSession)
        {
            // Create the plain text output.
            PlainTextOutputSpeech speech = new PlainTextOutputSpeech();
            speech.Text = output;

            // Create the speechlet response.
            SpeechletResponse response = new SpeechletResponse();
            response.ShouldEndSession = shouldEndSession;
            response.OutputSpeech = speech;
            
            return response;
        }
    }
    public class ApiMemberResult
    {
        public string Name { get; set; }
        public int Status { get; set; }
    }
    public class ApiEventResult
    {
        public string Name { get; set; }
        public string StartTime { get; set; }
        public string Id { get; set; }
    }
    public class ApiEventsAttendeesResult
    {
        public int EventId { get; set; }
    }
}