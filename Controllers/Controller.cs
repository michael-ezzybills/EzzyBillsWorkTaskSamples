using Microsoft.AspNetCore.Mvc;

using RestSharp;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;

namespace EzzyBillsWorkTaskSamples.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class Controller : ControllerBase
    {
        /*
       this tasks uses the EzzyBills REST api

        you need to publish this task to the web then add a worktask to Ezzybills>Workflow>New Task
        Name="LogTable"
        Description "Writes teh Invocie table details to the invocie log"
        Webhook "<your published domain>/Worktask/TASK_LogInvoiceTable"

        Now add this worktask to your workflow and it will get called

        note* you need to add yur api key (in settings)
        you need to enable wehooks
       */
        [HttpGet(Name = "LogInvoiceTable")]
        public int TASK_LogInvoiceTable(int docid, int parentid, int state, string token)
        {

            Task task = Task.Run(() =>
            {

                try
                {

                    string APIKey = "add your api key";


                    var options = new RestClientOptions("https://app.ezzydoc.com/EzzyService.svc/");
                    var client = new RestClient(options);



                    RestRequest login = new RestRequest($"/Rest/LoginToken?webhooktoken={token}");
                    login.AddParameter("APIKey", APIKey);   //need to add your API key


                    RestResponse response = client.Execute(login);
                    Debug.Assert(response.StatusCode == HttpStatusCode.OK);

                    int rc = Convert.ToInt32(response.Content);
                    if (rc != 1)
                    {
                        throw new Exception("login failed");
                    }


                    CookieContainer cookiecon = new CookieContainer();
                    Cookie? cookie = null;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        cookie = response.Cookies.FirstOrDefault(c => c.Name.Contains("ASPXAUTH"));
                    }



                    dynamic myObject = null;
                    {
                        //use to get settings, will contain filename and the docid.
                        RestRequest hdata = new RestRequest($"/Rest/getInvoiceHeaderBlocks?invoiceid={docid}");
                        hdata.AddCookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain);
                        hdata.AddParameter("APIKey", APIKey);   //need to add your API key
                        response = client.Execute(hdata);

                        myObject = JsonConvert.DeserializeObject<dynamic>(response.Content);
                        int i = 0;
                        foreach (var p in myObject["table"])
                        {
                            i++;
                            string s = $"row[{i}]   [{p.article_code.Value}]    [{p.description.Value}]     [{p.quantity.Value}]    [{p.total.Value}]";
                            RestRequest ldata = new RestRequest($"/Rest/logInvoice?invoiceid={docid}&log={s}");
                            ldata.AddCookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain);
                            ldata.AddParameter("APIKey", APIKey);   //need to add your API key
                            response = client.Execute(ldata);


                        }

                    }


                    //once done then end worktask and EzzyBills will restart the workflow.

                    {
                        RestRequest hdata = new RestRequest($"/Rest/finishExternalTask?invoiceid={docid}&state={state}", Method.Get);
                        hdata.AddCookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain);
                        hdata.AddParameter("APIKey", APIKey);   //need to add your API key
                        response = client.Execute(hdata);

                    }



                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }


            });



            return 1;
        }




    }
}