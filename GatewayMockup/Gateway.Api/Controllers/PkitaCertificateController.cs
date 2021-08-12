using Gateway.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System.Web.Http;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace Gateway.Api.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("[controller]")]
    public class PkitaCertificateController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };


        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly ActivitySource activitysource = new ActivitySource("PkitaController");

        private readonly ILogger<PkitaCertificateController> logger;

        public PkitaCertificateController(ILogger<PkitaCertificateController> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Microsoft.AspNetCore.Mvc.HttpPost]
        async public IAsyncEnumerable<PkitaCertificate> Post([Microsoft.AspNetCore.Mvc.FromBody] PkitaCertificate userData)
        {

            //var client = new HttpClient();
            //client.BaseAddress = new Uri("http://localhost:5002/ApiBase");

            Guid requestId = Guid.NewGuid();
            var requestTime = DateTime.Now;
            Console.WriteLine(requestId.ToString());


            //Setting Baggage
            Baggage.Current.SetBaggage("requestId: ", requestId.ToString());
            Baggage.Current.SetBaggage("clientID: ", userData.ClientId.ToString());
            Baggage.Current.SetBaggage("timeStamp: ", requestTime.ToString());


            Entry requestStatusEntry = new Entry()
            {
                clientId = userData.ClientId.ToString(),
                certificateName = userData.CertificateName.ToString(),
                timeStamp = requestTime.ToString(),
                status = "InProgress",
                RequestId = requestId.ToString()

            };

            //Span for storing Initial Request Status
            using (var activity = activitysource.StartActivity("GatewaySetStatusEvent"))

            {

                var requestStatusFileName = userData.ClientId + "." + requestId + ".json";
                var requestStatusFolderName = @"..\StatusUpdateData";
                var requestStatusFilePath = System.IO.Path.Combine(requestStatusFolderName, requestStatusFileName);
                




                if (System.IO.File.Exists(requestStatusFilePath))
                {
                    System.IO.File.Delete(requestStatusFilePath);
                }

                {

                    FileStream fs = new FileStream(requestStatusFilePath, FileMode.OpenOrCreate);
                    StreamWriter str = new StreamWriter(fs);
                    str.BaseStream.Seek(0, SeekOrigin.End);
                    //string json = JsonSerializer.Serialize(requestStatusEntry);
                    string json2 = JsonConvert.SerializeObject(requestStatusEntry, Formatting.Indented);

                    str.Write(json2);
                    str.Flush();
    
                    str.Close();
                    fs.Close();


                    using (var activity2 = activitysource.StartActivity("SendDataTOGatewayWorker"))
                    {

                        
                        using (var client = new HttpClient())
                        {

                            client.BaseAddress = new Uri("http://localhost:5002/ApiBase");

                            string json0 = JsonConvert.SerializeObject(requestStatusEntry, Formatting.Indented);
                            var content = new StringContent(json0, Encoding.UTF8, "application/json");


                            var result = await client.PostAsync(client.BaseAddress, content);
                            string resultContent = await result.Content.ReadAsStringAsync();
                        }

                    }


                }


                //Verification
                Console.WriteLine("File Created: {0}\n", requestStatusFilePath);


            }


            //Span for sending data to GatewayWorker




            yield return userData;
        }

        [Microsoft.AspNetCore.Mvc.Route("Pkita")]
        [Microsoft.AspNetCore.Mvc.HttpGet]
        public string Get([FromUri] string clientID, string requestID)
        {

            //string[] urlFromClient = url.Split(".");
            //var clientID = urlFromClient[0];
            //var requestID = urlFromClient[1];

            Console.WriteLine(clientID);
            Console.WriteLine(requestID);


            using (var activity = activitysource.StartActivity("GatewayGetStatusEvent"))
            {


                var fileToCheck = clientID + "." + requestID + ".json";
                var requestStatusFolderName = @"..\StatusUpdateData";
                var requestStatusFilePath = System.IO.Path.Combine(requestStatusFolderName, fileToCheck);
                Console.WriteLine(requestStatusFilePath);
                Console.WriteLine(requestStatusFolderName);

                //FileStream fs = new FileStream(requestStatusFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                //StreamWriter str = new StreamWriter(fs, Encoding.UTF8);
                //str.BaseStream.Seek(0, SeekOrigin.End);
                ////string json = JsonSerializer.Serialize(requestStatusEntry);
                //string json2 = JsonConvert.SerializeObject(request, Formatting.Indented);

                ////using (StreamReader r = new StreamReader("file.json"))
                ////{
                ////    string json = r.ReadToEnd();
                ////    List<Entry> items = JsonConvert.DeserializeObject<List<Entry>>(json);
                ////}

                //str.Write(json2);
                //str.Flush();
                //str.Close();
                //fs.Close();

                //var res = 2 + 2;




                {

                    FileStream fs = new FileStream(requestStatusFilePath, FileMode.Open, FileAccess.Read);
                    ////StreamReader str = new StreamReader(requestStatusFilePath);
                    //str.BaseStream.Seek(0, SeekOrigin.End);
                    //string json = JsonSerializer.Serialize(requestStatusEntry);

                    string status = null;

                    using (StreamReader sr = new StreamReader(requestStatusFilePath))
                    {
                        
                        // Read and display lines from the file until the end of
                        // the file is reached.
                        for(int i = 0; i<4; i++)
                        {
                            sr.ReadLine();
                            if (sr.EndOfStream)
                            {
                                Console.WriteLine($"End of file.  The file only contains {i} lines.");
                                break;
                            }
                        }

                        status = sr.ReadLine();
                        
                       
                    }
                    //string metadata = str.ReadToEnd();

                    //Entry json = JsonConvert.DeserializeObject<Entry>(metadata);

                    //str.Close();
                    fs.Close();

                    //Console.WriteLine(metadata);

                    
                    return status;



                }



            }



        }

    }
}
