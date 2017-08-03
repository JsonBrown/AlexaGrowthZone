using AlexaSkillsKit.Speechlet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AlexaSkillsKit.Authentication;
using AlexaSkillsKit.Json;
using AlexaSkillsKit.UI;

namespace AlexaGrowthZone.Business
{
    public class GrowthzoneSpeechlet : Speechlet
    {
        public override SpeechletResponse OnIntent(IntentRequest intentRequest, Session session)
        {
            return BuildSpeechletResponse("hello", true);
        }

        public override SpeechletResponse OnLaunch(LaunchRequest launchRequest, Session session)
        {
            return BuildSpeechletResponse("hello", false);
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
}