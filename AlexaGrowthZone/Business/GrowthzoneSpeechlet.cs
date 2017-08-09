using AlexaSkillsKit.Speechlet;
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
using System.Text;

namespace AlexaGrowthZone.Business
{
    public class GrowthzoneSpeechlet : Speechlet
    {
        public string result;
        public string _member;
        public MemberInfoApiResult current;
        public int ccid = 1532;
        public string ApiCall(string callURL)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://api.micronetonline.com/V1/");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-ApiKey", "a6f7d425-1a34-469f-9415-37b44edecb1f ");

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
                {"for", 2 },
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

            else if (intentName == "MemberInfo")
            {
                AlexaSkillsKit.Slu.Slot member;
                AlexaSkillsKit.Slu.Slot desiredInfo;

                if (slots.TryGetValue("member", out member))
                {
                    _member = member.Value.ToString().ToLower();

                    result = ApiCall("associations(" + ccid + ")/members/details");
                    var memberList = JsonConvert.DeserializeObject<List<MemberInfoApiResult>>(result);

                    for (int i = 0; i < memberList.Count(); i++)
                    {
                        if (memberList.ElementAt(i).OrganizationName.ToLower() == _member.ToLower())
                        {
                            current = memberList.ElementAt(i);
                        }
                    }
                
                if (slots.TryGetValue("desiredInfo", out desiredInfo))
                {
                    string comingIn = desiredInfo.Value.ToString();

                    Dictionary<string, int> whatInfo = new Dictionary<string, int>
                        {
                            {"address", 1 },
                            {"representatives", 2 },
                            {"reps", 2 },
                            {"rep", 2 },
                            {"representative", 2 },
                            {"name", 3 },
                            {"names", 3 }
                        };

                    if (whatInfo.ContainsKey(comingIn))
                    {
                        if ((whatInfo[desiredInfo.Value] == 1) && (_member != null))
                        {
                            StringBuilder strings = new StringBuilder();
                            if(current.Line1 != null)
                            {
                                strings.Append(current.Line1);
                            }
                            if(current.City != null)
                            {
                                strings.Append(current.City);
                            }
                            if(current.Region != null)
                            {
                                strings.Append(current.Region);
                            }
                            if(current.PostalCode != null)
                            {
                                strings.Append(current.PostalCode);
                            }
                            return BuildSpeechletResponse("The address of this member is " + strings, false);

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
                    return BuildSpeechletResponse("Desired info had no value", false);
                }

                }
                else
                {
                    return BuildSpeechletResponse("There is no member with that name", false);
                }

            //return BuildSpeechletResponse("I'm sorry, I didn't catch that", false);
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
    public class MemberInfoApiResult
    {
        public string OrganizationName { get; set; }
        public string Line1 { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string PrimaryRepFirstName { get; set; }
        public string PrimaryRepLastName { get; set; }
        public string PrimaryRepId { get; set; }

    }
}