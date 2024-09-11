using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Mvc;

namespace GoogleCalendarIntegration.Controllers
{
    public class CalendarController : Controller
    {
        static string[] Scopes = { CalendarService.Scope.Calendar };
        static string ApplicationName = "Google Calendar API Integration";

        private readonly IWebHostEnvironment _env;
        public CalendarController(IWebHostEnvironment env)
        {
            _env = env;
        }
        public async Task<IActionResult> Index()
        {
            UserCredential credential;

            var filePath = Path.Combine(_env.WebRootPath, "client_secret.json");
            var credPath = Path.Combine(_env.WebRootPath, "token.json");

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }

            // Create Calendar API service
            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define parameters of request
            EventsResource.ListRequest request = service.Events.List("primary");
            request.TimeMinDateTimeOffset = DateTimeOffset.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // Fetch events
            Events events = await request.ExecuteAsync();
            List<string> eventList = new List<string>();

            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    // Updated to use DateTimeOffset
                    string when = eventItem.Start.DateTimeDateTimeOffset?.ToString() ?? eventItem.Start.Date;
                    eventList.Add($"{eventItem.Summary} ({when})");
                }
            }
            else
            {
                eventList.Add("No upcoming events found.");
            }

            ViewBag.Events = eventList;
            return View();
        }

        public IActionResult CreateEvent()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent(string summary, DateTime startDate, DateTime endDate)
        {
            UserCredential credential;

            var filePath = Path.Combine(_env.WebRootPath, "client_secret.json");
            var credPath = Path.Combine(_env.WebRootPath, "token.json");


            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                     GoogleClientSecrets.FromStream(stream).Secrets,
                     Scopes,
                     "user",
                     CancellationToken.None,
                     new FileDataStore(credPath, true)
                  );
            }

            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var newEvent = new Event()
            {
                Summary = summary,
                Start = new EventDateTime()
                {

                    DateTimeDateTimeOffset = new DateTimeOffset(startDate),
                    TimeZone = "Asia/Kolkata",
                },
                End = new EventDateTime()
                {
                    DateTimeDateTimeOffset = new DateTimeOffset(endDate),
                    TimeZone = "Asia/Kolkata",
                }
            };

            var request = service.Events.Insert(newEvent, "primary");
            await request.ExecuteAsync();

            ViewData["Message"] = "Event created successfully!";
            return RedirectToAction("Index");
        }
    }
}
